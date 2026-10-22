# iCREAM - Ice Cream Parlour (ASP.NET Core 8 MVC)

A complete ice-cream parlour web application: public storefront, recipes, recipe books,
membership content, customer feedback, and a role-based administration area with
dashboard, orders, reports, inventory and user-recipe moderation.

---

## 1. Tech stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8 MVC (areas, Razor views) |
| Auth | ASP.NET Core Identity (roles: Admin, Manager, Staff, User) |
| Data | EF Core 8 - SQL Server (production) / SQLite (local development, CI) |
| UI | Bootstrap 5.3, Font Awesome 6, jQuery, Chart.js |
| E-mail | SMTP (`System.Net.Mail`), safe no-op fallback when not configured |

---

## 2. Features

**Public site**
- Home, About, Products (recipes), Services, Gallery, Blog, FAQ, Contact, Privacy
- Recipe catalogue, free recipes, recipe details (members only)
- Customer recipe submission (with image upload and admin moderation)
- Recipe-book store with stock-aware ordering and order tracking
- Feedback and contact forms (stored in the database)
- Registration, login, password reset (e-mail token), profile + profile picture

**Admin area (`/Admin`)**
- Dashboard: revenue, orders, customers, low stock, top sellers, 7-day chart
- Customers CRUD with order history and spend totals
- Categories CRUD (delete protected when products use the category)
- Products (recipe catalogue) CRUD with validated image upload
- Books CRUD
- Orders: list/search/filter, create with line items, invoice, status updates
- Reports: daily/weekly/monthly/yearly + CSV export (opens in Excel)
- User recipe review workflow: approve/reject/select, prize money, certificate

---

## 3. Getting started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Option A - SQLite (fastest, no database server required)

```bash
Database__Provider=Sqlite \
ConnectionStrings__icecreamcs="Data Source=icecream.db" \
ASPNETCORE_ENVIRONMENT=Development \
dotnet run
```

The schema is created automatically (`EnsureCreated`) and demo data is seeded when
`Seed:DemoData` is `true` (default in Development). Delete `icecream.db` after model
changes if you use SQLite.

### Option B - SQL Server

1. Update `ConnectionStrings:icecreamcs` in `appsettings.json`, user secrets or the
   `ConnectionStrings__icecreamcs` environment variable, for example:
   ```
   Server=.\\SQLEXPRESS;Database=Ice_Cream_Parlourdb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=true
   ```
2. Apply migrations:
   ```bash
   dotnet tool install --global dotnet-ef --version 8.0.28
   dotnet ef database update
   ```
3. Run:
   ```bash
   dotnet run
   ```

Default URLs: `http://localhost:5274` / `https://localhost:5275` (see `Properties/launchSettings.json`).
Health endpoint: `GET /health` (returns JSON with the database status).

---

## 4. Configuration

| Key | Purpose | Default |
|---|---|---|
| `ConnectionStrings:icecreamcs` | Database connection | SQL Express local DB |
| `Database:Provider` | `SqlServer` or `Sqlite` | `SqlServer` |
| `Seed:Enabled` | Create roles / bootstrap accounts | `true` |
| `Seed:AdminEmail`, `Seed:AdminPassword` | Bootstrap administrator | empty in `appsettings.json`, demo values in Development |
| `Seed:UserEmail`, `Seed:UserPassword` | Demo customer account | empty in `appsettings.json` |
| `Seed:DemoData` | Seed sample categories/recipes/books | `true` in Development only |
| `Smtp:Host`, `Port`, `UseSsl`, `UserName`, `Password`, `FromAddress`, `FromName` | Password-reset e-mail | not configured |

**Development demo accounts** (from `appsettings.Development.json`):

| Role | E-mail | Password |
|---|---|---|
| Admin | `admin@icream.com` | `Admin@123` |
| Customer | `user@icream.com` | `User@123` |

> These credentials are for local development only. Never enable them in production -
> set `Seed:AdminPassword` through a secret store (user secrets, Key Vault, environment
> variables) and change the password after the first login.

### Environment variables (production example)

```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__icecreamcs=Server=...;Database=...;User Id=...;Password=...;Encrypt=True
Seed__AdminEmail=owner@example.com
Seed__AdminPassword=<strong-password>
Smtp__Host=smtp.example.com
Smtp__Port=587
Smtp__UserName=...
Smtp__Password=...
Smtp__FromAddress=no-reply@example.com
```

---

## 5. Security notes

- All unsafe requests are covered by a global anti-forgery filter; logout is POST-only.
- Identity: 8+ char passwords with digit/upper/lower, unique e-mail, 5-attempt lockout (15 min).
- Passwords are hashed by ASP.NET Core Identity; reset tokens expire and are single use.
- Account enumeration is avoided on login/forgot-password responses.
- Order numbers are generated with a cryptographic RNG and checked for uniqueness.
- Book order totals are calculated **server-side** (client-supplied prices are ignored).
- Order tracking requires the order number **and** the e-mail used to order (or the owner's session).
- Uploads: extension + MIME + magic-byte validation, size limits and random file names.
- Uploaded/old images are deleted through a path-traversal-safe helper.
- Security headers: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`,
  `Permissions-Policy`, `X-Permitted-Cross-Domain-Policies`, HSTS in production.

---

## 6. Database migrations

SQLite development databases are created with `EnsureCreated`; SQL Server uses migrations.

```bash
# after changing the EF model
dotnet ef migrations add <Name>
dotnet ef database update
```

`AddIndexesAndConstraints` adds lookup indexes, aligns foreign-key delete behaviour
(book deletion sets `BookId` to null, customers with orders cannot be deleted) and
constrains the recipe status column. Order numbers are unique by application logic
(random + existence check) without a unique index, so the migration is safe for
existing data that may already contain duplicates.

---

## 7. Verification performed

- `dotnet build` - 0 warnings, 0 errors.
- End-to-end smoke test against a real SQLite database:
  public pages, admin login, all admin screens, book ordering (server-side total,
  stock decrement, order number), order tracking with/without e-mail, profile update,
  profile picture upload (including malicious file rejection), recipe submission and
  moderation, category/book creation, delete protection, reports CSV, 404 page, health check.
- Anti-forgery: POST without a token returns HTTP 400.
- Upload security: a `.txt` disguised as `image/jpeg` is rejected by magic-byte validation.

See `AUDIT-REPORT.md` for the full list of issues found and fixed.
