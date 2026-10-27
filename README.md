# IceCream-Parlour

A comprehensive ASP.NET Core MVC web application for an Ice Cream Parlour business. This is a full-featured e-commerce and content management system with user authentication, order management, recipe management, and an admin dashboard.

## Project Overview

### Tech Stack
- **Framework:** ASP.NET Core 8.0 MVC
- **Database:** Microsoft SQL Server with Entity Framework Core
- **Authentication:** ASP.NET Core Identity
- **Frontend:** Bootstrap 5, Font Awesome 6, Custom CSS
- **JavaScript:** jQuery, Bootstrap Bundle

### Features

#### Public Frontend
1. **Home Page** - Hero carousel, featured products, services, testimonials, portfolio gallery
2. **About Us** - Company history with team member profiles
3. **Products** - Ice cream flavor catalog with pricing
4. **Services** - Service offerings with animated counters
5. **Gallery** - Filterable image gallery (recipes, books, user submissions)
6. **Blog** - Blog post management with CRUD operations (localStorage)
7. **Recipe Management** - User recipe submission with localStorage
8. **FAQ** - Searchable accordion FAQ with category filters
9. **Contact** - Contact form with Google Maps integration
10. **Feedback** - Customer feedback form with star rating
11. **Privacy Policy** - Complete privacy policy page

#### User Features
- User registration and login
- Profile management with avatar upload
- Order tracking
- My orders history
- Recipe submission with admin review
- Password reset functionality

#### Admin Dashboard (Area: Admin)
- Dashboard with analytics
- Product/Recipe management
- Book management
- Order management
- Customer management
- Category management
- Reports

### Pages & Sections

| Page | URL | Description |
|------|-----|-------------|
| Home | `/` | Main landing page with carousel, products, testimonials |
| About | `/Home/About` | Company information and team |
| Products | `/Home/Product` | Ice cream flavor catalog |
| Services | `/Home/Service` | Service offerings with counters |
| Gallery | `/Gallery` | Filterable image gallery |
| Blog | `/Home/Blog` | Blog management (localStorage) |
| Recipes | `/Recipe` | Recipe management (localStorage) |
| FAQ | `/Home/Faq` | Searchable FAQ |
| Contact | `/Home/Contact` | Contact form + Google Map |
| Feedback | `/Home/Feedback` | Feedback form |
| Book Order | `/Home/Bookorder` | Special order request form |
| Privacy | `/Home/Privacy` | Privacy policy |
| Login | `/Account/Login` | User login |
| Register | `/Account/Register` | User registration |
| Profile | `/Account/Profile` | User profile |
| Forgot Password | `/Account/ForgotPassword` | Password reset request |
| Reset Password | `/Account/ResetPassword` | Password reset |
| Admin Dashboard | `/Admin/Dashboard` | Admin panel |

### Default Users

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@icream.com | Admin@123 |
| User | user@icream.com | User@123 |

### Project Structure

```
IceCream-Parlour/
├── Areas/
│   └── Admin/
│       ├── Controllers/
│       └── Views/
├── Controllers/
│   ├── AccountController.cs
│   ├── BookController.cs
│   ├── FeedbackController.cs
│   ├── GalleryController.cs
│   ├── HomeController.cs
│   ├── OrderController.cs
│   └── RecipeController.cs
├── Data/
│   └── ApplicationDbContext.cs
├── Models/
│   ├── Book.cs
│   ├── Customer.cs
│   ├── Feedback.cs
│   ├── GalleryItemViewModel.cs
│   ├── Order.cs
│   ├── Recipe.cs
│   ├── User.cs
│   └── UserRecipe.cs
├── Services/
│   ├── Interfaces/
│   ├── CustomerService.cs
│   ├── DashboardService.cs
│   ├── OrderService.cs
│   ├── ProductService.cs
│   └── ReportService.cs
├── Views/
│   ├── Account/
│   ├── Book/
│   ├── Home/
│   └── Shared/
├── wwwroot/
│   ├── css/
│   ├── img/
│   ├── js/
│   └── lib/
├── Program.cs
├── appsettings.json
└── Ice_Cream_Parlour_Eproject.csproj
```

### Configuration

Edit `appsettings.json` to configure your SQL Server connection:

```json
{
  "ConnectionStrings": {
    "icecreamcs": "Server=.\\SQLEXPRESS;Database=Ice_Cream_Parlourdb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=true"
  }
}
```

### Getting Started

1. Clone the repository
2. Update the connection string in `appsettings.json`
3. Run database migrations:
   ```bash
   dotnet ef database update
   ```
4. Run the application:
   ```bash
   dotnet run
   ```
5. Access the application at `https://localhost:5001` or `http://localhost:5000`

### Required Image Assets

The application expects images in `wwwroot/img/`. Key images include:
- `carousel-1.jpg`, `carousel-2.jpg` - Hero carousel
- `header.jpg` - Page headers
- `about.jpg` - About section
- `promotion.jpg` - Promotion banner
- `portfolio-1.jpg` through `portfolio-6.jpg` - Portfolio gallery
- `product-1.jpg` through `product-8.jpg` - Product images
- `team-1.jpg` through `team-4.jpg` - Team photos
- `gallery*.jpg` - Gallery images
- `service-*.jpg` - Service images
- `testimonial-*.jpg` - Testimonial photos

### License

This project is part of an academic e-project submission.

### Author

IceCream Parlor E-Project Team