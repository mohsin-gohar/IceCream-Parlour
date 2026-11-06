# Enterprise SaaS ERP Specification: Ice Cream Parlour ($100M-Grade Platform)

## Document Overview
- **Document Version:** 1.0.0-ENTERPRISE
- **System Target:** Global Enterprise Multi-Tenant SaaS ERP & Omnichannel Retail Platform
- **Foundational Codebase:** ASP.NET Core 8.0 MVC (`Ice_Cream_Parlour_Eproject`)
- **Status:** Architecture Blueprint & Phase-by-Phase Execution Roadmap

---

## 1. Executive Summary & Vision

This specification defines the transformation of the existing ASP.NET Core 8.0 MVC Ice Cream Parlour application into an enterprise-grade, multi-tenant SaaS ERP platform valued at scale ($100M+ ARR capabilities). The platform transitions single-parlour/monolithic operations into an enterprise retail, manufacturing, franchise, and supply-chain solution capable of supporting thousands of independent ice cream brands, franchise networks, and multi-unit parlours globally.

### Core Strategic Objectives
1. **Multi-Tenancy at Scale:** Complete per-parlour / per-franchise tenant isolation with dedicated subdomains, custom domains, and flexible data isolation tiers (pool, silo, and hybrid).
2. **End-to-End ERP Capabilities:** Seamless integration of product formulation, batch-recipe production, multi-warehouse cold-chain inventory, supplier procurement, modern POS terminal execution, omnichannel orders, CRM/loyalty, financial general ledger, and executive business intelligence.
3. **Modernized Experience:** Responsive, mobile-first design leveraging Tailwind CSS, dark mode theming, and full WCAG 2.1 AA accessibility compliance.
4. **Mission-Critical Security & Governance:** Zero-trust architecture, granular Role-Based / Attribute-Based Access Control (RBAC/ABAC), Multi-Factor Authentication (MFA), immutable event audit logging, field-level encryption, and GDPR/CCPA privacy compliance.
5. **Cloud-Native Tech Stack:** Evolution from SQLite/MySQL to enterprise PostgreSQL (with Citus / multi-database sharding), Redis distributed caching and message bus, Elasticsearch distributed indexing, Docker containerization, Kubernetes orchestration, and robust automated CI/CD pipelines.

---

## 2. System Architecture & Foundation Transition

### 2.1 Current Foundation vs. Target Architecture

| Component | Current State (`Ice_Cream_Parlour_Eproject`) | Target Enterprise SaaS ERP State |
|---|---|---|
| **Framework** | ASP.NET Core 8.0 MVC | ASP.NET Core 8.0 Clean Architecture (Modular Monolith / Microservices-Ready Web API + MVC/Blazor Hybrid Admin) |
| **Data Layer** | EF Core 8.0 (SQLite in Dev, MySQL in Prod), Single `ApplicationDbContext` | EF Core 8.0 + Dapper for high-throughput reads; Multi-tenant dynamic DbContext switching; Tenant Schema / Database-per-tenant support |
| **Primary Database** | SQLite (`icecream.db`) / MySQL (`icecreamcs`) | PostgreSQL 16+ with Row-Level Security (RLS) & partitioned multi-tenant tables; Read replicas |
| **Caching & Messaging** | ASP.NET In-Memory Session | Redis Cluster (distributed session, cache-aside, distributed locking with Redlock, pub/sub for real-time events) |
| **Search & Analytics** | SQL `LIKE` queries, basic relational reports | Elasticsearch 8+ / OpenSearch cluster for instant product catalog search, recipe ingredients, and audit/log analysis |
| **Authentication & AuthZ**| ASP.NET Core Identity (Admin/User cookie roles) | ASP.NET Core Identity + Duende IdentityServer / OpenIddict / JWT tokens + RBAC/ABAC claims + TOTP/WebAuthn MFA + SSO (SAML/OIDC) |
| **Frontend & UI/UX** | Bootstrap 5, jQuery, standard Razor views | Tailwind CSS + Alpine.js / Razor Components, full Dark/Light mode theme system, accessible POS keyboard-first UI |
| **File Storage** | Local disk `wwwroot/images/recipes/` | S3-compatible Object Storage (MinIO / AWS S3 / Azure Blob) with CDN edge caching and presigned URLs |
| **Infrastructure** | Standalone process (`dotnet run`) | Docker multi-stage images, Kubernetes (Helm charts), Horizontal Pod Autoscalers (HPA), Prometheus & Grafana telemetry |

### 2.2 Foundation Transition Blueprint
The existing classes and controllers serve as the direct domain seed:
- `Models/User.cs` & `Models/Signup.cs` $\rightarrow$ Extends to `TenantUser`, `TenantMembership`, `Role`, `Permission`, `UserSecurityProfile`.
- `Models/Recipe.cs` & `Models/UserRecipe.cs` $\rightarrow$ Extends to `RecipeFormula`, `RecipeBOM` (Bill of Materials), `NutritionalProfile`, `AllergenMatrix`, `CostingSheet`.
- `Models/Book.cs` & catalog products $\rightarrow$ Extends to `ProductVariant`, `InventoryItem`, `SKU`, `CategoryHierarchy`.
- `Models/Order.cs` $\rightarrow$ Extends to `SalesOrder`, `POSOrder`, `FulfillmentBatch`, `OrderSplitPayment`, `DispatchNote`.
- `Areas/Admin/` $\rightarrow$ Transforms into the Enterprise Multi-Tenant ERP Back-Office Portal.

---

## 3. Multi-Tenant SaaS Architecture

```
                    Internet Traffic (HTTPS)
                                │
                                ▼
                   Cloudflare Edge CDN / WAF
                                │
               Wildcard DNS (*.icecream-erp.com)
                                │
                                ▼
                     NGINX / Ingress Controller
                                │
                  Tenant Resolution Middleware
             (Subdomain / Header / Custom Domain)
                                │
           ┌────────────────────┴────────────────────┐
           ▼                                         ▼
   Tenant Context Pipeline                   Tenant Context Cache
(ITenantContextAccessor / AsyncLocal)          (Redis 60s TTL)
           │
           ├─────────────────────────────────────────┐
           ▼                                         ▼
 EF Core Tenant Query Filter              Tenant Shard Resolver
 (TenantId = CurrentTenant.Id)            (Dedicated DB vs Shared DB)
           │                                         │
           ▼                                         ▼
Shared DB (Tenant-Isolated Schema/RLS)     Dedicated Tenant Database
```

### 3.1 Tenant Resolution Strategy
The application dynamically resolves tenants on each HTTP request using a pipeline:
1. **Subdomain Resolution:** `https://{tenant_slug}.icecream-erp.com` (e.g., `gelato-paradise.icecream-erp.com`).
2. **Custom Domain Resolution:** `https://pos.gelatoparadise.com` via CNAME verification and SSL termination.
3. **HTTP Header (API / POS Terminals):** `X-Tenant-ID` or `X-Tenant-Key` authenticated via HMAC API keys.
4. **JWT Claim (Mobile / POS apps):** `tenant_id` embedded within authenticated user claims.

### 3.2 Tenant Isolation Models
- **Tier 1 (Starter / Standard):** Shared Database, Shared Schema with EF Core Global Query Filters (`builder.Entity<T>().HasQueryFilter(e => e.TenantId == _currentTenant.Id)`) combined with PostgreSQL Row-Level Security (RLS) as a defense-in-depth enforcement.
- **Tier 2 (Enterprise / Franchise):** Database-per-tenant or isolated schema for strict data sovereignty, compliance, and custom backup schedules.
- **Dynamic Connection Routing:** A custom `ITenantDatabaseProvider` injects connection strings dynamically based on the resolved `TenantContext`.

### 3.3 Tenant Lifecycle & Provisioning
- **Self-Service Onboarding:** Automated wizard provisioning tenant records, default chart of accounts, standard ice cream unit of measures (liters, scoops, gallons, grams), default role hierarchies, and trial subscription.
- **Data Seeding Engine:** Isolated database migration execution during tenant onboarding (`dotnet ef database update --connection ...`).
- **Tenant Soft Deletion & Data Purge:** Automated 30-day grace period, data export to cold storage, and cryptographically secure deletion upon subscription termination.

---

## 4. Full ERP Functional Modules

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                            ENTERPRISE ERP CORE                               │
├─────────────────────┬──────────────────────┬─────────────────────────────────┤
│ Catalog & Recipes   │ Inventory & Cold-Ch. │ Supply Chain & Production       │
│ • Flavor Formulas   │ • Multi-Warehouse    │ • Vendor Scorecards             │
│ • BOM & Costing     │ • Batch/Lot Expiry   │ • Automated PO Generation       │
│ • Allergen Tracking │ • Cold-Chain Temp Log│ • Production Batch Runs & QC    │
├─────────────────────┼──────────────────────┼─────────────────────────────────┤
│ POS & Omnichannel   │ Orders & Fulfillment │ CRM & Loyalty                   │
│ • Offline POS Synch │ • Multi-Channel Sync │ • Tiered Reward Points          │
│ • Dual-Screen/KDS   │ • Kitchen Display    │ • Personalized Promotions       │
│ • Split Tender/Cash │ • Click & Collect    │ • Customer Feedback & Ratings   │
├─────────────────────┼──────────────────────┼─────────────────────────────────┤
│ Finance & Ledger    │ BI & Analytics       │ Administration & Compliance     │
│ • Double-Entry GL   │ • Real-Time Margins  │ • Fine-Grained RBAC / ABAC      │
│ • Automated Tax Calc│ • Demand Forecasting │ • Immutable Audit Trails        │
│ • Recurring Billing │ • Waste & Yield KPIs │ • GDPR / Tenant Data Sovereignty│
└─────────────────────┴──────────────────────┴─────────────────────────────────┘
```

### 4.1 Products & Catalog Management
- **Hierarchical Taxonomies:** Flavors, Scoops, Sundaes, Pints, Cones, Cakes, Merchandising, and Recipe Books.
- **Product Variants:** Matrix generator for size, base (dairy, vegan oat, almond, sorbet), toppings, packaging, and custom labels.
- **Real-Time Dynamic Pricing:** Parlour-level pricing, happy hour rules, event-specific price books, and seasonal price scheduling.

### 4.2 Recipes, Formulations & Bill of Materials (BOM)
- **Precision Formulation:** Ingredients calculated to 0.01g precision, including overrun percentage (air incorporated into ice cream), fat content, and sugar/solids ratio.
- **Dynamic Costing Engine:** Real-time raw material cost calculation recalculating batch cost whenever raw ingredient supplier prices fluctuate.
- **Allergen & Nutritional Engine:** Automatic allergen identification (milk, nuts, gluten, soy, eggs) and nutritional label generation complying with FDA/EU food labeling regulations.

### 4.3 Multi-Warehouse Cold-Chain Inventory
- **Locations & Sub-Locations:** Central warehouse, cold-storage freezer rooms (-18°C to -25°C), retail display dipping cabinets, and mobile ice cream truck inventory.
- **Batch & Lot Number Tracking:** End-to-end traceability from raw milk/cream lot receipt to the exact customer carton or batch run.
- **IoT Cold-Chain Telemetry:** Ingestion of continuous temperature sensor logs with real-time alerting for freezer threshold breaches.
- **Stock Reorder Optimization:** Reorder points based on safety stock, lead times, and sales velocity with automatic purchase requisition generation.

### 4.4 Supply Chain & Procurement
- **Vendor Catalog & Contracts:** Vendor management, agreed tier-pricing, minimum order quantities (MOQ), and delivery lead times.
- **Purchase Order (PO) Workflow:** Automated PO generation from requisitions, multi-level approval workflows (Manager $\rightarrow$ Finance), and electronic PO dispatch (PDF/EDI/Email).
- **Goods Receipt & Inspection:** Mobile barcode scanning on dock, batch lot assignment, temperature check on receipt, and variance reconciliation (shortages, damaged goods).

### 4.5 Production & Manufacturing (Batch Operations)
- **Batch Scheduling:** Work order scheduling based on parlor demand and sales forecasts.
- **Pasteurization & Aging Batch Runs:** Tracking batch pasteurization cycles, aging tank holding times, flavor mix additions, and freezing/churning.
- **Quality Control (QC) Gateways:** Mandatory QA sign-offs (Brix test, butterfat %, sensory score, temperature, microbiological sampling) before release to sellable stock.
- **Yield Variance Analysis:** Expected vs. actual yield metrics tracking mix shrinkage and overrun efficiency.

### 4.6 Point of Sale (POS) Terminal Execution
- **High-Velocity Checkout:** Touchscreen-optimized interface, barcode/QR scanner integration, scale integration for weighed items (toppings, bulk tubs).
- **Offline Mode & Sync:** Progressive Web App (PWA) / local IndexedDB persistence allowing continuous sales during network outages with conflict-free vector-clock synchronization on reconnect.
- **Kitchen Display System (KDS):** Real-time order routing to kitchen prep displays for sundaes, waffles, and custom orders with prep timer color coding.
- **Payment Gateway Integration:** Stripe Terminal, Square, Adyen, cash drawer kick triggers, and gift card / store credit splits.

### 4.7 Omnichannel Orders & Fulfillment
- **Unified Order Engine:** Aggregation of online store orders, third-party delivery apps (UberEats, DoorDash via webhook ingestion), catering bookings, and in-store pickup.
- **Delivery Management:** Route planning, insulated carrier tracking, driver dispatch, and SMS arrival notification with customer OTP verification.

### 4.8 CRM, Loyalty & Promotions Engine
- **Customer 360:** Unified customer profiles across POS and web, tracking flavor preferences, purchase history, and lifetime value (LTV).
- **Loyalty Program:** Points accrual, punch-card mechanics (e.g., 10th scoop free), VIP membership tiers (Vanilla, Gelato Master, Gold Truffle).
- **Campaign & Coupon Engine:** Multi-buy promotions (Buy 2 Pints Get 1 Free), promo codes, geo-targeted SMS/Email marketing, and automated birthday rewards.

### 4.9 Financial Accounting, General Ledger & Invoicing
- **Multi-Tenant Double-Entry Ledger:** Automatic journal entry generation for sales receipts, cost of goods sold (COGS), inventory shrinkage, vendor payables, and asset depreciation.
- **Accounts Payable (AP) & Receivable (AR):** Invoice aging analysis, automated 3-way matching (PO, Receipt, Vendor Invoice).
- **Tax Compliance Engine:** Multi-jurisdiction sales tax, VAT, and automated filing report generation.
- **Multi-Currency & Franchise Royalty Calculation:** Automated periodic royalty extraction from franchisee gross sales.

### 4.10 Business Intelligence (BI) & Predictive Analytics
- **Executive Dashboards:** Real-time Gross Margin, COGS percentage, Top 10 Selling Flavors by Parlour, Wastage Rate, Average Basket Size, and Labor-to-Sales ratio.
- **Predictive Demand Forecasting:** Time-series forecasting (ARIMA / Prophet integration) correlating weather forecasts, local holidays, and historical velocity to predict daily scoop demand.
- **Export & Scheduled Reports:** Automated PDF/Excel report delivery to store managers and executives daily at 06:00 AM.

---

## 5. Premium UI/UX Modernization

### 5.1 Design System & Theme Engine
- **Tailwind CSS Infrastructure:** Modern atomic design system replacing static Bootstrap styling, featuring customizable CSS variables per tenant brand.
- **Dynamic Theming:** Per-tenant branding (primary brand colors, custom logos, typography) stored in tenant settings and applied at runtime.
- **True Dark Mode:** System-preference detection (`prefers-color-scheme`) with persistent local toggle for dark, light, and high-contrast modes.
- **Component Library:** Reusable Razor tag helpers / view components:
  - `<erp-data-table>`: Virtualized scrolling, multi-column sort, quick filter, column visibility toggle.
  - `<erp-stat-card>`: Sparkline metrics, percentage delta vs last week, KPI indicator.
  - `<erp-modal>`: Accessible focus trap, keyboard ESC binding, smooth animation.
  - `<erp-badge>`: Status indicators (In Stock, Critical, Expired, Fulfilled, Overrun Warning).

### 5.2 Accessibility & Mobile Usability (WCAG 2.1 AA)
- Full ARIA compliance across dropdowns, modal dialogs, drawer menus, and data grids.
- Complete keyboard navigation with hotkeys for POS speed (e.g., `F2` = Search Product, `F4` = Pay Cash, `F8` = Card Payment, `ESC` = Cancel).
- Screen-reader verified labeling and color-contrast ratios exceeding 4.5:1 for normal text and 3:1 for large graphical elements.

---

## 6. Security, Governance & Compliance

### 6.1 Authentication & Zero-Trust Access
- **Identity Architecture:** ASP.NET Core Identity modernized with OpenIddict / Duende IdentityServer token service issuing short-lived JWTs (15 min) and sliding refresh tokens.
- **Multi-Factor Authentication (MFA):** Mandatory TOTP (Google/Microsoft Authenticator) or FIDO2/WebAuthn hardware keys for managerial, financial, and admin roles.
- **Single Sign-On (SSO):** SAML 2.0 and OpenID Connect enterprise integration for large franchise corporate accounts (Azure AD, Okta, Google Workspace).

### 6.2 Granular Authorization (RBAC / ABAC)
- **Role Hierarchy:** `PlatformSuperAdmin` $\rightarrow$ `TenantAdmin` (Franchise Owner) $\rightarrow$ `StoreManager` $\rightarrow$ `InventoryLead` $\rightarrow$ `KitchenChef` $\rightarrow$ `Cashier` $\rightarrow$ `Auditor`.
- **Permission Matrix:** Granular claims (e.g., `inventory:adjust`, `recipe:view_secret_formula`, `pos:discount_override`, `finance:export_ledger`).
- **Resource-Based / Attribute-Based Access:** Policy enforcement ensuring users can only read/mutate data belonging to their assigned parlour location and tenant.

### 6.3 Audit Logging & Non-Repudiation
- **Immutable Audit Trail:** Dedicated `AuditLogs` table tracking `TenantId`, `UserId`, `Action`, `EntityName`, `EntityId`, `OldValuesJson`, `NewValuesJson`, `IpAddress`, `UserAgent`, and UTC Timestamp.
- **Tamper Resistance:** Cryptographic hash chaining on audit records preventing database-level alteration.

### 6.4 Data Protection & Regulatory Compliance
- **Encryption Standards:** TLS 1.3 in transit with HSTS; AES-256 for data at rest (database volumes and sensitive fields like customer PII and API keys).
- **GDPR & CCPA:** Automated self-service data export ("Takeout"), right-to-be-forgotten anonymization pipelines, and audit log consent tracking.

---

## 7. Cloud-Native Tech Stack Upgrade

```
┌────────────────────────────────────────────────────────┐
│               Kubernetes Cluster (K8s)                 │
│                                                        │
│  ┌─────────────────┐    ┌─────────────────┐            │
│  │  Web / API Pod  │    │  Web / API Pod  │  ... (HPA) │
│  └────────┬────────┘    └────────┬────────┘            │
│           │                      │                     │
│           └──────────┬───────────┘                     │
│                      │                                 │
│         ┌────────────┼────────────┐                    │
│         ▼            ▼            ▼                    │
│  ┌─────────────┐┌───────────┐┌──────────────┐          │
│  │ Redis 7.x   ││PostgreSQL ││Elasticsearch │          │
│  │ (Cache/Lock)││16+ (Data) ││ (Search/Log) │          │
│  └─────────────┘└───────────┘└──────────────┘          │
└────────────────────────────────────────────────────────┘
```

### 7.1 Infrastructure Components
1. **Primary Database:** PostgreSQL 16 with TimescaleDB extension for IoT cold-chain sensor logging and Citus for horizontal sharding.
2. **Distributed Cache & Locking:** Redis 7 Cluster for session state, tenant configuration cache, API rate limiting, and Redlock distributed locks for batch inventory updates.
3. **Search & Analytics:** Elasticsearch 8+ / OpenSearch for sub-10ms full-text catalog queries, instant recipe ingredient search, and audit log exploration.
4. **Asynchronous Messaging:** RabbitMQ / Azure Service Bus for event-driven decoupled operations (order placed, stock depleted, invoice generated, temperature breach alert).
5. **Observability Stack:** OpenTelemetry tracing, Serilog structured JSON logging, Prometheus metrics collection, Grafana dashboards, and Sentry error tracking.

### 7.2 Containerization & Orchestration
- **Docker Multi-Stage Build:** Minimal distroless ASP.NET Core 8 runtime image (< 180MB) ensuring minimal attack surface.
- **Kubernetes Helm Charts:** Production-grade deployments with Horizontal Pod Autoscaler (HPA), liveness/readiness probes, pod disruption budgets, and ingress TLS automation via Let's Encrypt / Cert-Manager.
- **CI/CD Pipelines:** GitHub Actions workflow executing unit tests, integration tests with Testcontainers, SonarQube static analysis, vulnerability scanning (Trivy), and automated canary deployments.

---

## 8. Implementation Roadmap (8 Phases)

```
Phase 1: Multi-Tenancy Foundation, Enterprise Auth & Stripe Billing
   │
   ▼
Phase 2: Master Data Catalog, Precision Recipes & Bill of Materials
   │
   ▼
Phase 3: Cold-Chain Multi-Warehouse Inventory & Reordering Engine
   │
   ▼
Phase 4: Supply Chain Procurement & Batch Production Operations
   │
   ▼
Phase 5: High-Speed POS Terminal, Offline PWA & Omnichannel Orders
   │
   ▼
Phase 6: CRM, Gamified Loyalty & Marketing Automation Engine
   │
   ▼
Phase 7: General Ledger Accounting, Automated Tax & Financials
   │
   ▼
Phase 8: Executive BI, Demand Forecasting & Cloud-Native Hardening
```

### Phase 1: Multi-Tenancy Foundation, Enterprise Auth & Subscription Billing
- Implement `Tenant` and `TenantSubscription` data models.
- Build `TenantResolutionMiddleware` (subdomain, header, custom domain).
- Configure EF Core global query filters and dynamic connection string provider.
- Upgrade ASP.NET Core Identity to support multi-tenant user stores, RBAC claims, and TOTP MFA.
- Integrate Stripe Billing API for multi-tenant SaaS tiers (Starter, Professional, Enterprise Franchise) with webhook handlers.

### Phase 2: Master Data Catalog, Precision Recipes & Bill of Materials (BOM)
- Build enterprise product taxonomy, variant generators, and dynamic price books.
- Implement recipe formulation engine with 0.01g precision, butterfat %, overrun calculation, and automated allergen detection.
- Develop dynamic BOM cost calculation linking raw material prices to finished product unit costs.

### Phase 3: Cold-Chain Multi-Warehouse Inventory & Reordering Engine
- Model warehouses, cold-room freezers, dipping cabinets, and mobile vans.
- Implement lot/batch number tracking with expiry alerts and FIFO stock depletion.
- Ingest simulated/IoT temperature logs with automated freezer breach notifications.
- Implement automated stock reorder calculation and purchase requisition trigger.

### Phase 4: Supply Chain Procurement & Batch Production Operations
- Implement vendor catalog, contract management, and automated PO generation.
- Build 3-way matching goods receipt workflow with mobile barcode scan validation.
- Implement production batch scheduling (pasteurization, aging, churning) and mandatory QA gate approvals.

### Phase 5: High-Speed POS Terminal, Offline PWA & Omnichannel Orders
- Create modern touch-first POS interface with keyboard hotkeys, barcode scanning, and split payments.
- Build offline IndexedDB synchronization engine for uninterrupted retail operations.
- Build Kitchen Display System (KDS) with live timers and status tracking.
- Aggregate online orders, catering requests, and third-party delivery webhooks.

### Phase 6: CRM, Gamified Loyalty & Marketing Automation Engine
- Build Customer 360 profile tracking omnichannel purchase history and flavor affinities.
- Implement tiered loyalty rewards engine (points, free scoop tiers, birthday rewards).
- Build automated SMS/Email campaign trigger system.

### Phase 7: General Ledger Accounting, Automated Tax & Financials
- Implement double-entry accounting ledger with automated journal entries for all sales, inventory adjustments, and purchase receipts.
- Implement multi-jurisdiction sales tax calculation engine.
- Build Accounts Payable/Receivable aging reports and franchise royalty calculation.

### Phase 8: Executive BI, Demand Forecasting & Cloud-Native Hardening
- Build real-time analytics dashboard (COGS %, gross margins, shrinkage, labor vs sales).
- Integrate predictive demand forecasting model factoring historical sales and weather data.
- Containerize application with Docker, generate Kubernetes Helm charts, configure OpenTelemetry, and establish CI/CD pipelines.

---

## 9. Architectural Entity Relationship Model

```
┌─────────────────┐         1:N         ┌──────────────────────┐
│     Tenant      ├────────────────────►│      TenantUser      │
└────────┬────────┘                     └──────────┬───────────┘
         │ 1:N                                     │ N:M
         ├────────────────────►┌───────────────────▼──────────┐
         │                     │        Role / Permission     │
         │ 1:N                 └──────────────────────────────┘
         ├────────────────────►┌──────────────────────────────┐
         │                     │  Warehouse / Freezer Storage │
         │ 1:N                 └──────────┬───────────────────┘
         ├────────────────────┐           │ 1:N
         │                    ▼           ▼
         │             ┌──────────────────────────────┐
         │             │   InventoryBatch (Lot, Exp)  │
         │             └──────────▲───────────────────┘
         │ 1:N                    │ 1:N
         ├────────────────────►┌──┴───────────────────────────┐
         │                     │    Product / Recipe Formula  │
         │ 1:N                 └──────────┬───────────────────┘
         │                                │ 1:N
         ├────────────────────►┌──────────▼───────────────────┐
         │                     │   SalesOrder / POS Transaction│
         │ 1:N                 └──────────┬───────────────────┘
         │                                │ 1:N
         └────────────────────►┌──────────▼───────────────────┐
                               │ GeneralLedgerJournalEntry    │
                               └──────────────────────────────┘
```

---

## 10. Copy-Paste AI Coding Agent Prompt for Phase 1

```markdown
You are an expert Principal .NET Cloud Architect and Full-Stack Software Engineer tasked with executing **Phase 1: Multi-Tenancy Foundation, Enterprise Auth & Subscription Billing** of the Enterprise SaaS ERP specification on the existing ASP.NET Core 8.0 MVC project `Ice_Cream_Parlour_Eproject`.

### Objective
Layer production-grade multi-tenancy, enterprise role-based authentication with MFA readiness, and a comprehensive subscription billing foundation on top of the existing codebase while keeping current features functional.

### Key Deliverables & Specifications

1. **Multi-Tenant Core Models & Interfaces (`Models/Tenant/`):**
   - Create `Tenant` entity: `Id` (Guid), `Name`, `Slug` (unique, e.g., 'gelato-paradise'), `CustomDomain` (nullable), `IsActive`, `CreatedAtUtc`, `IsolatedDatabaseConnectionString` (nullable for tier-2 silo), `SubscriptionPlan` enum (`Starter`, `Professional`, `EnterpriseFranchise`), `SubscriptionStatus` enum (`Trial`, `Active`, `PastDue`, `Canceled`).
   - Create `ITenantContext` and `ITenantContextAccessor` service interfaces with an `AsyncLocal<Tenant>` implementation to provide tenant context throughout request lifecycles.
   - Create `TenantResolutionMiddleware` to resolve tenants via:
     a) Subdomain (e.g., `{subdomain}.icecream-erp.com` or `{subdomain}.localhost`)
     b) Custom domain mapping
     c) Request header `X-Tenant-ID` (for API / POS terminals)
     d) Fallback to default tenant if running locally or during initial onboarding.

2. **Entity Framework Multi-Tenant Integration (`Data/`):**
   - Implement `IMustHaveTenant` interface (`Guid TenantId { get; set; }`) applied to all core business entities (`Product`, `Recipe`, `Order`, `Customer`, etc.).
   - Update `ApplicationDbContext` to inject `ITenantContextAccessor`.
   - Apply EF Core Global Query Filter on all `IMustHaveTenant` entities: `.HasQueryFilter(e => e.TenantId == _tenantContext.CurrentTenant.Id)`.
   - Override `SaveChangesAsync` to automatically populate `TenantId` on newly added entities implementing `IMustHaveTenant`.

3. **Enterprise Authentication & RBAC (`Models/Auth/`, `Services/`):**
   - Extend `User` entity with `TenantId` (Guid?), `IsTenantOwner` (bool), `JobTitle` (string), and MFA configuration fields (`IsMfaEnabled`, `TotpSecretKey`).
   - Define enterprise roles: `SuperAdmin`, `TenantAdmin`, `StoreManager`, `InventoryLead`, `KitchenChef`, `Cashier`.
   - Seed default platform and tenant roles during database initialization.
   - Ensure login authenticates against tenant context (preventing cross-tenant unauthorized logins).

4. **SaaS Subscription & Billing Architecture (`Services/Billing/`, `Controllers/`):**
   - Create `IBillingService` interface with mock/Stripe implementation stub:
     - `CreateCustomerSubscriptionAsync(Guid tenantId, string planTier, string paymentMethodId)`
     - `HandleWebhookAsync(string jsonPayload, string signatureHeader)`
     - `GetSubscriptionDetailsAsync(Guid tenantId)`
   - Create `TenantRegistrationController` (`/onboard`) with a clean onboarding view allowing a new parlour/brand to register their brand, choose their subdomain, pick a subscription plan, and create the initial `TenantAdmin` account.

5. **Tenant Admin Management Portal (`Areas/Admin/Controllers/TenantController.cs`):**
   - Add tenant settings management view (Brand Name, Subdomain, Theme Accent Color, Custom Domain, Active Subscription Status).
   - Display current tenant identity in the Admin layout navigation bar.

6. **Integrity & Verification:**
   - Ensure the application compiles with zero errors and warnings (`dotnet build`).
   - Verify existing public routes (`/`, `/Home/About`, `/Home/Product`, `/Recipe`) and Admin routes continue to work smoothly with tenant-aware fallback.
```

---

*Specification authored for Ice Cream Parlour Enterprise SaaS ERP Platform.*
