# RestaurantMS — نظام إدارة المطاعم

<div dir="rtl">

## نظرة عامة | Overview

**بالعربية:**
RestaurantMS هو نظام متكامل لإدارة المطاعم مبني على ASP.NET Core 8، يشمل نقطة البيع (POS)، إدارة الوردية، تسليم الخزينة، التقارير، والطباعة. يدعم اللغتين العربية والإنجليزية بالكامل.

**In English:**
RestaurantMS is a full-featured restaurant management system built on ASP.NET Core 8. It covers Point-of-Sale (POS), shift management, cash handover, daily/shift reports, and printing — with full Arabic/English bilingual support.

</div>

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core 8 MVC |
| ORM | Entity Framework Core 8 |
| Database | Microsoft SQL Server |
| Auth | ASP.NET Core Identity (cookie-based) |
| Validation | FluentValidation 11 |
| Mapping | AutoMapper 12 |
| PIN Hashing | BCrypt.Net-Next 4 |
| Logging | Serilog 8 (Console + File) |
| Frontend | AdminLTE 3, Bootstrap 5, FullCalendar |

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Microsoft SQL Server (LocalDB, Express, or full edition)
- Git

---

## Setup

### 1. Clone the repository

```bash
git clone <repo-url>
cd RestaurantApp_v2
```

### 2. Update the connection string

Open `appsettings.json` and update `DefaultConnection`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=RestaurantMS;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

Replace the server value with your SQL Server instance if needed (e.g. `Server=.\\SQLEXPRESS`).

### 3. Create the EF migration

```bash
dotnet ef migrations add InitialCreate
```

### 4. Apply the migration to the database

```bash
dotnet ef database update
```

### 5. Run the application

```bash
dotnet run
```

The app will auto-seed roles and the default admin user on first launch.

### 6. Login

| Field    | Value              |
|----------|--------------------|
| Username | `admin`            |
| Password | `Admin@123`        |
| PIN      | `1234`             |
| Role     | SuperAdmin         |

> **Important:** Change the password and PIN after the first login.

---

## Project Structure

```
RestaurantApp_v2/
├── Controllers/          # MVC controllers (POS, Shifts, Handover, Reports, Language)
├── Data/                 # RestaurantDbContext (EF Core + Identity)
├── Extensions/           # DI registrations (ServiceExtensions) & AutoMapper profile
├── Helpers/              # ApiResponse<T>, PaginationHelper, ManagerPinHelper
├── Middleware/           # Global exception handler
├── Models/
│   ├── Domain/           # EF entities (MenuItem, PosOrder, CashHandover, …)
│   └── DTOs/             # Request/response DTOs
├── Repositories/
│   ├── Interfaces/       # IUnitOfWork, IGenericRepository<T>, specialised interfaces
│   └── *.cs              # Implementations (GenericRepository, OrderRepository, …)
├── Services/
│   ├── Interfaces/       # IPosService, IReportService, IHandoverService, …
│   └── *.cs              # Implementations
├── Validators/           # FluentValidation rules
├── Views/                # Razor views (AdminLTE layout, POS Touch, Reports, …)
├── wwwroot/              # Static assets (CSS, JS, AdminLTE)
├── Scripts/SQL/          # Optional SQL migration/seed scripts
├── Program.cs            # App bootstrap, DI, middleware pipeline
├── appsettings.json      # Connection string, restaurant config, Serilog
└── RestaurantMS.csproj
```

---

## Features

| Feature | Description |
|---------|-------------|
| **POS Touch** | Touch-friendly order entry, barcode scanning, cash/card/split payments |
| **Cash Handover** | Open/close cashier sessions, expected vs. actual reconciliation, manager approval |
| **Shift Management** | Shift templates, clock-in/out, FullCalendar schedule view, overtime calculation |
| **Reports** | Daily sales, Z-Report (end-of-day), X-Report (current shift), best sellers, hourly breakdown, CSV export |
| **Printing** | Thermal receipt, kitchen ticket, A4 invoice, Z/X report print layouts |
| **Bilingual UI** | Full Arabic (RTL) and English support, cookie-based language switch |
| **Soft Deletes** | All records are soft-deleted (IsDeleted flag), never permanently removed |
| **Manager PIN** | BCrypt-hashed PIN required for discounts > 10%, voids, and handover approvals |

---

## Roles & Permissions

| Role | POS | Handover | Shifts | Reports | Admin |
|------|-----|----------|--------|---------|-------|
| **SuperAdmin** | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Manager** | ✅ | ✅ (approve) | ✅ (assign) | ✅ | ❌ |
| **Cashier** | ✅ | ✅ (own) | View own | ❌ | ❌ |
| **Waiter** | ❌ | ❌ | View own | ❌ | ❌ |
| **KitchenStaff** | ❌ | ❌ | View own | ❌ | ❌ |
| **Accountant** | ❌ | ❌ | ❌ | ✅ | ❌ |

---

## Configuration (`appsettings.json`)

```json
"RestaurantSettings": {
  "Name": "My Restaurant",
  "NameAr": "مطعمي",
  "TaxRate": 0.14,
  "CurrencySymbol": "EGP",
  "DiscountRequiresPinAbove": 10.0,
  "HandoverDifferenceThreshold": 5.0
}
```

---

## Screenshots

> _Screenshots will be added here._

| Screen | Preview |
|--------|---------|
| POS Touch | _(coming soon)_ |
| Cash Handover | _(coming soon)_ |
| Shift Schedule | _(coming soon)_ |
| Z-Report | _(coming soon)_ |
| Daily Report | _(coming soon)_ |

---

## License

MIT — free for personal and commercial use.
