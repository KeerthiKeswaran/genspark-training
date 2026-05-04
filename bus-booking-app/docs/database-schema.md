# Database Architecture & Schema Specification

This document provides a comprehensive technical breakdown of the Bus Booking System's database architecture. It covers everything from high-level conceptual models to low-level table structures, normalization standards, and performance strategies.
      
---

## System Overview

The Bus Booking System database is designed as a **Domain-Driven Relational Model**. Its primary purpose is to manage the complex lifecycle of inter-city bus travel, encompassing identity management, geographic logistics, inventory scheduling, and financial transactions.

### Core Objectives:
1.  **Identity Isolation**: Separating system admins, bus operators, and customers while sharing a common authentication base.
2.  **Inventory Integrity**: Ensuring that seat availability is accurately tracked in real-time, preventing overbooking.
3.  **Geographic Precision**: Managing routes and specific boarding/dropping hubs within cities.
4.  **Financial Auditability**: Maintaining a strict record of bookings, passenger details, and payment statuses.

---

## Entities & Roles

The system is built around several key entities that interact to form the booking ecosystem:

| Entity | Role |
| :--- | :--- |
| **User** | The base identity. Every person in the system (Customer, Admin, or Operator) is a User. |
| **Bus Operator** | A business entity that owns buses and manages schedules. Requires approval to operate. |
| **Admin** | System administrators who manage platform settings, approve operators, and view global analytics. |
| **Bus** | A physical vehicle with a specific seat capacity and layout configuration. |
| **Route** | A geographic path between two cities (Source and Destination). |
| **Schedule (Trip)** | A concrete instance of a Bus running on a Route at a specific time. |
| **Booking** | A transactional record representing a customer's reservation on a Schedule. |
| **Passenger** | An individual traveler associated with a Booking, assigned to a specific seat. |

---

## Table Catalog

### **1. Identity Domain**

#### **Users**
Stores the core authentication and profile data for all individuals.
- **Id** (`uuid`, PK): Unique identifier.
- **FullName** (`text`): User's legal name.
- **Email** (`text`): Unique email for login.
- **Phone** (`text`): Contact number.
- **PasswordHash** (`text`): Securely hashed password.
- **Role** (`integer`): Enum (0: Customer, 1: Operator, 2: Admin).
- **CreatedAt** (`timestamp`): Account creation time.

#### **Admins**
Extension of the Users table for administrative metadata (TPT Inheritance).
- **Id** (`uuid`, PK, FK -> Users.Id): Shared ID with Users table.
- **IsSuperAdmin** (`boolean`): High-level permissions toggle.
- **CreatedByAdminId** (`uuid`, FK -> Admins.Id): Reference for audit trail.
- **Department** (`text`): Administrative area (e.g., "Finance", "Support").

#### **BusOperators**
Extension of the Users table for operator-specific business data (TPT Inheritance).
- **Id** (`uuid`, PK, FK -> Users.Id): Shared ID with Users table.
- **CompanyName** (`text`): Business name.
- **IsApproved** (`boolean`): Toggle for platform access.
- **Address** (`text`): Business physical location.

---

### **2. Infrastructure Domain**

#### **Cities**
Reference table for geographic locations.
- **Id** (`uuid`, PK): Unique identifier.
- **Name** (`text`): City name.
- **State** (`text`): State/Province name.

#### **Hubs**
Specific boarding and dropping points within a city.
- **Id** (`uuid`, PK): Unique identifier.
- **Name** (`text`): Point name (e.g., "Main Terminal").
- **CityId** (`uuid`, FK -> Cities.Id): The city this hub belongs to.
- **Type** (`integer`): Enum (Boarding, Dropping, or Both).
- **OperatorId** (`uuid`, FK -> BusOperators.Id): Optional ownership of the hub.

#### **Routes**
Defines the connection between cities.
- **Id** (`uuid`, PK): Unique identifier.
- **Source** (`text`): Starting city/location name.
- **Destination** (`text`): Ending city/location name.
- **DistanceKm** (`double precision`): Distance for pricing/time logic.

---

### **3. Fleet & Logistics Domain**

#### **Buses**
Physical vehicle inventory managed by operators.
- **Id** (`uuid`, PK): Unique identifier.
- **BusNumber** (`text`): Registration/License plate number.
- **BusType** (`text`): Category (e.g., "AC Sleeper", "Luxury Semi-Sleeper").
- **TotalSeats** (`integer`): Total capacity.
- **OperatorId** (`uuid`, FK -> BusOperators.Id): Owner of the bus.
- **AssignedRouteId** (`uuid`, FK -> Routes.Id): Preferred route for this bus.
- **LayoutConfig** (`text`): JSON string defining the seat grid.
- **IsApproved** (`boolean`): Admin approval status (Default: `false`).

#### **Schedules**
The "Trips" or "Journeys" available for booking.
- **Id** (`uuid`, PK): Unique identifier.
- **BusId** (`uuid`, FK -> Buses.Id): The vehicle used.
- **RouteId** (`uuid`, FK -> Routes.Id): The path taken.
- **DepartureTime** (`timestamp`): Start time.
- **ArrivalTime** (`timestamp`): Estimated end time.
- **Price** (`numeric`): Base ticket price.
- **AvailableSeats** (`integer`): Real-time inventory count.
- **Status** (`integer`): Enum (0: Scheduled, 1: Completed, 2: Cancelled).
- **BoardingHubIds** (`uuid[]`): Array of IDs for boarding points.
- **DroppingHubIds** (`uuid[]`): Array of IDs for dropping points.

---

### **4. Transaction Domain**

#### **Bookings**
The parent record for a customer transaction.
- **Id** (`uuid`, PK): Unique identifier.
- **CustomerId** (`uuid`, FK -> Users.Id): The user who made the booking.
- **JourneyId** (`uuid`, FK -> Schedules.Id): The trip being booked.
- **Status** (`integer`): Enum (Confirmed, Cancelled).
- **TotalAmount** (`numeric`): Total paid (Seats * Price + Fees).
- **PlatformFee** (`numeric`): Service fee charged by the platform.
- **BoardingHubId** (`uuid`, FK -> Hubs.Id): Selected pickup point.
- **DroppingHubId** (`uuid`, FK -> Hubs.Id): Selected drop-off point.
- **CreatedAt** (`timestamp`): Transaction timestamp.

#### **Passengers**
Individual seat allocations within a booking.
- **Id** (`uuid`, PK): Unique identifier.
- **BookingId** (`uuid`, FK -> Bookings.Id): Parent booking record.
- **SeatNumber** (`text`): The specific seat assigned (e.g., "A1").
- **Name** (`text`): Passenger name.
- **Age** (`integer`): Passenger age.
- **Gender** (`integer`): Enum (M, F, Other).

#### **Payments**
Financial settlement details.
- **Id** (`uuid`, PK): Unique identifier.
- **BookingId** (`uuid`, PK, FK -> Bookings.Id): One-to-One link to Booking.
- **TransactionId** (`text`): External gateway reference.
- **Amount** (`numeric`): Total settled amount.
- **Status** (`integer`): Enum (Pending, Success, Failed, Refunded).
- **ProcessedAt** (`timestamp`): Settlement time.

#### **SeatLocks**
Ephemeral table for managing concurrency during the booking process.
- **Id** (`uuid`, PK): Unique identifier.
- **JourneyId** (`uuid`, FK -> Schedules.Id): The trip.
- **SeatNumber** (`text`): The seat being held.
- **LockedByUserId** (`uuid`, FK -> Users.Id): The user in checkout.
- **ExpiresAt** (`timestamp`): TTL for the lock (e.g., 10 minutes).

---

## Relationship Logic & ER Explanation

### **1. Identity Inheritance (Table-Per-Type)**
The system uses a **One-to-One** relationship between `Users` and its subtypes (`Admins`, `BusOperators`).
- **Reasoning**: This allows for a clean "Single Sign-On" experience where all users exist in the `Users` table for auth, but specific fields like `CompanyName` or `Department` don't clutter the base table.
- **ER View**: `Users (1) <---> (0..1) Admins` and `Users (1) <---> (0..1) BusOperators`.

### **2. The Core Scheduling Triangle**
A `Schedule` is the junction between a `Bus` and a `Route`.
- **Reasoning**: A Bus can have many Schedules over time, and a Route can be serviced by many Buses. This forms a **Many-to-Many** relationship mediated by the `Schedules` table.
- **ER View**: `Buses (1) ---< Schedules >--- (1) Routes`.

### **3. Transactional Hierarchy**
Bookings act as the container for Passengers and Payments.
- **One-to-Many**: One `Booking` can have multiple `Passengers`.
- **One-to-One**: One `Booking` has exactly one `Payment` (enforced by a Unique Index on `BookingId`).
- **ER View**: `Users ---< Bookings ---< Passengers` and `Bookings (1) <---> (1) Payments`.

---

## Data Flow Scenarios

### **Scenario A: Operator Onboarding & Bus Setup**
1.  **Creation**: A record is created in `Users` (Role: Operator) and `BusOperators`.
2.  **Validation**: Admin reviews the `BusOperators` record and sets `IsApproved = true`.
3.  **Inventory**: Operator creates a `Bus`. The record is initially `IsApproved = false`.
4.  **Approval**: Admin approves the `Bus`, making it eligible for scheduling.

### **Scenario B: The Booking Lifecycle**
1.  **Search**: Customer queries `Schedules` via `Routes` (Source/Destination).
2.  **Selection**: Customer selects a seat. A record is inserted into `SeatLocks` to "hold" the seat for 10 minutes.
3.  **Checkout**: Customer provides passenger details and payment.
4.  **Commitment**:
    - A `Booking` record is created.
    - `Passenger` records are created.
    - `Payment` record is created.
    - `Schedule.AvailableSeats` is decremented atomically.
    - The `SeatLock` is removed.
5.  **Completion**: If the `Payment` fails, the `Booking` is marked `Cancelled`, the seats are released back to the `Schedule`, and the seats are freed.

---

## Design Decisions & Normalization Audit

### **Design Rationale**
- **Lookup Tables**: `Cities` and `Hubs` exist to prevent string duplication and ensure geographic consistency (e.g., you can't have a boarding point in a city that doesn't exist).
- **Concurrency Control**: The `SeatLocks` table and unique constraints on `(JourneyId, SeatNumber)` in both `SeatLocks` and `Passengers` are critical business rules enforced at the DB level to prevent "double-booked" seats.
- **Historical Snapshots**: `TotalAmount` and `PlatformFee` are stored in the `Bookings` table rather than calculated on-the-fly. This is because fees or prices might change in the future, and we need to know exactly what the customer paid at the time of booking.

### **Normalization Analysis**

#### **First Normal Form (1NF)**
- **Requirement**: Atomic values, no repeating groups.
- **Evaluation**: **Satisfied**. All columns contain scalar values.
- **Exception**: `Schedules.BoardingHubIds` uses a Postgres UUID Array. While technically a "group," in modern Postgres engineering, arrays are considered atomic types for performance. Strictly speaking, a mapping table would be "more normalized," but this design prioritizes query performance for search results.

#### **Second Normal Form (2NF)**
- **Requirement**: 1NF + No partial dependencies (all non-key attributes depend on the *entire* PK).
- **Evaluation**: **Satisfied**. Every table uses a surrogate `Id` (UUID) as the primary key. All other columns depend solely on this unique ID.

#### **Third Normal Form (3NF)**
- **Requirement**: 2NF + No transitive dependencies (non-key attributes should not depend on other non-key attributes).
- **Evaluation**: **Mostly Satisfied**.
- **Justification**:
    - `Hubs` depend on `Cities`, which is a clean 3NF structure.
    - `Schedules.AvailableSeats` is a **derived field** (denormalization). Technically, this can be calculated by `Bus.TotalSeats - Count(ConfirmedPassengers)`. However, it is stored explicitly to allow for high-speed search filtering without complex aggregations.
    - `Bookings.TotalAmount` is a **historical snapshot**. While derived from `Schedule.Price`, it must be stored to preserve the financial record against future price changes.

---

## Performance & Indexing Strategy

### **Search Optimization**
Indexes are placed on the columns most frequently used in the "Search" flow:
- `Routes(Source, Destination)`: Combined index for route lookup.
- `Schedules(DepartureTime)`: For date-based filtering.
- `Buses(OperatorId)`: For operator dashboard performance.

### **Integrity Constraints**
- **Unique Index**: `IX_SeatLocks_JourneyId_SeatNumber` - Prevents two users from locking the same seat simultaneously.
- **Unique Index**: `IX_Payments_BookingId` - Ensures a booking cannot have duplicate payment records.

### **Trade-offs**
- **TPT Inheritance**: Choosing TPT (Table-Per-Type) for Users/Operators means that fetching a "Profile" requires a JOIN. This is a trade-off: better data integrity and normalization at the cost of slightly more complex READ queries.
- **UUIDs over Integers**: The use of `uuid` for PKs ensures global uniqueness and prevents ID enumeration attacks, though it consumes more storage (16 bytes vs 4 bytes) compared to `serial` integers.
