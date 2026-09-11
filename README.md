# Ice Cream Parlour E-Project

A professional ASP.NET Core MVC e-commerce platform for managing ice cream parlour operations including customer management, product catalog, order processing, and admin dashboard.

## Features

### Public Features
- **Product Browsing**: Browse ice cream products and recipes
- **Book Management**: Order books related to ice cream making
- **Recipe Sharing**: View and share ice cream recipes
- **Customer Feedback**: Submit feedback and testimonials
- **Gallery View**: Browse gallery of ice cream products
- **User Account**: Register, login, and manage profile

### Admin Features
- **Dashboard**: View key metrics and statistics
- **Customer Management**: Create, read, update, and delete customers
- **Product Management**: Manage ice cream products and categories
- **Order Management**: Process and track customer orders
- **Reports**: Generate sales and customer reports
- **Role-Based Access**: Admin and User roles with permission control

## Technology Stack

- **Framework**: ASP.NET Core 8.0
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: ASP.NET Core Identity
- **Logging**: Serilog
- **UI Framework**: Bootstrap 5
- **Frontend**: jQuery, jQuery Validation

## Project Structure

```
Ice_Cream_Parlour_Eproject/
├── Areas/                      # Admin area with role-based access
│   ├── Admin/
│   │   ├── Controllers/       # Admin controllers
│   │   └── Views/            # Admin views
│   └── Models/               # Admin-specific models and ViewModels
├── Controllers/              # Public controllers
├── Views/                    # Public views
├── Models/                   # Data models and ViewModels
├── Services/                 # Business logic layer
│   └── Interfaces/          # Service interfaces
├── Data/                     # Entity Framework DbContext
├── Helpers/                  # Extension methods and utilities
├── Middleware/               # Custom middleware (exception handling)
├── wwwroot/                  # Static files (CSS, JS, images)
└── Properties/               # Application settings
```

## Setup Instructions

### Prerequisites
- .NET 8 SDK or later
- SQL Server 2019 or later (or SQL Server Express)
- Visual Studio 2022 or Visual Studio Code

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/mohsin-gohar/IceCream-Parlour.git
   cd Ice_Cream_Parlour_Eproject
   ```

2. **Set up the database connection**
   - Update `appsettings.json` with your SQL Server connection string:
   ```json
   {
     "ConnectionStrings": {
       "icecreamcs": "Server=YOUR_SERVER;Database=IceCreamParlour;Trusted_Connection=true;Encrypt=false;"
     }
   }
   ```

3. **Apply database migrations**
   ```bash
   dotnet ef database update
   ```

4. **Restore NuGet packages**
   ```bash
   dotnet restore
   ```

5. **Build the project**
   ```bash
   dotnet build
   ```

6. **Run the application**
   ```bash
   dotnet run
   ```
   The application will be available at `https://localhost:7000` or `http://localhost:5000`

## Default Credentials

The application seeds two default users on first run:

- **Admin User**
  - Email: `admin@icream.com`
  - Password: `Admin@123`

- **Regular User**
  - Email: `user@icream.com`
  - Password: `User@123`

⚠️ **IMPORTANT**: Change these credentials in production!

## Configuration

### appsettings.json
Configure application-specific settings:

```json
{
  "AppSettings": {
    "MaxFileUploadSize": 5242880,
    "AllowedFileExtensions": [".jpg", ".jpeg", ".png", ".gif"],
    "PageSize": 10,
    "SessionTimeoutMinutes": 30,
    "Smtp": {
      "Host": "smtp.gmail.com",
      "Port": 587,
      "SenderEmail": "your-email@gmail.com",
      "SenderName": "Ice Cream Parlour",
      "Username": "your-email@gmail.com",
      "Password": "your-password",
      "UseSsl": true
    }
  }
}
```

### Logging
Logs are stored in the `logs/` directory with daily rolling files. Serilog is configured to:
- Output to console
- Write to daily rolling files
- Retain 30 days of logs

## API Endpoints

### Authentication
- `POST /Account/Register` - Register new user
- `POST /Account/Login` - Login user
- `POST /Account/Logout` - Logout user
- `GET /Account/Profile` - View user profile

### Public
- `GET /Home/Index` - Home page
- `GET /Home/Product` - Product list
- `GET /Home/Recipe` - Recipe list
- `GET /Book/Index` - Book list
- `POST /Book/Order` - Place book order

### Admin (Requires Admin Role)
- `GET /Admin/Dashboard` - Admin dashboard
- `GET /Admin/Customers` - Customer list
- `GET /Admin/Products` - Product management
- `GET /Admin/Orders` - Order management
- `GET /Admin/Reports` - Reports

## Error Handling

The application implements comprehensive error handling:

- **Global Exception Middleware**: Catches unhandled exceptions and returns appropriate responses
- **Model Validation**: Validates user input with detailed error messages
- **Null Safety**: Extension methods for safe null checks
- **Logging**: All errors are logged to Serilog

Error responses include:
- Status codes (400, 404, 500, etc.)
- User-friendly error messages
- Technical error details in logs

## Performance Considerations

- **Async/Await**: All database operations use async methods
- **Pagination**: Large result sets are paginated
- **Caching**: Database queries use `.AsNoTracking()` where appropriate
- **File Uploads**: Large file uploads are handled safely

## Security Features

- **Authentication**: ASP.NET Core Identity with role-based authorization
- **CSRF Protection**: Anti-forgery tokens on all forms
- **File Upload Validation**: Validates file types and sizes
- **SQL Injection Prevention**: Uses Entity Framework with parameterized queries
- **Session Security**: Secure session configuration with HTTPOnly cookies
- **Password Requirements**: Configurable password policies (default: 6 characters minimum)

## Development Guidelines

### Code Style
- Use PascalCase for class names and method names
- Use camelCase for private variables and parameters
- Add XML documentation comments to all public methods
- Use meaningful variable names

### Adding New Features
1. Create models in the `Models/` directory
2. Implement service logic in `Services/`
3. Create controllers in `Controllers/`
4. Add views in `Views/`
5. Add migrations if database changes are needed

### Testing
```bash
# Run tests (when available)
dotnet test
```

## Maintenance

### Database Maintenance
- **Backup**: Regular backups of the SQL Server database
- **Migrations**: Use `dotnet ef migrations add <Name>` for schema changes
- **Cleanup**: Archive old logs monthly

### Logs
- Located in `logs/` directory
- Retained for 30 days
- Review logs regularly for errors and warnings

## Troubleshooting

### Common Issues

**1. Database Connection Error**
   - Verify SQL Server is running
   - Check connection string in `appsettings.json`
   - Ensure database exists

**2. Migration Errors**
   ```bash
   dotnet ef database update
   ```

**3. File Upload Issues**
   - Check `wwwroot/images/` directory permissions
   - Verify file size is within limits
   - Check file extension is allowed

**4. Session Timeout**
   - Increase `SessionTimeoutMinutes` in `appsettings.json`
   - Default is 30 minutes

## Contributing

1. Create a feature branch: `git checkout -b feature/new-feature`
2. Commit changes: `git commit -am 'Add new feature'`
3. Push to branch: `git push origin feature/new-feature`
4. Submit a pull request

## License

This project is provided as-is for educational and commercial purposes.

## Support

For issues and questions:
- Create an issue on GitHub
- Review the logs for error details
- Check application documentation

## Version History

- **v2.0** (Current) - Professional code standards, error handling, logging
- **v1.0** - Initial release with basic functionality

---

**Last Updated**: January 2025

**Maintained By**: Ice Cream Parlour Development Team
