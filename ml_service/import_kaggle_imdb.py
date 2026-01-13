"""
Import IMDB data from Kaggle dataset to PostgreSQL
Kaggle dataset: https://www.kaggle.com/datasets/harshitshankhdhar/imdb-dataset-of-top-1000-movies-and-tv-shows
or: https://www.kaggle.com/datasets/ashirwadsangwan/imdb-dataset

Usage:
1. Download dataset from Kaggle
2. Place CSV file in ml_service/data/ folder
3. Run: python import_kaggle_imdb.py --file data/imdb_top_1000.csv
"""

import pandas as pd
import psycopg2
import requests
import os
from dotenv import load_dotenv
import time

load_dotenv()

DB_URL = os.getenv('DATABASE_URL', 'postgresql://postgres:Ignat2005@localhost:5432/movieRecom')

def get_connection():
    return psycopg2.connect(DB_URL)

def get_poster_url(imdb_id: str) -> str:
    """Try to get poster URL from OMDB API or construct IMDB poster URL"""
    # Можно использовать OMDB API с ключом или просто вернуть заглушку
    # Для простоты возвращаем URL-заглушку с IMDB ID
    return f"https://m.media-amazon.com/images/M/MV5B{imdb_id}.jpg"

def import_kaggle_csv(filepath: str, limit: int = 5000):
    """
    Import movies from Kaggle IMDB CSV
    Supports multiple Kaggle IMDB dataset formats
    """

    print(f"[INFO] Loading {filepath}...")

    # Try reading with different encodings
    try:
        df = pd.read_csv(filepath, encoding='utf-8')
    except:
        df = pd.read_csv(filepath, encoding='latin1')

    print(f"[INFO] Loaded {len(df)} rows")
    print(f"[INFO] Columns: {list(df.columns)}")

    # Map common column names from different Kaggle datasets
    column_mappings = {
        # Dataset: IMDB Top 1000
        'Series_Title': 'title',
        'Released_Year': 'year',
        'Runtime': 'runtime',
        'Genre': 'genres',
        'IMDB_Rating': 'rating',
        'Poster_Link': 'poster',
        'Overview': 'description',

        # Dataset: IMDB Dataset
        'Title': 'title',
        'Year': 'year',
        'Genres': 'genres',
        'Rating': 'rating',
        'Description': 'description',

        # Common names
        'title': 'title',
        'primaryTitle': 'title',
        'originalTitle': 'title',
        'startYear': 'year',
        'releaseYear': 'year',
        'genres': 'genres',
        'averageRating': 'rating',
        'imdb_rating': 'rating',
        'poster_url': 'poster',
        'posterUrl': 'poster',
        'overview': 'description',
        'plot': 'description',
        'tconst': 'imdb_id',
        'imdbId': 'imdb_id',
        'imdb_id': 'imdb_id',
    }

    # Rename columns
    df = df.rename(columns={k: v for k, v in column_mappings.items() if k in df.columns})

    # Ensure required columns
    if 'title' not in df.columns:
        print("[ERROR] No title column found!")
        return None

    # Clean data
    if 'year' in df.columns:
        df['year'] = pd.to_numeric(df['year'].astype(str).str.extract(r'(\d{4})')[0], errors='coerce')

    if 'rating' in df.columns:
        df['rating'] = pd.to_numeric(df['rating'], errors='coerce')
        df = df[df['rating'] >= 5.0]  # Filter low rated

    if 'runtime' in df.columns:
        # Extract minutes from "142 min" format
        df['runtime'] = df['runtime'].astype(str).str.extract(r'(\d+)')[0]
        df['runtime'] = pd.to_numeric(df['runtime'], errors='coerce')

    # Take top movies
    if 'rating' in df.columns:
        df = df.sort_values('rating', ascending=False)

    df = df.head(limit)
    print(f"[INFO] Processing {len(df)} movies")

    # Connect to database
    conn = get_connection()
    cursor = conn.cursor()

    # Extract and insert genres
    all_genres = set()
    for genres_str in df['genres'].dropna():
        for g in str(genres_str).split(','):
            g = g.strip()
            if g and g != 'nan':
                all_genres.add(g)

    print(f"[INFO] Found {len(all_genres)} unique genres")

    for genre_name in all_genres:
        cursor.execute(
            'INSERT INTO genres (name) VALUES (%s) ON CONFLICT (name) DO NOTHING',
            (genre_name,)
        )
    conn.commit()

    # Get genre IDs
    cursor.execute('SELECT "Id", name FROM genres')
    genre_map = {row[1]: row[0] for row in cursor.fetchall()}

    # Import movies
    stats = {'movies': 0, 'genres_linked': 0, 'skipped': 0}

    for idx, row in df.iterrows():
        try:
            title = str(row.get('title', ''))[:500]
            if not title or title == 'nan':
                stats['skipped'] += 1
                continue

            year = int(row['year']) if pd.notna(row.get('year')) else None
            rating = float(row['rating']) if pd.notna(row.get('rating')) else None
            runtime = int(row['runtime']) if pd.notna(row.get('runtime')) else None
            description = str(row.get('description', ''))[:2000] if pd.notna(row.get('description')) else None
            poster = str(row.get('poster', '')) if pd.notna(row.get('poster')) else None
            imdb_id = str(row.get('imdb_id', '')) if pd.notna(row.get('imdb_id')) else None

            # Generate IMDB ID if not present
            if not imdb_id or imdb_id == 'nan':
                imdb_id = f"tt{idx:07d}"

            # Insert movie
            cursor.execute('''
                INSERT INTO movies (title, description, release_year, poster_url, imdb_id, imdb_rating, runtime)
                VALUES (%s, %s, %s, %s, %s, %s, %s)
                ON CONFLICT (imdb_id) DO UPDATE SET
                    title = EXCLUDED.title,
                    description = EXCLUDED.description,
                    imdb_rating = EXCLUDED.imdb_rating,
                    poster_url = EXCLUDED.poster_url
                RETURNING "Id"
            ''', (title, description, year, poster, imdb_id, rating, runtime))

            result = cursor.fetchone()
            if result:
                movie_id = result[0]
                stats['movies'] += 1

                # Link genres
                genres_str = str(row.get('genres', ''))
                if genres_str and genres_str != 'nan':
                    for genre_name in genres_str.split(','):
                        genre_name = genre_name.strip()
                        genre_id = genre_map.get(genre_name)
                        if genre_id:
                            cursor.execute('''
                                INSERT INTO movie_genres ("MovieId", "GenreId")
                                VALUES (%s, %s)
                                ON CONFLICT DO NOTHING
                            ''', (movie_id, genre_id))
                            stats['genres_linked'] += 1

                if stats['movies'] % 100 == 0:
                    print(f"[PROGRESS] Imported {stats['movies']} movies...")
                    conn.commit()

        except Exception as e:
            print(f"[ERROR] {row.get('title', 'Unknown')}: {e}")
            stats['skipped'] += 1

    conn.commit()
    cursor.close()
    conn.close()

    print(f"\n[RESULT] Imported {stats['movies']} movies, {stats['genres_linked']} genre links, {stats['skipped']} skipped")
    return stats

def generate_test_users_and_ratings(n_users: int = 100, ratings_per_user: int = 50):
    """Generate test users and ratings for ML model training"""
    import random
    from hashlib import sha256

    conn = get_connection()
    cursor = conn.cursor()

    # Get all movie IDs
    cursor.execute('SELECT "Id" FROM movies')
    movie_ids = [row[0] for row in cursor.fetchall()]

    if not movie_ids:
        print("[ERROR] No movies in database!")
        return

    print(f"[INFO] Generating {n_users} test users with ratings for {len(movie_ids)} movies")

    stats = {'users': 0, 'ratings': 0}

    for i in range(n_users):
        email = f"testuser{i}@movierecom.test"
        password_hash = sha256(f"test{i}".encode()).hexdigest()

        cursor.execute('''
            INSERT INTO users (name, last_name, email, password, role)
            VALUES (%s, %s, %s, %s, 0)
            ON CONFLICT (email) DO NOTHING
            RETURNING "Id"
        ''', (f'TestUser{i}', f'Test', email, password_hash))

        result = cursor.fetchone()
        if result:
            user_id = result[0]
            stats['users'] += 1

            # Generate ratings with realistic distribution
            sample_size = min(ratings_per_user, len(movie_ids))
            sampled_movies = random.sample(movie_ids, sample_size)

            for movie_id in sampled_movies:
                # Weighted rating distribution (most users rate 3-4)
                rating = random.choices([1, 2, 3, 4, 5], weights=[5, 10, 25, 35, 25])[0]

                try:
                    cursor.execute('''
                        INSERT INTO ratings ("UserId", "MovieId", score, created_at)
                        VALUES (%s, %s, %s, NOW() - INTERVAL '%s days')
                        ON CONFLICT DO NOTHING
                    ''', (user_id, movie_id, rating, random.randint(0, 365)))
                    stats['ratings'] += 1
                except:
                    pass

    conn.commit()
    cursor.close()
    conn.close()

    print(f"[RESULT] Created {stats['users']} users with {stats['ratings']} ratings")
    return stats

if __name__ == '__main__':
    import argparse

    parser = argparse.ArgumentParser(description='Import IMDB data from Kaggle CSV')
    parser.add_argument('--file', type=str, default='data/imdb_top_1000.csv',
                        help='Path to Kaggle IMDB CSV file')
    parser.add_argument('--limit', type=int, default=5000,
                        help='Maximum number of movies to import')
    parser.add_argument('--users', type=int, default=100,
                        help='Number of test users to generate')
    parser.add_argument('--ratings', type=int, default=50,
                        help='Ratings per test user')
    parser.add_argument('--skip-movies', action='store_true',
                        help='Skip movie import, only generate test data')
    parser.add_argument('--skip-ratings', action='store_true',
                        help='Skip test rating generation')

    args = parser.parse_args()

    if not args.skip_movies:
        print("=== IMPORTING MOVIES FROM KAGGLE ===")
        if os.path.exists(args.file):
            import_kaggle_csv(args.file, limit=args.limit)
        else:
            print(f"[ERROR] File not found: {args.file}")
            print("\nPlease download IMDB dataset from Kaggle:")
            print("  1. https://www.kaggle.com/datasets/harshitshankhdhar/imdb-dataset-of-top-1000-movies-and-tv-shows")
            print("  2. https://www.kaggle.com/datasets/ashirwadsangwan/imdb-dataset")
            print("\nPlace CSV file in ml_service/data/ folder")

    if not args.skip_ratings:
        print("\n=== GENERATING TEST USERS AND RATINGS ===")
        generate_test_users_and_ratings(n_users=args.users, ratings_per_user=args.ratings)

    print("\n[DONE] Import complete!")
    print("\nNext steps:")
    print("  1. Train ML model: curl -X POST http://localhost:5001/train")
    print("  2. Test recommendations in the app")
