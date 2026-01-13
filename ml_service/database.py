"""
Database connection and data retrieval utilities
Connects to PostgreSQL database used by the C# backend
"""

import os
import pandas as pd
import psycopg2
from psycopg2.extras import RealDictCursor
from dotenv import load_dotenv

load_dotenv()


def get_db_connection():
    """
    Create a connection to the PostgreSQL database
    """
    database_url = os.getenv('DATABASE_URL')
    
    if database_url:
        return psycopg2.connect(database_url)
    
    # Fallback to individual params
    return psycopg2.connect(
        host=os.getenv('DB_HOST', 'localhost'),
        port=os.getenv('DB_PORT', '5432'),
        database=os.getenv('DB_NAME', 'movieRecom'),
        user=os.getenv('DB_USER', 'postgres'),
        password=os.getenv('DB_PASSWORD', 'Ignat2005')
    )


def get_user_ratings(conn) -> pd.DataFrame:
    """
    Fetch all user ratings from the database
    Returns DataFrame with columns: user_id, movie_id, score, created_at
    """
    query = """
        SELECT 
            r."UserId" as user_id,
            r."MovieId" as movie_id,
            r.score as rating,
            r.created_at
        FROM ratings r
        ORDER BY r.created_at
    """
    
    try:
        df = pd.read_sql_query(query, conn)
        return df
    except Exception as e:
        print(f"[ERROR] Failed to fetch ratings: {e}")
        return pd.DataFrame()


def get_all_movies(conn) -> pd.DataFrame:
    """
    Fetch all movies with their genres for content-based filtering
    Returns DataFrame with movie details
    """
    query = """
        SELECT 
            m."Id" as movie_id,
            m.title,
            m.description,
            m.release_year,
            m.imdb_id,
            m.imdb_rating,
            m.runtime,
            m.poster_url,
            STRING_AGG(g.name, ', ') as genres
        FROM movies m
        LEFT JOIN movie_genres mg ON m."Id" = mg."MovieId"
        LEFT JOIN genres g ON mg."GenreId" = g."Id"
        GROUP BY m."Id", m.title, m.description, m.release_year, 
                 m.imdb_id, m.imdb_rating, m.runtime, m.poster_url
    """
    
    try:
        df = pd.read_sql_query(query, conn)
        return df
    except Exception as e:
        print(f"[ERROR] Failed to fetch movies: {e}")
        return pd.DataFrame()


def get_user_wishlist(conn, user_id: int) -> pd.DataFrame:
    """
    Fetch user's wishlist movies
    """
    query = """
        SELECT 
            w."MovieId" as movie_id,
            m.title,
            w.added_at
        FROM wishlists w
        JOIN movies m ON w."MovieId" = m."Id"
        WHERE w."UserId" = %s
        ORDER BY w.added_at DESC
    """
    
    try:
        df = pd.read_sql_query(query, conn, params=(user_id,))
        return df
    except Exception as e:
        print(f"[ERROR] Failed to fetch wishlist: {e}")
        return pd.DataFrame()


def get_user_rated_movies(conn, user_id: int) -> set:
    """
    Get set of movie IDs that user has already rated
    Used to exclude from recommendations
    """
    query = """
        SELECT "MovieId" as movie_id
        FROM ratings
        WHERE "UserId" = %s
    """
    
    try:
        with conn.cursor() as cur:
            cur.execute(query, (user_id,))
            return {row[0] for row in cur.fetchall()}
    except Exception as e:
        print(f"[ERROR] Failed to fetch rated movies: {e}")
        return set()
