#!/bin/bash

echo "🔧 Setting up Orion Development Environment Variables..."

# Database
export DATABASE_CONNECTION_STRING="Host=localhost;Database=orion_db;Username=postgres;Password=postgres;Pooling=true"

# RabbitMQ
export RABBITMQ_HOSTNAME="localhost"
export RABBITMQ_USERNAME="guest"
export RABBITMQ_PASSWORD="guest"

# Email (SendGrid)
export SENDGRID_API_KEY="YOUR_SENDGRID_API_KEY_HERE"

# SMTP Fallback
export SMTP_HOST="smtp.gmail.com"
export SMTP_USERNAME="your-email@gmail.com"
export SMTP_PASSWORD="your-app-password"

# JWT
export JWT_SECRET_KEY="ThisIsMySuperSecretKeyForOrionApi12345!"

# API
export ORION_API_KEY="MySuperSecretWorkerApiKey123!@#"

# Monitoring (Optional)
export APPLICATION_INSIGHTS_KEY=""
export ELASTICSEARCH_URL=""
export PROMETHEUS_URL=""

echo "✅ Development environment variables set!"
echo "💡 Run: source ./Scripts/setup-dev-environment.sh"
echo "💡 Then: dotnet run"