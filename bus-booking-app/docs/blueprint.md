# Application Blueprint: Bus Booking Platform

This document serves as the master architectural blueprint for the Bus Booking System. It provides an end-to-end technical overview designed for engineers, architects, and product owners to understand the system's inner workings without direct code inspection.

---

## 🌍 1. High-Level Overview
The platform is a multi-tenant, role-based bus transportation management and booking system. It facilitates the entire lifecycle of bus travel: from bus operators registering their fleets and scheduling trips, to customers searching and booking seats, and administrators overseeing platform health and financial settlements.

### Core Objectives:
*   **Transparency**: Real-time seat availability and status tracking.
*   **Safety**: Validated bookings and secure payment workflows.
*   **Efficiency**: Automated scheduling and revenue calculation for operators.

---

## 🛠️ 2. Integrated Tech Stack
The application is built on a modern, decoupled architecture where the frontend and backend interact via a strictly typed RESTful contract.

*   **Frontend**: **Angular 17 (Standalone)**. Utilizes **Signals** for reactive state management, **Vanilla CSS** for a custom design system, and **RxJS** for asynchronous stream handling.
*   **Backend**: **.NET 8 Web API**. Uses a clean feature-based folder structure.
*   **ORM**: **Entity Framework Core**. Implements **Table-Per-Type (TPT)** inheritance for user roles and handles complex relational mapping.
*   **Database**: **PostgreSQL**. Chosen for its robust support for relational data and JSONB types (used for bus layouts).
*   **Security**: **JWT (JSON Web Tokens)** for stateless authentication and **BCrypt.Net** for credential hashing.

---

## 🔄 3. Complete System Workflow

1.  **Onboarding**: Users register as either Customers or Operators. Operators must be approved by an Admin before they can register vehicles.
2.  **Fleet & Route Setup**: Approved Operators register buses (defining physical seat layouts) and link them to system-defined Routes.
3.  **Trip Activation**: Operators "Launch" a trip by creating a Schedule. The system automatically assigns boarding/dropping hubs to make the trip bookable.
4.  **Customer Search**: Customers search by Source, Destination, and Date. The system filters active, non-cancelled trips.
5.  **Booking Lifecycle**: 
    *   **Selection**: Customer selects a trip and chooses specific seats from the bus layout.
    *   **Locking**: Seats are temporarily "locked" during passenger detail entry.
    *   **Confirmation**: Upon successful payment, the booking status moves from Pending to Confirmed.
6.  **Settlement**: Admin panel aggregates total bookings, calculates platform fees, and displays the net payout due to operators.

---

## 🏗️ 4. System Design Diagram

```text
[ Browser / Mobile ] 
      │
      ▼
[ Angular Frontend ] ◄─────[ Signals / RxJS State ]
      │
      ▼
[ REST API (ASP.NET Core) ] ◄───[ JWT Middleware ]
      │
      ▼
[ Business Logic Layer ] ◄─────[ Domain Entities ]
      │
      ▼
[ EF Core ORM ]
      │
      ▼
[ PostgreSQL Database ]
```

---

## 🧩 5. Module Breakdown

### **Customer Module**
Handles searching, seat selection, passenger profiling, and booking history. Its primary responsibility is the "Happy Path" conversion from search to payment.

### **Operator Module**
Dedicated to logistics management. Operators manage their "Fleet" (physical buses) and "Schedules" (active trips). It includes specialized logic for trip cancellation and customer notification.

### **Admin Module**
The "Command Center" of the platform. Responsible for approving new operators, authorizing new buses, managing global fee settings (commissions/flat fees), and monitoring platform-wide financial metrics.

---

## 💾 6. Data Flow & State Management

The system uses a **Hybrid State Model**:
*   **URL-Driven State**: Crucial booking parameters (Source, Destination, JourneyId) are stored in query parameters. This ensures that the state survives page refreshes and enables deep-linking.
*   **Angular Signals**: Used for real-time UI updates, such as the notification count in the header or the live "Syncing" status on the admin dashboard.
*   **Backend Source of Truth**: The database is the final validator. Even if the frontend passes a "Price," the backend re-calculates it during the booking process to prevent tampering.

---

## ⚙️ 7. Core Algorithms & Logic

### **Seat Layout Mapper**
Uses a coordinate-based JSON system `(row, col)` to render a 2D grid. The logic differentiates between "Walkways" and "Seats," mapping each seat coordinate to a unique ID for booking.

### **Automated Hub Assignment**
When an operator creates a trip, the system automatically fetches the top-ranked boarding point from the source city and the top-ranked dropping point from the destination city, ensuring every trip has valid stop locations by default.

### **Revenue Aggregation Engine**
Calculates `Platform Fee + Operator Payout = Total Amount`. It accounts for different fee types (Flat vs. Percentage) defined in the Global Configuration.

---

## 🚨 8. Edge Cases & Failure Scenarios

*   **The Double-Booking Race**: Two users click "Confirm" on the same seat at the same millisecond. The database enforces a `Unique Index` on `(ScheduleId, SeatNumber)`, causing the second transaction to fail safely.
*   **Trip Cancellation**: If an operator cancels a trip with 20 active bookings, the system triggers a recursive notification loop to alert all 20 customers and marks their bookings for refund.
*   **Bus Removal Protection**: A bus cannot be deleted if it has any historical booking records, ensuring that customers' "Past Trips" history remains intact.

---

## 🚀 9. Performance & Scalability

*   **Lazy Loading**: Modules are only loaded when accessed (e.g., the Admin dashboard code isn't downloaded for a Customer).
*   **Database Indexing**: Critical paths like `DepartureTime` and `Route Source/Destination` are indexed to handle thousands of trips.
*   **Caching**: City and Route definitions are cached in-memory on the backend to reduce redundant DB hits for static data.

---

## 🛡️ 10. Security Considerations

*   **Price Integrity**: The system ignores any price sent from the client during booking. It always performs a fresh server-side lookup of the `Schedule.Price`.
*   **Role-Based Access Control (RBAC)**: Controllers are decorated with `[Authorize(Roles = "Admin/Operator")]` to ensure users cannot access management endpoints via direct URL manipulation.

---

## ⚠️ 11. Common Pitfalls & Lessons

*   **Timezone Mismatch**: All timestamps are stored in **UTC** to avoid "Missing Bus" errors when users and servers are in different timezones.
*   **Data Leakage**: The system uses DTOs to strip away sensitive info (like Operator phone numbers or internal DB IDs) before sending data to the public search results.

---

## 🏙️ 12. Real-World Parallels
This system is architected similarly to industry leaders like **RedBus** or **MakeMyTrip**. Like these systems, it separates the **Inventory Provider** (Operator) from the **Marketplace** (Platform), using a centralized clearinghouse for payments and a distributed system for seat inventory.

---

## 📝 13. Summary
The platform is a robust, state-driven application that prioritizes data integrity and user experience. By combining the reactive power of Angular 17 with the reliability of .NET 8, it provides a scalable foundation for modern transportation logistics.
