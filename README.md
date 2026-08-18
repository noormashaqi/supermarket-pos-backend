# 🛒 Supermarket POS System - Backend API

A high-performance backend Web API for managing sales, cashier operations, inventory transactions, invoices, employees, and financial reports in a supermarket environment.

Built with **.NET 10** using the **CQRS pattern** with **MediatR** and **Dapper Micro-ORM**, ensuring low latency, high throughput, and atomic transaction integrity for all POS operations.

---

## Overview

The system provides the core business logic and API infrastructure for:

* Fast, transaction-safe POS invoice creation and stock deduction.
* Real-time item price override validation governed by permission claims.
* Server-side hold and resume invoice management.
* Pure product return and atomic product exchange workflows.
* Customer ledger and debt sales management.
* Atomic employee creation with role and fine-grained permission assignments.
* Sanitized HTML generation for thermal receipt printing (80mm and A4).
* Role-Based and Claim-Based Access Control (RBAC) via JWT authentication.
* Sales analytics, inventory tracking, and employee attendance history.

---

## Tech Stack

* **.NET 10 SDK** - Core framework
* **ASP.NET Core Web API** - RESTful API endpoints
* **CQRS Pattern** - Command Query Responsibility Segregation via **MediatR**
* **Dapper** - High-performance Micro-ORM for SQL operations
* **MySQL / MariaDB** - Relational database
* **BCrypt.NET-Next** - Secure password hashing
* **System.IdentityModel.Tokens.Jwt** - JWT bearer token authentication and claims
* **FluentValidation** - Automated request validation
* **Swagger / OpenAPI** - Interactive API documentation

---

## Project Structure

```text
supermarket-pos-backend/
├── src/
│   ├── SupermarketSystem.Api/
│   │   ├── Controllers/
│   │   ├── Middlewares/
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   ├── SupermarketSystem.Application/
│   │   ├── Behaviors/
│   │   ├── Commands/
│   │   ├── DTOs/
│   │   ├── Queries/
│   │   └── Validators/
│   │
│   ├── SupermarketSystem.Domain/
│   │   ├── Entities/
│   │   └── Interfaces/
│   │
│   └── SupermarketSystem.Infrastructure/
│       ├── Auth/
│       ├── Data/
│       └── Repositories/
│
├── database/
│   └── schema.sql
├── SupermarketSystem.sln
└── README.md
