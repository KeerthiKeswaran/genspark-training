# Backend Overview & Query Mapping

This document provides a comprehensive overview of the backend structure, detailing the role of every API endpoint in the bus booking application. It also maps the Entity Framework (EF) Core queries used within these endpoints to their equivalent PostgreSQL queries.

## Overview of Backend Roles

The backend is structured into modular feature-based controllers. Each controller handles a specific domain of the application:
- **Auth**: Manages user registration and login, including role-specific profile creation.
- **Admin**: Handles system-wide settings, master data management (cities, hubs, routes), operator/fleet approvals, and dashboard analytics.
- **Operator**: Provides endpoints for bus operators to manage their fleets, schedule trips, view statistics, and request re-evaluations.
- **Booking**: Manages the core reservation flow, including seat locking, booking confirmation, retrieving passenger history, and cancellations.
- **Search**: Facilitates discovering available trips based on source, destination, and date parameters.
- **Notifications**: Handles the retrieval, marking, and clearing of user notifications.
- **Locations**: Provides lightweight endpoints to fetch available cities and boarding/dropping hubs.

---

## 1. AuthController (`AuthController.cs`)

### `POST /api/Auth/register`
**Role & Explanation:**
This endpoint registers a new user into the platform. It checks if the email is already registered and creates a specialized entity based on the role (e.g., `Admin`, `BusOperator`, or a standard `User`). 

**PostgreSQL Mapping:**
```sql
-- Check if user exists
SELECT EXISTS (
    SELECT 1 FROM "Users" WHERE "Email" = @Email
);

-- Insert new user (Example for standard user)
INSERT INTO "Users" ("FullName", "Email", "Phone", "Role", "PasswordHash") 
VALUES (@FullName, @Email, @Phone, @Role, @PasswordHash) 
RETURNING "Id";
```
*(Note: If the user is an Operator, EF Core handles Table-Per-Type inheritance, effectively inserting into both `Users` and `BusOperators` tables).*

### `POST /api/Auth/login`
**Role & Explanation:**
Authenticates a user based on their email and password. If successful, it generates a JWT token. For operators, it additionally retrieves their approval status to include in the response.

**PostgreSQL Mapping:**
```sql
-- Fetch the user
SELECT * FROM "Users" WHERE "Email" = @Email LIMIT 1;

-- Fetch operator specific details if the user is an operator
SELECT * FROM "BusOperators" WHERE "Id" = @UserId LIMIT 1;
```

---

## 2. AdminController (`AdminController.cs`)

### `POST /api/Admin/cities`
**Role & Explanation:** Adds a new city to the master data records.
**PostgreSQL Mapping:**
```sql
INSERT INTO "Cities" ("Name", "State") 
VALUES (@Name, @State) RETURNING "Id";
```

### `POST /api/Admin/hubs`
**Role & Explanation:** Adds a new boarding or dropping hub for a specific city. Validates that the referenced city exists first.
**PostgreSQL Mapping:**
```sql
-- Validate City
SELECT EXISTS (SELECT 1 FROM "Cities" WHERE "Id" = @CityId);

-- Insert Hub
INSERT INTO "Hubs" ("Name", "CityId", "Type") 
VALUES (@Name, @CityId, @Type) RETURNING "Id";
```

### `GET /api/Admin/routes`
**Role & Explanation:** Retrieves a list of all defined routes with minimal information.
**PostgreSQL Mapping:**
```sql
SELECT "Id", "Source", "Destination", "DistanceKm" 
FROM "Routes";
```

### `POST /api/Admin/routes`
**Role & Explanation:** Creates a new route between two cities with an approximate distance.
**PostgreSQL Mapping:**
```sql
INSERT INTO "Routes" ("Source", "Destination", "DistanceKm") 
VALUES (@Source, @Destination, @DistanceKm) RETURNING "Id";
```

### `GET /api/Admin/operators`
**Role & Explanation:** Retrieves a list of all bus operators, prioritizing those with a "Pending" approval status so administrators can easily review them.
**PostgreSQL Mapping:**
```sql
SELECT "Id", "FullName", "CompanyName", "Email", "Phone", "IsApproved", "Status", "RejectionReason", "CreatedAt" 
FROM "BusOperators"
ORDER BY 
    CASE WHEN "Status" = 0 THEN 0 ELSE 1 END ASC, 
    "CreatedAt" DESC;
```

### `PUT /api/Admin/operators/{id}/approve`
**Role & Explanation:** Approves an operator's account and automatically approves any pending buses registered by that operator.
**PostgreSQL Mapping:**
```sql
-- Fetch Operator
SELECT * FROM "BusOperators" WHERE "Id" = @Id LIMIT 1;

-- Fetch pending buses for this operator
SELECT * FROM "Buses" 
WHERE "OperatorId" = @Id AND "IsApproved" = FALSE AND "Status" = 0;

-- Update Operator
UPDATE "BusOperators" 
SET "IsApproved" = TRUE, "Status" = 1, "RejectionReason" = NULL 
WHERE "Id" = @Id;

-- Update Buses
UPDATE "Buses" 
SET "IsApproved" = TRUE, "Status" = 1, "RejectionReason" = NULL 
WHERE "OperatorId" = @Id AND "IsApproved" = FALSE AND "Status" = 0;
```

### `PUT /api/Admin/operators/{id}/deny`
**Role & Explanation:** Rejects an operator's registration request with a specific reason. Simultaneously denies all their pending buses.
**PostgreSQL Mapping:**
```sql
-- Update Operator
UPDATE "BusOperators" 
SET "IsApproved" = FALSE, "Status" = 2, "RejectionReason" = @Reason 
WHERE "Id" = @Id;

-- Update Buses
UPDATE "Buses" 
SET "IsApproved" = FALSE, "Status" = 2, "RejectionReason" = @BusReason 
WHERE "OperatorId" = @Id AND "IsApproved" = FALSE AND "Status" = 0;
```

### `GET /api/Admin/buses/pending`
**Role & Explanation:** Retrieves all buses that are currently waiting for admin approval, joining with the Operator table to display the operator's name.
**PostgreSQL Mapping:**
```sql
SELECT b."Id", b."BusNumber", b."BusType", b."TotalSeats", 
       o."FullName" AS "OperatorName", o."CompanyName", 
       b."IsApproved", b."Status", b."RejectionReason" 
FROM "Buses" b 
LEFT JOIN "BusOperators" o ON b."OperatorId" = o."Id" 
WHERE b."IsApproved" = FALSE 
ORDER BY b."Id" DESC;
```

### `PUT /api/Admin/buses/{id}/approve`
**Role & Explanation:** Approves a specific bus for service. It first checks if the bus's owner (operator) is already approved.
**PostgreSQL Mapping:**
```sql
-- Check bus and operator
SELECT b.*, o.* 
FROM "Buses" b 
LEFT JOIN "BusOperators" o ON b."OperatorId" = o."Id" 
WHERE b."Id" = @Id LIMIT 1;

-- Update bus status
UPDATE "Buses" 
SET "IsApproved" = TRUE, "Status" = 1, "RejectionReason" = NULL 
WHERE "Id" = @Id;
```

### `PUT /api/Admin/buses/{id}/deny`
**Role & Explanation:** Rejects a specific bus registration with a provided reason.
**PostgreSQL Mapping:**
```sql
UPDATE "Buses" 
SET "IsApproved" = FALSE, "Status" = 2, "RejectionReason" = @Reason 
WHERE "Id" = @Id;
```

### `GET /api/Admin/settings/fee` & `PUT /api/Admin/settings/fee`
**Role & Explanation:** Gets or updates the platform's global configurations (like flat booking fees or percentage-based commission rates).
**PostgreSQL Mapping:**
```sql
-- GET
SELECT * FROM "GlobalConfigurations" LIMIT 1;

-- PUT (Update)
UPDATE "GlobalConfigurations" 
SET "PlatformFeeType" = @FeeType, 
    "PlatformFeeValue" = @FeeValue, 
    "OperatorCommissionPercentage" = @CommissionPercentage, 
    "LastUpdated" = @LastUpdated 
WHERE "Id" = @Id;
```

### `GET /api/Admin/stats`
**Role & Explanation:** Computes platform-wide analytics for the admin dashboard. Aggregates data about completed/upcoming bookings, generated revenue, total operators, and a feed of recent trips.
**PostgreSQL Mapping:**
```sql
-- Total valid bookings count
SELECT COUNT(*) 
FROM "Bookings" b 
INNER JOIN "Payments" p ON b."Id" = p."BookingId" 
INNER JOIN "Schedules" s ON b."JourneyId" = s."Id" 
WHERE b."Status" = 1 AND p."Status" = 1 AND s."DepartureTime" >= @start AND s."DepartureTime" <= @end;

-- Revenue summations
SELECT SUM(b."TotalAmount") FROM ...;
SELECT SUM(b."PlatformFee") FROM ...;

-- Recent Trips Feed
SELECT s."Id" AS "ScheduleId", r."Source", r."Destination", 
       o."CompanyName" AS "Operator", s."DepartureTime", s."ArrivalTime", 
       s."Status", s."Price", b."TotalSeats" AS "MaxSeats" 
FROM "Schedules" s 
LEFT JOIN "Routes" r ON s."RouteId" = r."Id" 
LEFT JOIN "Buses" b ON s."BusId" = b."Id" 
LEFT JOIN "BusOperators" o ON b."OperatorId" = o."Id" 
WHERE s."DepartureTime" >= @start AND s."DepartureTime" <= @end 
ORDER BY s."DepartureTime" DESC 
LIMIT 1000;
```

---

## 3. OperatorController (`OperatorController.cs`)

### `GET /api/Operator/{operatorId}/stats`
**Role & Explanation:** Computes performance metrics for a specific operator, including total buses, active schedules, and overall revenue generated in the last 30 days.
**PostgreSQL Mapping:**
```sql
-- Get Operator's buses
SELECT * FROM "Buses" WHERE "OperatorId" = @OperatorId;

-- Get recent schedules
SELECT s.*, b.*, r.* 
FROM "Schedules" s 
INNER JOIN "Buses" b ON s."BusId" = b."Id" 
LEFT JOIN "Routes" r ON s."RouteId" = r."Id" 
WHERE b."OperatorId" = @OperatorId AND s."DepartureTime" > @pastDate 
ORDER BY s."DepartureTime" DESC;

-- Get bookings for those schedules
SELECT b.*, p.* 
FROM "Bookings" b 
LEFT JOIN "Passengers" p ON b."Id" = p."BookingId" 
WHERE b."JourneyId" = ANY(@scheduleIds) AND b."Status" = 1;
```

### `GET /api/Operator/{operatorId}/buses`
**Role & Explanation:** Retrieves the fleet (list of buses) belonging to a given operator.
**PostgreSQL Mapping:**
```sql
SELECT * FROM "Buses" WHERE "OperatorId" = @OperatorId;
```

### `POST /api/Operator/{operatorId}/buses`
**Role & Explanation:** Registers a new bus for an operator. It ensures the bus number is unique, manages operator profile state, and submits the bus as "Pending" for Admin approval.
**PostgreSQL Mapping:**
```sql
-- Check for existing bus number
SELECT * FROM "Buses" WHERE "BusNumber" = @BusNumber LIMIT 1;

-- Raw SQL executed directly in the backend to handle TPT profile generation if it went missing
INSERT INTO "BusOperators" ("Id", "CompanyName", "IsApproved", "Address", "Status") 
VALUES (@Id, @CompanyName, FALSE, '', 0);

-- Insert new bus
INSERT INTO "Buses" ("BusNumber", "BusType", "TotalSeats", "LayoutConfig", "OperatorId", "IsApproved", "Status") 
VALUES (@BusNumber, @BusType, @TotalSeats, @LayoutConfig, @OperatorId, FALSE, 0) 
RETURNING "Id";
```

### `GET /api/Operator/{operatorId}/schedules`
**Role & Explanation:** Retrieves all trip schedules configured by the operator, joined with route and bus data.
**PostgreSQL Mapping:**
```sql
SELECT s.*, b.*, r.* 
FROM "Schedules" s 
INNER JOIN "Buses" b ON s."BusId" = b."Id" 
LEFT JOIN "Routes" r ON s."RouteId" = r."Id" 
WHERE b."OperatorId" = @OperatorId 
ORDER BY s."DepartureTime" DESC;
```

### `POST /api/Operator/{operatorId}/schedules`
**Role & Explanation:** Creates a new trip schedule for a selected route and bus. It also auto-assigns boarding and dropping hubs if none were provided.
**PostgreSQL Mapping:**
```sql
-- Find Default Hubs
SELECT "Id" FROM "Hubs" 
WHERE "CityId" = @CityId AND ("Type" = 0 OR "Type" = 2) 
LIMIT 2;

-- Insert Schedule
INSERT INTO "Schedules" ("BusId", "RouteId", "DepartureTime", "ArrivalTime", "Price", "AvailableSeats", "Status", "BoardingHubIds", "DroppingHubIds") 
VALUES (@BusId, @RouteId, @DepartureTime, @ArrivalTime, @Price, @AvailableSeats, 0, @BoardingIds, @DroppingIds) 
RETURNING "Id";
```

### `PUT /api/Operator/{operatorId}/schedules/{scheduleId}/cancel`
**Role & Explanation:** Cancels a scheduled trip. If customers have already booked tickets, this flags their bookings as cancelled and dispatches notifications.
**PostgreSQL Mapping:**
```sql
-- Find schedule
SELECT s.*, b.* 
FROM "Schedules" s 
INNER JOIN "Buses" b ON s."BusId" = b."Id" 
WHERE s."Id" = @ScheduleId AND b."OperatorId" = @OperatorId 
LIMIT 1;

-- Find affected bookings
SELECT b.*, j.*, r.* 
FROM "Bookings" b 
INNER JOIN "Schedules" j ON b."JourneyId" = j."Id" 
LEFT JOIN "Routes" r ON j."RouteId" = r."Id" 
WHERE b."JourneyId" = @ScheduleId AND b."Status" = 1;

-- Updates applied during SaveChanges
UPDATE "Schedules" SET "Status" = 3 WHERE "Id" = @ScheduleId;
UPDATE "Bookings" SET "Status" = 2 WHERE "Id" = ANY(@affectedBookingIds);
```

### `DELETE /api/Operator/{operatorId}/buses/{busId}`
**Role & Explanation:** Deletes a bus from the system, provided it has no booking history or active future schedules.
**PostgreSQL Mapping:**
```sql
-- Verify no bookings exist
SELECT EXISTS (
    SELECT 1 FROM "Bookings" b 
    INNER JOIN "Schedules" s ON b."JourneyId" = s."Id" 
    WHERE s."BusId" = @BusId
);

-- Delete bus
DELETE FROM "Buses" WHERE "Id" = @BusId AND "OperatorId" = @OperatorId;
```

### `PUT /api/Operator/{operatorId}/request-review`
**Role & Explanation:** Allows rejected operators to manually request an account review from the admin.
**PostgreSQL Mapping:**
```sql
UPDATE "BusOperators" 
SET "Status" = 0, "IsApproved" = FALSE, "RejectionReason" = 'Manual Re-review Requested' 
WHERE "Id" = @OperatorId AND "Status" = 2;
```

---

## 4. BookingController (`BookingController.cs`)

### `GET /api/Booking/layout/{journeyId}`
**Role & Explanation:** Provides seat availability for a specific trip. It combines confirmed bookings and temporary seat locks to return unavailable seats, and queries the hubs for the trip.
**PostgreSQL Mapping:**
```sql
-- Fetch Journey details
SELECT s.*, b.*, o.*, r.* 
FROM "Schedules" s 
LEFT JOIN "Buses" b ON s."BusId" = b."Id" 
LEFT JOIN "BusOperators" o ON b."OperatorId" = o."Id" 
LEFT JOIN "Routes" r ON s."RouteId" = r."Id" 
WHERE s."Id" = @JourneyId LIMIT 1;

-- Fetch booked seats
SELECT p."SeatNumber", p."Gender" 
FROM "Passengers" p 
INNER JOIN "Bookings" b ON p."BookingId" = b."Id" 
WHERE b."JourneyId" = @JourneyId AND b."Status" = 1;

-- Fetch temporarily locked/blocked seats
SELECT "SeatNumber" 
FROM "SeatLocks" 
WHERE "JourneyId" = @JourneyId AND "ExpiresAt" > @UtcNow;
```

### `POST /api/Booking/lock-seats`
**Role & Explanation:** Temporarily reserves seats for a user during the checkout flow to prevent double-booking.
*(Relies on atomic inserts and transaction locks abstractly managed by `_seatLockManager`).*

### `POST /api/Booking/confirm`
**Role & Explanation:** Finalizes a booking transaction. Validates seat locks and strict gender adjacency rules. It calculates final pricing with fees, generates a mock payment record, decreases available seats on the trip, and frees up the temporary seat locks.
**PostgreSQL Mapping:**
```sql
-- Validate Seat Locks
SELECT * FROM "SeatLocks" 
WHERE "JourneyId" = @JourneyId AND "SeatNumber" = ANY(@SeatNumbers) 
  AND "LockedByUserId" = @UserId AND "ExpiresAt" > @UtcNow;

-- Insert Booking and Payment
INSERT INTO "Bookings" ("CustomerId", "JourneyId", "TotalAmount", "PlatformFee", "Status", "BoardingHubId", "DroppingHubId") 
VALUES (...);

INSERT INTO "Passengers" ("BookingId", "SeatNumber", "Name", "Age", "Gender") 
VALUES (...);

INSERT INTO "Payments" ("BookingId", "Amount", "TransactionId", "Status", "ProcessedAt") 
VALUES (...);

-- Update Schedule Seats and Remove Locks
UPDATE "Schedules" SET "AvailableSeats" = @AvailableSeats WHERE "Id" = @JourneyId;
DELETE FROM "SeatLocks" WHERE "Id" = ANY(@lockIds);
```

### `GET /api/Booking/history`
**Role & Explanation:** Fetches all past and upcoming bookings for the authenticated user, assembling complex nested data including passengers and hubs.
**PostgreSQL Mapping:**
```sql
SELECT b.*, j.*, r.*, bus.*, op.*, p.*, bh.*, dh.* 
FROM "Bookings" b 
LEFT JOIN "Schedules" j ON b."JourneyId" = j."Id" 
LEFT JOIN "Routes" r ON j."RouteId" = r."Id" 
LEFT JOIN "Buses" bus ON j."BusId" = bus."Id" 
LEFT JOIN "BusOperators" op ON bus."OperatorId" = op."Id" 
LEFT JOIN "Passengers" p ON b."Id" = p."BookingId" 
LEFT JOIN "Hubs" bh ON b."BoardingHubId" = bh."Id" 
LEFT JOIN "Hubs" dh ON b."DroppingHubId" = dh."Id" 
WHERE b."CustomerId" = @UserId 
ORDER BY b."CreatedAt" DESC;
```

---

## 5. SearchController (`SearchController.cs`)

### `GET /api/Search`
**Role & Explanation:** Executes a search for available buses traveling between two points on a given date. Filters out inactive schedules and ensures departures are at least 10 minutes in the future.
**PostgreSQL Mapping:**
```sql
SELECT s."Id", b."BusNumber", b."BusType", o."CompanyName", o."Address", 
       r."Source", r."Destination", s."DepartureTime", s."ArrivalTime", 
       s."Price", s."AvailableSeats" 
FROM "Schedules" s 
INNER JOIN "Buses" b ON s."BusId" = b."Id" 
INNER JOIN "BusOperators" o ON b."OperatorId" = o."Id" 
INNER JOIN "Routes" r ON s."RouteId" = r."Id" 
WHERE r."Source" ILIKE @FromTerm 
  AND r."Destination" ILIKE @ToTerm 
  AND s."DepartureTime" >= @StartDate 
  AND s."DepartureTime" < @EndDate 
  AND s."DepartureTime" >= @MinDeparture 
  AND s."Status" = 1;
```

### `GET /api/Search/cities`
**Role & Explanation:** Returns an autocomplete list of unique cities that have active routes. Uses an in-memory cache to reduce load on the database.
**PostgreSQL Mapping:**
```sql
-- Union query executed when cache is missed
SELECT DISTINCT "Source" AS "CityName" FROM "Routes"
UNION
SELECT DISTINCT "Destination" AS "CityName" FROM "Routes"
ORDER BY "CityName";
```

---

## 6. NotificationsController (`NotificationsController.cs`)

### `GET /api/Notifications/user/{userId}`
**Role & Explanation:** Retrieves the 20 most recent notifications for a user.
**PostgreSQL Mapping:**
```sql
SELECT * FROM "Notifications" 
WHERE "UserId" = @UserId 
ORDER BY "CreatedAt" DESC 
LIMIT 20;
```

### `PUT /api/Notifications/{id}/read`
**Role & Explanation:** Marks a specific notification as read.
**PostgreSQL Mapping:**
```sql
UPDATE "Notifications" 
SET "IsRead" = TRUE 
WHERE "Id" = @Id;
```

### `DELETE /api/Notifications/user/{userId}/clear`
**Role & Explanation:** Deletes all notifications associated with a given user.
**PostgreSQL Mapping:**
```sql
DELETE FROM "Notifications" 
WHERE "UserId" = @UserId;
```

---

## 7. LocationsController (`LocationsController.cs`)

### `GET /api/Locations/cities`
**Role & Explanation:** Simple endpoint to retrieve an ordered list of all cities.
**PostgreSQL Mapping:**
```sql
SELECT "Id", "Name", "State" 
FROM "Cities" 
ORDER BY "Name";
```

### `GET /api/Locations/hubs`
**Role & Explanation:** Retrieves hubs belonging to a particular city, typically used to populate dropdowns in the UI.
**PostgreSQL Mapping:**
```sql
SELECT "Id", "Name", "Type" 
FROM "Hubs" 
WHERE "CityId" = @CityId 
ORDER BY "Name";
```
