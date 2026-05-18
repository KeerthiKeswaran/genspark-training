# Community Library Management System

## Project Overview

The Community Library Management System is a .NET Core Console Application engineered to facilitate the end-to-end operational requirements of a small community library. In accordance with the project case study, the system was meticulously planned—defining entities, relationships, business rules, and database design before any implementation began. Model classes, interfaces, and interactions were strictly defined to enforce clean architecture.

The application leverages **Entity Framework Core (EF Core)** and **PostgreSQL** using a strict **Database-First Approach**. The relational schema was designed and executed within PostgreSQL first, followed by scaffolding the database models into the application. The system operates through a segmented presentation layer divided into Administrative and Member portals, enforcing strict separation of concerns to decouple data persistence, business logic, and presentation interfaces.

## Setup Instructions

Follow these instructions to configure and run the application locally.

### Prerequisites
1. **.NET SDK**: Ensure the .NET 10.0 SDK (or compatible version) is installed on your machine.
2. **PostgreSQL**: Install PostgreSQL and ensure the database server is actively running.
3. **Database Script**: You will need the provided `docs/script.sql` (or `documents/psql_script.sql`) file to build the initial database schema.

### 1. Database Initialization
This project uses a **Database-First** approach. Before running the application, you must provision the PostgreSQL database:
1. Open your preferred PostgreSQL client (e.g., pgAdmin or `psql` CLI).
2. Create a new database named `librarydb` (or your preferred name).
3. Execute the provided SQL script (`documents/script.sql`) against your new database to generate all required tables, relationships, and PostgreSQL stored procedures.

### 2. Entity Framework Core Scaffolding
Because this is a Database-First architecture, you must scaffold the database schema into C# models using the EF Core CLI tools. 
1. Open your terminal in the root directory (`/library-system/`).
2. Run the following scaffold command (ensure you replace the placeholder credentials with your actual database credentials):
```bash
dotnet ef dbcontext scaffold "Host=localhost;Database=librarydb;Username=YOUR_USERNAME;Password=YOUR_PASSWORD" Npgsql.EntityFrameworkCore.PostgreSQL --output-dir Models/Models --context-dir Contexts --context LibraryDbContext --force --no-onconfiguring --project LibrarySystem.Data
```
*Note: The `--no-onconfiguring` flag is utilized intentionally to prevent hardcoding connection strings in the generated `LibraryDbContext`, allowing dynamic injection via `appsettings.json`.*

### 3. Application Configuration
The application securely manages connection strings using an `appsettings.json` file.
1. Navigate to the `LibrarySystem.Presentation` directory.
2. Open or create the `appsettings.json` file.
3. Update the `DefaultConnection` string with your PostgreSQL credentials:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=librarydb;Username=YOUR_USERNAME;Password=YOUR_PASSWORD"
  }
}
```

### 3. Build and Run
Once the database is seeded and the connection string is configured, build and execute the application:
1. Open your terminal and navigate to the root directory of the solution (`/library-system/`).
2. Run the application utilizing the .NET CLI:
```bash
dotnet run --project LibrarySystem.Presentation
```
3. The console interface will launch, allowing you to register as a Member or Admin and interact with the library system.

## Core Features and Capabilities

### Authentication and Identity Management
The system features a centralized authentication engine that independently manages user identities across different permission tiers. 
*   **Universal Authentication Layer**: Employs a dedicated `Passwords` table, decoupling raw credentials from administrative and membership personnel records. 
*   **Cryptographic Hashing**: All registered user passwords are encrypted using the SHA-256 hashing algorithm prior to persistence, ensuring adherence to modern security practices. 
*   **Prefix-Based User Identification**: The authentication pipeline parses distinct `mem_` (Member) and `adm_` (Admin) user prefixes to correctly route user sessions and apply role-specific authorizations dynamically.

### Administrative Portal
The Administrative layer grants authorized personnel oversight over the library catalog and user operations.
*   **Executive Dashboard**: Generates real-time statistical reports detailing the most frequently borrowed catalog items and the current active borrowing ledgers.
*   **Catalog Management**: Supports the continuous ingestion of new book titles, author metadata, categorical assignments, and bulk inventory quantity updates.
*   **Return Workflow Processing**: Provides administrators the authority to review pending return submissions from members, enabling them to evaluate the physical condition of the returned item and explicitly approve or reject the submission.
*   **Dynamic Financial Configuration**: Allows for real-time adjustments to global fine rates (e.g., standard late fees vs. damaged copy fees) which automatically apply to future return evaluations.
*   **Member Account Governance**: Administrators have full authority to manually suspend or reactivate member accounts as well as generate reports identifying users with outstanding financial dues or significantly overdue items.

### Member Portal
The Member interface provides library patrons with self-service capabilities to independently manage their accounts.
*   **Catalog Search and Discovery**: A sophisticated querying engine that permits members to locate materials via title, author name, or category nomenclature.
*   **Self-Service Checkout**: Members may directly process material checkouts. The business logic inherently validates the member's current status, maximum allowable borrow count, and physical copy availability before finalizing the transaction.
*   **Active Ledger Management**: Displays an ongoing ledger of currently borrowed materials alongside their respective due dates.
*   **Return Initiation**: Facilitates the initiation of material returns, passing the record into a pending state pending administrative review. 
*   **Financial Due Oversight**: Generates an itemized statement of any accumulated fines resulting from late returns or damaged materials, allowing the member to clear their dues programmatically.

> **Note on Testing & Timestamps:** For testing and demonstration purposes, the system currently prompts the user to manually input the checkout (borrow) and return dates in `dd-mm-yyyy` format rather than fetching the system time dynamically. If no input or an invalid date format is provided, the application will automatically default to the current system date.

## Database Schema and Architecture

The underlying relational database structure enforces data integrity, tracks continuous state changes, and normalizes financial and inventory records.

### Personnel Entities
*   **Admins**: Stores unique identifiers, contact information, and names of internal personnel.
*   **Members**: Stores patron demographic information and enforces categorical types (`Basic`, `Premium`, `Student`) which dictate the limits of their library usage. Tracks the current authorization status (`Active`, `Inactive`) to mitigate unauthorized transactions.
*   **Passwords**: The centralized credential repository mapping unique alphanumeric identifiers to their SHA-256 hash equivalents.

### Inventory Entities
*   **BookCategories**: Defines the overarching classification hierarchy utilized for catalog filtration. 
*   **Books**: The primary material record containing title, author, description, and categorical metadata.
*   **BookCopies**: Represents individual physical units of a book, managing their condition (`Good`, `Damaged`) and their real-time availability status (`Available`, `Borrowed`, `Unavailable`).

### Transactional Entities
*   **Borrowings**: Serves as the primary transaction record linking a Member, a Book, the checkout date, and the expected due date. Includes data fields to capture eventual return states and administrative remarks.
*   **Returns**: Evaluates the completion of a Borrowing transaction. Records the actual return date, calculates any resulting fine logic, and tracks the approval pipeline (`Pending`, `Approved`).
*   **FineCalculation**: Operates as a financial ledger tracking the amount owed, the reason for the fine, and whether the invoice remains unpaid.
*   **FineConfiguration**: A strictly controlled mapping table establishing standard rates for operational penalties (such as `LateReturn`).
*   **MembershipLimits**: Dictates the business rules associated with different member tiers, specifically outlining the maximum concurrent borrow limits and maximum checkout durations.

## Technical Specifications

The system is constructed across a multi-tier C# solution utilizing the following architectural paradigms:
*   **Presentation Layer**: Responsible for standardizing console interactions, explicit user input parsing, real-time password masking, and robust exception handling.
*   **Business Layer**: Enforces the domain logic, evaluating membership conditions, computing cryptographic hashes, tracking return approvals, and calculating financial penalties.
*   **Data Access Layer**: Implements the Repository Pattern, acting as the singular conduit to the Entity Framework Core data context. 
*   **Database Management**: Connects strictly through Npgsql to a PostgreSQL environment. The data context is injected dynamically at runtime utilizing `appsettings.json` and standard .NET Configuration Builders to protect environmental connection strings.
