# 🛒 Supermarket POS System - Backend API

A backend REST API for managing sales, cashier operations, products, categories, inventory, invoices, employees, and reports in a supermarket environment.

The API is designed around a clean CQRS structure with strict validation, transactional stock operations, and permission-based access control, so every screen and action in the frontend can be safely gated by the authenticated user's role and permissions.

---

## Overview

The system exposes the following main capabilities:

* Category and product management, with active/inactive product status.
* Inventory management with atomic stock-add operations and full stock history.
* Low-stock and out-of-stock product views.
* Invoice creation and printable thermal/A4 receipt generation.
* Returns and exchange operations.
* Employee management, roles, and permission assignment.
* Attendance tracking (login/logout).
* Permission-based access to every endpoint via JWT claims.
* Dashboard and sales reports.

---

## Tech Stack

* **.NET 10** - Web API framework
* **CQRS Pattern** via **MediatR** - clean separation of commands/queries and handlers
* **Dapper** (Micro-ORM) - fast, explicit SQL data access (no EF Core)
* **MySQL / MariaDB** - relational database
* **FluentValidation** - request validation, wired into the MediatR pipeline
* **JWT (JSON Web Tokens)** - authentication and permission claims
* **BCrypt.NET** - password hashing
* **Custom SQL migration runner** - numbered `.sql` scripts tracked via a `SchemaMigrations` table

---

## Project Structure

```text
SupermarketSystem.Api/
├── Controllers/
│   ├── AuthController.cs
│   ├── EmployeesController.cs
│   ├── CategoriesController.cs
│   ├── ProductsController.cs
│   ├── InvoicesController.cs
│   ├── AttendanceController.cs
│   └── ...
│
├── Features/
│   ├── Auth/
│   │   ├── Login/
│   │   └── Logout/
│   ├── Categories/
│   ├── Products/
│   │   ├── CreateProductCommand.cs
│   │   ├── UpdateProductCommand.cs
│   │   ├── DeactivateProductCommand.cs
│   │   ├── AddStockCommand.cs
│   │   ├── GetStockHistoryQuery.cs
│   │   ├── GetLowStockProductsQuery.cs
│   │   └── GetOutOfStockProductsQuery.cs
│   ├── Invoices/
│   └── Permission/
│
├── Common/
│   ├── PermissionKeys.cs
│   └── PermissionRequirementAttribute.cs
│
├── Models/
├── DTOs/
├── Interface/
├── Data/
│   └── DbConnectionFactory.cs
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs
│   └── ValidationBehavior.cs
├── Migrations/
├── Program.cs
└── appsettings.json
```

---

## Main Features

### 1. Categories

* Create and list product categories.
* Unique category names enforced at the validation layer.

### 2. Product Management

* Create, read, and update products (name, category, selling price, unit).
* Products are sold either **by Piece or by Package** — no barcode scanning required.
* `Quantity` can never be edited directly through the update endpoint — it can only change through stock-add operations or confirmed sales.
* Deactivate products instead of deleting them, so historical invoice data stays intact and accurate.

### 3. Inventory & Stock History

* Add-stock operations run inside a single database transaction: the product quantity and the stock history record are updated atomically — either both succeed or both roll back.
* Every stock addition is tied to the employee who performed it and a timestamp.
* Full, chronologically ordered stock history per product.
* Low-stock and out-of-stock product views for inventory monitoring.

### 4. Invoices & Printing

* Invoice line items store a **price and name snapshot** at the time of sale, so later price or product changes never affect historical invoices.
* Printable HTML receipt generation for 80mm thermal printers, with auto-print on open.
* Invoice totals, discount, cashier, and timestamp included on the printed receipt.

### 5. Returns and Exchanges

* Returns are tracked via a `HasReturn` flag rather than modifying the original invoice.
* Exchanges return the original quantity to stock and create a new invoice for the replacement item.

### 6. Employees & Attendance

* Employee creation, role assignment, and permission management.
* Login/logout attendance logging tied to each employee session.
* Admins automatically receive all permissions.

### 7. Reports & Dashboard

* Sales reports by date, employee, and product.
* Inventory and low-stock reporting.
* Dashboard summary data for authorized users.

---

## Permission System

Every endpoint is protected by `[Authorize]` plus a `[PermissionRequirement(...)]` attribute that checks the authenticated employee's permission claims (issued at login) against the required key for that action. Admin accounts receive every permission automatically.

| Permission Key                   | Grants Access To                          |
| --------------------------------- | ------------------------------------------ |
| `categories.view`                 | List categories                            |
| `categories.create`               | Create categories                          |
| `products.view`                   | List/search products, stock history, low-stock & out-of-stock views |
| `products.create`                 | Create products                            |
| `products.update`                 | Update product details                     |
| `products.deactivate`             | Deactivate products                        |
| `products.stock_add`              | Add stock to a product                     |
| `invoices.create`                 | Create sales invoices                      |
| `invoices.view`                   | View invoice list, details, and printable receipts |
| `invoices.return`                 | Process returns                            |
| `invoices.exchange`               | Process exchanges                          |
| `employees.view`                  | View employee list and details             |
| `employees.create`                | Create employees                           |
| `employees.update`                | Update employee details                    |
| `employees.deactivate`            | Deactivate employees                       |
| `employees.manage_permissions`    | Edit an employee's permissions             |
| `attendance.view`                 | View attendance for all employees          |
| `attendance.view_employee`        | View a single employee's attendance        |
| `reports.view`                    | Access reports                             |
| `dashboard.view`                  | Access dashboard analytics                 |

Permission checks always happen on the server — the API never trusts the frontend to hide a button as its only line of defense.

---

## API Integration

The API returns JSON for all endpoints, with a consistent error shape produced by a centralized exception-handling middleware:

```json
{
  "status": 400,
  "message": "Validation failed",
  "errors": [
    { "field": "Unit", "message": "Unit must be either 'Piece' or 'Package'" }
  ]
}
```

Authenticated requests must include a Bearer token issued by `POST /api/auth/login`, containing the employee's identity and permission claims.

---

## Setup

### Requirements

* **.NET 10 SDK**
* **MySQL Server 8.0+**

### 1. Clone the repository

```bash
git clone https://github.com/noormashaqi/supermarket-pos-backend.git
cd supermarket-pos-backend/SupermarketSystem.Api
```

### 2. Configure the environment

Create a `.env` file (or update `appsettings.json`) with your database and JWT settings:

```env
ConnectionStrings__DefaultConnection=Server=localhost;Port=3306;Database=SupermarketSystemDb;User=root;Password=your_password;
Jwt__Secret=YourSuperSecretKeyHere_MustBeAtLeast32Chars!
Jwt__Issuer=SupermarketSystem
Jwt__Audience=SupermarketSystem
Jwt__ExpiryMinutes=480
```

### 3. Create the database and run migrations

```sql
CREATE DATABASE SupermarketSystemDb;
```

Run each file under `Migrations/` in numeric order against the new database.

### 4. Run the application

```bash
dotnet restore
dotnet build
dotnet run
```

The API will be available at:

```text
http://localhost:5206
```

Swagger UI is available in development mode at `/swagger`.

---

## Development

The project follows a feature-based (vertical slice) structure where:

* `Controllers` are thin — they only send commands/queries through MediatR.
* `Features` contains one folder per capability, each with its Command/Query, Handler, and Validator grouped together.
* `Models` contains the database-shaped POCO classes used by Dapper.
* `DTOs` contains the response shapes returned to the frontend.
* `Common` contains cross-cutting pieces like `PermissionKeys` and the permission-checking attribute.
* `Middleware` contains the validation pipeline and centralized exception handling.

This structure keeps each feature self-contained and makes it straightforward to add new capabilities as the project grows.

---

## Frontend

This API is designed to work with a React + TypeScript frontend (Vite, React Router, fetch-based API client — no Axios) responsible for the POS screen, product/category/inventory management, invoice printing, employee management, and reporting dashboards.

Make sure this backend is running before starting the frontend application.

---

## Project Status

The backend currently includes complete, tested implementations for:

* Categories
* Products (create, read, update, deactivate)
* Stock History (add stock, view history)
* Low Stock / Out of Stock views
* Printable Sales Invoice & Thermal Receipt Generation
* JWT Authentication & Permission-based authorization

Further features (full invoice CRUD, returns/exchange, reports, dashboard) are in active development.
