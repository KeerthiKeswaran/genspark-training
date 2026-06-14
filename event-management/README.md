# Event Management Platform

A premium, high-performance event hosting, ticket booking, and ticketing management system built with **.NET 10.0**, **ASP.NET Core Web API**, and **PostgreSQL (Entity Framework Core)**. The platform provides comprehensive features for users (Attendees and Organizers), system administrators, and financial administrators, featuring automated QR ticket check-ins, automated staff allocation, role-bound login, and integrated Stripe payments.

---

## Project Architecture & Design

The project is structured following clean architecture guidelines:
*   **Event.API**: The API endpoints, validation logic, request/response models, and route mappings.
*   **Event.Business**: The service layer hosting the core domain logic, exceptions, transaction handling, helper services, and background workers.
*   **Event.Contracts**: Interface definitions for repositories and services to maintain loose coupling.
*   **Event.Data**: Database access repositories, migrations, and PostgreSQL DbContext configuration.
*   **Event.Models**: Shared entity definitions, DTO schemas, and model binding definitions.

### Technical Stack:
*   **Backend Framework**: .NET 10.0 (C#)
*   **Database**: PostgreSQL
*   **Caching Layer**: Redis Cache (via StackExchange)
*   **Payment Gateway**: Stripe
*   **Email Engine**: Brevo SMTP Integration
*   **Testing Suite**: NUnit, Coverlet, Moq

---

## Database Schema Details

The database is built on **PostgreSQL** using Entity Framework Core. Specific sequences start IDs at **10000** for all primary keys (excluding transactions, which utilize a 16-digit sequence starting at `1000000000000000`) to guarantee uniform 5-digit entity tracking.

### Entity Relationship Diagram (ERD)

```mermaid
erDiagram
    Users ||--o{ Bookings : "makes"
    Users ||--o{ Events : "organizes"
    Users ||--o{ SupportTickets : "submits"
    Users }|--|| TermsAndConditions : "consents to"
    Admins ||--o{ PlatformSettings : "updates"
    Management ||--o{ Staffs : "has"
    Management ||--o{ Venues : "has"
    Venues ||--o{ VenueSeatCapacities : "defines"
    Events }|--|| Venues : "held at"
    Events ||--o{ EventTicketTiers : "defines"
    Events ||--o{ EventStaffAllocations : "assigns"
    Events ||--o{ Bookings : "receives"
    Bookings ||--o{ BookingDetails : "contains"
    Bookings ||--o{ BookingPayments : "has"
    BookingPayments }|--|| Transactions : "records"
    Events ||--o{ EventReports : "receives"
    Events ||--o{ EventFeedbacks : "gets"
```

### Table Definitions & Key Constraints
1.  **Users (`"Users"`)**: Attendee and Organizer accounts. Key fields: `User_Id` (PK), `Email` (Unique), `Password_Hash`, `Consented_Terms_Id` (FK to `"TermsAndConditions"`).
2.  **Admins (`"Admins"`)**: Platform staff accounts. Key fields: `Admin_Id` (PK - starts with `ADM` or `FIN`), `Email`, `Password_Hash`.
3.  **Regions (`"Management"`)**: Regional operational centers. Key fields: `Region_Id` (PK), `No_Of_Staffs`.
4.  **Venues (`"Venues"`)**: Physical event spaces. Key fields: `Venue_Id` (PK), `Region_Id` (FK to `"Management"`), `Hourly_Price`.
5.  **Venue Seat Capacities (`"VenueSeatCapacities"`)**: Composite key: `(Venue_Id, Tier_Name)`. Tracks seat limits.
6.  **Events (`"Events"`)**: Key fields: `Event_Id` (PK), `Organizer_Id` (FK to `"Users"`), `Venue_Id` (FK to `"Venues"`), `Status` (Live, Pending, Cancelled).
7.  **Event Ticket Tiers (`"EventTicketTiers"`)**: Composite key: `(Event_Id, Tier_Name)`. Tracks prices and capacities per tier.
8.  **Bookings (`"Bookings"`)**: Key fields: `Booking_Id` (PK), `Attendee_Id` (FK), `Event_Id` (FK), `Booking_Status` (Live, Pending, Cancelled), `Qr_Secret_Hash` (SHA256 hash verified for ticket check-ins).
9.  **Platform Settings (`"PlatformSettings"`)**: Dynamic parameters. Key fields: `Staff_Flat_Rate`, `Ticket_Commission_Percentage`, `Max_Tickets_Per_Booking`, `Updated_By_Admin_Id` (FK to `"Admins"`).
10. **Support Tickets (`"SupportTickets"`)**: Key fields: `Ticket_Id` (PK), `User_Id` (FK to `"Users"`), `Subject`, `Message`, `Status` (Open, Resolved).

---

## Service Layer Responsibilities (Event.Business)

The service layer contains the foundational business workflows of the application:

*   **`UserAuthService`**: Handles user authentication, hashes password entries using PBKDF2 SHA256, verifies OTP caches, and enforces that registration requires consent to the activeTerms.
*   **`DeptAuthService`**: Handles Admin and Finance logins. Implements multi-factor authentication (OTP) for accounts starting with the `FIN` prefix.
*   **`EventService`**: Validates event details, physical boundaries, checks and schedules virtual meetings, checks staff regional counts, and updates event state status.
*   **`BookingService`**: Orchestrates ticket reservations, checks remaining capacity in real-time, processes payments via Stripe integrations, generates custom QR check-in hashes, and manages check-in state.
*   **`RefundService`**: Contains cancellation calculations to compute dynamic refunds (e.g. 100% refund for cancellations > 48h before event, 50% refund for 24h-48h, and 0% refund for < 24h).
*   **`FinanceService`**: Aggregates billing records, executes payout commands to organizers, reviews escalated disputes, and processes refund allocations.
*   **`SupportService`**: Submits and processes user tickets, escalates items to administrators, and registers resolutions.
*   **`BackgroundService`**: A hosted worker executing periodically to:
    1. Revert ticket reservations that remained in "Pending Payment" status for more than 15 minutes.
    2. Clean up expired temporary tokens and OTP caches.

---

## Local Setup & Configuration Instructions

Follow these steps to set up the project locally:

### 1. Prerequisites
Ensure the following are installed:
*   [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
*   [PostgreSQL Database Server](https://www.postgresql.org/)
*   [Redis Cache Server](https://redis.io/)

### 2. Connection Settings Configuration
Open [server/Event.API/appsettings.json](server/Event.API/appsettings.json) and set up the connections:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=event_management_db;Username=YOUR_USERNAME;Password=YOUR_PASSWORD",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "SecretKey": "YOUR_SUPER_SECRET_KEY_MUST_BE_AT_LEAST_32_CHARACTERS",
    "Issuer": "EventPlatform",
    "Audience": "EventPlatformUsers",
    "ExpiryHours": "24"
  },
  "Stripe": {
    "SecretKey": "sk_test_..."
  },
  "Brevo": {
    "ApiKey": "YOUR_BREVO_API_KEY",
    "SenderEmail": "YOUR_SENDER_EMAIL",
    "SenderName": "Event Management System"
  }
}
```

### 3. Initialize the Database Schema (EF Migrations)
From the `server` directory, apply EF Core migrations to generate all the system tables:
```bash
dotnet ef database update --project Event.Data --startup-project Event.API
```

### 4. Seed Reference Tables
Open your PostgreSQL database tool and run the seeding script located at `docs/workflows/postmanflow.md` (or refer to the project SQL script). This seeds initial:
*   Regions (in `"Management"`)
*   Terms and Conditions (in `"TermsAndConditions"`)
*   Default Admin & Finance Users (in `"Admins"`)
*   Platform Settings, Venues, and Staffing profiles.

### 5. Running the Application
From the `server/Event.API` directory, start the development server:
```bash
dotnet run
```
The API server will run at `http://localhost:5106`.

---

## Unit Test Execution & Code Coverage

Our comprehensive test suite validates all critical paths, mocking DB adapters and Stripe integration nodes to ensure reliable test execution.

### Test Run Summaries

*   **Business Layer Tests (`Event.Business.Tests.dll`)**:
    ```text
    Passed!  - Failed:     0, Passed:   165, Skipped:     0, Total:   165, Duration: 27 s - Event.Business.Tests.dll (net10.0)
    ```
*   **Data Layer Tests (`Event.Data.Tests.dll`)**:
    ```text
    Passed!  - Failed:     0, Passed:    96, Skipped:     0, Total:    96, Duration: 3.0 s - Event.Data.Tests.dll (net10.0)
    ```

### Detailed Test Logs

> [!IMPORTANT]
> Detailed test reports are written directly to file structures checked into git control:
> *   [server/Event.Business.Tests/test_results.log](server/Event.Business.Tests/test_results.log) (Detailed NUnit execution log for service validation rules).
> *   [server/Event.Data.Tests/test_results.log](server/Event.Data.Tests/test_results.log) (Detailed NUnit execution log for repository integrations).
> 
> When committing edits, please verify these log files are updated cleanly to represent the state of verification records. Ensure no sensitive passwords or private API key parameters are printed in these log files as they are pushed to GitHub.

### Coverage Report Table
The coverage metrics generated using `coverlet` and `dotnet test` are summarized below:

| Module | Line Coverage | Branch Coverage | Method Coverage |
| :--- | :--- | :--- | :--- |
| **Event.Models** | 79.1% | 16.66% | 80.43% |
| **Event.Data** | 0% | 0% | 0% |
| **Event.Business** | **90.09%** | **73.75%** | **96.63%** |
| **Event.Contracts** | 100% | 100% | 100% |
| **Total** | **10.15%** | **66.84%** | **62.64%** |
| **Average** | **67.29%** | **47.6%** | **69.26%** |

> [!NOTE]
> `Event.Data` registers 0% coverage because the database repositories rely on concrete PostgreSQL instances, which are excluded from standard Business logic unit tests to preserve test execution speeds.

---

### Why `Event.Business` is at ~90% (Not 100%)

While all critical code paths are thoroughly validated, achieving 100% coverage is restricted by defensive infrastructure constraints:

1.  **Network and Gateway Catch Blocks**:
    *   `StripePaymentService.cs` and `EmailService.cs` communicate with remote APIs (Stripe and Brevo). Exceptions related to network dropouts, socket timeouts, and API rate-limiting are bypassed as mock interceptors handle the test paths.
2.  **Infinite Background Loops**:
    *   The hosted `BackgroundService` runs continuously on background loops using `Task.Delay`. Invoking these loops directly in tests would cause the test harness to hang indefinitely.
3.  **Unreachable Defensive Guard Clauses**:
    *   Many service-level validation statements check for null parameters that are resolved earlier or guaranteed by database seeding configurations. For instance, the registration guard check `if (activeTerms == null)` will never fire in tests since active terms are seeded during DB setup.
4.  **Catch Blocks for Malformed Encodings**:
    *   The exception catch statement in `PasswordHasher.Verify` only runs if it receives base64 string inputs that are completely corrupted. Standard test cases pass clean strings, keeping these lines from firing.

---

## Roadmap & Future Upgrades

1.  **Stripe Webhooks**: Replace manual API payment check-ins with automated Stripe Webhook events (`payment_intent.succeeded` and `payment_intent.failed`) to improve security.
2.  **Real-Time Notifications**: Integrate SignalR / WebSockets to provide attendees and administrators with instant notifications upon booking approvals, ticket check-ins, or cancellations.
3.  **Advanced Analytics Dashboard**: Introduce visual metrics charts (e.g. daily booking volumes, regional event density mapping, and revenue progression) using Postman Flows or Grafana.
4.  **Elasticsearch Event Engine**: Implement indexed searches for the `/api/event` endpoint to allow deep full-text keyword searches across large-scale event lists.
5.  **Multi-Venue Capacity Checks**: Support booking flows checking for seat allocations across hybrid events (simultaneously tracking physical seat boundaries and virtual connection capacities).
