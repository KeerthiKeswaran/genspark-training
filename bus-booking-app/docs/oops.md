# Object-Oriented Programming (OOP) Architecture

This document details the Object-Oriented Programming principles utilized within the backend of the Bus Booking Application. It provides a comprehensive overview of the classes, inheritance hierarchies, polymorphism implementations, and the interface-driven service architecture present in the current implementation.

## 1. Domain Models (Classes)

The system relies on various model classes to represent domain entities. These classes encapsulate the data attributes and sometimes fundamental logic pertaining to the application's business rules.

### Core Entities
- **User**: The base entity representing any authenticated individual in the system.
  - Path: `server/Core/Entities/User.cs`
- **Admin**: Represents administrative personnel with elevated privileges.
  - Path: `server/Core/Entities/Admin.cs`
- **BusOperator**: Represents a vendor or company managing a fleet of buses.
  - Path: `server/Core/Entities/BusOperator.cs`
- **Bus**: Represents an individual bus unit managed by a bus operator.
  - Path: `server/Core/Entities/BusEntities.cs`
- **BusRoute**: Represents a geographical route that buses travel across.
  - Path: `server/Core/Entities/BusEntities.cs`
- **Schedule**: Defines the timing, pricing, and specific route instantiation for a bus trip.
  - Path: `server/Core/Entities/BusEntities.cs`
- **City**: Represents a primary geographical node for routes.
  - Path: `server/Core/Entities/LocationEntities.cs`
- **Hub**: Represents specific boarding or dropping points within a city.
  - Path: `server/Core/Entities/LocationEntities.cs`
- **Booking**: Represents a ticket reservation made by a user.
  - Path: `server/Core/Entities/BookingEntities.cs`
- **Passenger**: Represents individual passenger details associated with a booking.
  - Path: `server/Core/Entities/BookingEntities.cs`
- **Payment**: Represents the financial transaction corresponding to a booking.
  - Path: `server/Core/Entities/BookingEntities.cs`
- **SeatLock**: Represents a temporary reservation of seats during the checkout process to prevent double booking.
  - Path: `server/Core/Entities/BookingEntities.cs`
- **Notification**: Represents system alerts or messages directed to users or operators.
  - Path: `server/Core/Entities/Notification.cs`
- **PlatformSetting**: Represents configurable system-wide parameters (like fee percentages).
  - Path: `server/Core/Entities/PlatformSetting.cs`
- **GlobalConfiguration**: Represents single-instance application configurations.
  - Path: `server/Core/Entities/GlobalConfiguration.cs`

## 2. Inheritance Architectures

Inheritance is extensively used to establish "is-a" relationships, promoting code reusability and establishing logical entity hierarchies.

### User Hierarchy
The most prominent domain-level inheritance exists within the user system:
- **User** is the base class containing common properties (Id, FullName, Email, Phone, PasswordHash, Role, CreatedAt).
- **Admin** inherits from **User**. It extends the base user with administrative specific properties (`IsSuperAdmin`, `CreatedByAdminId`, `Department`).
- **BusOperator** inherits from **User**. It extends the base user with operator-specific details (`CompanyName`, `IsApproved`, `Status`, `RejectionReason`, `Address`).

### Framework Level Inheritance
The project also inherits heavily from foundational ASP.NET Core and Entity Framework Core classes:
- **Controllers**: All API controllers (`AuthController`, `AdminController`, `BookingController`, `SearchController`, `OperatorController`, `LocationsController`, `NotificationsController`) inherit from the ASP.NET Core `ControllerBase` class to access HTTP context, routing, and response formatting capabilities.
  - Example Path: `server/Features/Admin/AdminController.cs`
- **Database Context**: `AppDbContext` inherits from Entity Framework Core's `DbContext` class to inherit database connection and entity-mapping features.
  - Path: `server/Infrastructure/Data/AppDbContext.cs`
- **Migrations**: Database migration files inherit from Entity Framework's `Migration` base class to execute schema changes.
  - Example Path: `server/Migrations/20260422093433_InitialCreate.cs`

## 3. Polymorphism Implementations

Polymorphism allows objects to be treated as instances of their parent class or implemented interfaces, allowing for highly decoupled and extensible code.

### Interface-Based Polymorphism (Dependency Injection)
The most critical use of polymorphism in this project is achieved via interfaces and Dependency Injection (DI). Controllers and services depend on abstractions (Interfaces) rather than concrete implementations. At runtime, the IoC (Inversion of Control) container injects the appropriate class. 
For example, a controller requesting an `INotificationService` can be provided with either a `NotificationService` or a `MockNotificationService` without altering the controller's code. This is dynamic polymorphism that ensures loosely coupled architecture.

### Object-Relational Polymorphism
Entity Framework Core maps the `User`, `Admin`, and `BusOperator` inheritance hierarchy to the underlying relational database using strategies like Table-Per-Hierarchy (TPH). EF Core leverages polymorphism to load the correct derived entity type when querying the `Users` DbSet based on a discriminator column, allowing queries against the generic `User` type to yield specific `Admin` or `BusOperator` instances at runtime.

## 4. Interfaces and Services

Interfaces define behavioral contracts. The project implements a clean architecture where feature logic is encapsulated within services that adhere to specific interfaces.

### Notification System
- **Interface**: `INotificationService`
  - Path: `server/Core/Interfaces/INotificationService.cs`
- **Implementations**:
  - `NotificationService`: The concrete implementation for production use, handling logic to record and dispatch system notifications.
    - Path: `server/Infrastructure/Services/NotificationService.cs`
  - `MockNotificationService`: A secondary polymorphic implementation, likely used for testing or bypassing actual notification dispatch in developmental environments.
    - Path: `server/Infrastructure/Services/MockNotificationService.cs`

### Booking and Cancellation
- **Interface**: `ICancellationService`
  - Path: `server/Features/Booking/CancellationService.cs`
- **Implementation**: `CancellationService` implements the contract to handle the business rules regarding booking cancellations and refund calculations.
  - Path: `server/Features/Booking/CancellationService.cs`

### Concurrency and Seat Management
- **Interface**: `ISeatLockManager`
  - Path: `server/Features/Booking/SeatLockManager.cs`
- **Implementation**: `SeatLockManager` implements the contract for creating, validating, and expiring temporary holds on bus seats to manage concurrency during the checkout flow.
  - Path: `server/Features/Booking/SeatLockManager.cs`

### Administrative Workflows
- **Interface**: `IOperatorWorkflowService`
  - Path: `server/Features/Admin/OperatorWorkflowService.cs`
- **Implementation**: `OperatorWorkflowService` implements the governance logic for approving, rejecting, and tracking bus operator registrations within the administration portal.
  - Path: `server/Features/Admin/OperatorWorkflowService.cs`
