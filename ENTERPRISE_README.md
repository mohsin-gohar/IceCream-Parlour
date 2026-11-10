# Ice Cream Parlour - Enterprise Edition

Enterprise-ready ASP.NET Core 8 Web Application for an Ice Cream Business.

## Features

### Core Features
- 🍦 Home page with hero carousel
- 📖 Product/Recipe catalog with category filtering
- 📚 Recipe books
- 🛒 Shopping cart with session management
- 💳 Checkout flow with order placement
- 👤 User authentication (Login, Register, Profile)
- 🏆 VIP Membership system (Monthly/Annual)
- 👑 Admin dashboard
- ❤️ Health check endpoint (`/health`)
- 📧 Email service integration

### Technical Features
- 🔒 ASP.NET Core Identity with role-based access
- 🗄️ SQLite (development) / SQL Server (production) database
- ⚡ Session management
- 🕒 Security headers
- 🔐 Anti-forgery token validation
- 📧 Custom password validator
- 🎨 Responsive premium UI
- 📱 Mobile-friendly design

## Quick Start

### Prerequisites
- .NET 8.0 SDK
- Git

### Running Locally

```bash
# Clone the repository
git clone <repository-url>
cd Ice_Cream_Parlour_Eproject

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run --urls "http://localhost:5000"
```

The application will be available at: `http://localhost:5000`

### Default Credentials
- **Admin:** `admin@icream.com` / `Admin@123`
- **User:** `user@icream.com` / `User@123`

## Configuration

### appsettings.json
- **Database.Provider:** `Sqlite` (dev) or `SqlServer` (production)
- **ConnectionStrings:** Configure your database connection
- **Redis.ConnectionString:** (Optional) Redis for distributed caching
- **Email.Enabled:** Enable SMTP or null email sender
- **Seed:** Demo data and credentials

### Environment Variables
```bash
# Production settings
ConnectionStrings__icecreamcs="Server=localhost;Database=icecream;User Id=sa;Password=YourPassword;"
Redis__ConnectionString="localhost:6379"
Email__Enabled=true
SMTP:Host="smtp.gmail.com"
SMTP:Port="587"
SMTP:UserName="admin@icream.com"
SMTP:Password="app-password"
SMTP:FromAddress="admin@icream.com"
```

## Production Deployment

### Using IIS
1. Publish the application: `dotnet publish -c Release -o ./publish`
2. Configure IIS as a reverse proxy
3. Set up health checks endpoint: `/health`

### Using Kubernetes
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: icream-parlour
spec:
  replicas: 3
  selector:
    matchLabels:
      app: icream-parlour
  template:
    metadata:
      labels:
        app: icream-parlour
    spec:
      containers:
      - name: icream
        image: icream-parlour:latest
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__icecreamcs
          valueFrom:
            secretKeyRef:
              name: db-credentials
              key: connectionString
```

## Health Checks

| Endpoint | Description |
|----------|-------------|
| `/health` | Application health status |
| `/health/database` | Database connectivity |
| `/health/cache` | Cache status (Redis or in-memory) |

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/home` | Home page data |
| GET | `/api/products` | Product catalog |
| GET | `/api/recipes` | Recipe list |
| GET | `/api/books` | Recipe books |
| POST | `/api/cart` | Add to cart |
| GET | `/api/cart` | Get cart |
| DELETE | `/api/cart/{id}` | Remove item |
| POST | `/api/checkout` | Process checkout |
| GET | `/api/orders` | Get user orders |
| POST | `/api/orders` | Create order |

## Security

### Security Headers
- X-Content-Type-Options: nosniff
- X-Frame-Options: SAMEORIGIN
- Referrer-Policy: strict-origin-when-cross-origin
- Permissions-Policy: restricted
- Strict-Transport-Security: max-age=31536000

### Authentication
- ASP.NET Core Identity
- Role-based authorization
- Custom password validation
- Cookie-based authentication
- CSRF protection via anti-forgery tokens

## Monitoring

### Logging
- Structured logging with Microsoft.Extensions.Logging
- Log rotation configured
- Debug logging in development
- Information logging in production

### Health Monitoring
- Application state on `/health`
- Database check included
- Cache health monitoring
- Custom health checks

## License
Proprietary - All rights reserved.

## Support
For support, contact: `support@icream.com`
