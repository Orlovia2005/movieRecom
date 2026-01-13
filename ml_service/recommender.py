"""
Recommendation Model using Collaborative Filtering
Uses SVD from scikit-surprise for matrix factorization
"""

import os
import pickle
import pandas as pd
import numpy as np
from typing import List, Dict, Any, Optional
from collections import defaultdict

from surprise import Dataset, Reader, SVD
from surprise.model_selection import cross_validate
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.metrics.pairwise import cosine_similarity


class RecommenderModel:
    """
    Hybrid Recommendation System combining:
    1. Collaborative Filtering (SVD) - based on user ratings
    2. Content-Based Filtering (TF-IDF) - based on movie descriptions/genres
    """
    
    def __init__(self):
        self.svd_model: Optional[SVD] = None
        self.movies_df: Optional[pd.DataFrame] = None
        self.content_similarity: Optional[np.ndarray] = None
        self.movie_id_to_idx: Dict[int, int] = {}
        self.idx_to_movie_id: Dict[int, int] = {}
        self.is_trained: bool = False
        self.user_rated_movies: Dict[int, set] = defaultdict(set)
        
    def train(self, ratings_df: pd.DataFrame, movies_df: pd.DataFrame) -> Dict[str, Any]:
        """
        Train the recommendation model
        
        Args:
            ratings_df: DataFrame with columns [user_id, movie_id, rating]
            movies_df: DataFrame with movie details including genres, description
            
        Returns:
            Training metrics
        """
        print("[INFO] Training recommendation model...")
        
        # Store movies data
        self.movies_df = movies_df.copy()
        self._build_movie_mappings()
        
        # Build user rated movies cache
        self._build_user_ratings_cache(ratings_df)
        
        # Train collaborative filtering model
        cf_metrics = self._train_collaborative_filtering(ratings_df)
        
        # Build content-based similarity matrix
        self._build_content_similarity()
        
        self.is_trained = True
        print("[INFO] Model training complete!")
        
        return {
            'collaborative_filtering': cf_metrics,
            'content_based': {
                'movies_processed': len(self.movies_df),
                'similarity_matrix_shape': self.content_similarity.shape if self.content_similarity is not None else None
            }
        }
    
    def _build_movie_mappings(self):
        """Build mapping between movie IDs and matrix indices"""
        for idx, movie_id in enumerate(self.movies_df['movie_id'].values):
            self.movie_id_to_idx[movie_id] = idx
            self.idx_to_movie_id[idx] = movie_id
    
    def _build_user_ratings_cache(self, ratings_df: pd.DataFrame):
        """Cache which movies each user has rated"""
        self.user_rated_movies.clear()
        for _, row in ratings_df.iterrows():
            self.user_rated_movies[row['user_id']].add(row['movie_id'])
    
    def _train_collaborative_filtering(self, ratings_df: pd.DataFrame) -> Dict[str, float]:
        """Train SVD model for collaborative filtering"""
        # Prepare data for Surprise
        reader = Reader(rating_scale=(1, 5))
        data = Dataset.load_from_df(
            ratings_df[['user_id', 'movie_id', 'rating']],
            reader
        )
        
        # Train SVD model
        self.svd_model = SVD(
            n_factors=100,
            n_epochs=20,
            lr_all=0.005,
            reg_all=0.02,
            random_state=42
        )
        
        # Cross-validate and get metrics
        cv_results = cross_validate(
            self.svd_model,
            data,
            measures=['RMSE', 'MAE'],
            cv=3,
            verbose=True
        )
        
        # Fit on full dataset
        trainset = data.build_full_trainset()
        self.svd_model.fit(trainset)
        
        return {
            'rmse': float(np.mean(cv_results['test_rmse'])),
            'mae': float(np.mean(cv_results['test_mae'])),
            'n_ratings': len(ratings_df),
            'n_users': ratings_df['user_id'].nunique(),
            'n_movies': ratings_df['movie_id'].nunique()
        }
    
    def _build_content_similarity(self):
        """Build content-based similarity matrix using TF-IDF on genres and descriptions"""
        if self.movies_df is None or len(self.movies_df) == 0:
            return
        
        # Create combined text features
        self.movies_df['content'] = (
            self.movies_df['genres'].fillna('') + ' ' +
            self.movies_df['description'].fillna('') + ' ' +
            self.movies_df['title'].fillna('')
        )
        
        # TF-IDF vectorization
        tfidf = TfidfVectorizer(
            max_features=5000,
            stop_words='english',
            ngram_range=(1, 2)
        )
        
        tfidf_matrix = tfidf.fit_transform(self.movies_df['content'])
        
        # Compute cosine similarity
        self.content_similarity = cosine_similarity(tfidf_matrix, tfidf_matrix)
    
    def get_recommendations(
        self,
        user_id: int,
        n: int = 10,
        include_explanation: bool = True
    ) -> List[Dict[str, Any]]:
        """
        Get top-N movie recommendations for a user
        
        Uses hybrid approach:
        - Primary: Collaborative filtering (SVD)
        - Fallback: Content-based for cold start
        """
        if not self.is_trained or self.svd_model is None:
            raise ValueError("Model not trained")
        
        # Get movies user hasn't rated
        rated_movies = self.user_rated_movies.get(user_id, set())
        candidate_movies = set(self.movies_df['movie_id'].values) - rated_movies
        
        if not candidate_movies:
            return []
        
        # Predict ratings for all candidate movies
        predictions = []
        for movie_id in candidate_movies:
            pred = self.svd_model.predict(user_id, movie_id)
            predictions.append({
                'movie_id': int(movie_id),
                'predicted_rating': float(pred.est)
            })
        
        # Sort by predicted rating
        predictions.sort(key=lambda x: x['predicted_rating'], reverse=True)
        top_predictions = predictions[:n]
        
        # Enrich with movie details and explanations
        recommendations = []
        for pred in top_predictions:
            movie_id = pred['movie_id']
            movie_row = self.movies_df[self.movies_df['movie_id'] == movie_id]
            
            if movie_row.empty:
                continue
            
            movie = movie_row.iloc[0]
            rec = {
                'movie_id': movie_id,
                'title': movie.get('title', 'Unknown'),
                'predicted_rating': round(pred['predicted_rating'], 2),
                'genres': movie.get('genres', ''),
                'imdb_rating': movie.get('imdb_rating'),
                'poster_url': movie.get('poster_url'),
                'release_year': int(movie.get('release_year')) if pd.notna(movie.get('release_year')) else None
            }
            
            if include_explanation:
                rec['explanation'] = self._generate_explanation(user_id, movie_id)
            
            recommendations.append(rec)
        
        return recommendations
    
    def _generate_explanation(self, user_id: int, movie_id: int) -> str:
        """Generate human-readable explanation for recommendation"""
        movie_row = self.movies_df[self.movies_df['movie_id'] == movie_id]
        if movie_row.empty:
            return "Рекомендовано на основе ваших предпочтений"
        
        movie = movie_row.iloc[0]
        genres = movie.get('genres', '')
        
        explanations = []
        
        if genres:
            genre_list = genres.split(', ')[:2]  # Top 2 genres
            explanations.append(f"Жанр: {', '.join(genre_list)}")
        
        imdb_rating = movie.get('imdb_rating')
        if imdb_rating and imdb_rating >= 7.0:
            explanations.append(f"Высокий рейтинг IMDB: {imdb_rating}")
        
        if not explanations:
            explanations.append("Похоже на фильмы, которые вам понравились")
        
        return "; ".join(explanations)
    
    def get_similar_movies(self, movie_id: int, n: int = 10) -> List[Dict[str, Any]]:
        """
        Get movies similar to a given movie (content-based)
        """
        if not self.is_trained or self.content_similarity is None:
            raise ValueError("Model not trained")
        
        if movie_id not in self.movie_id_to_idx:
            return []
        
        idx = self.movie_id_to_idx[movie_id]
        sim_scores = list(enumerate(self.content_similarity[idx]))
        sim_scores = sorted(sim_scores, key=lambda x: x[1], reverse=True)
        
        # Skip the first one (itself) and get top N
        sim_scores = sim_scores[1:n+1]
        
        similar_movies = []
        for movie_idx, score in sim_scores:
            similar_movie_id = self.idx_to_movie_id[movie_idx]
            movie_row = self.movies_df[self.movies_df['movie_id'] == similar_movie_id]
            
            if movie_row.empty:
                continue
            
            movie = movie_row.iloc[0]
            similar_movies.append({
                'movie_id': int(similar_movie_id),
                'title': movie.get('title', 'Unknown'),
                'similarity_score': round(float(score), 3),
                'genres': movie.get('genres', ''),
                'poster_url': movie.get('poster_url')
            })
        
        return similar_movies
    
    def save(self, filepath: str):
        """Save the trained model to disk"""
        os.makedirs(os.path.dirname(filepath), exist_ok=True)
        
        model_data = {
            'svd_model': self.svd_model,
            'movies_df': self.movies_df,
            'content_similarity': self.content_similarity,
            'movie_id_to_idx': self.movie_id_to_idx,
            'idx_to_movie_id': self.idx_to_movie_id,
            'user_rated_movies': dict(self.user_rated_movies),
            'is_trained': self.is_trained
        }
        
        with open(filepath, 'wb') as f:
            pickle.dump(model_data, f)
        
        print(f"[INFO] Model saved to {filepath}")
    
    def load(self, filepath: str):
        """Load a trained model from disk"""
        with open(filepath, 'rb') as f:
            model_data = pickle.load(f)
        
        self.svd_model = model_data['svd_model']
        self.movies_df = model_data['movies_df']
        self.content_similarity = model_data['content_similarity']
        self.movie_id_to_idx = model_data['movie_id_to_idx']
        self.idx_to_movie_id = model_data['idx_to_movie_id']
        self.user_rated_movies = defaultdict(set, model_data['user_rated_movies'])
        self.is_trained = model_data['is_trained']
        
        print(f"[INFO] Model loaded from {filepath}")
