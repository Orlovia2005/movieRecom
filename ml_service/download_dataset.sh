#!/bin/bash

# Скрипт для скачивания IMDB датасета из Kaggle
# Перед использованием нужно настроить Kaggle API

echo "=== Скачивание IMDB датасета ==="

# Создаём папку для данных
mkdir -p data
cd data

# Вариант 1: Через Kaggle CLI (рекомендуется)
# Установка: pip install kaggle
# Настройка: положить kaggle.json в ~/.kaggle/

if command -v kaggle &> /dev/null; then
    echo "[INFO] Используем Kaggle CLI..."
    kaggle datasets download -d harshitshankhdhar/imdb-dataset-of-top-1000-movies-and-tv-shows
    unzip -o imdb-dataset-of-top-1000-movies-and-tv-shows.zip
    rm imdb-dataset-of-top-1000-movies-and-tv-shows.zip
    echo "[OK] Датасет скачан!"
else
    echo "[INFO] Kaggle CLI не установлен. Устанавливаем..."
    pip install kaggle

    echo ""
    echo "=== ИНСТРУКЦИЯ ПО НАСТРОЙКЕ KAGGLE ==="
    echo "1. Зайди на https://www.kaggle.com/settings"
    echo "2. Нажми 'Create New Token' - скачается kaggle.json"
    echo "3. Выполни команды:"
    echo "   mkdir -p ~/.kaggle"
    echo "   mv ~/Downloads/kaggle.json ~/.kaggle/"
    echo "   chmod 600 ~/.kaggle/kaggle.json"
    echo "4. Запусти этот скрипт снова"
    echo ""

    # Альтернатива: прямая ссылка (может не работать)
    echo "[INFO] Пробуем альтернативный способ..."
    curl -L -o imdb_top_1000.csv "https://raw.githubusercontent.com/rashida048/Datasets/master/imdb_top_1000.csv" 2>/dev/null

    if [ -f "imdb_top_1000.csv" ]; then
        echo "[OK] Датасет скачан альтернативным способом!"
    else
        echo "[ERROR] Не удалось скачать. Настрой Kaggle CLI."
    fi
fi

echo ""
echo "=== Проверка файлов ==="
ls -la *.csv 2>/dev/null || echo "CSV файлы не найдены"

echo ""
echo "=== Следующий шаг ==="
echo "python import_kaggle_imdb.py --file data/imdb_top_1000.csv"
