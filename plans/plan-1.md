Act as a Senior Solution Architect, .NET Technical Lead, and Enterprise Software Engineer.

Design and generate a production-ready Enterprise Leave Management System that can completely replace traditional Excel and Microsoft Access leave registers. The application should be scalable, maintainable, and suitable for deployment in small to medium-sized organizations.

The solution must follow enterprise software engineering best practices and include complete architecture, source code, database design, deployment strategy, installer, documentation, and future scalability recommendations.

=========================================
TECHNOLOGY STACK
=========================================

Backend
- ASP.NET Core 8
- Entity Framework Core 8
- SQL Server Express / SQL Server LocalDB
- Clean Architecture
- Repository Pattern (only where appropriate)
- Unit of Work (if required)
- Dependency Injection
- FluentValidation
- JWT Authentication
- Role-Based Authorization
- AutoMapper
- Serilog Logging
- Swagger/OpenAPI
- Global Exception Handling Middleware
- API Versioning
- SOLID Principles
- CQRS where beneficial

Frontend (Choose One)

Preferred:
- Angular 18
- Bootstrap 5

Alternative:
- React + TypeScript + Bootstrap 5

=========================================
APPLICATION MODULES
=========================================

1. Authentication
- Login
- Logout
- Change Password
- Forgot Password
- Password Reset
- JWT Authentication
- Secure Password Hashing

2. User Roles
- Administrator
- HR
- Manager
- Employee

Each role should have configurable permissions.

=========================================
EMPLOYEE MANAGEMENT
=========================================

Provide complete CRUD operations.

Employee Details:
- Employee ID
- Employee Name
- Department
- Designation
- Date of Joining
- Status
- Email
- Phone Number
- Manager
- Opening Leave Balances

Features
- Search
- Filtering
- Pagination
- Sorting
- Employee Profile

=========================================
LEAVE TYPES
=========================================

Support configurable leave types.

Default Leave Types

- Earned Leave
- Sick Leave
- Compensatory Leave

Future leave types should be configurable from the UI without changing code.

=========================================
LEAVE MANAGEMENT
=========================================

Allow employees to

- Apply Leave
- Edit Leave
- Cancel Leave

Fields

- Leave Type
- From Date
- To Date
- Half Day
- Number of Days
- Remarks

System should automatically

- Calculate Leave Days
- Skip Weekends (configurable)
- Skip Holidays
- Detect overlapping leave requests
- Prevent invalid date ranges
- Prevent negative leave balances

Statuses

- Draft
- Pending
- Approved
- Rejected
- Cancelled

=========================================
LEAVE BALANCE MANAGEMENT
=========================================

Automatically manage

- Opening Balance
- Current Balance
- Leave Consumed
- Remaining Balance
- Carry Forward Rules
- Yearly Reset

Support

- Manual Adjustments
- HR Corrections

=========================================
APPROVAL WORKFLOW
=========================================

Workflow

Employee

↓

Manager

↓

HR

↓

Approved

Features

- Approval History
- Comments
- Notifications
- Re-approval after modifications
- Escalation Support

=========================================
DASHBOARD
=========================================

Admin Dashboard

- Total Employees
- Active Employees
- Employees on Leave Today
- Pending Approvals
- Department Statistics
- Leave Analytics
- Charts

Manager Dashboard

- Team Leave Calendar
- Pending Requests
- Leave Statistics

Employee Dashboard

- Current Leave Balance
- Upcoming Leaves
- Leave History
- Pending Requests

=========================================
REPORTS
=========================================

Generate

- Employee Leave Report
- Monthly Leave Report
- Yearly Leave Report
- Department Leave Report
- Leave Balance Report
- Attendance Summary
- Leave Trends

Export Formats

- Excel
- PDF
- CSV

=========================================
NOTIFICATIONS
=========================================

Email Notifications

- Leave Submitted
- Leave Approved
- Leave Rejected
- Leave Cancelled
- Pending Approval Reminder

Future Support

- SMS
- Microsoft Teams
- Slack

=========================================
AUDIT LOGGING
=========================================

Track

Created By

Created Date

Modified By

Modified Date

Deleted By

Deleted Date

Soft Delete

Complete Activity History

=========================================
DATABASE DESIGN
=========================================

Generate

ER Diagram

Normalized Database

Tables

Relationships

Primary Keys

Foreign Keys

Indexes

Stored Procedures (if beneficial)

Triggers (if required)

Entity Framework Migrations

Seed Data

=========================================
NON-FUNCTIONAL REQUIREMENTS
=========================================

Use

- Clean Architecture
- SOLID Principles
- Dependency Injection
- Repository Pattern where beneficial
- DTOs
- FluentValidation
- Global Exception Handling
- Logging
- Pagination
- Filtering
- Sorting
- Secure APIs
- Input Validation
- High Performance

=========================================
DEPLOYMENT REQUIREMENTS
=========================================

The application must be deployable on any Windows 10 or Windows 11 machine.

Requirements

- One-click installer (.exe or .msi)
- No manual installation steps
- Automatic prerequisite detection
- Automatic installation of missing components
- Automatic configuration

Installer should automatically install

- .NET 8 Runtime (or publish as Self-Contained Deployment)
- SQL Server Express or SQL Server LocalDB
- Visual C++ Redistributables (if needed)
- Required Windows Components

Database Setup

Automatically

- Create Database
- Apply Entity Framework Migrations
- Seed Default Data
- Create Administrator Account
- Configure Roles
- Configure Default Leave Types

Installer Features

- Desktop Shortcut
- Start Menu Shortcut
- Custom Installation Directory
- Uninstaller
- Upgrade Existing Installation
- Silent Installation Support
- Installation Logs

Publishing

- Self-Contained Deployment
- Single File Publish (if possible)
- x64 Support
- x86 Support

Configuration

- appsettings.json
- Environment Support
- Configurable Database Connection

Recommended Installer

- WiX Toolset (Preferred)
- Inno Setup
- MSIX (Optional)

=========================================
BACKUP & RESTORE
=========================================

Include

- Database Backup
- Database Restore
- Scheduled Backup
- Export Entire Database

=========================================
FUTURE SCALABILITY
=========================================

Architecture should support

- SQL Server Enterprise
- Azure SQL
- PostgreSQL
- Docker
- Kubernetes
- Azure Deployment
- AWS Deployment
- Mobile App
- REST APIs
- LDAP
- Active Directory
- Single Sign-On
- Multi-Tenant Support

=========================================
PROJECT STRUCTURE
=========================================

Generate complete Visual Studio Solution with

- Clean Folder Structure
- Domain Layer
- Application Layer
- Infrastructure Layer
- Persistence Layer
- API Layer
- Angular/React UI

=========================================
DELIVERABLES
=========================================

Generate

1. Solution Architecture
2. High-Level Design
3. Low-Level Design
4. ER Diagram
5. Database Schema
6. Entity Framework Models
7. DTOs
8. Services
9. Repositories
10. API Controllers
11. Authentication Module
12. Authorization Module
13. Angular/React UI
14. Bootstrap Responsive Layout
15. Dashboard
16. Reports
17. Email Service
18. Installer Project
19. SQL Scripts
20. Entity Framework Migrations
21. Seed Data
22. Docker Configuration
23. Swagger Documentation
24. Deployment Guide
25. Administrator Guide
26. User Manual
27. Future Enhancement Recommendations

=========================================
CODING STANDARDS
=========================================

Follow

- Enterprise coding standards
- SOLID Principles
- Clean Code
- DRY
- KISS
- Proper Naming Conventions
- XML Documentation
- Unit Testing
- Integration Testing
- Best Security Practices

=========================================
IMPLEMENTATION PLAN
=========================================

Do not generate all the code at once.

First provide:

1. Solution Architecture
2. Database Design
3. Folder Structure
4. UI Wireframes
5. ER Diagram
6. API Design
7. Project Roadmap

After approval, implement the application module-by-module in production-ready quality with complete, well-documented code.