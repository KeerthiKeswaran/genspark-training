# Backend Overview & Query Mapping

This document provides a comprehensive overview of the backend structure, detailing the role of every API endpoint in the bus booking application. It also provides the actual Entity Framework (EF) Core code used for database operations, isolated queries, and maps them to their equivalent PostgreSQL queries.

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

**Endpoint Code:**
```csharp
[HttpPost("register")]
public async Task<IActionResult> Register([FromBody] RegisterRequest request)
{
    // --- EF CORE QUERY START ---
    if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        return BadRequest("User with this email already exists.");
    // --- EF CORE QUERY END ---

    User user;
    
    // Create specialized entity based on role
    switch (request.Role)
    {
        case UserRole.Admin:
            user = new server.Core.Entities.Admin { FullName = request.FullName, Email = request.Email, Phone = request.Phone, Role = request.Role };
            break;
        case UserRole.Operator:
            user = new BusOperator { /* mapping... */ };
            break;
        default:
            user = new User { FullName = request.FullName, Email = request.Email, Phone = request.Phone, Role = request.Role };
            break;
    }

    user.PasswordHash = _authService.HashPassword(request.Password);

    // --- EF CORE QUERY START ---
    _context.Users.Add(user);
    await _context.SaveChangesAsync();
    // --- EF CORE QUERY END ---

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

-- Insert new user
INSERT INTO "Users" ("FullName", "Email", "Phone", "Role", "PasswordHash") 
VALUES (@FullName, @Email, @Phone, @Role, @PasswordHash) 
RETURNING "Id";
```

### `POST /api/Auth/login`
**Role & Explanation:**
Authenticates a user based on their email and password. If successful, it generates a JWT token. For operators, it additionally retrieves their approval status to include in the response.

**Endpoint Code:**
```csharp
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    // --- EF CORE QUERY START ---
    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    // --- EF CORE QUERY END ---
    
    if (user == null || !_authService.VerifyPassword(request.Password, user.PasswordHash))
        return Unauthorized("Invalid credentials.");

    var token = _authService.GenerateToken(user);
    
    bool isApproved = true;
    string status = "Approved";
    string? rejectionReason = null;

    if (user.Role == UserRole.Operator)
    {
        // --- EF CORE QUERY START ---
        var op = await _context.BusOperators.FirstOrDefaultAsync(o => o.Id == user.Id);
        // --- EF CORE QUERY END ---
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

## 2. AdminController (`AdminController.cs`)

### `POST /api/Admin/cities`
**Role & Explanation:** Adds a new city to the master data records.

**Endpoint Code:**
```csharp
[HttpPost("cities")]
public async Task<ActionResult> AddCity([FromBody] CityDto dto)
{
    var city = new City { Name = dto.Name, State = dto.State };
    // --- EF CORE QUERY START ---
    _context.Cities.Add(city);
    await _context.SaveChangesAsync();
    // --- EF CORE QUERY END ---
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

### `POST /api/Admin/hubs`
**Role & Explanation:** Adds a new boarding or dropping hub for a specific city. Validates that the referenced city exists first.

**Endpoint Code:**
```csharp
[HttpPost("hubs")]
public async Task<ActionResult> AddHub([FromBody] HubDto dto)
{
    // --- EF CORE QUERY START ---
    var cityExists = await _context.Cities.AnyAsync(c => c.Id == dto.CityId);
    // --- EF CORE QUERY END ---
    if (!cityExists) return BadRequest("City does not exist.");

    if (!Enum.TryParse(dto.Type, out HubType parsedType))
    {
        parsedType = HubType.Both;
    }

    var hub = new Hub { Name = dto.Name, CityId = dto.CityId, Type = parsedType };
    
    // --- EF CORE QUERY START ---
    _context.Hubs.Add(hub);
    await _context.SaveChangesAsync();
    // --- EF CORE QUERY END ---
    
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

### `GET /api/Admin/routes`
**Role & Explanation:** Retrieves a list of all defined routes with minimal information.

**Endpoint Code:**
```csharp
[HttpGet("routes")]
public async Task<ActionResult<IEnumerable<RouteResponseDto>>> GetRoutes()
{
    // --- EF CORE QUERY START ---
    var routes = await _context.Routes
        .Select(r => new RouteResponseDto
        {
            Id = r.Id,
            Source = r.Source,
            Destination = r.Destination,
            DistanceKm = r.DistanceKm
        })
        .ToListAsync();
    // --- EF CORE QUERY END ---

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

### `POST /api/Admin/routes`
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

    // --- EF CORE QUERY START ---
    _context.Routes.Add(route);
    await _context.SaveChangesAsync();
    // --- EF CORE QUERY END ---
    
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

### `GET /api/Admin/operators`
**Role & Explanation:** Retrieves a list of all bus operators, prioritizing those with a "Pending" approval status so administrators can easily review them.

**Endpoint Code:**
```csharp
[HttpGet("operators")]
public async Task<ActionResult<IEnumerable<OperatorResponseDto>>> GetOperators()
{
    // --- EF CORE QUERY START ---
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
    // --- EF CORE QUERY END ---

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

### `PUT /api/Admin/operators/{id}/approve`
**Role & Explanation:** Approves an operator's account and automatically approves any pending buses registered by that operator.

**Endpoint Code:**
```csharp
[HttpPut("operators/{id}/approve")]
public async Task<ActionResult> ApproveOperator(Guid id)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try 
    {
        // --- EF CORE QUERY START ---
        var op = await _context.BusOperators.FirstOrDefaultAsync(o => o.Id == id);
        if (op == null) return NotFound("Operator not found");

        op.IsApproved = true;
        op.Status = ApprovalStatus.Approved;
        op.RejectionReason = null;
        
        _context.BusOperators.Update(op);

        // Automatically approve all pending buses for this operator
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
        // --- EF CORE QUERY END ---
        
        // Notifications...
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

### `PUT /api/Admin/operators/{id}/deny`
**Role & Explanation:** Rejects an operator's registration request with a specific reason. Simultaneously denies all their pending buses.

**Endpoint Code:**
```csharp
[HttpPut("operators/{id}/deny")]
public async Task<ActionResult> DenyOperator(Guid id, [FromBody] RejectionRequest request)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // --- EF CORE QUERY START ---
        var op = await _context.BusOperators.FirstOrDefaultAsync(o => o.Id == id);
        if (op == null) return NotFound("Operator not found");

        op.IsApproved = false;
        op.Status = ApprovalStatus.Rejected;
        op.RejectionReason = request.Reason;

        // Automatically deny all pending buses for this operator
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
        // --- EF CORE QUERY END ---

        // Notifications...
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

### `GET /api/Admin/buses/pending`
**Role & Explanation:** Retrieves all buses that are currently waiting for admin approval, joining with the Operator table to display the operator's name.

**Endpoint Code:**
```csharp
[HttpGet("buses/pending")]
public async Task<ActionResult<IEnumerable<BusApprovalDto>>> GetPendingBuses()
{
    // --- EF CORE QUERY START ---
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
    // --- EF CORE QUERY END ---

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

### `PUT /api/Admin/buses/{id}/approve`
**Role & Explanation:** Approves a specific bus for service. It first checks if the bus's owner (operator) is already approved.

**Endpoint Code:**
```csharp
[HttpPut("buses/{id}/approve")]
public async Task<ActionResult> ApproveBus(Guid id)
{
    // --- EF CORE QUERY START ---
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
    // --- EF CORE QUERY END ---

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

### `PUT /api/Admin/buses/{id}/deny`
**Role & Explanation:** Rejects a specific bus registration with a provided reason.

**Endpoint Code:**
```csharp
[HttpPut("buses/{id}/deny")]
public async Task<ActionResult> DenyBus(Guid id, [FromBody] RejectionRequest request)
{
    // --- EF CORE QUERY START ---
    var bus = await _context.Buses.Include(b => b.Operator).FirstOrDefaultAsync(b => b.Id == id);
    if (bus == null) return NotFound("Bus not found");

    bus.IsApproved = false;
    bus.Status = ApprovalStatus.Rejected;
    bus.RejectionReason = request.Reason;

    await _context.SaveChangesAsync();
    // --- EF CORE QUERY END ---

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

### `GET /api/Admin/settings/fee` & `PUT /api/Admin/settings/fee`
**Role & Explanation:** Gets or updates the platform's global configurations (like flat booking fees or percentage-based commission rates).

**Endpoint Code:**
```csharp
[HttpPut("settings/fee")]
public async Task<ActionResult> UpdateFeeSettings([FromBody] FeeSettingDto dto)
{
    // --- EF CORE QUERY START ---
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
    // --- EF CORE QUERY END ---
    
    return Ok(new { message = "Platform settings updated successfully." });
}
```

**Entity Framework Queries:**
```csharp
await _context.GlobalConfigurations.FirstOrDefaultAsync();
_context.GlobalConfigurations.Add(config); // If new
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

### `GET /api/Admin/stats`
**Role & Explanation:** Computes platform-wide analytics for the admin dashboard. Aggregates data about completed/upcoming bookings, generated revenue, total operators, and a feed of recent trips.

**Endpoint Code:**
```csharp
[HttpGet("stats")]
public async Task<ActionResult<AdminStatsDto>> GetStats(...)
{
    // ... setup date boundaries ...
    
    // --- EF CORE QUERY START ---
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
    // --- EF CORE QUERY END ---

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
SELECT SUM(b."TotalAmount") FROM ...;
SELECT SUM(b."PlatformFee") FROM ...;

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

## 3. OperatorController (`OperatorController.cs`)

### `GET /api/Operator/{operatorId}/stats`
**Role & Explanation:** Computes performance metrics for a specific operator, including total buses, active schedules, and overall revenue generated in the last 30 days.

**Endpoint Code:**
```csharp
[HttpGet("{operatorId}/stats")]
public async Task<ActionResult<OperatorStatsDto>> GetStats(Guid operatorId)
{
    var now = DateTime.UtcNow;

    // --- EF CORE QUERY START ---
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
    // --- EF CORE QUERY END ---

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

### `GET /api/Operator/{operatorId}/buses`
**Role & Explanation:** Retrieves the fleet (list of buses) belonging to a given operator.

**Endpoint Code:**
```csharp
[HttpGet("{operatorId}/buses")]
public async Task<ActionResult<IEnumerable<Bus>>> GetBuses(Guid operatorId)
{
    // --- EF CORE QUERY START ---
    var buses = await _context.Buses
        .Where(b => b.OperatorId == operatorId)
        .ToListAsync();
    // --- EF CORE QUERY END ---
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

### `POST /api/Operator/{operatorId}/buses`
**Role & Explanation:** Registers a new bus for an operator. It ensures the bus number is unique, manages operator profile state, and submits the bus as "Pending" for Admin approval.

**Endpoint Code:**
```csharp
[HttpPost("{operatorId}/buses")]
public async Task<ActionResult> AddBus(Guid operatorId, [FromBody] BusRequestDto dto)
{
    // --- EF CORE QUERY START ---
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
    // --- EF CORE QUERY END ---

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

### `GET /api/Operator/{operatorId}/schedules`
**Role & Explanation:** Retrieves all trip schedules configured by the operator, joined with route and bus data.

**Endpoint Code:**
```csharp
[HttpGet("{operatorId}/schedules")]
public async Task<ActionResult<IEnumerable<Schedule>>> GetSchedules(Guid operatorId)
{
    // --- EF CORE QUERY START ---
    var schedules = await _context.Schedules
        .Include(s => s.Bus)
        .Include(s => s.Route)
        .Where(s => s.Bus!.OperatorId == operatorId)
        .OrderByDescending(s => s.DepartureTime)
        .ToListAsync();
    // --- EF CORE QUERY END ---

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

### `POST /api/Operator/{operatorId}/schedules`
**Role & Explanation:** Creates a new trip schedule for a selected route and bus. It also auto-assigns boarding and dropping hubs if none were provided.

**Endpoint Code:**
```csharp
[HttpPost("{operatorId}/schedules")]
public async Task<ActionResult> CreateSchedule(Guid operatorId, [FromBody] ScheduleRequestDto dto)
{
    // --- EF CORE QUERY START ---
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
    // --- EF CORE QUERY END ---

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

### `PUT /api/Operator/{operatorId}/schedules/{scheduleId}/cancel`
**Role & Explanation:** Cancels a scheduled trip. If customers have already booked tickets, this flags their bookings as cancelled and dispatches notifications.

**Endpoint Code:**
```csharp
[HttpPut("{operatorId}/schedules/{scheduleId}/cancel")]
public async Task<ActionResult> CancelSchedule(Guid operatorId, Guid scheduleId)
{
    // --- EF CORE QUERY START ---
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
    // --- EF CORE QUERY END ---

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

### `DELETE /api/Operator/{operatorId}/buses/{busId}`
**Role & Explanation:** Deletes a bus from the system, provided it has no booking history or active future schedules.

**Endpoint Code:**
```csharp
[HttpDelete("{operatorId}/buses/{busId}")]
public async Task<ActionResult> DeleteBus(Guid operatorId, Guid busId)
{
    // --- EF CORE QUERY START ---
    var bus = await _context.Buses.FirstOrDefaultAsync(b => b.Id == busId && b.OperatorId == operatorId);
    
    var hasBookings = await _context.Bookings.AnyAsync(b => b.Journey!.BusId == busId);
    if (hasBookings) return BadRequest("...");

    var hasSchedules = await _context.Schedules.AnyAsync(s => s.BusId == busId && s.Status != JourneyStatus.Cancelled && s.DepartureTime > DateTime.UtcNow);
    if (hasSchedules) return BadRequest("...");

    _context.Buses.Remove(bus);
    await _context.SaveChangesAsync();
    // --- EF CORE QUERY END ---

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

## 4. BookingController (`BookingController.cs`)

### `GET /api/Booking/layout/{journeyId}`
**Role & Explanation:** Provides seat availability for a specific trip. It combines confirmed bookings and temporary seat locks to return unavailable seats, and queries the hubs for the trip.

**Endpoint Code:**
```csharp
[HttpGet("layout/{journeyId}")]
public async Task<ActionResult<SeatLayoutDto>> GetLayout(Guid journeyId)
{
    // --- EF CORE QUERY START ---
    var journey = await _context.Schedules
        .Include(s => s.Bus)
            .ThenInclude(b => b!.Operator)
        .Include(s => s.Route)
        .AsNoTracking()
        .FirstOrDefaultAsync(s => s.Id == journeyId);

    var bookedSeats = await _context.Passengers
        .Where(p => p.Booking!.JourneyId == journeyId && p.Booking.Status == BookingStatus.Confirmed)
        .Select(p => new SeatStatusDto { /* ... */ })
        .AsNoTracking()
        .ToListAsync();

    var blockedSeats = await _context.SeatLocks
        .Where(l => l.JourneyId == journeyId && l.ExpiresAt > DateTime.UtcNow)
        .Select(l => new SeatStatusDto { /* ... */ })
        .AsNoTracking()
        .ToListAsync();
        
    var boardingHubs = await _context.Hubs.Where(...).ToListAsync();
    // --- EF CORE QUERY END ---

    // return Dto...
}
```

**Entity Framework Queries:**
```csharp
await _context.Schedules.Include(...).FirstOrDefaultAsync(...);
await _context.Passengers.Where(...).Select(...).ToListAsync();
await _context.SeatLocks.Where(...).Select(...).ToListAsync();
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

### `POST /api/Booking/confirm`
**Role & Explanation:** Finalizes a booking transaction. Validates seat locks and strict gender adjacency rules. It calculates final pricing with fees, generates a mock payment record, decreases available seats on the trip, and frees up the temporary seat locks.

**Endpoint Code:**
```csharp
[HttpPost("confirm")]
public async Task<ActionResult<BookingResponse>> ConfirmBooking([FromBody] ConfirmBookingRequest request)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // --- EF CORE QUERY START ---
        var journey = await _context.Schedules.FindAsync(request.JourneyId);
        
        var locks = await _context.SeatLocks
            .Where(l => l.JourneyId == request.JourneyId && seatNumbers.Contains(l.SeatNumber) && l.LockedByUserId == userId && l.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        var existingPassengers = await _context.Passengers
            .Where(p => p.Booking!.JourneyId == request.JourneyId && p.Booking.Status == BookingStatus.Confirmed)
            .Select(p => new { p.SeatNumber, p.Gender })
            .ToListAsync();

        // Validation Logic...

        var booking = new Core.Entities.Booking { /* ... */ };
        _context.Bookings.Add(booking);

        foreach (var pDto in request.Passengers)
        {
            _context.Passengers.Add(new Passenger { /* ... */ });
        }

        _context.Payments.Add(new Payment { /* ... */ });

        journey.AvailableSeats -= request.Passengers.Count;
        _context.SeatLocks.RemoveRange(locks);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        // --- EF CORE QUERY END ---

        return Ok(new BookingResponse { /* ... */ });
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        return StatusCode(500, "Error");
    }
}
```

**Entity Framework Queries:**
```csharp
await _context.Schedules.FindAsync(request.JourneyId);
await _context.SeatLocks.Where(...).ToListAsync();
await _context.Passengers.Where(...).ToListAsync();
_context.Bookings.Add(booking);
_context.SeatLocks.RemoveRange(locks);
await _context.SaveChangesAsync();
```

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

**Endpoint Code:**
```csharp
[HttpGet("history")]
public async Task<ActionResult<List<BookingHistoryDto>>> GetHistory()
{
    var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // --- EF CORE QUERY START ---
    var bookings = await _context.Bookings
        .Include(b => b.Journey)
            .ThenInclude(j => j!.Route)
        .Include(b => b.Journey)
            .ThenInclude(j => j!.Bus)
                .ThenInclude(bus => bus!.Operator)
        .Include(b => b.Passengers)
        .Include(b => b.BoardingHub)
        .Include(b => b.DroppingHub)
        .Where(b => b.CustomerId == userId)
        .OrderByDescending(b => b.CreatedAt)
        .ToListAsync();
    // --- EF CORE QUERY END ---

    // mapping to Dto...
    return Ok(historyDtos);
}
```

**Entity Framework Queries:**
```csharp
await _context.Bookings.Include(...).Where(...).OrderByDescending(...).ToListAsync();
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

## 5. SearchController (`SearchController.cs`)

### `GET /api/Search`
**Role & Explanation:** Executes a search for available buses traveling between two points on a given date. Filters out inactive schedules and ensures departures are at least 10 minutes in the future.

**Endpoint Code:**
```csharp
[HttpGet]
public async Task<IActionResult> SearchBuses([FromQuery] string from, [FromQuery] string to, [FromQuery] DateTime date)
{
    // ... validation and date setup ...

    // --- EF CORE QUERY START ---
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
    // --- EF CORE QUERY END ---

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

### `GET /api/Search/cities`
**Role & Explanation:** Returns an autocomplete list of unique cities that have active routes. Uses an in-memory cache to reduce load on the database.

**Endpoint Code:**
```csharp
[HttpGet("cities")]
public async Task<IActionResult> GetCities([FromQuery] string query)
{
    if (!_cache.TryGetValue("AllCities", out List<string>? allCities))
    {
        // --- EF CORE QUERY START ---
        var sources = await _context.Routes.Select(r => r.Source).Distinct().ToListAsync();
        var destinations = await _context.Routes.Select(r => r.Destination).Distinct().ToListAsync();
        // --- EF CORE QUERY END ---
        
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

## 6. NotificationsController (`NotificationsController.cs`)

### `GET /api/Notifications/user/{userId}`
**Role & Explanation:** Retrieves the 20 most recent notifications for a user.

**Endpoint Code:**
```csharp
[HttpGet("user/{userId}")]
public async Task<ActionResult<IEnumerable<Notification>>> GetUserNotifications(Guid userId)
{
    // --- EF CORE QUERY START ---
    var notifications = await _context.Notifications
        .Where(n => n.UserId == userId)
        .OrderByDescending(n => n.CreatedAt)
        .Take(20)
        .ToListAsync();
    // --- EF CORE QUERY END ---

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

### `PUT /api/Notifications/{id}/read`
**Role & Explanation:** Marks a specific notification as read.

**Endpoint Code:**
```csharp
[HttpPut("{id}/read")]
public async Task<ActionResult> MarkAsRead(Guid id)
{
    // --- EF CORE QUERY START ---
    var notification = await _context.Notifications.FindAsync(id);
    if (notification == null) return NotFound();

    notification.IsRead = true;
    await _context.SaveChangesAsync();
    // --- EF CORE QUERY END ---

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

### `DELETE /api/Notifications/user/{userId}/clear`
**Role & Explanation:** Deletes all notifications associated with a given user.

**Endpoint Code:**
```csharp
[HttpDelete("user/{userId}/clear")]
public async Task<ActionResult> ClearAll(Guid userId)
{
    // --- EF CORE QUERY START ---
    var notifications = await _context.Notifications
        .Where(n => n.UserId == userId)
        .ToListAsync();

    _context.Notifications.RemoveRange(notifications);
    await _context.SaveChangesAsync();
    // --- EF CORE QUERY END ---

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

## 7. LocationsController (`LocationsController.cs`)

### `GET /api/Locations/cities`
**Role & Explanation:** Simple endpoint to retrieve an ordered list of all cities.

**Endpoint Code:**
```csharp
[HttpGet("cities")]
public async Task<IActionResult> GetCities()
{
    // --- EF CORE QUERY START ---
    var cities = await _context.Cities
        .Select(c => new { c.Id, c.Name, c.State })
        .OrderBy(c => c.Name)
        .ToListAsync();
    // --- EF CORE QUERY END ---

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

### `GET /api/Locations/hubs`
**Role & Explanation:** Retrieves hubs belonging to a particular city, typically used to populate dropdowns in the UI.

**Endpoint Code:**
```csharp
[HttpGet("hubs")]
public async Task<IActionResult> GetHubs([FromQuery] Guid cityId)
{
    // --- EF CORE QUERY START ---
    var hubs = await _context.Hubs
        .Where(h => h.CityId == cityId)
        .Select(h => new { h.Id, h.Name, Type = h.Type.ToString() })
        .OrderBy(h => h.Name)
        .ToListAsync();
    // --- EF CORE QUERY END ---

    return Ok(hubs);
}
```

**Entity Framework Queries:**
```csharp
await _context.Hubs.Where(h => h.CityId == cityId).Select(...).OrderBy(h => h.Name).ToListAsync();
```

**PostgreSQL Mapping:**
```sql
SELECT "Id", "Name", "Type" 
FROM "Hubs" 
WHERE "CityId" = @CityId 
ORDER BY "Name";
```
