"""
Movie Recommendation ML Service
Flask API for collaborative filtering recommendations
"""

import os
from flask import Flask, jsonify, request
from flask_cors import CORS
from dotenv import load_dotenv

# Load environment variables
load_dotenv()

app = Flask(__name__)
CORS(app)

# Import after app creation to avoid circular imports
from recommender import RecommenderModel
from database import get_db_connection, get_user_ratings, get_all_movies

# Initialize the recommender model
recommender = RecommenderModel()


@app.route('/health', methods=['GET'])
def health_check():
    """Health check endpoint"""
    return jsonify({
        'status': 'healthy',
        'model_loaded': recommender.is_trained,
        'service': 'ml-recommendations'
    })


@app.route('/recommendations/<int:user_id>', methods=['GET'])
def get_recommendations(user_id: int):
    """
    Get top-N movie recommendations for a user
    
    Query params:
        - n: number of recommendations (default: 10)
        - explain: include explanation (default: true)
    """
    n = request.args.get('n', default=10, type=int)
    explain = request.args.get('explain', default='true').lower() == 'true'
    
    if not recommender.is_trained:
        return jsonify({
            'error': 'Model not trained yet',
            'message': 'Please train the model first using POST /train'
        }), 503
    
    try:
        recommendations = recommender.get_recommendations(
            user_id=user_id,
            n=n,
            include_explanation=explain
        )
        
        return jsonify({
            'user_id': user_id,
            'recommendations': recommendations,
            'count': len(recommendations)
        })
    except Exception as e:
        return jsonify({
            'error': str(e),
            'message': 'Failed to generate recommendations'
        }), 500


@app.route('/similar/<int:movie_id>', methods=['GET'])
def get_similar_movies(movie_id: int):
    """
    Get movies similar to a given movie (content-based)
    
    Query params:
        - n: number of similar movies (default: 10)
    """
    n = request.args.get('n', default=10, type=int)
    
    if not recommender.is_trained:
        return jsonify({
            'error': 'Model not trained yet'
        }), 503
    
    try:
        similar = recommender.get_similar_movies(movie_id=movie_id, n=n)
        return jsonify({
            'movie_id': movie_id,
            'similar_movies': similar,
            'count': len(similar)
        })
    except Exception as e:
        return jsonify({
            'error': str(e)
        }), 500


@app.route('/train', methods=['POST'])
def train_model():
    """
    Train or retrain the recommendation model
    Uses ratings from the database
    """
    try:
        # Load data from database
        conn = get_db_connection()
        ratings_df = get_user_ratings(conn)
        movies_df = get_all_movies(conn)
        conn.close()
        
        if ratings_df.empty:
            return jsonify({
                'error': 'No ratings data found',
                'message': 'Please ensure there are ratings in the database'
            }), 400
        
        # Train the model
        metrics = recommender.train(ratings_df, movies_df)
        
        # Save the model
        model_path = os.getenv('MODEL_PATH', './models/recommender.pkl')
        os.makedirs(os.path.dirname(model_path), exist_ok=True)
        recommender.save(model_path)
        
        return jsonify({
            'status': 'success',
            'message': 'Model trained successfully',
            'metrics': metrics,
            'saved_to': model_path
        })
    except Exception as e:
        return jsonify({
            'error': str(e),
            'message': 'Training failed'
        }), 500


@app.route('/load', methods=['POST'])
def load_model():
    """Load a previously saved model"""
    try:
        model_path = os.getenv('MODEL_PATH', './models/recommender.pkl')
        recommender.load(model_path)
        return jsonify({
            'status': 'success',
            'message': f'Model loaded from {model_path}'
        })
    except FileNotFoundError:
        return jsonify({
            'error': 'Model file not found',
            'message': 'Please train the model first'
        }), 404
    except Exception as e:
        return jsonify({
            'error': str(e)
        }), 500


if __name__ == '__main__':
    # Try to load existing model on startup
    model_path = os.getenv('MODEL_PATH', './models/recommender.pkl')
    if os.path.exists(model_path):
        try:
            recommender.load(model_path)
            print(f"[INFO] Loaded model from {model_path}")
        except Exception as e:
            print(f"[WARN] Could not load model: {e}")
    
    port = int(os.getenv('PORT', 5001))
    app.run(host='0.0.0.0', port=port, debug=True)
