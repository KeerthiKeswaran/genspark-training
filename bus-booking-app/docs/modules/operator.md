# Operator Module Technical Documentation

This document provides a comprehensive A–Z implementation blueprint of the Operator Module in the Bus Booking System. It covers the architecture, data structures, business logic, and cross-layer interactions.

---

## Section 1 — High-Level Workflow

The operator workflow follows a strict sequential lifecycle to ensure data integrity and platform safety.

**Workflow Path:**
`Registration → Admin Approval → Fleet Registration → Route Assignment → Trip Scheduling → Live Monitoring`

### Workflow Step Breakdown:

1.  **Operator Onboarding**:
    *   **Internal**: Operator registers via the portal. Account is created in `Users` and `BusOperators` tables.
    *   **Data**: Full name, Company details, Contact info.
    *   **Systems**: Auth service, Notification service (Admin alert).

2.  **Fleet Registration (Add Bus)**:
    *   **Internal**: Operator adds a vehicle. Status is set to `Pending`.
    *   **Data**: Bus Number, Type (AC/Non-AC), Total Seats, Seat Layout JSON.
    *   **Systems**: Operator Controller, Database (Buses table).

3.  **Route & Schedule Lifecycle**:
    *   **Internal**: Operator selects an available route and assigns a bus to a specific time slot.
    *   **Data**: Route ID, Bus ID, Departure/Arrival Times, Price.
    *   **Systems**: Route Service, Schedule Management.

4.  **Trip Publishing**:
    *   **Internal**: Once scheduled, the trip becomes searchable by customers.
    *   **Systems**: Search Index (Real-time).

---

## Section 2 — Feature Breakdown

### 🚌 Fleet Management
*   **Purpose**: Register and manage the physical bus fleet.
*   **Logic**: Prevent deletion if the bus has active bookings. Validates bus number uniqueness.
*   **Input**: Bus details (JSON).
*   **Output**: Success/Error notification.

### 📐 Seat Configuration
*   **Purpose**: Define the physical layout of the bus (Grid structure).
*   **Logic**: Uses a JSON string (`LayoutConfig`) to store row/column coordinates for seats.
*   **Dependencies**: UI Seat Mapper component.

### 🗺️ Route Assignment
*   **Purpose**: Link a vehicle to a specific geographic path.
*   **Logic**: Fetches available system routes created by Admin.
*   **Restrictions**: A bus cannot be assigned to overlapping routes at the same time.

### ⏰ Schedule Management (Trips)
*   **Purpose**: Launch specific journeys for booking.
*   **Logic**: Validates that Arrival Time > Departure Time. Automatically assigns default boarding/dropping hubs based on city defaults.
*   **Input**: Route, Bus, Time, Price.

---

## Section 3 — Frontend Implementation (File-Level)

### [OperatorService](file:///Users/keerthikeswaran/Documents/Internship%20Tasks/bus-booking-app/client/src/app/core/services/operator.service.ts)
*   **Responsibility**: Centralized API communication for all operator actions.
*   **Key Functions**: `getBuses()`, `addBus()`, `deleteBus()`, `getSchedules()`, `createSchedule()`.
*   **API Calls**: `GET/POST/DELETE /api/Operator/*`.

### [FleetManagementComponent](file:///Users/keerthikeswaran/Documents/Internship%20Tasks/bus-booking-app/client/src/app/features/operator/fleet-management/fleet-management.component.ts)
*   **Responsibility**: UI for adding/removing buses and viewing approval status.
*   **Data Handled**: Array of `Bus` objects, `BusForm` state.
*   **Features**: Status badges (Pending/Approved/Rejected), Revoke Request logic.

### [ScheduleManagementComponent](file:///Users/keerthikeswaran/Documents/Internship%20Tasks/bus-booking-app/client/src/app/features/operator/schedule-management/schedule-management.component.ts)
*   **Responsibility**: Interface for launching and cancelling trips.
*   **Key Functions**: `onRouteSelect()`, `createSchedule()`, `cancelTrip()`.
*   **Features**: Read-only hubs preview, automated hub assignment preview.

---

## Section 4 — Backend Implementation

### [OperatorController](file:///Users/keerthikeswaran/Documents/Internship%20Tasks/bus-booking-app/server/Features/Operator/OperatorController.cs)
*   **Endpoint**: `api/Operator`
*   **Business Logic**:
    *   `CreateSchedule`: If hubs are omitted, it fetches the first 2 hubs for the source/destination cities automatically.
    *   `CancelSchedule`: Updates status to `Cancelled`, notifies all affected customers, and alerts Admin.
    *   `DeleteBus`: Prevents removal if any booking history exists.

### [OperatorDtos](file:///Users/keerthikeswaran/Documents/Internship%20Tasks/bus-booking-app/server/Features/Operator/OperatorDtos.cs)
*   **Responsibility**: Defines the request/response shapes for bus registration and scheduling.
*   **Validation**: Uses Data Annotations (`[Required]`, `[Range]`) for price and seat counts.

---

## Section 5 — API Endpoints

| Method | URL | Purpose | Request Body | Response |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/Operator/{id}/buses` | Fetch fleet | None | `Bus[]` |
| `POST` | `/api/Operator/{id}/buses` | Register Bus | `BusRequestDto` | `201 Created` |
| `DELETE` | `/api/Operator/{id}/buses/{bid}` | Revoke/Remove Bus | None | `204 No Content` |
| `POST` | `/api/Operator/{id}/schedules` | Launch Trip | `ScheduleRequestDto` | `Schedule` |
| `PUT` | `/api/Operator/{id}/schedules/{sid}/cancel` | Cancel Trip | None | `Message` |

---

## Section 6 — Database Schema

### Table: `Buses`
| Column | Type | Purpose |
| :--- | :--- | :--- |
| `Id` | `Guid` | Primary Key |
| `BusNumber` | `string` | Unique registration number |
| `LayoutConfig` | `string` | JSON string of seat coordinates |
| `OperatorId` | `Guid` | Foreign Key to `BusOperators` |

### Table: `Schedules`
| Column | Type | Purpose |
| :--- | :--- | :--- |
| `Id` | `Guid` | Primary Key |
| `BusId` | `Guid` | FK to `Buses` (Cascade delete restricted if bookings exist) |
| `RouteId` | `Guid` | FK to `Routes` |
| `Status` | `int` | `Scheduled (0)`, `Completed (1)`, `Cancelled (2)` |

---

## Section 7 — Data Flow (End-to-End)

1.  **Fleet Creation**: Operator submits Form → Frontend `OperatorService.addBus()` → Backend `OperatorController.AddBus()` → DB `Buses` table → Admin receives notification.
2.  **Trip Launch**: Operator selects Route → Backend fetches default hubs → Schedule saved with `JourneyStatus.Scheduled` → Real-time Search Controller now includes this ID in results.
3.  **Booking Impact**: Customer books seat → `Schedule.AvailableSeats` decrements → Operator Dashboard reflects updated occupancy % in real-time.

---

## Section 8 — Business Rules & Constraints

*   **Bus Removal**: Forbidden if the bus has **any** historical or future bookings. This preserves the customer's "My Bookings" history.
*   **Trip Cancellation**: Only allowed for future trips. Triggers automatic customer notifications and refund initiation.
*   **Seat Capacity**: Must be between 1 and 60.
*   **Hub Assignment**: If the operator doesn't select hubs, the system defaults to the top 2 ranked hubs per city to ensure the trip is valid for search.

---

## Section 9 — Libraries & Dependencies

### Frontend
*   **Angular 17 (Signals)**: Used for reactive UI updates (Notifications, Fleet status).
*   **RxJS**: Handles asynchronous API streams and `forkJoin` for combining data.
*   **Vanilla CSS**: Custom design system for the premium dashboard look.

### Backend
*   **Entity Framework Core**: ORM for PostgreSQL. Handles complex TPT (Table-Per-Type) inheritance for Users.
*   **BCrypt.Net**: Password hashing for security.
*   **System.Text.Json**: Handles serialization of the Seat Layout configurations.

---

## Section 10 — Error Handling & Edge Cases

*   **Conflict (409)**: Occurs if trying to register a Bus Number that already exists in the system.
*   **Dependency Violation**: Occurs if trying to delete a bus linked to existing bookings. Handled via a defensive check in `OperatorController`.
*   **Past Departure**: The system automatically hides trips from search results if the departure time is in the past or within 10 minutes of now.

---

## Section 11 — Project Structure

```text
/client
  /src/app/features/operator
    /fleet-management       <-- Bus registration & listing
    /schedule-management    <-- Trip launching & cancellation
    /operator-home          <-- Dashboard & Analytics
/server
  /Features/Operator
    OperatorController.cs   <-- Core business logic
    OperatorDtos.cs         <-- Data contracts
  /Core/Entities
    BusEntities.cs          <-- DB Models (Bus, Schedule)
```

---

## Section 12 — Feature Mapping

| Feature | Files | APIs | DB Tables |
| :--- | :--- | :--- | :--- |
| Bus Registration | `fleet-management.component.ts` | `POST /buses` | `Buses` |
| Trip Launching | `schedule-management.component.ts`| `POST /schedules` | `Schedules`, `Routes` |
| Occupancy Tracking| `operator-home.component.ts` | `GET /schedules` | `Bookings`, `Schedules` |

---

## Section 13 — Risks & Improvements

*   **Risk**: If an operator cancels a trip with 50+ bookings, the sequential notification loop might be slow.
*   **Improvement**: Implement a message queue (e.g., RabbitMQ or Azure Service Bus) for bulk notifications and background refund processing.
*   **Gap**: Currently, the system does not support "Seasonal Pricing" (automated price hikes for weekends). This is a recommended future enhancement.
