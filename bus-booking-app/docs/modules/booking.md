# Booking Module: Complete Implementation Blueprint

This document provides a deep, engineering-grade decomposition of the entire Booking Module for the Bus Reservation System. It covers the full flow from seat selection to payment, detailing the frontend architecture, backend services, database schema, and critical business constraints.

---

## 1. Sub-Modules Overview

The booking system is composed of several tightly integrated sub-modules:

1. **Seat Selection**: Renders a dynamic, visual 2D grid of the bus layout. Identifies available, booked, and blocked seats (with gender-specific coloring).
2. **Passenger Details Collection**: Captures names, ages, and genders for selected seats, while ensuring dynamic form generation based on the seat count.
3. **Boarding/Dropping Selection**: Integrates with route data to let users choose specific pickup and drop-off hubs.
4. **Pricing Calculation**: Dynamically computes base fare and mandatory convenience fees in real-time.
5. **Seat Locking (Concurrency Control)**: Temporarily reserves seats (5-minute grace period) to prevent race conditions while the user completes payment.
6. **Payment Processing**: Validates the locked seats, confirms total price, and finalizes the booking status.
7. **Booking History**: Allows users to view past and upcoming journeys, with 24-hour strict cancellation policies.

---

## 2. File-Level Breakdown

### 2.1. Frontend (Angular Client)

#### Components
* **`seat-layout.component.ts`**
  * **Purpose**: Displays the bus seating chart and handles seat selection.
  * **Key Methods**: `loadLayout()`, `proceedToDetails()`.
  * **Dependencies**: `BookingService` for layout data and lock availability.
  * **State**: Uses Signals (`loading`, `selectedSeats`, `layout`) to manage reactive updates. Computes `basePrice` and `totalPrice`.
* **`passenger-form.component.ts`**
  * **Purpose**: Collects passenger information and locks seats upon submission.
  * **Key Methods**: `buildForm()`, `onSubmit()`, `fetchJourneyDetails()`.
  * **Dependencies**: `BookingService`, `Router` state navigation.
  * **Logic**: Employs Angular Reactive Forms (`FormArray`) to dynamically generate passenger inputs. Triggers the backend `lockSeats` endpoint only when proceeding to payment.
* **`payment.component.ts`**
  * **Purpose**: Final checkout, dummy payment gateway animation, and booking confirmation.
  * **Key Methods**: `processPayment()`, `handleExpiry()`.
  * **Dependencies**: `BookingService`.
  * **Logic**: Simulates a payment gateway with animated visual feedback (1.5s spinner -> checkmark), enforces the 5-minute strict countdown timer. Falls back to `localStorage` or secondary `fetchJourneyDetails` if router state is dropped. Upon success, clears pending local storage and triggers the final confirm endpoint.
* **`booking-history.component.ts`**
  * **Purpose**: Displays user bookings and handles cancellations. Accessible globally via the main Navigation Bar.
  * **Key Methods**: `loadBookings()`, `cancelBooking()`.
  * **Logic**: Consumes the `GET /history` endpoint. Distinguishes between "Upcoming" and "Past" bookings using strictly server-managed UTC logic to prevent timezone drift. Displays full route, seat, status, and bus operator metadata.

#### Services & Models
* **`booking.service.ts`**
  * **Purpose**: The central API communication layer for booking operations.
  * **Key Endpoints Wrapped**: `getLayout()`, `lockSeats()`, `releaseLocks()`, `confirmBooking()`.
* **`booking.models.ts`**
  * **Purpose**: Defines TypeScript interfaces (`SeatLayout`, `SeatStatus`, `BookingDto`) to strictly type all API requests/responses.

#### State Management & Routing
* **State Transfer**: Angular's Router `state` object is the primary method for moving large objects (like `journeyDetails` and `passengers`) between sequential components (`SeatLayout` → `PassengerForm` → `Payment`).
* **Persistence**: LocalStorage (`pendingBookings`) acts as a fallback to reconstruct state if the user refreshes the page or navigates via the Home page's "Continue Booking" banner.

---

### 2.2. Backend (ASP.NET Core Server)

#### Controllers
* **`BookingController.cs`**
  * **Responsibilities**: Serves REST endpoints for the complete booking funnel.
  * **Endpoints**: 
    * `GET /layout/{journeyId}`: Returns grid configurations, blocked/booked seats, and parses full Operator strings (e.g., "Intercity").
    * `POST /lock-seats`: Initiates the atomic 5-minute seat lock.
    * `POST /confirm`: Validates the lock and creates the finalized Booking and Payment records.

#### Services & Managers
* **`SeatLockManager.cs`**
  * **Purpose**: Manages optimistic concurrency for seats.
  * **Logic**: Periodically triggers `ReleaseExpiredLocks` (or runs inline validation) using strictly UTC timestamps to clear abandoned locks back to the available pool.
* **`CancellationService.cs`**
  * **Purpose**: Handles strict business rules around booking reversals.
  * **Logic**: Validates the 24-hour pre-departure rule before marking a booking as `CANCELLED` and releasing the seats.

#### DTOs
* **`BookingDtos.cs`**: Contains strongly-typed incoming models (`LockSeatsRequest`, `ConfirmBookingRequest`) and outgoing models (`SeatLayoutDto`, `SeatStatusDto`).

---

## 3. Database Schema Design (EF Core / PostgreSQL)

### Core Tables

1. **`Bookings`**
   * **Columns**: `Id`, `CustomerId`, `JourneyId`, `BookingDate`, `TotalAmount`, `Status` (Enum: Pending, Confirmed, Cancelled), `BoardingHubId`, `DroppingHubId`.
   * **Indexes**: Composite Index on `[CustomerId, JourneyId]` for fast history lookups.
2. **`SeatLocks`**
   * **Columns**: `Id`, `SeatNumber`, `JourneyId`, `LockedByUserId`, `ExpiresAt`.
   * **Indexes**: Composite Index on `[LockedByUserId, JourneyId]`.
3. **`Passengers`**
   * **Columns**: `Id`, `BookingId`, `FullName`, `Age`, `Gender` (Enum), `SeatNumber`.
   * **Relationships**: Belongs to one `Booking`.
4. **`Payments`**
   * **Columns**: `Id`, `BookingId`, `Amount`, `TransactionId`, `Status`, `CreatedAt`.
5. **`Schedules` (Journeys)**
   * **Columns**: `Id`, `BusId`, `RouteId`, `DepartureTime`, `AvailableSeats`.
   * **Indexes**: Indexed heavily on `DepartureTime` for search and cancellation validation.
6. **`Buses` & `BusOperators` (TPH/TPT Inheritance)**
   * **Columns**: `BusNumber`, `LayoutConfig` (JSON representing the grid matrix). Operators are tied to buses via `OperatorId` (Foreign Key to `Users` table acting as BusOperators).

### Consistency Mechanisms
* **Transactions**: `confirmBooking` is wrapped in an implicit database transaction. If payment record creation fails, the passenger and booking records are rolled back.
* **Concurrency**: Utilizing explicit EF Core `.AsNoTracking()` on heavy read operations (like Layout rendering) ensures real-time accurate counts without locking the context.

---

## 4. End-to-End Data Flow

1. **Initialization**: User navigates from Search Results. `SeatLayoutComponent` mounts and calls `BookingController.GetLayout`.
2. **Pre-flight Checks**: UI validates the user's JWT token is valid before allowing seat selection.
3. **Seat Selection**: User visually clicks seats. Signal state arrays are updated.
4. **Passenger Details**: Router state transfers seat selection to `PassengerFormComponent`. User assigns details and picks Hubs.
5. **Atomic Lock**: Upon clicking "Proceed", `POST /lock-seats` is fired. 
   * *Backend Check*: Verifies seats aren't booked/blocked by others.
   * *Backend Action*: Creates `SeatLock` records expiring in exactly 5 minutes (UTC).
6. **Payment Phase**: Navigates to `PaymentComponent`. A rigid 5-minute UI countdown begins.
7. **Confirmation**: `POST /confirm` is triggered.
   * *Backend Validation*: Checks if `ExpiresAt` is still valid.
   * *Commit*: Creates `Booking`, `Passengers`, and `Payment`. Deletes `SeatLocks`.
8. **Completion**: Navigates to `HistoryComponent`. `localStorage` pending flags are cleaned up.

---

## 5. Critical Business Rules & Constraints

* **Seat Locking & Timeout**:
  * *Enforcement*: Frontend blocks progression after 5 mins; Backend definitively refuses `confirm` operations if `DateTime.UtcNow > SeatLock.ExpiresAt`. 
* **Gender-Based Restriction Logic**:
  * *Enforcement*: Passenger forms require `Gender`. The layout dynamically maps backend enums to distinct UI indicators (`.booked-female` in pink, `.booked-male` in blue). Furthermore, a **Female Zone Advisory** is enforced: if a user selects a seat immediately adjacent (e.g., A1↔A2) to a seat already booked by a female, a non-intrusive 5-second pink warning toast pops up. This alerts the user that the section is reserved for female travellers and warns of strict action against male strangers boarding in that pair.
* **Booking Expiry Handling**:
  * *Enforcement*: If the payment timer hits zero, the UI automatically fires `releaseLocks` and routes the user back to the layout screen, forcefully flushing the `localStorage`.
* **Booking Categorization**:
  * *Enforcement*: Bookings are dynamically categorized into three tabs: **Upcoming**, **Completed**, and **Cancelled**. A trip is only considered "Completed" once the **ArrivalTime** (dropping time) has passed. If the status is explicitly set to `Cancelled`, it takes precedence and remains in the Cancelled tab regardless of time.

---

## 6. Error Handling & Edge Cases

* **API Failures / Dropped Connections**: 
  * If state drops, components execute `fetchJourneyDetails()` fallbacks to reconstruct context based on `journeyId` and URL query parameters.
* **Seat Sniping (Already Booked/Locked)**: 
  * If a user tries to lock a seat taken milliseconds prior, the backend returns a `400 Bad Request`. The UI catches this, alerts the user ("Seats might have been taken"), and gracefully refreshes the layout.
* **Expiry During Payment Execution**: 
  * If the user clicks "Pay" exactly as the timer hits zero, the backend validates the UTC timestamp, rejects the commit, and redirects them to the beginning.
* **Incomplete User Input**: 
  * Angular Reactive Forms strictly enforce `Validators.required` and `Validators.min(1)`. The "Proceed" buttons remain physically disabled (`[disabled]="bookingForm.invalid"`) preventing bad API calls.

---

## 7. Folder & Project Structure

A clean separation of concerns is maintained:

### Frontend (`client/src/app/`)
```
core/
 ├── services/
 │   ├── booking.service.ts       # Central API calls
 ├── models/
 │   ├── booking.models.ts        # Interfaces
features/
 ├── booking/
 │   ├── seat-layout/             # Selection grid UI
 │   ├── passenger-form/          # Passenger detail array
 │   ├── payment/                 # Countdown and checkout
 │   ├── history/                 # Past/upcoming bookings
```

### Backend (`server/`)
```
Core/
 ├── Entities/
 │   ├── BookingEntities.cs       # Booking, Passenger, Payment, SeatLock
 │   ├── BusEntities.cs           # Bus, Schedule, Route
Features/
 ├── Booking/
 │   ├── BookingController.cs     # Main entry point
 │   ├── BookingDtos.cs           # Request/Response shapes
 │   ├── SeatLockManager.cs       # Concurrency logic
 │   ├── CancellationService.cs   # Refund/Cancellation business logic
```

---

## 8. Summary, Risks & Next Steps

### A-Z Flow Summary
The module provides a fully transactional, robust pipeline that handles edge cases around seat contention, user abandonment, and state persistence flawlessly without requiring page reloads.

### Architectural Gaps & Risks
* **In-Memory/DB Lock Overhead**: Currently, `SeatLocks` are handled in PostgreSQL. Under massive concurrent load (e.g., thousands of users hitting the same festival bus), querying the DB for lock expiration could bottleneck. 
* **State Tampering**: Relying heavily on `localStorage` for the "Continue Booking" banner allows advanced users to theoretically manipulate local timers, though the strict backend UTC checks prevent any actual exploits.

### Suggestions for Improvement
1. **Redis Implementation**: Migrate `SeatLocks` from the PostgreSQL `SeatLocks` table to Redis using simple Key-Value pairs with Native TTLs (Time-To-Live). This eliminates the need for manual `ReleaseExpiredLocks` logic and speeds up checking.
2. **SignalR / WebSockets**: Instead of users clicking "Proceed" to find out a seat is taken, broadcast seat locks over WebSockets to turn other users' screens red in real-time.
3. **CSS Budgets**: Certain components (`payment`, `seat-layout`) are breaching Angular's 4kB styling budget. Minor refactoring of shared CSS variables could optimize bundle sizes.
