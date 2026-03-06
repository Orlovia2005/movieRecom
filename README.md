# 🎬 MovieRecom - Система Рекомендаций Фильмов

Интеллектуальная система рекомендаций фильмов с использованием машинного обучения (SVD + Content-Based Filtering).

## 🌟 Возможности

### Пользовательские функции
- 📽️ **Каталог фильмов** — просмотр, поиск, фильтрация по жанрам и годам
- ⭐ **Рейтинг и комментарии** — оценка фильмов и обсуждение
- 🎯 **Персонализированные рекомендации** — ML-алгоритм подбирает фильмы на основе ваших предпочтений
- 📋 **Wishlist** — список желаемых фильмов
- 👤 **Профиль пользователя** — управление аккаунтом и аватаром

### ML-рекомендации
- **SVD (Collaborative Filtering)** — анализ паттернов оценок пользователей
- **TF-IDF (Content-Based)** — анализ жанров и описаний фильмов
- **Гибридный подход** — комбинация обоих методов для точных рекомендаций

### Админ-панель
- 📊 Статистика системы
- 👥 Управление пользователями
- 🎬 Управление фильмами
- 💬 Модерация комментариев

## 🛠️ Технологии

| Компонент | Технология |
|-----------|-----------|
| Backend | ASP.NET Core 9.0 |
| Frontend | Razor Views, Bootstrap 5 |
| База данных | PostgreSQL |
| ML сервис | Python, Flask, scikit-surprise, scikit-learn |
| Контейнеризация | Docker, Docker Compose |
| ORM | Entity Framework Core |

## 🚀 Запуск проекта

### Требования
- Docker & Docker Compose
- Git

### Установка

```bash
# Клонировать репозиторий
git clone https://github.com/YOUR_USERNAME/MovieRecom.git
cd MovieRecom

# Запустить все сервисы
docker-compose up -d

# Импортировать данные о фильмах
docker exec movierecom-ml python import_imdb.py --movies 2000 --users 50 --ratings 30

# Обучить ML модель
curl -X POST http://localhost:5001/train
```

### Доступ
- **Web-приложение**: http://localhost:5050
- **ML API**: http://localhost:5001

## 📁 Структура проекта

```
movieRecom/
├── plt/                    # ASP.NET Core приложение
│   ├── Controllers/        # MVC и API контроллеры
│   ├── Models/             # Модели данных
│   ├── Views/              # Razor представления
│   ├── Services/           # Сервисы (ML интеграция)
│   └── wwwroot/            # Статические файлы (CSS, JS)
├── ml_service/             # Python ML сервис
│   ├── app.py              # Flask API
│   ├── recommender.py      # Алгоритмы рекомендаций
│   ├── database.py         # Работа с БД
│   └── data/               # Данные IMDB
└── docker-compose.yml      # Оркестрация контейнеров
```

## 🔌 API Endpoints

### REST API
| Метод | Endpoint | Описание |
|-------|----------|----------|
| GET | `/api/movies` | Список фильмов |
| GET | `/api/movies/{id}` | Детали фильма |
| POST | `/api/movies/{id}/rate` | Оценить фильм |
| GET | `/api/recommendations` | Рекомендации |
| GET/POST | `/api/wishlist` | Wishlist |

### ML Service API
| Метод | Endpoint | Описание |
|-------|----------|----------|
| GET | `/health` | Статус сервиса |
| POST | `/train` | Обучить модель |
| GET | `/recommendations/{userId}` | Рекомендации для пользователя |
| GET | `/similar/{movieId}` | Похожие фильмы |

## 📊 База данных

- **users** — пользователи
- **movies** — фильмы
- **genres** — жанры
- **ratings** — оценки пользователей
- **comments** — комментарии
- **wishlists** — списки желаемого

## 🎨 Дизайн

- Тёмная тема с 3-тоновой палитрой
- Адаптивный интерфейс
- Карусели для рекомендаций
- Оптимизация для Safari и Windows

## 📜 Лицензия

MIT License

## 👤 Автор

Munir
