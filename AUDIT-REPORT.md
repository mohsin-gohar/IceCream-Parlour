# iCREAM - Code Audit & Remediation Report

Scope: full ASP.NET Core 8 MVC solution (controllers, services, EF Core model,
Razor views, client-side JavaScript, configuration and build output).
Goal: remove runtime errors, fix functional bugs, close security gaps and prepare the
project for a production deployment.

Build result after remediation: **0 errors, 0 warnings**.
All findings below were reproduced and re-tested against a live instance.

---

## 1. Critical issues (crashes, data loss, security)

| # | Issue | Impact | Fix |
|---|---|---|---|
| 1 | `wwwroot/js/main.js` contained pasted HTML and an unterminated script after line 617 | JavaScript syntax error - **no script on the home page ran** | Rewrote the file with clean, guarded code |
| 2 | Public pages referenced 5 libraries that do not exist (`easing`, `waypoints`, `owlcarousel`, `isotope`, `lightbox`) | 404s and `owlCarousel is not a function` errors killed the page scripts | Removed dead script tags; plugin calls now check `$.fn.*` first |
| 3 | Book order accepted the price from a hidden client field | Anyone could order a book for $0.01 | Total is calculated from the database price; client value is ignored |
| 4 | No stock validation or decrement for book orders / admin orders | Overselling, stock never changed | Stock checked, decremented in a transaction |
| 5 | Order tracking exposed customer name and delivery address to anyone who guessed an order number (~9,000 combos/day) | Privacy leak | Tracking now requires the order number **and** the e-mail used to order (or the owner's signed-in account) |
| 6 | Admin order creation never stored line items or a total (service ignored `Lines`, linked recipe ids into a `Products` foreign key) | Order creation produced empty $0 orders or a FK error | Line items, server-side prices, stock updates, totals and transactions implemented |
| 7 | `GetCreateModelAsync` listed Recipes while `OrderItem` referenced Products | FK violation on submit | Uses the Product catalogue; products are bootstrapped from recipes for existing data |
| 8 | Java-Script-free password login had lockout disabled | Unlimited brute-force attempts | Lockout after 5 failures / 15 min, generic errors, e-mail lookup |
| 9 | Logout was a GET action | CSRF logout / link prefetch side effects | POST-only + anti-forgery token |
| 10 | `HomeController.Feedback` POST had no anti-forgery and did not persist anything | Fake feature, CSRF risk | Removed; the real endpoint validates, persists and is protected by the global anti-forgery filter |

## 2. High severity (500 errors, broken pages/flows)

| # | Issue | Fix |
|---|---|---|
| 11 | `Recipe/FreeRecipes` returned a view that does not exist | Reuses the recipe list view filtered to free recipes |
| 12 | `Feedback/Create` GET and validation failures returned a missing `Views/Feedback/Create` view | Redirects to `Home/Feedback` with a flash message |
| 13 | `Home/Gallery` passed no model to a strongly-typed view | Redirects to the real `GalleryController` |
| 14 | Status-code middleware re-executed a missing `StatusCodePage` view (every 404 became a 500) | Added `Views/Home/StatusCodePage.cshtml` with friendly 400/401/403/404/500 pages |
| 15 | `DashboardController.Products`, `CreateProduct`, `EditProduct`, `DeleteProduct` and admin `Books/Order` had no views | Removed dead actions (orders are handled by the public/book flow) |
| 16 | `Home/Bookorder` and `Book/Bookorder` had no views | Removed dead actions |
| 17 | Order numbers used `new Random()` per request and only 4 digits | Collision-prone; uniqueness now uses a cryptographic RNG + database existence check with retry |
| 18 | Seeding ran unguarded at startup and crashed the app when the database was unavailable | Seeding is wrapped, logged, and reports through `/health`; the app keeps running |
| 19 | Only `Admin`/`User` roles were seeded while `[Authorize(Roles = "Admin,Manager,Staff")]` was used | Manager/Staff accounts could never be authorised | All four roles are seeded |
| 20 | Admin order line template reused `Lines[0]` for every added row | Only the last row was bound | Template uses `__index__`, JS substitutes the next index |
| 21 | Dashboard monthly sales ignored the year; total customers counted Identity users; low-stock list was fabricated; chart used 7 sequential queries | Correct period filter, real customer count, real product stock, single grouped query with zero-filled days |
| 22 | Deleting a category used by products caused a foreign-key error | Guarded with a clear message |
| 23 | Admin forms did not render validation/upload errors | Added `asp-validation-summary` to all admin/public forms |

## 3. Medium severity (security hardening & data quality)

| # | Issue | Fix |
|---|---|---|
| 24 | File uploads validated only the extension; no size/content checks; delete used raw paths | Shared `FileUploadHelper`: whitelist, MIME + magic bytes, size limit, random names, traversal-safe delete |
| 25 | Customer create never checked duplicate e-mails; updates were case-sensitive | Case-insensitive validation, friendly errors |
| 26 | `Category`/`Product`/`Book`/`Recipe` images were not deleted on every update path | Old files removed safely |
| 27 | Layout executed two database queries per request (`@inject ApplicationDbContext`) | Profile/avatar/membership values are now claims issued at sign-in and refreshed on profile changes |
| 28 | Bootstrap 4 carousel/modal attributes (`data-ride`, `data-slide`, `data-dismiss`) on Bootstrap 5 | Converted to `data-bs-*` / ratio utilities |
| 29 | Home page "Order Now" sent a Recipe id to `Book/Order` | Links to the recipe detail page; book ordering is only offered for books |
| 30 | Missing `product-placeholder.jpg` and a fallback `product-7.jpg` (the file is `.webp`) | Uses existing images only |
| 31 | Reports "Export PDF/Excel" returned an empty byte array (or 404) | Real CSV export with BOM for Excel; removed fake PDF endpoints and dead links |
| 32 | Report totals ignored the selected period end day, included cancelled/refunded orders and never set items sold | Correct exclusive range, status filtering, items-sold aggregation |
| 33 | Customer spend included cancelled/refunded orders | Excluded |
| 34 | No pagination clamps (`page=0`, `pageSize=100000`) | Clamped in services |
| 35 | Password policy allowed weak 6-character passwords | 8+ characters, digit/upper/lower required, 4 unique characters |
| 36 | No security headers, HSTS, forwarded-header support, response compression or health endpoint | Added (see `Program.cs`), plus `/health` JSON check |
| 37 | `ForgotPassword` generated a token but never sent it, yet claimed an e-mail was sent | SMTP sender with safe logging fallback; neutral response prevents account enumeration |
| 38 | Login page advertised the seeded admin e-mail as a placeholder | Generic placeholder |

## 4. Low severity / cleanup

- `async` methods without `await` in `ReportService`/`ProductService` (warnings) removed by rewriting the services.
- `AsNoTracking()` applied to all read-only queries.
- Consistent UTC timestamps for new records.
- Removed duplicate `dashboard.css` reference and fixed the missing space between attributes in the admin sidebar.
- Added "User Recipes" to the admin navigation and `noindex` for the admin area.
- Track-order timeline now recognises the `Shipped` status; payment badge is colour-coded.
- Profile page now allows editing name, phone and address.
- Added `.dotnet/`, `*.db` and log folders to `.gitignore`.

## 4a. UI / UX overhaul (premium rebuild)

| # | Issue found | Fix |
|---|---|---|
| 39 | `AOS.init()` was called but the AOS library was never loaded - a JavaScript error on **every** page | AOS CSS + JS added and guarded with `if (window.AOS)` |
| 40 | The home page contained lorem-ipsum copy and a Bootstrap-4 carousel | Rebuilt: premium hero, trust strip, story section, dynamic flavours, membership band, services, gallery, testimonials, final CTA, video modal |
| 41 | About page contained lorem ipsum and a `margin-left:100px` layout hack | Rebuilt with real story copy, image collage, stats, values and team |
| 42 | Contact page had Urdu placeholder copy and duplicated alerts | Rebuilt with a real form, contact cards, opening hours and map block |
| 43 | Service page had malformed HTML `<istyle ="color:black;">` inside a dark header | Header replaced with the shared premium page header |
| 44 | Feedback page header used black text on a dark image (invisible breadcrumb) | Replaced with the shared premium page header |
| 45 | Products page used Bootstrap-negative-margin tricks and Order links that pointed at books | Rebuilt as a filterable menu with correct recipe links |
| 46 | FAQ page had no page header and misaligned stat cards | Premium header added; stat layout fixed in CSS |
| 47 | Gallery, recipes and books had no filtering and inconsistent cards | Client-side category filters and one consistent card component |
| 48 | Admin layout did not load the design system and never initialised AOS | Added `premium.css`, AOS init, grouped sidebar, sticky top bar, page filter, footer |
| 49 | Dashboard was a static grid with a hard-coded 7-day query loop | Rebuilt with KPI cards, Chart.js revenue + pipeline charts, low-stock and top-seller panels, recent orders |
| 50 | Admin tables had no empty states; three admin views contained large commented-out duplicate markup | Empty states added, dead blocks removed |
| 51 | Checkout displayed a shipping fee that the server never charged | Shipping (free over $100, otherwise $4.99) now calculated server-side and recorded on the order |
| 52 | Profile page had no way to edit details and inline styles only | Rebuilt with identity card, editable details form, quick actions |
| 53 | No favicon/theme-colour, no client-side validation in admin | Both added |

## 5. New migration

`20261022181717_AddIndexesAndConstraints` adds indexes for the columns used by search,
reports and order lookups, aligns FK delete behaviour (`SetNull` for books, `Restrict` for
customers) and constrains `UserRecipes.Status`. It deliberately does **not** create a
unique index on `OrderNumber`, so it can be applied safely to databases that may contain
historic duplicates; uniqueness is enforced by the application before insert.

## 6. Verification checklist (executed)

- `dotnet build` - 0 errors / 0 warnings
- `/health` returns `Healthy` with database check
- Public routes: `/`, `/Home/About`, `/Home/Product`, `/Recipe`, `/Recipe/FreeRecipes`,
  `/Gallery`, `/Book`, `/Book/Order/1`, `/Home/Contact`, `/Home/Faq`, `/Home/Feedback`,
  `/Order/Track` - all HTTP 200
- 404 route returns the custom error page with HTTP 404 (not 500)
- Admin login (role claims) and all admin screens return 200
- Book order: total recomputed as $24.99 despite a tampered $0.01 hidden field, stock 25 -> 24,
  order number generated, order visible on the tracking page
- Tracking: correct e-mail shows the order; wrong e-mail and missing e-mail show guidance instead
- Admin order creation: 2 x $3.99 = $7.98, line item stored, stock 100 -> 98, invoice correct
- Category delete protection, category/book creation, reports CSV export (valid CSV)
- Registration, profile update, profile picture upload, malicious upload rejection
- Recipe submission -> admin approval -> appears in the gallery
- Contact and feedback submissions persisted
- POST without anti-forgery token -> HTTP 400; GET logout -> HTTP 405

## 7. Recommended next steps (out of current scope)

1. Add automated tests (xUnit + `WebApplicationFactory` + SQLite in-memory) for the order and auth flows.
2. Add rate limiting for login/contact/feedback endpoints (ASP.NET Core rate limiter).
3. Replace e-mail-only password reset with confirmed e-mail + 2FA for administrators.
4. Move uploaded media to object storage/CDN and add virus scanning for user uploads.
5. Add audit-trail tables for order status changes instead of free-text notes.
6. Add CI (build + test) and container/deployment manifests.
