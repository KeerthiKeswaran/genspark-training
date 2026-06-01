# Database Schema, API Contract & Query Mapping Specification

This document combines the database schema contract and query mapping specifications for the Bus Booking System. It defines the formal contract between the database layer, the repository/business service layer, and the API layer, serving as an implementation-level blueprint for how data is structured, validated, and transformed across the system. It also provides a comprehensive overview of the backend structure, detailing the role of every API endpoint, the Entity Framework (EF) Core code used for database operations (separated into Controllers, Services, and Repositories under the layered architecture), and maps them to their equivalent PostgreSQL queries.

---

## 1. Database Schema & Architecture Blueprint

### Philosophy of Schema Design

The system follows a domain-driven relational model designed to balance data integrity with high-performance real-time operations. The core philosophy is to enforce strict normalization for static data (Cities, Routes, Bus Specifications) while utilizing controlled redundancy and status-based state machines for dynamic transactional data (Schedules, Bookings, Payments).

The schema is divided into four primary domains:
1. **Identity Domain**: Handles Users, Operators, and Admins via Table-Per-Type (TPT) inheritance, ensuring shared authentication credentials while maintaining isolated profile data.
2. **Infrastructure Domain**: Manages Cities, Hubs (Boarding/Dropping points), and Routes, providing the geographic foundation for the platform.
3. **Fleet & Logistics Domain**: Covers Buses and Schedules (Trips), where physical vehicles are linked to routes and time slots.
4. **Transaction Domain**: The most critical layer, managing Bookings, Passengers, and Payments. This domain utilizes state-driven logic (e.g., Journey Status and Booking Status) to track the lifecycle of a seat from selection to completion.

---

### Core Table Descriptions

#### Schedules (Trips)
The `Schedules` table is the engine of the platform. It defines the availability of a specific bus on a specific route at a specific time. It stores pricing, departure/arrival timestamps, and the current `JourneyStatus` (Scheduled, Completed, or Cancelled). This table is responsible for inventory management; as bookings are confirmed, the `AvailableSeats` column is updated to reflect real-time capacity.

#### Bookings
The `Bookings` table manages the lifecycle of a customer transaction. It tracks the `BookingStatus` (Confirmed or Cancelled) and serves as the parent record for all passengers in a single transaction. It stores financial metadata, including the `TotalAmount`, `PlatformFee`, and `OperatorCommission`, acting as the primary source of truth for revenue auditing.

#### Buses
The `Buses` table stores physical vehicle data, including registration numbers and seat counts. Most importantly, it contains the `LayoutConfig`, a JSON-serialized grid that defines the physical arrangement of seats. This layout is used by the frontend to render the interactive seat selection map.

#### Hubs (Boarding & Dropping Points)
The `Hubs` table stores specific locations within a city where a bus stops. Each hub is categorized as a "Boarding" or "Dropping" point. During trip creation, the system automatically links the most relevant hubs to a schedule, providing customers with clear instructions on where to meet the vehicle.

---

### Interconnections and Relationships

Data flows through the system via a chain of Foreign Key (FK) relationships that ensure referential integrity.
* **Search Flow**: A search request queries `Routes` filtered by Source/Destination cities, then joins with `Schedules` to find active trips on a specific date.
* **Booking Flow**: A `Booking` record is linked to a `Schedule` (the journey) and a `User` (the customer). Each booking contains one or more `Passengers`, each mapped to a specific `SeatNumber`.
* **Revenue Flow**: When a payment is processed, the `Payments` table links to a `Booking`, which in turn links to a `Schedule`. This allows the Admin panel to aggregate revenue by summing the `PlatformFee` across all confirmed bookings for a specific period.

**Indexes and Constraints**:
To prevent race conditions and overbooking, a Unique Constraint is enforced on the combination of `ScheduleId` and `SeatNumber` within the `Passengers` table. Additionally, high-frequency indexes are placed on `Schedules(DepartureTime)` and `Routes(Source, Destination)` to ensure sub-second search responses.

---

### API-to-Database Mapping (Conceptual)

#### Search (GET /api/Search)
This endpoint queries the `Schedules` table, joined with `Routes` and `Buses`. It applies filters for `Source`, `Destination`, and `Date`. Before returning results, the business logic filters out any schedules with a `Cancelled` status or a departure time in the past.

#### Seat Selection (GET /api/Booking/layout/{id})
This endpoint fetches the `LayoutConfig` from the `Buses` table via the `ScheduleId`. It then queries the `Passengers` table for all existing bookings associated with that schedule to determine which seats are "Taken." The API response is a merged object of the physical layout and the current availability state.

#### Booking Confirmation (POST /api/Booking/confirm)
This is a multi-table atomic operation. It creates a `Booking` record, inserts multiple `Passenger` records, and updates the `AvailableSeats` in the `Schedules` table. If the payment fails, the transaction is rolled back, ensuring no "ghost" bookings are left in the database.

---

### The API/Database Contract

The contract between these layers is governed by two strict rules:
1. **DTO Isolation**: Database entities are never exposed directly to the frontend. DTOs (Data Transfer Objects) are used to map only the necessary fields, ensuring that internal DB structures (like password hashes or internal status codes) remain hidden.
2. **Server-Side Re-validation**: No critical data from the client (like `Price`) is trusted. When a booking is submitted, the API re-fetches the price from the `Schedules` table and re-calculates the `TotalAmount` on the server to prevent price manipulation.

---

### Edge Cases & Failure Scenarios

* **Race Conditions**: If two users attempt to book the same seat simultaneously, the database's Unique Constraint on `ScheduleId + SeatNumber` will trigger a conflict, and the second user will receive a "Seat already taken" error, preventing duplicate sales.
* **Expired Bookings**: If a payment is not received within a set timeout, the system uses a background task to mark the booking as "Expired" and releases the blocked seats back into the available inventory.
* **Concurrency**: All updates to `AvailableSeats` use atomic decrement operations to ensure that the seat count remains accurate even under heavy concurrent load.

---

### Cohesive System Understanding

Every feature in the platform follows a traceable path:
`UI Request → DTO Mapping → Service Logic (Validation) → EF Core Query (DB Operation) → Result Transformation → API Response.`

This layered approach ensures that the database remains the immutable source of truth, while the API acts as a sophisticated guardian that validates business rules and transforms raw data into a premium user experience.

---

## 2. Overview of Backend Roles & Layered Architecture

The backend is structured into modular feature-based layers. Under the multi-layered architecture, each feature is divided into:
- **API (Controllers)**: Receives HTTP requests, maps them to and from DTOs, and delegates execution to Business Services.
- **Business (Services)**: Contains the core business logic, validation rules, state transitions, and coordination between different repositories. Decoupled completely from direct database context interactions.
- **Contracts (Interfaces)**: Defines contracts for repositories and services to enable dependency injection and mock testing.
- **Data (Repositories & Contexts)**: Encapsulates EF Core operations and performs actual queries/mutations against PostgreSQL.
- **Models (Entities & DTOs)**: Defines the database schema models and request/response payloads.

Each controller handles a specific domain of the application:
- **Auth**: Manages user registration and login, including role-specific profile creation.
- **Admin**: Handles system-wide settings, master data management (cities, hubs, routes), operator/fleet approvals, and dashboard analytics.
- **Operator**: Provides endpoints for bus operators to manage their fleets, schedule trips, view statistics, and request re-evaluations.
- **Booking**: Manages the core reservation flow, including seat locking, booking confirmation, retrieving passenger history, and cancellations.
- **Search**: Facilitates discovering available trips based on source, destination, and date parameters.
- **Notifications**: Handles the retrieval, marking, and clearing of user notifications.
- **Locations**: Provides lightweight endpoints to fetch available cities and boarding/dropping hubs.

---

## 3. Query Mapping & Layer-Wise Code Specification

This section provides the C# code blocks representing the controller actions, business service methods, and repository implementations (matching the layered architecture), followed by the equivalent raw PostgreSQL SQL query.

---

### 1. AuthController (`AuthController.cs`)

#### `POST /api/Auth/register`
**Role & Explanation:**
This endpoint registers a new user into the platform. It checks if the email is already registered and creates a specialized entity based on the role (e.g., `Admin`, `BusOperator`, or a standard `User`).

**Endpoint Code:**
```csharp
[HttpPost("register")]
public async Task<IActionResult> Register([FromBody] RegisterRequest request)
{
    if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        return BadRequest("User with this email already exists.");

    User user;
    
    switch (request.Role)
    {
        case UserRole.Admin:
            user = new server.Core.Entities.Admin { FullName = request.FullName, Email = request.Email, Phone = request.Phone, Role = request.Role };
            break;
        case UserRole.Operator:
            user = new BusOperator 
            { 
                FullName = request.FullName, 
                Email = request.Email, 
                Phone = request.Phone, 
                Role = request.Role,
                CompanyName = request.CompanyName ?? request.FullName,
                Address = request.Address ?? string.Empty,
                Status = ApprovalStatus.Pending,
                IsApproved = false
            };
            break;
        default:
            user = new User { FullName = request.FullName, Email = request.Email, Phone = request.Phone, Role = request.Role };
            break;
    }

    user.PasswordHash = _authService.HashPassword(request.Password);

    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    return Ok(new { Message = "Registration successful" });
}
```

**Entity Framework Queries:**
```csharp
await _context.Users.AnyAsync(u => u.Email == request.Email);
_context.Users.Add(user);
await _context.SaveChangesAsync();
```

**PostgreSQL Mapping:**
```sql
-- Check if user exists
SELECT EXISTS (
    SELECT 1 FROM "Users" WHERE "Email" = @Email
);

-- Insert base user info (into Users table)
INSERT INTO "Users" ("Id", "FullName", "Email", "Phone", "Role", "PasswordHash") 
VALUES (@Id, @FullName, @Email, @Phone, @Role, @PasswordHash);

-- Insert operator specific info (only if role is Operator, due to TPT)
INSERT INTO "BusOperators" ("Id", "CompanyName", "Address", "Status", "IsApproved") 
VALUES (@Id, @CompanyName, @Address, @Status, @IsApproved);
```

#### `POST /api/Auth/login`
**Role & Explanation:**
Authenticates a user based on their email and password. If successful, it generates a JWT token. For operators, it additionally retrieves their approval status to include in the response.

**Endpoint Code:**
```csharp
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    
    if (user == null || !_authService.VerifyPassword(request.Password, user.PasswordHash))
        return Unauthorized("Invalid credentials.");

    var token = _authService.GenerateToken(user);
    
    bool isApproved = true;
    string status = "Approved";
    string? rejectionReason = null;

    if (user.Role == UserRole.Operator)
    {
        var op = await _context.BusOperators.FirstOrDefaultAsync(o => o.Id == user.Id);
        if (op != null)
        {
            isApproved = op.IsApproved;
            status = op.Status.ToString();
            rejectionReason = op.RejectionReason;
        }
    }

    return Ok(new AuthResponse(token, user.FullName, user.Email, user.Role, user.Id, isApproved, status, rejectionReason));
}
```

**Entity Framework Queries:**
```csharp
await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
await _context.BusOperators.FirstOrDefaultAsync(o => o.Id == user.Id);
```

**PostgreSQL Mapping:**
```sql
-- Fetch the user
SELECT * FROM "Users" WHERE "Email" = @Email LIMIT 1;

-- Fetch operator specific details if the user is an operator
SELECT * FROM "BusOperators" WHERE "Id" = @UserId LIMIT 1;
```

---

### 2. AdminController (`AdminController.cs`)

#### `POST /api/Admin/cities`
**Role & Explanation:** Adds a new city to the master data records.

**Endpoint Code:**
```csharp
[HttpPost("cities")]
public async Task<ActionResult> AddCity([FromBody] CityDto dto)
{
    var city = new City { Name = dto.Name, State = dto.State };
    _context.Cities.Add(city);
    await _context.SaveChangesAsync();
    return Ok(new { message = "City added successfully", city });
}
```

**Entity Framework Queries:**
```csharp
_context.Cities.Add(city);
await _context.SaveChangesAsync();
```

**PostgreSQL Mapping:**
```sql
INSERT INTO "Cities" ("Name", "State") 
VALUES (@Name, @State) RETURNING "Id";
```

#### `POST /api/Admin/hubs`
**Role & Explanation:** Adds a new boarding or dropping hub for a specific city. Validates that the referenced city exists first.

**Endpoint Code:**
```csharp
[HttpPost("hubs")]
public async Task<ActionResult> AddHub([FromBody] HubDto dto)
{
    var cityExists = await _context.Cities.AnyAsync(c => c.Id == dto.CityId);
    if (!cityExists) return BadRequest("City does not exist.");

    if (!Enum.TryParse(dto.Type, out HubType parsedType))
    {
        parsedType = HubType.Both;
    }

    var hub = new Hub { Name = dto.Name, CityId = dto.CityId, Type = parsedType };
    _context.Hubs.Add(hub);
    await _context.SaveChangesAsync();
    return Ok(new { message = "Hub added successfully", hub });
}
```

**Entity Framework Queries:**
```csharp
await _context.Cities.AnyAsync(c => c.Id == dto.CityId);
_context.Hubs.Add(hub);
await _context.SaveChangesAsync();
```

**PostgreSQL Mapping:**
```sql
-- Validate City
SELECT EXISTS (SELECT 1 FROM "Cities" WHERE "Id" = @CityId);

-- Insert Hub
INSERT INTO "Hubs" ("Name", "CityId", "Type") 
VALUES (@Name, @CityId, @Type) RETURNING "Id";
```

#### `GET /api/Admin/routes`
**Role & Explanation:** Retrieves a list of all defined routes with minimal information.

**Endpoint Code:**
```csharp
[HttpGet("routes")]
public async Task<ActionResult<IEnumerable<RouteResponseDto>>> GetRoutes()
{
    var routes = await _context.Routes
        .Select(r => new RouteResponseDto
        {
            Id = r.Id,
            Source = r.Source,
            Destination = r.Destination,
            DistanceKm = r.DistanceKm
        })
        .ToListAsync();

    return Ok(routes);
}
```

**Entity Framework Queries:**
```csharp
await _context.Routes.Select(r => new RouteResponseDto { ... }).ToListAsync();
```

**PostgreSQL Mapping:**
```sql
SELECT "Id", "Source", "Destination", "DistanceKm" 
FROM "Routes";
```

#### `POST /api/Admin/routes`
**Role & Explanation:** Creates a new route between two cities with an approximate distance.

**Endpoint Code:**
```csharp
[HttpPost("routes")]
public async Task<ActionResult> AddRoute([FromBody] RouteRequestDto dto)
{
    if (string.IsNullOrWhiteSpace(dto.Source) || string.IsNullOrWhiteSpace(dto.Destination))
        return BadRequest("Source and Destination are required.");

    var route = new BusRoute
    {
        Source = dto.Source,
        Destination = dto.Destination,
        DistanceKm = dto.DistanceKm
    };

    _context.Routes.Add(route);
    await _context.SaveChangesAsync();
    return Ok(new { message = "Route added successfully", route });
}
```

**Entity Framework Queries:**
```csharp
_context.Routes.Add(route);
await _context.SaveChangesAsync();
```

**PostgreSQL Mapping:**
```sql
INSERT INTO "Routes" ("Source", "Destination", "DistanceKm") 
VALUES (@Source, @Destination, @DistanceKm) RETURNING "Id";
```

#### `GET /api/Admin/operators`
**Role & Explanation:** Retrieves a list of all bus operators, prioritizing those with a "Pending" approval status so administrators can easily review them.

**Endpoint Code:**
```csharp
[HttpGet("operators")]
public async Task<ActionResult<IEnumerable<OperatorResponseDto>>> GetOperators()
{
    var ops = await _context.BusOperators
        .Select(o => new OperatorResponseDto
        {
            Id = o.Id,
            CompanyName = (string.IsNullOrEmpty(o.CompanyName) || o.CompanyName == "Unspecified Company") ? o.FullName : o.CompanyName,
            Email = o.Email,
            Phone = o.Phone,
            IsApproved = o.IsApproved,
            Status = o.Status,
            RejectionReason = o.RejectionReason,
            CreatedAt = o.CreatedAt
        })
        .OrderBy(o => o.Status == ApprovalStatus.Pending ? 0 : 1) // Pending first
        .ThenByDescending(o => o.CreatedAt) 
        .ToListAsync();

    return Ok(ops);
}
```

**Entity Framework Queries:**
```csharp
await _context.BusOperators.Select(...).OrderBy(...).ThenByDescending(...).ToListAsync();
```

**PostgreSQL Mapping:**
```sql
SELECT "Id", "FullName", "CompanyName", "Email", "Phone", "IsApproved", "Status", "RejectionReason", "CreatedAt" 
FROM "BusOperators"
ORDER BY 
    CASE WHEN "Status" = 0 THEN 0 ELSE 1 END ASC, 
    "CreatedAt" DESC;
```

#### `PUT /api/Admin/operators/{id}/approve`
**Role & Explanation:** Approves an operator's account and automatically approves any pending buses registered by that operator.

**Endpoint Code:**
```csharp
[HttpPut("operators/{id}/approve")]
public async Task<ActionResult> ApproveOperator(Guid id)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try 
    {
        var op = await _context.BusOperators.FirstOrDefaultAsync(o => o.Id == id);
        if (op == null) return NotFound("Operator not found");

        op.IsApproved = true;
        op.Status = ApprovalStatus.Approved;
        op.RejectionReason = null;
        
        _context.BusOperators.Update(op);

        var pendingBuses = await _context.Buses
            .Where(b => b.OperatorId == id && !b.IsApproved && b.Status == ApprovalStatus.Pending)
            .ToListAsync();

        foreach (var b in pendingBuses)
        {
            b.IsApproved = true;
            b.Status = ApprovalStatus.Approved;
            b.RejectionReason = null;
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        
        return Ok(new { message = "Operator approved successfully" });
    }
    catch (Exception)
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

**Entity Framework Queries:**
```csharp
await _context.BusOperators.FirstOrDefaultAsync(o => o.Id == id);
_context.BusOperators.Update(op);
await _context.Buses.Where(b => b.OperatorId == id && !b.IsApproved && b.Status == ApprovalStatus.Pending).ToListAsync();
await _context.SaveChangesAsync();
```

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

#### `PUT /api/Admin/operators/{id}/deny`
**Role & Explanation:** Rejects an operator's registration request with a specific reason. Simultaneously denies all their pending buses.

**Endpoint Code:**
```csharp
[HttpPut("operators/{id}/deny")]
public async Task<ActionResult> DenyOperator(Guid id, [FromBody] RejectionRequest request)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        var op = await _context.BusOperators.FirstOrDefaultAsync(o => o.Id == id);
        if (op == null) return NotFound("Operator not found");

        op.IsApproved = false;
        op.Status = ApprovalStatus.Rejected;
        op.RejectionReason = request.Reason;

        var pendingBuses = await _context.Buses
            .Where(b => b.OperatorId == id && !b.IsApproved && b.Status == ApprovalStatus.Pending)
            .ToListAsync();

        foreach (var b in pendingBuses)
        {
            b.IsApproved = false;
            b.Status = ApprovalStatus.Rejected;
            b.RejectionReason = $"Operator account rejected: {request.Reason}";
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new { message = "Operator registration rejected" });
    }
    catch (Exception)
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

**Entity Framework Queries:**
```csharp
await _context.BusOperators.FirstOrDefaultAsync(o => o.Id == id);
await _context.Buses.Where(b => b.OperatorId == id && !b.IsApproved && b.Status == ApprovalStatus.Pending).ToListAsync();
await _context.SaveChangesAsync();
```

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

#### `GET /api/Admin/buses/pending`
**Role & Explanation:** Retrieves all buses that are currently waiting for admin approval, joining with the Operator table to display the operator's name.

**Endpoint Code:**
```csharp
[HttpGet("buses/pending")]
public async Task<ActionResult<IEnumerable<BusApprovalDto>>> GetPendingBuses()
{
    var buses = await _context.Buses
        .Include(b => b.Operator)
        .Where(b => !b.IsApproved)
        .OrderByDescending(b => b.Id)
        .Select(b => new BusApprovalDto
        {
            Id = b.Id,
            BusNumber = b.BusNumber,
            BusType = b.BusType,
            TotalSeats = b.TotalSeats,
            OperatorName = b.Operator != null ? b.Operator.FullName : "Unknown",
            CompanyName = (b.Operator != null && !string.IsNullOrEmpty(b.Operator.CompanyName) && b.Operator.CompanyName != "Unspecified Company") 
                          ? b.Operator.CompanyName : (b.Operator != null ? b.Operator.FullName : "Unknown"),
            IsApproved = b.IsApproved,
            Status = b.Status,
            RejectionReason = b.RejectionReason
        })
        .ToListAsync();

    return Ok(buses);
}
```

**Entity Framework Queries:**
```csharp
await _context.Buses.Include(b => b.Operator).Where(b => !b.IsApproved).OrderByDescending(b => b.Id).Select(...).ToListAsync();
```

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

#### `PUT /api/Admin/buses/{id}/approve`
**Role & Explanation:** Approves a specific bus for service. It first checks if the bus's owner (operator) is already approved.

**Endpoint Code:**
```csharp
[HttpPut("buses/{id}/approve")]
public async Task<ActionResult> ApproveBus(Guid id)
{
    var bus = await _context.Buses.Include(b => b.Operator).FirstOrDefaultAsync(b => b.Id == id);
    if (bus == null) return NotFound("Bus not found");

    if (bus.Operator == null || !bus.Operator.IsApproved)
    {
        return BadRequest("Cannot approve bus because the operator is not yet approved.");
    }

    bus.IsApproved = true;
    bus.Status = ApprovalStatus.Approved;
    bus.RejectionReason = null;

    await _context.SaveChangesAsync();

    return Ok(new { message = "Bus approved successfully" });
}
```

**Entity Framework Queries:**
```csharp
await _context.Buses.Include(b => b.Operator).FirstOrDefaultAsync(b => b.Id == id);
await _context.SaveChangesAsync();
```

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

#### `PUT /api/Admin/buses/{id}/deny`
**Role & Explanation:** Rejects a specific bus registration with a provided reason.

**Endpoint Code:**
```csharp
[HttpPut("buses/{id}/deny")]
public async Task<ActionResult> DenyBus(Guid id, [FromBody] RejectionRequest request)
{
    var bus = await _context.Buses.Include(b => b.Operator).FirstOrDefaultAsync(b => b.Id == id);
    if (bus == null) return NotFound("Bus not found");

    bus.IsApproved = false;
    bus.Status = ApprovalStatus.Rejected;
    bus.RejectionReason = request.Reason;

    await _context.SaveChangesAsync();

    return Ok(new { message = "Bus registration rejected" });
}
```

**Entity Framework Queries:**
```csharp
await _context.Buses.Include(b => b.Operator).FirstOrDefaultAsync(b => b.Id == id);
await _context.SaveChangesAsync();
```

**PostgreSQL Mapping:**
```sql
UPDATE "Buses" 
SET "IsApproved" = FALSE, "Status" = 2, "RejectionReason" = @Reason 
WHERE "Id" = @Id;
```

#### `GET /api/Admin/settings/fee` & `PUT /api/Admin/settings/fee`
**Role & Explanation:** Gets or updates the platform's global configurations (like flat booking fees or percentage-based commission rates).

**Endpoint Code:**
```csharp
[HttpPut("settings/fee")]
public async Task<ActionResult> UpdateFeeSettings([FromBody] FeeSettingDto dto)
{
    var config = await _context.GlobalConfigurations.FirstOrDefaultAsync();
    
    if (config == null)
    {
        config = new GlobalConfiguration();
        _context.GlobalConfigurations.Add(config);
    }

    config.PlatformFeeType = dto.FeeType;
    config.PlatformFeeValue = dto.FeeValue;
    config.OperatorCommissionPercentage = dto.CommissionPercentage;
    config.LastUpdated = DateTime.UtcNow;

    await _context.SaveChangesAsync();
    
    return Ok(new { message = "Platform settings updated successfully." });
}
```

**Entity Framework Queries:**
```csharp
await _context.GlobalConfigurations.FirstOrDefaultAsync();
_context.GlobalConfigurations.Add(config);
await _context.SaveChangesAsync();
```

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

#### `GET /api/Admin/stats`
**Role & Explanation:** Computes platform-wide analytics for the admin dashboard. Aggregates data about completed/upcoming bookings, generated revenue, total operators, and a feed of recent trips.

**Endpoint Code:**
```csharp
[HttpGet("stats")]
public async Task<ActionResult<AdminStatsDto>> GetStats(...)
{
    // ... setup date boundaries ...
    
    var validBookingsQuery = _context.Bookings
        .Where(b => b.Status == BookingStatus.Confirmed && b.Payment != null && b.Payment.Status == PaymentStatus.Success)
        .Where(b => b.Journey!.DepartureTime >= start && b.Journey!.DepartureTime <= end);
    
    // ... operator filtering ...

    var totalBookings = await validBookingsQuery.CountAsync();
    var totalBaseAmount = await validBookingsQuery.SumAsync(b => b.TotalAmount);
    var totalFees = await validBookingsQuery.SumAsync(b => b.PlatformFee);
    
    var upcomingBookings = await validBookingsQuery.CountAsync(b => b.Journey != null && b.Journey.DepartureTime > now);
    var completedBookings = await validBookingsQuery.CountAsync(b => b.Journey != null && b.Journey.ArrivalTime < now);

    var activeOperators = await _context.BusOperators.CountAsync(o => o.IsApproved);
    var totalCities = await _context.Cities.CountAsync();

    var recentTrips = await schedulesQuery
        .Include(s => s.Route)
        .Include(s => s.Bus)
        .ThenInclude(b => b!.Operator)
        .OrderByDescending(s => s.DepartureTime)
        .Take(1000)
        .Select(...)
        .ToListAsync();

    // ... return stats ...
}
```

**Entity Framework Queries:**
```csharp
await validBookingsQuery.CountAsync();
await validBookingsQuery.SumAsync(b => b.TotalAmount);
await _context.BusOperators.CountAsync(o => o.IsApproved);
await schedulesQuery.Include(...).OrderByDescending(...).Take(1000).Select(...).ToListAsync();
```

**PostgreSQL Mapping:**
```sql
-- Total valid bookings count
SELECT COUNT(*) 
FROM "Bookings" b 
INNER JOIN "Payments" p ON b."Id" = p."BookingId" 
INNER JOIN "Schedules" s ON b."JourneyId" = s."Id" 
WHERE b."Status" = 1 AND p."Status" = 1 AND s."DepartureTime" >= @start AND s."DepartureTime" <= @end;

-- Revenue summations
SELECT SUM(b."TotalAmount") FROM "Bookings" b INNER JOIN ...;
SELECT SUM(b."PlatformFee") FROM "Bookings" b INNER JOIN ...;

-- Active Operators Count
SELECT COUNT(*) FROM "BusOperators" WHERE "IsApproved" = TRUE;

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

### 3. OperatorController (`OperatorController.cs`)

#### `GET /api/Operator/{operatorId}/stats`
**Role & Explanation:** Computes performance metrics for a specific operator, including total buses, active schedules, and overall revenue generated in the last 30 days.

**Endpoint Code:**
```csharp
[HttpGet("{operatorId}/stats")]
public async Task<ActionResult<OperatorStatsDto>> GetStats(Guid operatorId)
{
    var now = DateTime.UtcNow;

    var buses = await _context.Buses
        .Where(b => b.OperatorId == operatorId)
        .ToListAsync();

    var schedules = await _context.Schedules
        .Include(s => s.Bus)
        .Include(s => s.Route)
        .Where(s => s.Bus!.OperatorId == operatorId && s.DepartureTime > now.AddDays(-30))
        .OrderByDescending(s => s.DepartureTime)
        .ToListAsync();

    var scheduleIds = schedules.Select(s => s.Id).ToList();

    var bookings = await _context.Bookings
        .Include(b => b.Passengers)
        .Where(b => scheduleIds.Contains(b.JourneyId) && b.Status == BookingStatus.Confirmed)
        .ToListAsync();

    // ... calculations & return
}
```

**Entity Framework Queries:**
```csharp
await _context.Buses.Where(b => b.OperatorId == operatorId).ToListAsync();
await _context.Schedules.Include(...).Where(...).ToListAsync();
await _context.Bookings.Include(...).Where(...).ToListAsync();
```

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

#### `GET /api/Operator/{operatorId}/buses`
**Role & Explanation:** Retrieves the fleet (list of buses) belonging to a given operator.

**Endpoint Code:**
```csharp
[HttpGet("{operatorId}/buses")]
public async Task<ActionResult<IEnumerable<Bus>>> GetBuses(Guid operatorId)
{
    var buses = await _context.Buses
        .Where(b => b.OperatorId == operatorId)
        .ToListAsync();
    return Ok(buses);
}
```

**Entity Framework Queries:**
```csharp
await _context.Buses.Where(b => b.OperatorId == operatorId).ToListAsync();
```

**PostgreSQL Mapping:**
```sql
SELECT * FROM "Buses" WHERE "OperatorId" = @OperatorId;
```

#### `POST /api/Operator/{operatorId}/buses`
**Role & Explanation:** Registers a new bus for an operator. It ensures the bus number is unique, manages operator profile state, and submits the bus as "Pending" for Admin approval.

**Endpoint Code:**
```csharp
[HttpPost("{operatorId}/buses")]
public async Task<ActionResult> AddBus(Guid operatorId, [FromBody] BusRequestDto dto)
{
    var existingBus = await _context.Buses.FirstOrDefaultAsync(b => b.BusNumber == busNum);
    // ... handle existing

    var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == operatorId);
    var op = await _context.BusOperators.FirstOrDefaultAsync(o => o.Id == operatorId);

    if (op == null) 
    {
        // RAW SQL INSERT to avoid EF Core TPT tracking conflict
        await _context.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"BusOperators\" (\"Id\", \"CompanyName\", \"IsApproved\", \"Address\", \"Status\") VALUES ({0}, {1}, {2}, {3}, {4})",
            user.Id, user.FullName, false, "", (int)ApprovalStatus.Pending
        );
    }
    
    var bus = new Bus { /* initialization */ };

    _context.Buses.Add(bus);
    await _context.SaveChangesAsync();

    return Ok(new { message = "Bus added successfully.", busId = bus.Id });
}
```

**Entity Framework Queries:**
```csharp
await _context.Buses.FirstOrDefaultAsync(b => b.BusNumber == busNum);
await _context.Database.ExecuteSqlRawAsync("INSERT INTO \"BusOperators\" ...");
_context.Buses.Add(bus);
await _context.SaveChangesAsync();
```

**PostgreSQL Mapping:**
```sql
-- Check for existing bus number
SELECT * FROM "Buses" WHERE "BusNumber" = @BusNumber LIMIT 1;

-- Raw SQL executed directly in the backend to handle TPT profile generation
INSERT INTO "BusOperators" ("Id", "CompanyName", "IsApproved", "Address", "Status") 
VALUES (@Id, @CompanyName, FALSE, '', 0);

-- Insert new bus
INSERT INTO "Buses" ("BusNumber", "BusType", "TotalSeats", "LayoutConfig", "OperatorId", "IsApproved", "Status") 
VALUES (@BusNumber, @BusType, @TotalSeats, @LayoutConfig, @OperatorId, FALSE, 0) 
RETURNING "Id";
```

#### `GET /api/Operator/{operatorId}/schedules`
**Role & Explanation:** Retrieves all trip schedules configured by the operator, joined with route and bus data.

**Endpoint Code:**
```csharp
[HttpGet("{operatorId}/schedules")]
public async Task<ActionResult<IEnumerable<Schedule>>> GetSchedules(Guid operatorId)
{
    var schedules = await _context.Schedules
        .Include(s => s.Bus)
        .Include(s => s.Route)
        .Where(s => s.Bus!.OperatorId == operatorId)
        .OrderByDescending(s => s.DepartureTime)
        .ToListAsync();

    return Ok(schedules);
}
```

**Entity Framework Queries:**
```csharp
await _context.Schedules.Include(...).Where(...).OrderByDescending(...).ToListAsync();
```

**PostgreSQL Mapping:**
```sql
SELECT s.*, b.*, r.* 
FROM "Schedules" s 
INNER JOIN "Buses" b ON s."BusId" = b."Id" 
LEFT JOIN "Routes" r ON s."RouteId" = r."Id" 
WHERE b."OperatorId" = @OperatorId 
ORDER BY s."DepartureTime" DESC;
```

#### `POST /api/Operator/{operatorId}/schedules`
**Role & Explanation:** Creates a new trip schedule for a selected route and bus. It also auto-assigns boarding and dropping hubs if none were provided.

**Endpoint Code:**
```csharp
[HttpPost("{operatorId}/schedules")]
public async Task<ActionResult> CreateSchedule(Guid operatorId, [FromBody] ScheduleRequestDto dto)
{
    var bus = await _context.Buses.FindAsync(dto.BusId);
    var route = await _context.Routes.FindAsync(dto.RouteId);
    
    // Default Hub Assignment
    var sourceCity = await _context.Cities.FirstOrDefaultAsync(c => c.Name == route.Source);
    var defaultHubs = await _context.Hubs
        .Where(h => h.CityId == sourceCity.Id && (h.Type == HubType.Boarding || h.Type == HubType.Both))
        .Take(2)
        .Select(h => h.Id)
        .ToListAsync();

    var schedule = new Schedule { /* initialization */ };

    _context.Schedules.Add(schedule);
    await _context.SaveChangesAsync();

    return Ok(new { message = "Schedule created successfully", scheduleId = schedule.Id });
}
```

**Entity Framework Queries:**
```csharp
await _context.Buses.FindAsync(dto.BusId);
await _context.Cities.FirstOrDefaultAsync(c => c.Name == route.Source);
await _context.Hubs.Where(...).Take(2).Select(...).ToListAsync();
_context.Schedules.Add(schedule);
await _context.SaveChangesAsync();
```

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

#### `PUT /api/Operator/{operatorId}/schedules/{scheduleId}/cancel`
**Role & Explanation:** Cancels a scheduled trip. If customers have already booked tickets, this flags their bookings as cancelled and dispatches notifications.

**Endpoint Code:**
```csharp
[HttpPut("{operatorId}/schedules/{scheduleId}/cancel")]
public async Task<ActionResult> CancelSchedule(Guid operatorId, Guid scheduleId)
{
    var schedule = await _context.Schedules
        .Include(s => s.Bus)
        .FirstOrDefaultAsync(s => s.Id == scheduleId && s.Bus!.OperatorId == operatorId);
        
    schedule.Status = JourneyStatus.Cancelled;

    var bookings = await _context.Bookings
        .Include(b => b.Journey)
        .ThenInclude(j => j!.Route)
        .Where(b => b.JourneyId == scheduleId && b.Status == BookingStatus.Confirmed)
        .ToListAsync();

    foreach (var booking in bookings)
    {
        booking.Status = BookingStatus.Cancelled;
    }

    await _context.SaveChangesAsync();

    return Ok(new { message = $"Schedule cancelled. {bookings.Count} customers notified." });
}
```

**Entity Framework Queries:**
```csharp
await _context.Schedules.Include(s => s.Bus).FirstOrDefaultAsync(...);
await _context.Bookings.Include(...).Where(...).ToListAsync();
await _context.SaveChangesAsync();
```

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

#### `DELETE /api/Operator/{operatorId}/buses/{busId}`
**Role & Explanation:** Deletes a bus from the system, provided it has no booking history or active future schedules.

**Endpoint Code:**
```csharp
[HttpDelete("{operatorId}/buses/{busId}")]
public async Task<ActionResult> DeleteBus(Guid operatorId, Guid busId)
{
    var bus = await _context.Buses.FirstOrDefaultAsync(b => b.Id == busId && b.OperatorId == operatorId);
    
    var hasBookings = await _context.Bookings.AnyAsync(b => b.Journey!.BusId == busId);
    if (hasBookings) return BadRequest("...");

    var hasSchedules = await _context.Schedules.AnyAsync(s => s.BusId == busId && s.Status != JourneyStatus.Cancelled && s.DepartureTime > DateTime.UtcNow);
    if (hasSchedules) return BadRequest("...");

    _context.Buses.Remove(bus);
    await _context.SaveChangesAsync();

    return Ok(new { message = "Bus removed successfully." });
}
```

**Entity Framework Queries:**
```csharp
await _context.Buses.FirstOrDefaultAsync(...);
await _context.Bookings.AnyAsync(...);
await _context.Schedules.AnyAsync(...);
_context.Buses.Remove(bus);
await _context.SaveChangesAsync();
```

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

---

### 4. BookingController (Fully Layered Architecture)

This section maps the refactored endpoints where operations are cleanly split across **Controllers**, **Services**, and **Repositories** to fully realize the multi-layered architecture.

#### `GET /api/Booking/layout/{journeyId}`
**Role & Explanation:** Provides seat layout configuration and status (Blocked or Booked) for a specific trip, combined with boarding and dropping hubs.

**1. Controller Layer (`BookingController.cs`):**
```csharp
[HttpGet("layout/{journeyId}")]
public async Task<ActionResult<SeatLayoutDto>> GetLayout(Guid journeyId)
{
    var layout = await _bookingService.GetLayoutAsync(journeyId);
    if (layout == null) return NotFound("Journey not found.");
    return Ok(layout);
}
```

**2. Service Layer (`BookingService.cs`):**
```csharp
public async Task<SeatLayoutDto?> GetLayoutAsync(Guid journeyId)
{
    // Clean up expired seat locks
    await _seatLockService.ReleaseExpiredLocks();

    var journey = await _bookingRepository.GetJourneyByIdAsync(journeyId);
    if (journey == null) return null;

    var confirmedPassengers = await _bookingRepository.GetConfirmedPassengersForJourneyAsync(journeyId);
    var bookedSeats = confirmedPassengers.Select(p => new SeatStatusDto
    {
        SeatNumber = p.SeatNumber,
        Status = "Booked",
        Gender = p.Gender == Core.Enums.Gender.F ? "Female" : p.Gender == Core.Enums.Gender.M ? "Male" : "Other"
    }).ToList();

    var locks = await _bookingRepository.GetActiveLocksForJourneyAsync(journeyId);
    var blockedSeats = locks.Select(l => new SeatStatusDto { SeatNumber = l.SeatNumber, Status = "Blocked" }).ToList();

    var unavailableSeats = bookedSeats.Concat(blockedSeats).ToList();

    var op = journey.Bus?.Operator;
    var company = string.IsNullOrWhiteSpace(op?.CompanyName) ? op?.FullName : op?.CompanyName;
    var busNumberPart = journey.Bus?.BusNumber?.Split('-').Last() ?? "";

    var config = await _bookingRepository.GetGlobalConfigurationAsync();
    var sourceCity = await _bookingRepository.GetCityByNameAsync(journey.Route!.Source);
    var destCity = await _bookingRepository.GetCityByNameAsync(journey.Route!.Destination);

    if (sourceCity == null || destCity == null) return null;

    var boardingHubs = await _bookingRepository.GetHubsByCityIdAndTypeAsync(sourceCity.Id, new List<HubType> { HubType.Boarding, HubType.Both });
    var droppingHubs = await _bookingRepository.GetHubsByCityIdAndTypeAsync(destCity.Id, new List<HubType> { HubType.Dropping, HubType.Both });

    if (journey.BoardingHubIds != null && journey.BoardingHubIds.Any())
        boardingHubs = boardingHubs.Where(h => journey.BoardingHubIds.Contains(h.Id)).ToList();
    
    if (journey.DroppingHubIds != null && journey.DroppingHubIds.Any())
        droppingHubs = droppingHubs.Where(h => journey.DroppingHubIds.Contains(h.Id)).ToList();

    return new SeatLayoutDto
    {
        LayoutConfig = journey.Bus?.LayoutConfig,
        UnavailableSeats = unavailableSeats,
        Source = journey.Route?.Source ?? "",
        Destination = journey.Route?.Destination ?? "",
        DepartureTime = journey.DepartureTime,
        BusNumber = busNumberPart,
        BusName = company,
        PlatformFeeType = config?.PlatformFeeType ?? "Fixed",
        PlatformFeeValue = config?.PlatformFeeValue ?? 50.00m,
        BoardingHubs = boardingHubs.Select(h => new HubStatusDto { Id = h.Id, Name = h.Name, Type = h.Type.ToString() }).ToList(),
        DroppingHubs = droppingHubs.Select(h => new HubStatusDto { Id = h.Id, Name = h.Name, Type = h.Type.ToString() }).ToList()
    };
}
```

**3. Repository Layer (`BookingRepository.cs`):**
```csharp
public async Task<Schedule?> GetJourneyByIdAsync(Guid journeyId)
{
    return await _context.Schedules
        .Include(s => s.Bus).ThenInclude(b => b!.Operator)
        .Include(s => s.Route)
        .FirstOrDefaultAsync(s => s.Id == journeyId);
}

public async Task<List<Passenger>> GetConfirmedPassengersForJourneyAsync(Guid journeyId)
{
    return await _context.Passengers
        .Where(p => p.Booking!.JourneyId == journeyId && p.Booking.Status == BookingStatus.Confirmed)
        .ToListAsync();
}

public async Task<List<SeatLock>> GetActiveLocksForJourneyAsync(Guid journeyId)
{
    return await _context.SeatLocks
        .Where(l => l.JourneyId == journeyId && l.ExpiresAt > DateTime.UtcNow)
        .ToListAsync();
}
```

**PostgreSQL Mapping:**
```sql
-- Fetch Journey details
SELECT s.*, b.*, o.*, r.* 
FROM "Schedules" s 
LEFT JOIN "Buses" b ON s."BusId" = b."Id" 
LEFT JOIN "BusOperators" o ON b."OperatorId" = o."Id" 
LEFT JOIN "Routes" r ON s."RouteId" = r."Id" 
WHERE s."Id" = @JourneyId LIMIT 1;

-- Fetch booked passengers
SELECT p.* 
FROM "Passengers" p 
INNER JOIN "Bookings" b ON p."BookingId" = b."Id" 
WHERE b."JourneyId" = @JourneyId AND b."Status" = 1;

-- Fetch active seat locks
SELECT * 
FROM "SeatLocks" 
WHERE "JourneyId" = @JourneyId AND "ExpiresAt" > @UtcNow;
```

---

#### `POST /api/Booking/confirm`
**Role & Explanation:** Confirms and saves a booking. This is executed in a database transaction to ensure atomicity, updating available seats, adding passenger entries, processing mock payments, and releasing seat locks.

**1. Controller Layer (`BookingController.cs`):**
```csharp
[HttpPost("confirm")]
public async Task<ActionResult<BookingResponse>> ConfirmBooking([FromBody] ConfirmBookingRequest request)
{
    var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    try
    {
        var response = await _bookingService.ConfirmBookingAsync(userId, request);
        return Ok(response);
    }
    catch (EntityNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    catch (BookingValidationException ex) { return BadRequest(new { message = ex.Message }); }
    catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
}
```

**2. Service Layer (`BookingService.cs`):**
```csharp
public async Task<BookingResponse> ConfirmBookingAsync(Guid userId, ConfirmBookingRequest request)
{
    await using var transaction = await _bookingRepository.BeginTransactionAsync();
    try
    {
        var journey = await _bookingRepository.GetJourneyByIdAsync(request.JourneyId);
        if (journey == null) throw new EntityNotFoundException("Journey not found");

        var seatNumbers = request.Passengers.Select(p => p.SeatNumber).ToList();
        var locks = await _bookingRepository.GetActiveLocksForSeatsAsync(request.JourneyId, seatNumbers, userId);
        if (locks.Count != seatNumbers.Count)
            throw new BookingValidationException("Locks expired or not acquired for all requested seats.");

        var existingPassengers = await _bookingRepository.GetConfirmedPassengersForJourneyAsync(request.JourneyId);
        
        // Gender adjacency rules & seat validations...

        var config = await _bookingRepository.GetGlobalConfigurationAsync();
        decimal platformFee = config?.PlatformFeeType == "Percentage" 
            ? (journey.Price * (config.PlatformFeeValue / 100)) * request.Passengers.Count 
            : (config?.PlatformFeeValue ?? 50.00m) * request.Passengers.Count;

        decimal totalBase = journey.Price * request.Passengers.Count;
        decimal totalAmount = totalBase + platformFee;

        var booking = new Booking
        {
            CustomerId = userId,
            JourneyId = request.JourneyId,
            TotalAmount = totalBase,
            PlatformFee = platformFee,
            Status = BookingStatus.Confirmed,
            BoardingHubId = request.BoardingHubId,
            DroppingHubId = request.DroppingHubId,
            CreatedAt = DateTime.UtcNow
        };
        await _bookingRepository.AddBookingAsync(booking);
        await _bookingRepository.SaveChangesAsync(); // Generates BookingId

        foreach (var pDto in request.Passengers)
        {
            var passenger = new Passenger
            {
                BookingId = booking.Id,
                SeatNumber = pDto.SeatNumber,
                Name = pDto.Name,
                Age = pDto.Age,
                Gender = Enum.Parse<Gender>(pDto.Gender)
            };
            await _bookingRepository.AddPassengerAsync(passenger);
        }

        var payment = new Payment
        {
            BookingId = booking.Id,
            Amount = totalAmount,
            TransactionId = "TXN-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
            Status = PaymentStatus.Success,
            ProcessedAt = DateTime.UtcNow
        };
        await _bookingRepository.AddPaymentAsync(payment);

        journey.AvailableSeats -= request.Passengers.Count;
        await _bookingRepository.RemoveSeatLocksAsync(locks);

        await _bookingRepository.SaveChangesAsync();
        await transaction.CommitAsync();

        return new BookingResponse { BookingId = booking.Id, Status = "Success" };
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

**3. Repository Layer (`BookingRepository.cs`):**
```csharp
public async Task AddBookingAsync(Booking booking) => await _context.Bookings.AddAsync(booking);
public async Task AddPassengerAsync(Passenger passenger) => await _context.Passengers.AddAsync(passenger);
public async Task AddPaymentAsync(Payment payment) => await _context.Payments.AddAsync(payment);
public Task RemoveSeatLocksAsync(List<SeatLock> locks)
{
    _context.SeatLocks.RemoveRange(locks);
    return Task.CompletedTask;
}
```

**PostgreSQL Mapping:**
```sql
-- Get locks for seats
SELECT * FROM "SeatLocks" 
WHERE "JourneyId" = @JourneyId AND "SeatNumber" = ANY(@SeatNumbers) 
  AND "LockedByUserId" = @UserId AND "ExpiresAt" > @UtcNow;

-- Insert Booking
INSERT INTO "Bookings" ("CustomerId", "JourneyId", "TotalAmount", "PlatformFee", "Status", "BoardingHubId", "DroppingHubId", "CreatedAt")
VALUES (@CustomerId, @JourneyId, @TotalAmount, @PlatformFee, 1, @BoardingHubId, @DroppingHubId, @CreatedAt)
RETURNING "Id";

-- Insert Passengers
INSERT INTO "Passengers" ("BookingId", "SeatNumber", "Name", "Age", "Gender")
VALUES (@BookingId, @SeatNumber, @Name, @Age, @Gender);

-- Insert Payment
INSERT INTO "Payments" ("BookingId", "Amount", "TransactionId", "Status", "ProcessedAt")
VALUES (@BookingId, @Amount, @TransactionId, 1, @ProcessedAt);

-- Update Journey AvailableSeats
UPDATE "Schedules" SET "AvailableSeats" = @AvailableSeats WHERE "Id" = @JourneyId;

-- Delete Locks
DELETE FROM "SeatLocks" WHERE "Id" = ANY(@lockIds);
```

---

#### `GET /api/Booking/history`
**Role & Explanation:** Retrieves the history of bookings for a specific customer, pulling relative schedule, route, bus, operator, and passenger details.

**1. Controller Layer (`BookingController.cs`):**
```csharp
[HttpGet("history")]
public async Task<ActionResult<List<BookingHistoryDto>>> GetHistory()
{
    var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var history = await _bookingService.GetHistoryAsync(userId);
    return Ok(history);
}
```

**2. Service Layer (`BookingService.cs`):**
```csharp
public async Task<List<BookingHistoryDto>> GetHistoryAsync(Guid userId)
{
    var bookings = await _bookingRepository.GetBookingsByCustomerIdAsync(userId);
    // Maps list of Booking objects to DTOs and returns...
}
```

**3. Repository Layer (`BookingRepository.cs`):**
```csharp
public async Task<List<Booking>> GetBookingsByCustomerIdAsync(Guid customerId)
{
    return await _context.Bookings
        .Include(b => b.Journey).ThenInclude(j => j!.Route)
        .Include(b => b.Journey).ThenInclude(j => j!.Bus).ThenInclude(bus => bus!.Operator)
        .Include(b => b.Passengers)
        .Include(b => b.BoardingHub)
        .Include(b => b.DroppingHub)
        .Where(b => b.CustomerId == customerId)
        .OrderByDescending(b => b.CreatedAt)
        .ToListAsync();
}
```

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

### 5. SearchController (`SearchController.cs`)

#### `GET /api/Search`
**Role & Explanation:** Executes a search for available buses traveling between two points on a given date. Filters out inactive schedules and ensures departures are at least 10 minutes in the future.

**Endpoint Code:**
```csharp
[HttpGet]
public async Task<IActionResult> SearchBuses([FromQuery] string from, [FromQuery] string to, [FromQuery] DateTime date)
{
    // ... validation and date setup ...

    var results = await _context.Schedules
        .Include(s => s.Bus)
        .ThenInclude(b => b!.Operator)
        .Include(s => s.Route)
        .Where(s => EF.Functions.ILike(s.Route!.Source, fromTerm) && 
                    EF.Functions.ILike(s.Route!.Destination, toTerm) &&
                    s.DepartureTime >= startDate && 
                    s.DepartureTime < endDate &&
                    s.DepartureTime >= minDeparture &&
                    s.Status == server.Core.Enums.JourneyStatus.Scheduled)
        .Select(s => new BusSearchResult(
            s.Id, s.Bus!.BusNumber, s.Bus.BusType, s.Bus.Operator!.CompanyName,
            s.Bus.Operator.Address, s.Route!.Source, s.Route.Destination,
            s.DepartureTime, s.ArrivalTime, s.Price, s.AvailableSeats
        ))
        .ToListAsync();

    return Ok(results);
}
```

**Entity Framework Queries:**
```csharp
await _context.Schedules.Include(...).Where(...).Select(...).ToListAsync();
```

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

#### `GET /api/Search/cities`
**Role & Explanation:** Returns an autocomplete list of unique cities that have active routes. Uses an in-memory cache to reduce load on the database.

**Endpoint Code:**
```csharp
[HttpGet("cities")]
public async Task<IActionResult> GetCities([FromQuery] string query)
{
    if (!_cache.TryGetValue("AllCities", out List<string>? allCities))
    {
        var sources = await _context.Routes.Select(r => r.Source).Distinct().ToListAsync();
        var destinations = await _context.Routes.Select(r => r.Destination).Distinct().ToListAsync();
        
        allCities = sources.Union(destinations).Distinct().OrderBy(c => c).ToList();
        _cache.Set("AllCities", allCities, TimeSpan.FromMinutes(30));
    }

    var results = allCities!.Where(c => c.ToLower().Contains(searchTerm)).Take(10).ToList();
    return Ok(results);
}
```

**Entity Framework Queries:**
```csharp
await _context.Routes.Select(r => r.Source).Distinct().ToListAsync();
await _context.Routes.Select(r => r.Destination).Distinct().ToListAsync();
```

**PostgreSQL Mapping:**
```sql
-- Union query executed when cache is missed
SELECT DISTINCT "Source" AS "CityName" FROM "Routes"
UNION
SELECT DISTINCT "Destination" AS "CityName" FROM "Routes"
ORDER BY "CityName";
```

---

### 6. NotificationsController (`NotificationsController.cs`)

#### `GET /api/Notifications/user/{userId}`
**Role & Explanation:** Retrieves the 20 most recent notifications for a user.

**Endpoint Code:**
```csharp
[HttpGet("user/{userId}")]
public async Task<ActionResult<IEnumerable<Notification>>> GetUserNotifications(Guid userId)
{
    var notifications = await _context.Notifications
        .Where(n => n.UserId == userId)
        .OrderByDescending(n => n.CreatedAt)
        .Take(20)
        .ToListAsync();

    return Ok(notifications);
}
```

**Entity Framework Queries:**
```csharp
await _context.Notifications.Where(n => n.UserId == userId).OrderByDescending(n => n.CreatedAt).Take(20).ToListAsync();
```

**PostgreSQL Mapping:**
```sql
SELECT * FROM "Notifications" 
WHERE "UserId" = @UserId 
ORDER BY "CreatedAt" DESC 
LIMIT 20;
```

#### `PUT /api/Notifications/{id}/read`
**Role & Explanation:** Marks a specific notification as read.

**Endpoint Code:**
```csharp
[HttpPut("{id}/read")]
public async Task<ActionResult> MarkAsRead(Guid id)
{
    var notification = await _context.Notifications.FindAsync(id);
    if (notification == null) return NotFound();

    notification.IsRead = true;
    await _context.SaveChangesAsync();

    return Ok();
}
```

**Entity Framework Queries:**
```csharp
await _context.Notifications.FindAsync(id);
await _context.SaveChangesAsync();
```

**PostgreSQL Mapping:**
```sql
UPDATE "Notifications" 
SET "IsRead" = TRUE 
WHERE "Id" = @Id;
```

#### `DELETE /api/Notifications/user/{userId}/clear`
**Role & Explanation:** Deletes all notifications associated with a given user.

**Endpoint Code:**
```csharp
[HttpDelete("user/{userId}/clear")]
public async Task<ActionResult> ClearAll(Guid userId)
{
    var notifications = await _context.Notifications
        .Where(n => n.UserId == userId)
        .ToListAsync();

    _context.Notifications.RemoveRange(notifications);
    await _context.SaveChangesAsync();

    return Ok();
}
```

**Entity Framework Queries:**
```csharp
await _context.Notifications.Where(n => n.UserId == userId).ToListAsync();
_context.Notifications.RemoveRange(notifications);
await _context.SaveChangesAsync();
```

**PostgreSQL Mapping:**
```sql
DELETE FROM "Notifications" 
WHERE "UserId" = @UserId;
```

---

### 7. LocationsController (`LocationsController.cs`)

#### `GET /api/Locations/cities`
**Role & Explanation:** Simple endpoint to retrieve an ordered list of all cities.

**Endpoint Code:**
```csharp
[HttpGet("cities")]
public async Task<IActionResult> GetCities()
{
    var cities = await _context.Cities
        .Select(c => new { c.Id, c.Name, c.State })
        .OrderBy(c => c.Name)
        .ToListAsync();

    return Ok(cities);
}
```

**Entity Framework Queries:**
```csharp
await _context.Cities.Select(c => new { c.Id, c.Name, c.State }).OrderBy(c => c.Name).ToListAsync();
```

**PostgreSQL Mapping:**
```sql
SELECT "Id", "Name", "State" 
FROM "Cities" 
ORDER BY "Name";
```

#### `GET /api/Locations/hubs`
**Role & Explanation:** Retrieves hubs belonging to a particular city, typically used to populate dropdowns in the UI.

**Endpoint Code:**
```csharp
[HttpGet("hubs")]
public async Task<IActionResult> GetHubs([FromQuery] Guid cityId)
{
    var hubs = await _context.Hubs
        .Where(h => h.CityId == cityId)
        .Select(h => new { h.Id, h.Name, Type = h.Type.ToString() })
        .OrderBy(h => h.Name)
        .ToListAsync();

    return Ok(hubs);
}
```

**Entity Framework Queries:**
```csharp
await _context.Hubs.Where(h => h.CityId == cityId).Select(h => new { h.Id, h.Name, Type = h.Type.ToString() }).OrderBy(h => h.Name).ToListAsync();
```

**PostgreSQL Mapping:**
```sql
SELECT "Id", "Name", "Type" 
FROM "Hubs" 
WHERE "CityId" = @CityId 
ORDER BY "Name";
```
