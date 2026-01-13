# ML Recommendation Service

Flask-based API service for movie recommendations using collaborative filtering.

## Setup

```bash
cd ml_service
python -m venv venv
source venv/bin/activate  # Windows: venv\Scripts\activate
pip install -r requirements.txt
```

## Environment Variables

Create `.env` file:
```
DATABASE_URL=postgresql://postgres:password@localhost:5432/movieRecom
MODEL_PATH=./models/recommender.pkl
```

## Running

```bash
# Development
flask run --port 5001

# Production
gunicorn -w 4 -b 0.0.0.0:5001 app:app
```

## API Endpoints

- `GET /health` - Health check
- `GET /recommendations/<user_id>` - Get top-10 recommendations for user
- `POST /train` - Retrain the model
- `GET /similar/<movie_id>` - Get similar movies
