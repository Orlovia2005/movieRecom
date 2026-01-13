"""
Import IMDB TSV data to PostgreSQL
Processes title.basics.tsv and title.ratings.tsv from official IMDB datasets
"""

import pandas as pd
import psycopg2
from psycopg2.extras import execute_values
import os
import sys
from dotenv import load_dotenv

load_dotenv()

# Database connection
DB_URL = os.getenv('DATABASE_URL', 'postgresql://postgres:Ignat2005@localhost:5432/movieRecom')

def get_connection():
    return psycopg2.connect(DB_URL)

def import_imdb_data(limit: int = 2000):
    """Import movies from IMDB TSV files"""
    
    print("[INFO] Loading title.basics.tsv...")
    basics_df = pd.read_csv(
        'data/title.basics.tsv',
        sep='\t',
        na_values='\\N',
        dtype={'startYear': str, 'runtimeMinutes': str}
    )
    
    print(f"[INFO] Loaded {len(basics_df)} titles")
    
    # Filter only movies
    movies_df = basics_df[basics_df['titleType'] == 'movie'].copy()
    print(f"[INFO] Filtered to {len(movies_df)} movies")
    
    print("[INFO] Loading title.ratings.tsv...")
    ratings_df = pd.read_csv('data/title.ratings.tsv', sep='\t')
    print(f"[INFO] Loaded {len(ratings_df)} ratings")
    
    # Merge movies with ratings
    merged_df = movies_df.merge(ratings_df, on='tconst', how='left')
    
    # Filter: has rating >= 5.0, at least 1000 votes, year >= 1990
    merged_df = merged_df[
        (merged_df['averageRating'] >= 5.0) &
        (merged_df['numVotes'] >= 1000)
    ]
    
    # Convert year
    merged_df['startYear'] = pd.to_numeric(merged_df['startYear'], errors='coerce')
    merged_df = merged_df[merged_df['startYear'] >= 1990]
    
    # Sort by votes and take top movies
    merged_df = merged_df.sort_values('numVotes', ascending=False).head(limit)
    print(f"[INFO] Selected {len(merged_df)} top movies")
    
    # Connect to database
    conn = get_connection()
    cursor = conn.cursor()
    
    # Get existing genres and create new ones
    all_genres = set()
    for genres_str in merged_df['genres'].dropna():
        for g in genres_str.split(','):
            if g and g != '\\N':
                all_genres.add(g)
    
    print(f"[INFO] Found {len(all_genres)} unique genres: {all_genres}")
    
    # Insert genres
    for genre_name in all_genres:
        cursor.execute(
            'INSERT INTO genres (name) VALUES (%s) ON CONFLICT (name) DO NOTHING',
            (genre_name,)
        )
    conn.commit()
    
    # Get genre IDs
    cursor.execute('SELECT "Id", name FROM genres')
    genre_map = {row[1]: row[0] for row in cursor.fetchall()}
    print(f"[INFO] Genre mapping: {genre_map}")
    
    # Import movies
    stats = {'movies': 0, 'genres_linked': 0}
    
    for _, row in merged_df.iterrows():
        try:
            # Parse runtime
            runtime = None
            if pd.notna(row.get('runtimeMinutes')):
                try:
                    runtime = int(row['runtimeMinutes'])
                except:
                    pass
            
            # Insert movie
            cursor.execute('''
                INSERT INTO movies (title, release_year, imdb_id, imdb_rating, runtime)
                VALUES (%s, %s, %s, %s, %s)
                ON CONFLICT (imdb_id) DO UPDATE SET
                    title = EXCLUDED.title,
                    imdb_rating = EXCLUDED.imdb_rating
                RETURNING "Id"
            ''', (
                row['primaryTitle'][:500] if len(row['primaryTitle']) > 500 else row['primaryTitle'],
                int(row['startYear']) if pd.notna(row['startYear']) else None,
                row['tconst'],
                float(row['averageRating']) if pd.notna(row['averageRating']) else None,
                runtime
            ))
            
            result = cursor.fetchone()
            if result:
                movie_id = result[0]
                stats['movies'] += 1
                
                # Link genres
                genres_str = row.get('genres', '')
                if pd.notna(genres_str) and genres_str != '\\N':
                    for genre_name in genres_str.split(','):
                        genre_id = genre_map.get(genre_name)
                        if genre_id:
                            cursor.execute('''
                                INSERT INTO movie_genres ("MovieId", "GenreId")
                                VALUES (%s, %s)
                                ON CONFLICT DO NOTHING
                            ''', (movie_id, genre_id))
                            stats['genres_linked'] += 1
        
        except Exception as e:
            print(f"[ERROR] {row.get('primaryTitle', 'Unknown')}: {e}")
    
    conn.commit()
    cursor.close()
    conn.close()
    
    print(f"\n[RESULT] Imported {stats['movies']} movies, {stats['genres_linked']} genre links")
    return stats

def generate_test_ratings(n_users: int = 50, ratings_per_user: int = 30):
    """Generate synthetic ratings for testing"""
    import random
    
    conn = get_connection()
    cursor = conn.cursor()
    
    # Get all movie IDs
    cursor.execute('SELECT "Id" FROM movies')
    movie_ids = [row[0] for row in cursor.fetchall()]
    
    if not movie_ids:
        print("[ERROR] No movies in database!")
        return
    
    print(f"[INFO] Generating ratings for {n_users} users across {len(movie_ids)} movies")
    
    stats = {'users': 0, 'ratings': 0}
    
    for i in range(n_users):
        email = f"testuser{i}@example.com"
        
        cursor.execute('''
            INSERT INTO users (name, last_name, email, password, role)
            VALUES (%s, %s, %s, %s, 0)
            ON CONFLICT (email) DO NOTHING
            RETURNING "Id"
        ''', (f'User{i}', f'Test{i}', email, 'hash_placeholder'))
        
        result = cursor.fetchone()
        if result:
            user_id = result[0]
            stats['users'] += 1
            
            # Sample movies and rate them
            sample_size = min(ratings_per_user, len(movie_ids))
            sampled_movies = random.sample(movie_ids, sample_size)
            
            for movie_id in sampled_movies:
                # Weighted random rating (prefer higher ratings like real users)
                rating = random.choices([1, 2, 3, 4, 5], weights=[5, 10, 20, 35, 30])[0]
                
                try:
                    cursor.execute('''
                        INSERT INTO ratings ("UserId", "MovieId", score)
                        VALUES (%s, %s, %s)
                        ON CONFLICT DO NOTHING
                    ''', (user_id, movie_id, rating))
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
    
    parser = argparse.ArgumentParser()
    parser.add_argument('--movies', type=int, default=2000, help='Number of movies to import')
    parser.add_argument('--users', type=int, default=50, help='Number of test users')
    parser.add_argument('--ratings', type=int, default=30, help='Ratings per user')
    parser.add_argument('--skip-movies', action='store_true', help='Skip movie import')
    
    args = parser.parse_args()
    
    if not args.skip_movies:
        print("=== IMPORTING MOVIES ===")
        import_imdb_data(limit=args.movies)
    
    print("\n=== GENERATING TEST RATINGS ===")
    generate_test_ratings(n_users=args.users, ratings_per_user=args.ratings)
    
    print("\n[DONE] Data import complete!")
