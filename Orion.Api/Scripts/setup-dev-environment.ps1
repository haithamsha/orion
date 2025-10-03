Write-Host "🔧 Setting up Orion Development Environment Variables..." -ForegroundColor Cyan

# Database
$env:DATABASE_CONNECTION_STRING = "Host=localhost;Database=orion_dev_db;Username=postgres;Password=postgres;Pooling=true"

# RabbitMQ
$env:RABBITMQ_HOSTNAME = "localhost"
$env:RABBITMQ_USERNAME = "guest"
$env:RABBITMQ_PASSWORD = "guest"

# Email (SendGrid)
$env:SENDGRID_API_KEY = "YOUR_SENDGRID_API_KEY_HERE"

# SMTP Fallback
$env:SMTP_HOST = "smtp.gmail.com"
$env:SMTP_USERNAME = "your-email@gmail.com"
$env:SMTP_PASSWORD = "your-app-password"

# JWT
$env:JWT_SECRET_KEY = "ThisIsMySuperSecretKeyForOrionApi12345!"

# API
$env:ORION_API_KEY = "MySuperSecretWorkerApiKey123!@#"

# Monitoring (Optional)
$env:APPLICATION_INSIGHTS_KEY = ""
$env:ELASTICSEARCH_URL = ""
$env:PROMETHEUS_URL = ""

Write-Host "✅ Development environment variables set!" -ForegroundColor Green
Write-Host "💡 Run: .\Scripts\setup-dev-environment.ps1" -ForegroundColor Yellow
Write-Host "💡 Then: dotnet run" -ForegroundColor Yellow