# Deploy Guide

## Prerequisites

- VPS/Server với Ubuntu 22.04+
- Docker & Docker Compose
- Domain (optional)
- SSL certificate (Let's Encrypt)

## Option 1: Docker Compose (Recommended)

### 1. Clone repository

```bash
git clone <your-repo>
cd you_source
```

### 2. Configure environment

```bash
# Backend
cp backend/StarterAPI/appsettings.json backend/StarterAPI/appsettings.Production.json
nano backend/StarterAPI/appsettings.Production.json
```

Update:
- `ConnectionStrings:DefaultConnection` - production database
- `JwtSettings:Secret` - strong random key (min 32 chars)

### 3. Build and run

```bash
docker-compose up -d --build
```

### 4. Check status

```bash
docker-compose ps
docker-compose logs -f
```

## Option 2: Manual Deploy

### Backend

```bash
cd backend/StarterAPI
dotnet publish -c Release -o ../../publish/backend

# Run with systemd
sudo nano /etc/systemd/system/starter-api.service
```

```ini
[Unit]
Description=Starter API
After=network.target

[Service]
Type=notify
User=www-data
WorkingDirectory=/var/www/starter/backend
ExecStart=/usr/bin/dotnet /var/www/starter/backend/StarterAPI.dll
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl enable starter-api
sudo systemctl start starter-api
sudo systemctl status starter-api
```

### Frontend

```bash
cd frontend/starter-ui
npm run build

# Copy to web server
sudo cp -r dist/* /var/www/starter/frontend/
```

### Nginx Config

```nginx
server {
    listen 80;
    server_name your-domain.com;

    # Frontend
    location / {
        root /var/www/starter/frontend;
        try_files $uri $uri/ /index.html;
    }

    # Backend API
    location /api/ {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Health check
    location /health {
        proxy_pass http://localhost:5000;
    }
}
```

### SSL with Let's Encrypt

```bash
sudo apt install certbot python3-certbot-nginx
sudo certbot --nginx -d your-domain.com
```

## Database Migration

```bash
# On server
cd backend/StarterAPI
dotnet ef database update

# Or auto-migrate on startup (already configured in Program.cs)
```

## Monitoring

### Health Check

```bash
curl http://localhost:5000/health
```

### Logs

```bash
# Backend logs
tail -f logs/log-*.txt

# Docker logs
docker-compose logs -f backend
```

## Backup

### Database

```bash
# SQL Server backup
docker exec -it <container_name> /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P <password> \
  -Q "BACKUP DATABASE [StarterDB] TO DISK = N'/var/opt/mssql/backup/StarterDB.bak'"

# Copy backup file
docker cp <container_name>:/var/opt/mssql/backup/StarterDB.bak ./backup/
```

### Uploads

```bash
tar -czf uploads-backup-$(date +%Y%m%d).tar.gz wwwroot/uploads/
```

## Security Checklist

- [ ] Change JWT Secret (min 32 chars, random)
- [ ] Change default admin password
- [ ] Configure CORS for production domain
- [ ] Enable HTTPS redirect
- [ ] Set up firewall (UFW)
- [ ] Configure rate limiting (already enabled)
- [ ] Enable SSL certificate
- [ ] Remove Swagger in production
- [ ] Set strong database password
- [ ] Regular backups

## Troubleshooting

### Port already in use

```bash
sudo lsof -i :5000
sudo kill -9 <PID>
```

### Database connection failed

```bash
# Check SQL Server is running
docker-compose ps sqlserver

# Check connection string
cat backend/StarterAPI/appsettings.Production.json
```

### Permission denied

```bash
sudo chown -R www-data:www-data /var/www/starter
sudo chmod -R 755 /var/www/starter
```
