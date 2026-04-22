# Bus Booking App - Feature-Wise Modular Design

This document outlines the architecture of the Bus Booking Application. The system is segregated into `/client` and `/server` boundaries and organized around **Feature-Wise Modules** based on the business requirements.

## 1. High-Level Folder Structure

The application adopts a vertical slice architecture, grouping files by feature rather than strictly by technical layer.

```text
bus-booking-app/
├── client/                             # Angular Frontend Application
│   ├── src/app/
│   │   ├── core/                       # Interceptors, guards, global state
│   │   ├── shared/                     # Reusable UI components and pipes
│   │   └── features/                   # Feature-based vertical slices
│   │       ├── auth/                   # User & Operator Auth
│   │       ├── search/                 # Bus Discovery
│   │       ├── booking/                # Reservations & History
│   │       ├── payment/                # Checkout & Ticketing
│   │       ├── operator/               # Fleet & Route Management
│   │       └── admin/                  # Platform Oversight
├── server/                             # .NET C# Backend Solution
│   ├── src/
│   │   ├── Core/                       # Shared Domain Entities & Interfaces
│   │   ├── Infrastructure/             # EF Core DbContext, External APIs
│   │   └── Features/                   # Feature-based vertical slices
│   │       ├── Auth/                   
│   │       ├── Search/                 
│   │       ├── Booking/                
│   │       ├── Payment/                
│   │       ├── Operator/               
│   │       └── Admin/                  
├── infrastructure/                     # Docker & Azure configuration
└── docs/                               # System Documentation
```

## 2. Feature-Wise Modules

Each module represents a distinct business capability outlined in the `requirements.md`.

### 2.1. Auth Module (Authentication & Profile)
*   **Description Logic:** Manages user registration, login, and access control. It handles the identification of Customers, Bus Operators, and Admins via simple username/password, phone, or email. It issues and validates session tokens (JWT) and optionally handles SSO.
*   **Responsible Files:**
    *   **Client:** `client/src/app/features/auth/login.component.ts`, `auth.service.ts`, `role.guard.ts`
    *   **Server:** `server/src/Features/Auth/AuthController.cs`, `AuthService.cs`, `TokenProvider.cs`
*   **Data Handled:** User credentials, profile data, JWT tokens.

### 2.2. Search Module (Discovery)
*   **Description Logic:** Allows users (both guest and logged-in) to find available buses. Implements fuzzy logic for pre-populated source/destination fields. Returns a list of buses matching the criteria along with real-time available seat counts.
*   **Responsible Files:**
    *   **Client:** `client/src/app/features/search/search-bar.component.ts`, `bus-listing.component.ts`, `search.service.ts`
    *   **Server:** `server/src/Features/Search/SearchController.cs`, `SearchQueryHandler.cs`
*   **Data Handled:** Search criteria (source, destination, date), Bus availability lists.

### 2.3. Booking Module (Reservations & Management)
*   **Description Logic:** The core module. Renders the visual seat layout reflecting bus capacity (including edge cases like female-only seats). Manages the **Grace Period** by temporarily blocking seats during checkout. Captures passenger details (name, age, gender). Also handles post-booking travel history and the strict 24-hour cancellation rules, including real-time seat unblocking.
*   **Responsible Files:**
    *   **Client:** `client/src/app/features/booking/seat-layout.component.ts`, `passenger-form.component.ts`, `history.component.ts`
    *   **Server:** `server/src/Features/Booking/BookingController.cs`, `SeatLockManager.cs`, `CancellationService.cs`
*   **Data Handled:** Seat block timestamps, passenger demographics, booking statuses.

### 2.4. Payment Module (Checkout & Ticketing)
*   **Description Logic:** Integrates with payment gateways (Stripe/RazorPay or Dummy). Calculates total costs including the admin-configured platform "convenience" fee. Upon successful payment, it finalizes the booking, triggers confirmation notifications (via Event Grid), and generates downloadable tickets.
*   **Responsible Files:**
    *   **Client:** `client/src/app/features/payment/checkout.component.ts`, `ticket-view.component.ts`
    *   **Server:** `server/src/Features/Payment/PaymentController.cs`, `StripeIntegrationService.cs`, `TicketGenerator.cs`
*   **Data Handled:** Payment tokens, transaction IDs, fee calculations.

### 2.5. Operator Module (Bus Operator Capabilities)
*   **Description Logic:** Facilitates the business side for operators. Handles the onboarding workflow (pending admin approval), adding/deactivating buses, uploading custom seat layouts, and selecting point-to-point routes from master locations. It also allows setting flat-rate pricing per seat and viewing revenue analytics.
*   **Responsible Files:**
    *   **Client:** `client/src/app/features/operator/fleet-manager.component.ts`, `route-selector.component.ts`, `revenue-dashboard.component.ts`
    *   **Server:** `server/src/Features/Operator/OperatorController.cs`, `FleetService.cs`, `PricingManager.cs`
*   **Data Handled:** Bus registration numbers, seat layout matrices, operator revenue stats.

### 2.6. Admin Module (Platform Oversight)
*   **Description Logic:** Used by platform owners. Responsible for managing master data (adding valid source and destination cities). Admins review and approve/disable operators here. Handles adjusting global platform fees. Triggers cascading cancellation workflows if an operator is disabled.
*   **Responsible Files:**
    *   **Client:** `client/src/app/features/admin/location-master.component.ts`, `operator-approvals.component.ts`, `fee-settings.component.ts`
    *   **Server:** `server/src/Features/Admin/AdminController.cs`, `MasterDataService.cs`, `OperatorWorkflowService.cs`
*   **Data Handled:** Master location lists, platform fee percentages, operator approval states.

## 3. Technology Choices per Boundary

*   **Client (Angular):** Chosen for its robust structure and reactive capabilities (RxJS). Feature modules are lazy-loaded to improve performance. Reactive Forms handle complex passenger data collection cleanly.
*   **Server (.NET C#):** C# and ASP.NET Core provide a highly performant API.
    *   **PostgreSQL / EF Core:** Entity Framework Core maps C# objects to PostgreSQL tables. PostgreSQL is chosen for handling complex stored procedures and geospatial queries (for locations).
    *   **Azure Event Grid:** Used by the Server to decouple slow tasks (like emailing tickets after payment) from the main user web request.

## 4. Separation of Concerns & Architecture Benefits

### Why the system is split feature-wise
Instead of grouping all Controllers together and all Services together, grouping by feature (e.g., `server/src/Features/Booking/`) ensures that everything related to a specific business capability lives in one place.
*   **Maintainability:** If the Seat Locking logic needs an update, developers only look in the `Booking` folder. They don't have to jump between layers across the entire codebase.
*   **Coupling:** It naturally prevents the `Search` logic from becoming tightly coupled to the `Payment` logic.

## 5. Execution Flow Across Modules

**Example: Canceling a Ticket**
1.  `client/booking` (`history.component.ts`) captures the "Cancel" click.
2.  `client/booking` (Service) makes an HTTP DELETE to the Server.
3.  `server/Booking` (`BookingController.cs`) receives the request.
4.  `server/Booking` (`CancellationService.cs`) checks if the departure is > 24 hours away.
5.  If valid, it asks `server/Infrastructure` (Database) to release the seats to `Available`.
6.  `server/Booking` publishes a `TicketCancelled` event to Azure Event Grid.
7.  `server/Payment` (or a background worker) listens to the event and issues a refund via Stripe.
8.  Client updates UI to show "Cancelled".

## 6. Critical Modules for Stability

*   **`server/Features/Booking/SeatLockManager.cs`:** Must be robust. It handles database concurrency to guarantee two users cannot book the same seat at the same exact time.
*   **`server/Features/Auth`:** Secures the platform. Proper role validation is required so Customers cannot access Operator routes.

## 7. Common Design Mistakes to Avoid

*   **Cross-Feature Contamination:** For example, the `PaymentModule` should not directly alter the `Bus` table. It should only emit events or talk to the `BookingModule` to finalize a state.
*   **Leaky Abstractions:** Returning raw database models from the `server` to the `client`. Always use tailored Data Transfer Objects (DTOs).
*   **Fat Components on Client:** Putting business logic (like checking the 24-hour rule) in Angular UI components. That rule must be strictly enforced on the Server.
