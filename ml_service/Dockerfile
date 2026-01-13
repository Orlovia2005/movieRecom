FROM python:3.11-slim

WORKDIR /app

# Install system dependencies for psycopg2 and scikit-surprise
RUN apt-get update && apt-get install -y \
    gcc \
    libpq-dev \
    && rm -rf /var/lib/apt/lists/*

# Copy requirements first for better caching
COPY requirements.txt .
RUN pip install --no-cache-dir --timeout=300 --retries=5 -r requirements.txt

# Copy application code
COPY . .

# Create models directory
RUN mkdir -p /app/models

# Environment variables
ENV FLASK_APP=app.py
ENV FLASK_ENV=production
ENV PORT=5001

# Expose port
EXPOSE 5001

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5001/health || exit 1

# Run with gunicorn
CMD ["gunicorn", "-w", "2", "-b", "0.0.0.0:5001", "app:app"]
