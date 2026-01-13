"""
IMDB Dataset Loader
Downloads and imports IMDB dataset from Kaggle into PostgreSQL
Dataset: https://www.kaggle.com/datasets/ashirwadsangwan/imdb-dataset
"""

import os
import pandas as pd
import psycopg2
from psycopg2.extras import execute_values
from dotenv import load_dotenv
from typing import Optional

load_dotenv()


def load_imdb_movies(filepath: str) -> pd.DataFrame:
    """
    Load IMDB movies from CSV file
    Expected columns: imdb_id, title, year, runtime, genres, rating, description, poster
    """
    print(f"[INFO] Loading movies from {filepath}")
    
    df = pd.read_csv(filepath, encoding='utf-8')
    
    # Normalize column names
    df.columns = df.columns.str.lower().str.strip()
    
    # Map common column variations
    column_mapping = {
        # IMDB
        'tconst': 'imdb_id',
        'primarytitle': 'title',
        'originaltitle': 'original_title',
        'startyear': 'year',
        'runtimeminutes': 'runtime',
        'averagerating': 'rating',
        'numvotes': 'votes',
        # Alternative names
        'name': 'title',
        'release_year': 'year',
        'imdb_rating': 'rating',
        'movie_id': 'imdb_id',
        'overview': 'description',
        'poster_path': 'poster'
    }
    
    for old_col, new_col in column_mapping.items():
        if old_col in df.columns and new_col not in df.columns:
            df.rename(columns={old_col: new_col}, inplace=True)
    
    print(f"[INFO] Loaded {len(df)} movies")
    print(f"[INFO] Columns: {list(df.columns)}")
    
    return df


def import_movies_to_db(
    df: pd.DataFrame,
    conn,
    batch_size: int = 500,
    limit: Optional[int] = None
) -> dict:
    """
    Import movies DataFrame into PostgreSQL database
    """
    if limit:
        df = df.head(limit)
    
    cursor = conn.cursor()
    
    # Stats
    stats = {
        'movies_inserted': 0,
        'genres_inserted': 0,
        'movie_genres_linked': 0,
        'errors': []
    }
    
    # Cache existing genres
    cursor.execute('SELECT "Id", name FROM genres')
    existing_genres = {row[1].lower(): row[0] for row in cursor.fetchall()}
    
    # Collect all genres from dataset
    all_genres = set()
    if 'genres' in df.columns:
        for genres_str in df['genres'].dropna():
            if isinstance(genres_str, str):
                for g in genres_str.split(','):
                    g = g.strip()
                    if g and g.lower() != '\\n':
                        all_genres.add(g)
    
    # Insert new genres
    new_genres = [g for g in all_genres if g.lower() not in existing_genres]
    if new_genres:
        print(f"[INFO] Inserting {len(new_genres)} new genres")
        for genre_name in new_genres:
            try:
                cursor.execute(
                    'INSERT INTO genres (name) VALUES (%s) ON CONFLICT DO NOTHING RETURNING "Id"',
                    (genre_name,)
                )
                result = cursor.fetchone()
                if result:
                    existing_genres[genre_name.lower()] = result[0]
                    stats['genres_inserted'] += 1
            except Exception as e:
                stats['errors'].append(f"Genre {genre_name}: {e}")
        conn.commit()
        
        # Refresh genre cache
        cursor.execute('SELECT "Id", name FROM genres')
        existing_genres = {row[1].lower(): row[0] for row in cursor.fetchall()}
    
    # Insert movies in batches
    print(f"[INFO] Inserting {len(df)} movies in batches of {batch_size}")
    
    for start_idx in range(0, len(df), batch_size):
        batch = df.iloc[start_idx:start_idx + batch_size]
        
        for _, row in batch.iterrows():
            try:
                # Prepare movie data
                title = str(row.get('title', ''))[:500] if pd.notna(row.get('title')) else None
                if not title:
                    continue
                
                imdb_id = str(row.get('imdb_id', ''))[:20] if pd.notna(row.get('imdb_id')) else None
                description = str(row.get('description', '')) if pd.notna(row.get('description')) else None
                year = int(row['year']) if pd.notna(row.get('year')) and str(row.get('year')).isdigit() else None
                rating = float(row['rating']) if pd.notna(row.get('rating')) else None
                runtime = int(row['runtime']) if pd.notna(row.get('runtime')) and str(row.get('runtime')).replace('.', '').isdigit() else None
                poster = str(row.get('poster', ''))[:1000] if pd.notna(row.get('poster')) else None
                
                # Insert movie
                cursor.execute('''
                    INSERT INTO movies (title, description, release_year, imdb_id, imdb_rating, runtime, poster_url)
                    VALUES (%s, %s, %s, %s, %s, %s, %s)
                    ON CONFLICT (imdb_id) DO UPDATE SET
                        title = EXCLUDED.title,
                        description = EXCLUDED.description,
                        imdb_rating = EXCLUDED.imdb_rating
                    RETURNING "Id"
                ''', (title, description, year, imdb_id, rating, runtime, poster))
                
                result = cursor.fetchone()
                if result:
                    movie_id = result[0]
                    stats['movies_inserted'] += 1
                    
                    # Link genres
                    genres_str = row.get('genres', '')
                    if pd.notna(genres_str) and isinstance(genres_str, str):
                        for genre_name in genres_str.split(','):
                            genre_name = genre_name.strip()
                            genre_id = existing_genres.get(genre_name.lower())
                            if genre_id:
                                try:
                                    cursor.execute('''
                                        INSERT INTO movie_genres ("MovieId", "GenreId")
                                        VALUES (%s, %s)
                                        ON CONFLICT DO NOTHING
                                    ''', (movie_id, genre_id))
                                    stats['movie_genres_linked'] += 1
                                except Exception:
                                    pass
                
            except Exception as e:
                stats['errors'].append(f"Movie {row.get('title', 'Unknown')}: {e}")
        
        conn.commit()
        print(f"[INFO] Processed {min(start_idx + batch_size, len(df))}/{len(df)} movies")
    
    cursor.close()
    return stats


def generate_synthetic_ratings(conn, n_users: int = 100, ratings_per_user: int = 20) -> dict:
    """
    Generate synthetic ratings for testing the recommendation system
    Creates fake users and random ratings
    """
    import random
    
    cursor = conn.cursor()
    stats = {'users_created': 0, 'ratings_created': 0}
    
    # Get all movie IDs
    cursor.execute('SELECT "Id" FROM movies')
    movie_ids = [row[0] for row in cursor.fetchall()]
    
    if not movie_ids:
        print("[WARN] No movies found in database")
        return stats
    
    print(f"[INFO] Generating ratings for {n_users} synthetic users")
    
    for i in range(n_users):
        # Create synthetic user
        email = f"synthetic_user_{i}@test.com"
        name = f"User{i}"
        password_hash = "synthetic_password_hash"  # Not for real login
        
        try:
            cursor.execute('''
                INSERT INTO users (name, last_name, email, password, role)
                VALUES (%s, %s, %s, %s, 0)
                ON CONFLICT (email) DO UPDATE SET name = EXCLUDED.name
                RETURNING "Id"
            ''', (name, f"Test{i}", email, password_hash))
            
            result = cursor.fetchone()
            if result:
                user_id = result[0]
                stats['users_created'] += 1
                
                # Generate random ratings
                sample_movies = random.sample(movie_ids, min(ratings_per_user, len(movie_ids)))
                for movie_id in sample_movies:
                    # Weighted random rating (more 4s and 5s like real data)
                    rating = random.choices([1, 2, 3, 4, 5], weights=[5, 10, 20, 35, 30])[0]
                    
                    try:
                        cursor.execute('''
                            INSERT INTO ratings ("UserId", "MovieId", score)
                            VALUES (%s, %s, %s)
                            ON CONFLICT ON CONSTRAINT ix_ratings_user_movie DO NOTHING
                        ''', (user_id, movie_id, rating))
                        stats['ratings_created'] += 1
                    except Exception:
                        pass
        
        except Exception as e:
            print(f"[ERROR] User {i}: {e}")
    
    conn.commit()
    cursor.close()
    
    print(f"[INFO] Created {stats['users_created']} users with {stats['ratings_created']} ratings")
    return stats


if __name__ == '__main__':
    import argparse
    
    parser = argparse.ArgumentParser(description='Import IMDB data into database')
    parser.add_argument('--csv', type=str, help='Path to IMDB CSV file')
    parser.add_argument('--limit', type=int, default=None, help='Limit number of movies to import')
    parser.add_argument('--generate-ratings', action='store_true', help='Generate synthetic ratings')
    parser.add_argument('--n-users', type=int, default=100, help='Number of synthetic users')
    parser.add_argument('--ratings-per-user', type=int, default=20, help='Ratings per synthetic user')
    
    args = parser.parse_args()
    
    # Connect to database
    from database import get_db_connection
    conn = get_db_connection()
    
    if args.csv:
        # Import movies from CSV
        df = load_imdb_movies(args.csv)
        stats = import_movies_to_db(df, conn, limit=args.limit)
        print(f"\n[RESULT] Import stats: {stats}")
    
    if args.generate_ratings:
        # Generate synthetic ratings
        stats = generate_synthetic_ratings(
            conn,
            n_users=args.n_users,
            ratings_per_user=args.ratings_per_user
        )
        print(f"\n[RESULT] Rating generation stats: {stats}")
    
    conn.close()
    print("[DONE] Data import complete")
