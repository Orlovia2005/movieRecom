# 🎬 MovieRecom - Инструкция для тестирования

## 🚀 Доступ к приложению

**Web-приложение:** http://localhost:5050
**ML API:** http://localhost:5001

---

## 👤 Тестовые учетные данные

### Пользователь #29 (с wishlist Horror фильмов)
- **Email:** testuser27@example.com
- **Password:** (используйте существующего пользователя через регистрацию)

### Или создайте нового пользователя:
- Перейдите на http://localhost:5050/Account/Register
- Зарегистрируйтесь с любым email/паролем

---

## ✅ Что протестировать:

### 1. Персонализированные рекомендации
1. Войдите в систему
2. Перейдите на страницу **Рекомендации** (Recommendations)
3. Оцените несколько фильмов (1-5 звезд)
4. **Обновите ML-модель:**
   ```bash
   curl -X POST http://localhost:5001/train
   ```
5. Обновите страницу рекомендаций
6. **Результат:** Рекомендации должны измениться в соответствии с вашими оценками

### 2. Wishlist интеграция ✨ НОВОЕ!
1. Добавьте несколько фильмов одного жанра (например, Horror) в **Wishlist**
2. **Переобучите модель:**
   ```bash
   curl -X POST http://localhost:5001/train
   ```
3. Проверьте рекомендации
4. **Результат:** Фильмы похожего жанра должны появиться в топе рекомендаций

### 3. Похожие фильмы (Content-Based)
1. Откройте страницу любого фильма
2. Посмотрите раздел "Похожие фильмы"
3. **Результат:** Фильмы со схожими жанрами и описаниями

### 4. Каталог и фильтры
1. Перейдите в **Каталог фильмов** (Movies)
2. Используйте фильтры по жанрам и годам
3. Используйте поиск

---

## 🧪 API тестирование

### Health Check
```bash
curl http://localhost:5001/health | jq .
```

### Получить рекомендации для пользователя
```bash
curl "http://localhost:5001/recommendations/29?n=10" | jq .
```

### Похожие фильмы (The Matrix = ID 8)
```bash
curl "http://localhost:5001/similar/8?n=10" | jq .
```

### Переобучить модель
```bash
curl -X POST http://localhost:5001/train | jq .
```

---

## 📊 Проверка Wishlist интеграции

### Добавить Horror фильмы в wishlist для пользователя #29:
```bash
docker exec movierecom-db psql -U postgres -d movieRecom -c "
INSERT INTO wishlists (\"UserId\", \"MovieId\", added_at)
SELECT 29, m.\"Id\", NOW()
FROM movies m
LEFT JOIN movie_genres mg ON m.\"Id\" = mg.\"MovieId\"
LEFT JOIN genres g ON mg.\"GenreId\" = g.\"Id\"
WHERE g.name = 'Horror'
LIMIT 5
ON CONFLICT DO NOTHING;"
```

### Переобучить модель с wishlist:
```bash
curl -X POST http://localhost:5001/train
```

### Проверить рекомендации:
```bash
curl "http://localhost:5001/recommendations/29?n=15" | jq '.recommendations[] | {title, genres, predicted_rating}'
```

**Ожидаемый результат:** Horror фильмы появятся в топе с высокими predicted_rating!

---

## 📁 База данных

### Подключение к PostgreSQL:
```bash
docker exec -it movierecom-db psql -U postgres -d movieRecom
```

### Полезные запросы:

**Посмотреть фильмы:**
```sql
SELECT * FROM movies LIMIT 10;
```

**Посмотреть оценки пользователя:**
```sql
SELECT m.title, r.score, STRING_AGG(g.name, ', ') as genres
FROM ratings r
JOIN movies m ON r."MovieId" = m."Id"
LEFT JOIN movie_genres mg ON m."Id" = mg."MovieId"
LEFT JOIN genres g ON mg."GenreId" = g."Id"
WHERE r."UserId" = 29
GROUP BY m.title, r.score
ORDER BY r.score DESC;
```

**Посмотреть wishlist:**
```sql
SELECT m.title, w.added_at
FROM wishlists w
JOIN movies m ON w."MovieId" = m."Id"
WHERE w."UserId" = 29;
```

---

## 🎯 Что проверяет ML-система:

### SVD (Collaborative Filtering):
- ✅ Обучается на оценках пользователей
- ✅ **НОВОЕ:** Использует wishlist как implicit feedback (rating 4.5)
- ✅ Предсказывает оценки для непросмотренных фильмов
- ✅ Персонализирует рекомендации

### TF-IDF (Content-Based):
- ✅ Анализирует жанры, описания, названия фильмов
- ✅ Находит похожие фильмы по содержанию
- ✅ Cosine similarity для вычисления сходства

### Метрики качества:
- **RMSE:** 1.14 (средняя ошибка предсказания)
- **MAE:** 0.92 (средняя абсолютная ошибка)
- **Wishlist entries:** 11 (используется для обучения)

---

## 🐛 Известные проблемы:

1. **Docker networking:** ASP.NET Web не видит ML-сервис через localhost
   - **Решение:** Используйте прямые вызовы ML API через curl

2. **Модель не обучена:** При первом запуске модель не загружена
   - **Решение:** Выполните `curl -X POST http://localhost:5001/train`

---

## 📝 Документация:

- **ML_ANALYSIS.md** — Детальный анализ алгоритмов SVD и TF-IDF
- **ML_TEST_RESULTS.md** — Результаты тестирования и метрики
- **GSD_SETUP.md** — Настройка Claude Code + GSD

---

**Создано:** 2026-03-05
**Wishlist интеграция:** ✅ Работает
**Готово к тестированию:** ✅ Да
