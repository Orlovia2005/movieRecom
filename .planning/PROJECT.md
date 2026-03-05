# MovieRecom ML System Audit

## What This Is

Comprehensive audit and verification of the ML-powered movie recommendation system. The project uses hybrid collaborative filtering (SVD) and content-based filtering (TF-IDF) to provide personalized movie recommendations based on user ratings, wishlist preferences, and viewing history. This audit will validate that the ML algorithms work correctly, recommendations are truly personalized, and the system handles edge cases appropriately.

## Core Value

Verify that the recommendation engine delivers genuinely personalized suggestions by accurately learning from user preferences (ratings, wishlist) and movie attributes (genres, descriptions) to match users with films they'll enjoy.

## Requirements

### Validated

- ✓ ASP.NET Core 9.0 web application with MVC and REST API — existing
- ✓ Python Flask ML microservice with SVD + TF-IDF algorithms — existing
- ✓ PostgreSQL database with users, movies, ratings, wishlists — existing
- ✓ Docker Compose orchestration for all services — existing
- ✓ JWT + Cookie authentication system — existing
- ✓ Dual-mode recommendations (ML with local fallback) — existing
- ✓ Health checks and monitoring — existing
- ✓ Unit and integration tests for services — existing

### Active

- [ ] **"Liked Movies" page** — New page showing all movies user has rated, with ability to remove ratings
- [ ] **Profile page redesign** — Improve UI/UX of /Account/Profile with better layout and styling
- [ ] **Wishlist integration** — Already done ✓ (implicit feedback rating 4.5)

### Out of Scope

- Adding new ML algorithms (deep learning, neural collaborative filtering) — Future enhancement
- Production deployment — Dev/test environment only
- Real user testing — Synthetic test scenarios for now

## Context

**Existing Implementation:**
- SVD (Singular Value Decomposition) via scikit-surprise for collaborative filtering
- TF-IDF (Term Frequency-Inverse Document Frequency) via scikit-learn for content similarity
- Model persisted to pickle file (`ml_service/models/recommender.pkl`)
- Training triggered via POST /train endpoint
- Recommendations served via GET /recommendations/{userId}?n=10
- Local fallback algorithm in ASP.NET when ML service unavailable

**Current Codebase State:**
- Architecture: Two-service microservices (ASP.NET + Flask)
- Database: 8 tables including Users, Movies, Ratings, Wishlists, HiddenRecommendations
- Existing codebase map in `.planning/codebase/` (7 documents)
- Test suite exists but ML-specific tests may be incomplete

**Known Considerations:**
- ML service runs on port 5001, main app on port 5050
- Model retraining should be tested with realistic data volumes
- Confidence levels: 🔥 ≥80%, 👍 60-79%, 🎲 <60%
- Audit logging captures all data modifications

## Constraints

- **Environment**: Docker Compose stack must be running for all tests
- **Database**: PostgreSQL 15 with existing schema (no migrations during audit)
- **Language**: Python 3.11 for ML service, .NET 9.0 for web app
- **Data**: Use existing IMDB import script for test data (import_imdb.py)
- **Timeline**: Comprehensive audit, not rushed — thorough verification matters

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Audit existing system without modifications | Verify current implementation before any enhancements | — Pending |
| Focus on ML correctness over performance | Accuracy of recommendations more critical than speed | — Pending |
| Test with realistic data volumes | Use import_imdb.py to generate 2000+ movies, 50+ users | — Pending |
| Document ML behavior in detail | Create comprehensive analysis for future development | — Pending |

---
*Last updated: 2026-03-05 after project initialization*
