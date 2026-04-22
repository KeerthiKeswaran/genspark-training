# Bus Booking App - Schema & API Contracts

This document outlines the database schema (optimized for PostgreSQL) and the server-side API contracts (REST endpoints and data models) required for the Bus Booking Application.

---

## 1. Database Schema

The database is designed with standard relational principles, ensuring data integrity and allowing for concurrency control during the booking process.

### 1.1. User Management
*   **Users**
    *   `Id` (UUID, PK)
    *   `Role` (Enum: `Admin`, `Operator`, `Customer`)
    *   `FullName` (String)
    *   `Email` (String, Unique)
    *   `Phone` (String, Unique)
    *   `PasswordHash` (String)
    *   `CreatedAt` (Timestamp)

*   **BusOperators** (Extension of Users for Operator specific data)
    *   `UserId` (UUID, PK, FK -> Users.Id)
    *   `CompanyName` (String)
    *   `IsApproved` (Boolean, Default: false)
    *   `Status` (Enum: `Active`, `Disabled`)
    *   `Address` (String) - *Used as presence point*

*   **Admins** (Extension of Users for Administrative metadata)
    *   `UserId` (UUID, PK, FK -> Users.Id)
    *   `IsSuperAdmin` (Boolean, Default: false)
    *   `CreatedByAdminId` (UUID, FK -> Users.Id, Nullable)
    *   `Department` (String)

### 1.2. Core Domain (Buses & Routes)
*   **Locations** (Managed by Admin)
    *   `Id` (Int, PK)
    *   `CityName` (String)
    *   `IsActive` (Boolean)

*   **Buses** (Managed by Operators)
    *   `Id` (UUID, PK)
    *   `OperatorId` (UUID, FK -> BusOperators.UserId)
    *   `RegistrationNumber` (String, Unique)
    *   `TotalCapacity` (Int)
    *   `LayoutConfig` (JSONB) - *Defines grid, aisles, female-only designations*
    *   `IsActive` (Boolean)

*   **Routes** (Point-to-Point configured by Operators)
    *   `Id` (UUID, PK)
    *   `OperatorId` (UUID, FK -> BusOperators.UserId)
    *   `SourceLocationId` (Int, FK -> Locations.Id)
    *   `DestinationLocationId` (Int, FK -> Locations.Id)
    *   `FlatPrice` (Decimal) - *Operator sets the price*

*   **Journeys (Schedules)**
    *   `Id` (UUID, PK)
    *   `BusId` (UUID, FK -> Buses.Id)
    *   `RouteId` (UUID, FK -> Routes.Id)
    *   `DepartureTime` (Timestamp)
    *   `ArrivalTime` (Timestamp)
    *   `Status` (Enum: `Scheduled`, `Completed`, `Cancelled`)

### 1.3. Booking & Locking
*   **SeatLocks** (Handles the Grace Period)
    *   `Id` (UUID, PK)
    *   `JourneyId` (UUID, FK -> Journeys.Id)
    *   `SeatNumber` (String)
    *   `LockedByUserId` (UUID, FK -> Users.Id)
    *   `ExpiresAt` (Timestamp)
    *   *Constraint: Unique(JourneyId, SeatNumber)* - Prevents double locking.

*   **Bookings**
    *   `Id` (UUID, PK)
    *   `CustomerId` (UUID, FK -> Users.Id)
    *   `JourneyId` (UUID, FK -> Journeys.Id)
    *   `BookingStatus` (Enum: `Confirmed`, `Cancelled`)
    *   `TotalAmount` (Decimal)
    *   `PlatformFee` (Decimal)
    *   `CreatedAt` (Timestamp)

*   **Passengers**
    *   `Id` (UUID, PK)
    *   `BookingId` (UUID, FK -> Bookings.Id)
    *   `SeatNumber` (String)
    *   `Name` (String)
    *   `Age` (Int)
    *   `Gender` (Enum: `M`, `F`, `Other`)

*   **Payments**
    *   `Id` (UUID, PK)
    *   `BookingId` (UUID, FK -> Bookings.Id)
    *   `TransactionId` (String) - *From Stripe/Razorpay*
    *   `Amount` (Decimal)
    *   `Status` (Enum: `Success`, `Failed`, `Refunded`)
    *   `ProcessedAt` (Timestamp)

---

## 2. Server API Contracts

### 2.1. Authentication (`/api/auth`)
**POST `/login`**
*   **Request:** `{ "email": "user@test.com", "password": "..." }`
*   **Response (200):** `{ "token": "jwt...", "role": "Customer", "userId": "uuid" }`

**POST `/register`**
*   **Request:** `{ "name": "...", "email": "...", "password": "...", "phone": "...", "role": "Customer|Operator" }`
*   **Response (201):** `{ "userId": "uuid" }`

### 2.2. Search (`/api/search`)
**GET `/buses`**
*   **Query Params:** `?sourceId=1&destinationId=2&date=2024-12-01`
*   **Response (200):**
```json
[
  {
    "journeyId": "uuid",
    "operatorName": "Express Travels",
    "departureTime": "2024-12-01T10:00:00Z",
    "arrivalTime": "2024-12-01T14:00:00Z",
    "price": 500.00,
    "availableSeats": 24,
    "totalCapacity": 40
  }
]
```

### 2.3. Booking (`/api/booking`)
**GET `/layout/{journeyId}`**
*   **Response (200):**
```json
{
  "layoutConfig": { /* grid definition */ },
  "unavailableSeats": [
     { "seatNumber": "A1", "status": "Booked" },
     { "seatNumber": "A2", "status": "Blocked" }
  ]
}
```

**POST `/lock-seats`** (Initiates Grace Period)
*   **Request:** `{ "journeyId": "uuid", "seatNumbers": ["B1", "B2"] }`
*   **Response (200):** `{ "lockId": "uuid", "expiresAt": "2024-12-01T09:05:00Z" }`
*   **Error (409):** `{ "message": "One or more seats are already taken." }`

**POST `/confirm`**
*   **Request:** 
```json
{
  "lockId": "uuid",
  "paymentToken": "tok_123",
  "passengers": [
    { "seatNumber": "B1", "name": "John", "age": 30, "gender": "M" },
    { "seatNumber": "B2", "name": "Jane", "age": 28, "gender": "F" }
  ]
}
```
*   **Response (201):** `{ "bookingId": "uuid", "ticketUrl": "..." }`

**DELETE `/{bookingId}`** (Cancellation)
*   **Response (200):** `{ "message": "Booking cancelled. Refund of 80% initiated." }`

### 2.4. Operator (`/api/operator`)
**POST `/buses`**
*   **Request:** `{ "registrationNumber": "...", "totalCapacity": 40, "layoutConfig": { ... } }`
*   **Response (201):** `{ "busId": "uuid" }`

**POST `/routes`**
*   **Request:** `{ "sourceLocationId": 1, "destinationLocationId": 2, "flatPrice": 600.00 }`
*   **Response (201):** `{ "routeId": "uuid" }`

### 2.5. Admin (`/api/admin`)
**POST `/locations`**
*   **Request:** `{ "cityName": "Chennai" }`
*   **Response (201):** `{ "locationId": 1 }`

**PUT `/operators/{operatorId}/status`**
*   **Request:** `{ "status": "Active" | "Disabled" }`
*   **Response (200):** `{ "message": "Operator status updated" }`

**POST `/register-admin`** (Internal/Restricted)
*   **Request:** `{ "name": "...", "email": "...", "password": "...", "department": "..." }`
*   **Response (201):** `{ "userId": "uuid" }`
