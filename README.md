# AcxiomCRM — Enterprise CRM Web Application

**AcxiomCRM** is a production-quality, enterprise-grade Customer Relationship Management (CRM) web application built using **ASP.NET Core 8.0 MVC**, **C#**, **Entity Framework Core**, **SQL Server / SQLite**, **ASP.NET Core Identity**, **Bootstrap 5**, and **Chart.js**.

---

## 1. Project Architecture

The application uses an enterprise-grade layered architecture:

```
AcxiomCRM/
├── AcxiomCRM/                      # ASP.NET Core 8 MVC Application
│   ├── Controllers/               # MVC Controllers (Dashboard, Customers, Leads, etc.)
│   ├── ApiControllers/            # REST API Controllers (/api/auth, /api/customers, etc.)
│   ├── Models/
│   │   ├── Entities/              # Domain Models (Customer, Lead, Opportunity, etc.)
│   │   └── Enums/                 # Status & Type Enums (UserRoles, LeadStatus, etc.)
│   ├── Data/
│   │   ├── ApplicationDbContext.cs# EF Core DbContext with Fluent API relationships
│   │   ├── DbInitializer.cs       # Seed data engine (Users, Roles, CRM records)
│   │   └── Migrations/            # EF Core Migrations
│   ├── Services/
│   │   ├── Interfaces/            # Business & Scoping Interfaces
│   │   └── Implementations/       # Role-scoped services & audit logging
│   ├── ViewModels/                # Strongly-typed models for views
│   ├── DTOs/                      # Data Transfer Objects for REST APIs
│   ├── Middleware/                # Global exception handling & API error handling
│   ├── Views/                     # Modern Razor views with Bootstrap 5
│   │   ├── Dashboard/             # KPI metrics & 3 interactive Chart.js charts
│   │   ├── Customers/             # Customer CRUD & account history
│   │   ├── Leads/                 # Lead management & conversion workflow
│   │   ├── Opportunities/         # Deal pipeline & weighted forecasting
│   │   ├── FollowUps/             # Scheduling, rescheduling & reminders
│   │   ├── Activities/            # Call, meeting, email & task tracking
│   │   ├── Reports/               # 8 analytical reports with CSV export
│   │   ├── Users/                 # Admin user & role management
│   │   └── AuditLogs/             # Admin security & compliance audit log
│   ├── wwwroot/
│   │   ├── css/crm.css            # Enterprise CRM styling
│   │   └── js/crm.js              # Sidebar toggle, toasts, modals
│   ├── appsettings.json           # Dual database provider configuration
│   ├── AcxiomCRM_Schema.sql       # Complete DDL script
│   └── AcxiomCRM_SqlServer_Schema.sql # Ready-to-run SQL Server / SSMS DDL script
└── AcxiomCRM.Tests/               # xUnit Automated Test Suite (18 tests passed)
```

---

## 2. Pre-Configured Development Seed Accounts

The database is automatically created and seeded on application startup with the following accounts:

| Role | Email | Password | Full Name | Department / Access Scope |
| :--- | :--- | :--- | :--- | :--- |
| **Admin** | `admin@acxiomcrm.local` | `Admin@12345!` | System Administrator | Full access: All records, User Management, Audit Logs, All Reports |
| **Manager** | `manager@acxiomcrm.local` | `Manager@12345!` | Sarah Jenkins | Team access: Own records + direct reports (Alex & Emma) |
| **SalesExecutive** | `sales1@acxiomcrm.local` | `Sales@12345!` | Alex Rivera | Assigned records only (AssignedTo = Alex) |
| **SalesExecutive** | `sales2@acxiomcrm.local` | `Sales@12345!` | Emma Watson | Assigned records only (AssignedTo = Emma) |

*A one-click login helper is also provided directly on the sign-in page (`/Account/Login`) for instant credential filling.*

---

## 3. Database Configuration

The application features **intelligent dual-mode database support**:

1. **Automatic SQLite (Zero Setup Required)**: Out-of-the-box, the app uses `AcxiomCRM.db` for instant local development with zero database server installation needed.
2. **Microsoft SQL Server / LocalDB / Azure SQL**: Configure `"DatabaseProvider": "SqlServer"` in `appsettings.json` to connect to SQL Server.

### Connection Strings (`appsettings.json`):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=AcxiomCRM;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True",
    "SqliteConnection": "Data Source=AcxiomCRM.db"
  },
  "DatabaseProvider": "Auto"
}
```

---

## 4. How to Run the Application

### Option A: From Command Line
```powershell
# Navigate to the AcxiomCRM web project directory
cd C:\Users\prana\.gemini\antigravity\scratch\AcxiomCRM\AcxiomCRM

# Run the web application
dotnet run
```
Then open your browser at:
- Web Application: **`http://localhost:5000`** or **`http://localhost:5249`**
- Interactive Swagger REST API Docs: **`http://localhost:5249/swagger`**

### Option B: Run Automated Tests
```powershell
cd C:\Users\prana\.gemini\antigravity\scratch\AcxiomCRM
dotnet test
```

---

## 5. REST API Endpoints

All endpoints are fully secured, validate input payloads, enforce role authorization, and return standard HTTP status codes:

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `POST` | `/api/auth/login` | Authenticate user and issue secure session cookie |
| `POST` | `/api/auth/logout` | Terminate session and record audit logout |
| `GET` | `/api/customers` | Scoped list of customers with search, filter, pagination |
| `GET` | `/api/customers/{id}` | Get customer details (authorized records only) |
| `POST` | `/api/customers` | Create new customer (with uniqueness validation) |
| `PUT` | `/api/customers/{id}` | Update customer record |
| `DELETE`| `/api/customers/{id}` | Deactivate or delete customer |
| `GET` | `/api/leads` | Scoped list of sales leads |
| `GET` | `/api/leads/{id}` | Get lead details |
| `POST` | `/api/leads` | Create new prospect lead |
| `PUT` | `/api/leads/{id}` | Update lead record |
| `DELETE`| `/api/leads/{id}` | Delete lead record |
| `GET` | `/api/opportunities` | Scoped list of opportunity deals |
| `GET` | `/api/opportunities/{id}`| Get opportunity details |
| `POST` | `/api/opportunities` | Create opportunity (amount > 0, probability 0-100) |
| `PUT` | `/api/opportunities/{id}`| Update opportunity |
| `DELETE`| `/api/opportunities/{id}`| Delete opportunity |
| `GET` | `/api/followups` | Scoped list of scheduled follow-ups |
| `POST` | `/api/followups` | Schedule follow-up (planned date >= today) |
| `GET` | `/api/reports/pipeline` | Aggregate pipeline summary and stage distribution |

---

## 6. Security & Business Rules Enforced

1. **Role-Based Data Scoping**:
   - `Admin`: Full unrestricted access.
   - `Manager`: Access to self and all supervised team members.
   - `SalesExecutive`: Strict record-level boundary. Tampering with IDs via URL or API is blocked (returns 403 Forbidden or 404 Not Found).
2. **Lead-to-Customer Conversion**:
   - Converted leads are marked with status `Converted`.
   - Links directly to the newly created customer account.
   - Duplicate conversion is prevented.
   - Optional deal creation with deal amount validation.
3. **Validation Rules**:
   - Opportunity amount must be greater than 0: `"Opportunity Amount must be greater than 0."`
   - Probability must be between 0 and 100: `"Probability must be between 0 and 100."`
   - Expected close date cannot be in past for active opportunities: `"Expected Close Date cannot be in the past."`
   - Follow-up date cannot be in past for planned follow-ups: `"Follow-up date cannot be earlier than today."`
   - Customer email and phone must be unique across accounts.
4. **Weighted Pipeline Calculation**:
   - `WeightedPipeline = Amount × Probability / 100`
5. **Append-Only Audit Logging**:
   - Records user, action, entity, record ID, IP address, timestamp, and sanitized payload details.
   - Excludes plain-text passwords and sensitive secrets.

