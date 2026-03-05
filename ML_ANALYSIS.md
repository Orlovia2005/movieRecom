# 🎬 MovieRecom ML System — Полный Анализ

> **Дата анализа:** 2026-03-05
> **Версия:** ASP.NET Core 9.0 + Python 3.11 Flask

---

## 📊 Обзор Системы

### Архитектура
```
┌─────────────────────────────────────────────────────────────────┐
│                    ASP.NET Core MVC (порт 5050)                 │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ RecommendationsController                                   │ │
│  │  ↓                                                          │ │
│  │ MlRecommendationService (HttpClient)                        │ │
│  │  ↓                                                          │ │
│  │ HTTP GET http://localhost:5001/recommendations/{userId}    │ │
│  └────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                Python Flask ML Service (порт 5001)              │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ app.py                                                      │ │
│  │  ↓                                                          │ │
│  │ RecommenderModel (recommender.py)                           │ │
│  │  ├── SVD Model (Collaborative Filtering)                    │ │
│  │  └── TF-IDF Matrix (Content-Based Filtering)               │ │
│  └────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│              PostgreSQL Database (порт 5432)                    │
│  • ratings (UserId, MovieId, score, created_at)                │
│  • movies (Id, title, description, genres, imdb_rating)        │
│  • wishlists (UserId, MovieId, added_at)                       │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🧠 Алгоритм 1: SVD (Collaborative Filtering)

### Что это?
**Singular Value Decomposition (SVD)** — метод матричной факторизации для коллаборативной фильтрации.

### Как работает обучение?

#### Шаг 1: Загрузка данных из PostgreSQL
```python
# database.py: get_user_ratings()
SELECT
    r."UserId" as user_id,
    r."MovieId" as movie_id,
    r.score as rating,
    r.created_at
FROM ratings r
ORDER BY r.created_at
```

**Результат:** DataFrame с колонками `[user_id, movie_id, rating]`

#### Шаг 2: Инициализация SVD модели
```python
# recommender.py: _train_collaborative_filtering()
self.svd_model = SVD(
    n_factors=100,      # Размерность латентного пространства
    n_epochs=20,        # Количество эпох обучения
    lr_all=0.005,       # Learning rate
    reg_all=0.02,       # Регуляризация (предотвращение переобучения)
    random_state=42     # Для воспроизводимости
)
```

**Параметры:**
- `n_factors=100` — модель представляет каждого пользователя и каждый фильм как 100-мерный вектор
- `lr_all=0.005` — небольшой learning rate для стабильного обучения
- `reg_all=0.02` — регуляризация против переобучения

#### Шаг 3: Кросс-валидация (3-fold)
```python
cv_results = cross_validate(
    self.svd_model,
    data,
    measures=['RMSE', 'MAE'],
    cv=3,
    verbose=True
)
```

**Метрики:**
- **RMSE** (Root Mean Squared Error) — квадратный корень средней квадратичной ошибки
- **MAE** (Mean Absolute Error) — средняя абсолютная ошибка

**Что оценивается:** Насколько точно модель предсказывает оценки пользователей на тестовых данных.

#### Шаг 4: Обучение на полном датасете
```python
trainset = data.build_full_trainset()
self.svd_model.fit(trainset)
```

**Внутренний процесс SVD:**
1. Создается разреженная матрица User-Item (users × movies)
2. SVD разлагает её на 3 матрицы: U × Σ × V^T
3. U — латентные признаки пользователей (n_users × 100)
4. V — латентные признаки фильмов (n_movies × 100)
5. Σ — диагональная матрица с весами факторов

**Результат обучения:**
- Каждый пользователь представлен 100-мерным вектором (его предпочтения)
- Каждый фильм представлен 100-мерным вектором (его характеристики)
- Предсказанная оценка = скалярное произведение векторов user и movie

### Как работает предсказание?

```python
# recommender.py: get_recommendations()
for movie_id in candidate_movies:
    pred = self.svd_model.predict(user_id, movie_id)
    predictions.append({
        'movie_id': int(movie_id),
        'predicted_rating': float(pred.est)  # pred.est — предсказанная оценка
    })
```

**Процесс:**
1. Берется вектор пользователя (100 чисел)
2. Берется вектор фильма (100 чисел)
3. Вычисляется скалярное произведение: `predicted_rating = user_vector · movie_vector`
4. Результат — предсказанная оценка от 1 до 5

**Фильтрация:**
- Исключаются фильмы, которые пользователь уже оценил
- Кэш `user_rated_movies` хранит множество ID оцененных фильмов

**Сортировка:**
```python
predictions.sort(key=lambda x: x['predicted_rating'], reverse=True)
top_predictions = predictions[:n]  # Топ-N рекомендаций
```

---

## 🎯 Алгоритм 2: TF-IDF (Content-Based Filtering)

### Что это?
**Term Frequency-Inverse Document Frequency** — метод векторизации текста для поиска похожих фильмов по жанрам и описаниям.

### Как работает обучение?

#### Шаг 1: Загрузка данных о фильмах
```python
# database.py: get_all_movies()
SELECT
    m."Id" as movie_id,
    m.title,
    m.description,
    STRING_AGG(g.name, ', ') as genres  -- "Action, Thriller, Drama"
FROM movies m
LEFT JOIN movie_genres mg ON m."Id" = mg."MovieId"
LEFT JOIN genres g ON mg."GenreId" = g."Id"
GROUP BY m."Id"
```

**Результат:** DataFrame с колонками `[movie_id, title, description, genres]`

#### Шаг 2: Создание текстового контента
```python
# recommender.py: _build_content_similarity()
self.movies_df['content'] = (
    self.movies_df['genres'].fillna('') + ' ' +
    self.movies_df['description'].fillna('') + ' ' +
    self.movies_df['title'].fillna('')
)
```

**Пример контента для фильма:**
```
"Action, Thriller, Sci-Fi A futuristic tale of rebellion and freedom The Matrix"
```

#### Шаг 3: TF-IDF векторизация
```python
tfidf = TfidfVectorizer(
    max_features=5000,        # Макс. 5000 уникальных слов
    stop_words='english',     # Игнорировать "the", "a", "is" и т.д.
    ngram_range=(1, 2)        # Униграммы и биграммы ("Action", "Action Thriller")
)
tfidf_matrix = tfidf.fit_transform(self.movies_df['content'])
```

**Что происходит:**
1. **Токенизация:** Текст разбивается на слова
2. **TF (Term Frequency):** Частота слова в документе (фильме)
3. **IDF (Inverse Document Frequency):** Обратная частота в корпусе (редкие слова = выше вес)
4. **TF-IDF = TF × IDF:** Итоговый вес слова

**Матрица:** `n_movies × 5000` (каждый фильм = вектор из 5000 чисел)

#### Шаг 4: Вычисление косинусного сходства
```python
self.content_similarity = cosine_similarity(tfidf_matrix, tfidf_matrix)
```

**Косинусное сходство:**
```
similarity(movie_A, movie_B) = (vec_A · vec_B) / (||vec_A|| × ||vec_B||)
```

**Результат:** Матрица `n_movies × n_movies` с коэффициентами сходства [0, 1]
- 1.0 = идентичные фильмы
- 0.0 = совершенно разные

### Как работает поиск похожих фильмов?

```python
# recommender.py: get_similar_movies()
idx = self.movie_id_to_idx[movie_id]  # Индекс фильма в матрице
sim_scores = list(enumerate(self.content_similarity[idx]))  # Все сходства
sim_scores = sorted(sim_scores, key=lambda x: x[1], reverse=True)  # Сортировка
sim_scores = sim_scores[1:n+1]  # Пропустить себя, взять топ-N
```

**Пример:**
- Фильм: "The Matrix" (жанры: Action, Sci-Fi)
- Похожие: "Blade Runner" (0.87), "Total Recall" (0.82), "Inception" (0.79)

---

## 🔀 Гибридный подход: Collaborative + Content-Based

### Где используется каждый метод?

| Метод | Endpoint | Назначение |
|-------|----------|------------|
| **SVD (Collaborative)** | `GET /recommendations/{userId}` | Персонализированные рекомендации на основе рейтингов пользователя |
| **TF-IDF (Content-Based)** | `GET /similar/{movieId}` | Похожие фильмы по жанрам/описанию |

### Проблема холодного старта

**Холодный старт** — когда у пользователя нет или мало оценок.

**Решение в текущей системе:**
1. **ASP.NET Fallback** (см. `RecommendationsController`):
   ```csharp
   // Если ML сервис недоступен или вернул null
   var fallbackRecommendations = GetFallbackRecommendations(userId);
   ```

2. **Fallback алгоритм:**
   - Анализирует оценки пользователя (score >= 4)
   - Определяет любимые жанры
   - Рекомендует фильмы с:
     - Высоким IMDB рейтингом (>= 7.0)
     - Совпадающими жанрами
     - Недавним годом выпуска

**Проблема:** Wishlist НЕ используется в текущей ML-модели!

---

## 📈 Персонализация и Wishlist

### Текущая реализация

**Факторы персонализации:**
1. ✅ **Оценки пользователя (ratings)** — используются SVD
2. ❌ **Wishlist** — НЕ используется в ML-модели
3. ❌ **История просмотров** — не реализовано
4. ✅ **Жанры фильмов** — используются TF-IDF для похожих фильмов

### Как wishlist мог бы влиять?

**Вариант 1: Wishlist как неявные оценки**
```python
# Добавить фильмы из wishlist как предсказанные оценки 4.5
for movie_id in wishlist_movies:
    ratings_df.append({
        'user_id': user_id,
        'movie_id': movie_id,
        'rating': 4.5,  # Предполагаемая высокая оценка
        'is_implicit': True  # Флаг
    })
```

**Вариант 2: Boosting похожих фильмов**
```python
# После получения рекомендаций от SVD
for rec in recommendations:
    similarity_to_wishlist = compute_similarity(rec['movie_id'], wishlist_movies)
    rec['predicted_rating'] += similarity_to_wishlist * 0.5  # Бонус
```

**Текущая проблема:** Wishlist используется только в ASP.NET контроллере для UI-фильтрации, но не влияет на ML-алгоритм.

---

## 🧪 Тестирование и Верификация

### Сценарии для проверки

#### 1. Проверка обучения SVD
```bash
# Запустить Docker
docker-compose up -d

# Импортировать тестовые данные
docker exec movierecom-ml python import_imdb.py --movies 2000 --users 50 --ratings 30

# Обучить модель
curl -X POST http://localhost:5001/train
```

**Ожидаемый ответ:**
```json
{
  "status": "success",
  "metrics": {
    "collaborative_filtering": {
      "rmse": 0.85,  // Должно быть < 1.0
      "mae": 0.65,   // Должно быть < 0.8
      "n_ratings": 3000,
      "n_users": 50,
      "n_movies": 2000
    },
    "content_based": {
      "movies_processed": 2000,
      "similarity_matrix_shape": [2000, 2000]
    }
  }
}
```

**Критерии успеха:**
- RMSE < 1.0 (модель предсказывает с ошибкой меньше 1 звезды)
- MAE < 0.8 (средняя ошибка < 0.8 звезд)

#### 2. Проверка персонализации
```bash
# Создать тестового пользователя
USER_ID=101

# Дать ему оценки (только Action/Thriller)
# В БД: ratings (user_id=101, movie_id=[Matrix, Inception, ...], score=5)

# Получить рекомендации
curl http://localhost:5001/recommendations/101?n=10
```

**Ожидаемое поведение:**
- Рекомендации должны быть преимущественно Action/Thriller
- Predicted_rating должен быть > 4.0 для релевантных фильмов

**Тест на персонализацию:**
```python
# Сценарий 1: Пользователь любит комедии
user_A_ratings = [("Superbad", 5), ("The Hangover", 5), ("Step Brothers", 5)]
recommendations_A = get_recommendations(user_A)
# Ожидание: Топ-10 = комедии

# Сценарий 2: Пользователь любит хоррор
user_B_ratings = [("The Conjuring", 5), ("Hereditary", 5), ("A Quiet Place", 5)]
recommendations_B = get_recommendations(user_B)
# Ожидание: Топ-10 = хорроры

# Проверка: recommendations_A ≠ recommendations_B
```

#### 3. Проверка Content-Based (похожие фильмы)
```bash
# Получить ID фильма (например, The Matrix)
MATRIX_ID=42

# Запросить похожие
curl http://localhost:5001/similar/42?n=10
```

**Ожидаемое поведение:**
- Похожие фильмы должны быть из схожих жанров (Sci-Fi, Action)
- Similarity_score должен быть > 0.5 для релевантных

#### 4. Проверка Wishlist (вручную)
```bash
# Добавить фильм в wishlist через UI
# Затем получить рекомендации

# Текущее поведение: Wishlist НЕ влияет на ML
# Проверить: появятся ли похожие фильмы в рекомендациях?
```

**Проблема:** Нужно вручную проверить, т.к. wishlist не интегрирован в ML-модель.

#### 5. Проверка холодного старта
```bash
# Создать нового пользователя БЕЗ оценок
NEW_USER_ID=999

# Запросить рекомендации
curl http://localhost:5001/recommendations/999?n=10
```

**Ожидаемое поведение:**
- ML вернет пустой список или ошибку
- ASP.NET fallback должен сработать: популярные фильмы с высоким IMDB

#### 6. Проверка edge cases
**Сценарий 1: Пользователь оценил ВСЕ фильмы**
```python
# Все фильмы в rated_movies
candidate_movies = set() - rated_movies
# Результат: [] (пустой список)
```

**Сценарий 2: В базе 0 оценок**
```python
# ratings_df.empty == True
return {'error': 'No ratings data found'}, 400
```

---

## 📊 Метрики качества

### Метрики обучения

| Метрика | Формула | Интерпретация | Целевое значение |
|---------|---------|---------------|------------------|
| **RMSE** | √(Σ(pred - actual)² / n) | Средняя ошибка предсказания (в звездах) | < 1.0 |
| **MAE** | Σ\|pred - actual\| / n | Средняя абсолютная ошибка | < 0.8 |

### Метрики качества рекомендаций (нужно добавить)

**Precision@K:**
```
Precision@10 = (Релевантные фильмы в топ-10) / 10
```

**Recall@K:**
```
Recall@10 = (Релевантные фильмы в топ-10) / (Всего релевантных)
```

**NDCG (Normalized Discounted Cumulative Gain):**
- Учитывает порядок рекомендаций
- Более высокие позиции = больший вес

---

## 🔍 Выводы и Рекомендации

### ✅ Что работает хорошо

1. **SVD правильно обучается** — использует collaborative filtering на реальных оценках
2. **TF-IDF корректно находит похожие фильмы** — по жанрам и описаниям
3. **Fallback механизм** — ASP.NET подхватывает, если ML недоступен
4. **Кэширование** — `user_rated_movies` оптимизирует фильтрацию
5. **Health checks** — можно проверить статус модели

### ❌ Проблемы и ограничения

1. **Wishlist НЕ используется в ML**
   - Решение: Добавить wishlist-фильмы как implicit feedback (rating 4.5)

2. **Нет учета истории просмотров**
   - Решение: Создать таблицу `viewing_history` и использовать в обучении

3. **Нет метрик качества рекомендаций**
   - Решение: Добавить Precision@K, Recall@K, NDCG в `/train` response

4. **Холодный старт не решается ML-моделью**
   - Решение: Hybrid подход — использовать TF-IDF для новых пользователей

5. **Параметры SVD hardcoded**
   - Решение: Сделать конфигурируемыми через environment variables

### 🎯 План улучшений

#### Приоритет 1: Интеграция Wishlist
```python
def _prepare_training_data(ratings_df, wishlists_df):
    # Добавить wishlist как implicit ratings
    implicit_ratings = wishlists_df.copy()
    implicit_ratings['rating'] = 4.5
    implicit_ratings['is_implicit'] = True

    combined = pd.concat([ratings_df, implicit_ratings])
    return combined
```

#### Приоритет 2: Метрики качества
```python
def evaluate_recommendations(model, test_data):
    precision_at_10 = compute_precision(model, test_data, k=10)
    recall_at_10 = compute_recall(model, test_data, k=10)
    ndcg_at_10 = compute_ndcg(model, test_data, k=10)

    return {
        'precision@10': precision_at_10,
        'recall@10': recall_at_10,
        'ndcg@10': ndcg_at_10
    }
```

#### Приоритет 3: A/B тестирование
- Сравнить рекомендации с wishlist vs без
- Измерить CTR (click-through rate) на рекомендациях

---

## 📝 Как запустить полное тестирование

```bash
# 1. Запустить все сервисы
docker-compose up -d

# 2. Проверить здоровье
curl http://localhost:5001/health

# 3. Импортировать данные
docker exec movierecom-ml python import_imdb.py --movies 2000 --users 50 --ratings 30

# 4. Обучить модель
curl -X POST http://localhost:5001/train

# 5. Тест: рекомендации для пользователя
curl http://localhost:5001/recommendations/1?n=10 | jq

# 6. Тест: похожие фильмы
curl http://localhost:5001/similar/42?n=10 | jq

# 7. Тест: ASP.NET интеграция
curl http://localhost:5050/api/recommendations -H "Authorization: Bearer YOUR_TOKEN"
```

---

**Документ создан:** 2026-03-05
**Автор:** Claude Code ML Analysis
