# Bus Booking App - Requirements Decomposition

Based on the project meeting notes, here is a detailed and clean decomposition of the requirements for the Bus Booking Application.

## 1. Technology Stack
The application is mandated to be built using a specific, modern technology stack:
*   **Database:** PostgreSQL (Note: Azure deployment section briefly mentions MySQL, but PostgreSQL is strictly emphasized as mandatory).
*   **Backend:** .NET Web APIs (C#)
*   **Frontend:** Angular
*   **Styling:** CSS, Bootstrap, or Material Design
*   **Cloud Provider:** Microsoft Azure
*   **Containerization & Orchestration:** Docker, Azure Kubernetes Service (AKS)
*   **Messaging:** Azure Event Grid (for event-based triggering)

## 2. User Roles
The system will cater to three distinct types of users:
1.  **Customers (Users):** End-users who search for, book, and manage their bus tickets.
2.  **Bus Operators:** Business entities that manage their fleet, set up routes, and monitor bookings.
3.  **Administrators:** Platform owners who manage master data (locations), platform fees, and oversee operator approvals.

---

## 3. Functional Requirements

### 3.1. Customer (User) Features
**Authentication & Profile Management**
*   **Login/Registration:** Users must log in to book seats. Identification can be via email, phone number, or a simple username/password.
*   **Single Sign-On (SSO):** Optional, but not mandatory.

**Search & Discovery**
*   **Guest Access:** Users can view and search for buses without logging in.
*   **Search Functionality:** Users can search for buses by source, destination, and travel date.
*   **Smart Input:** Pre-populated source and destination fields must support fuzzy logic for better user experience.
*   **Bus Listing:** The search results should display available bus options along with real-time available seat counts.

**Booking & Reservation**
*   **Seat Selection:** Users must be able to view a visual layout reflecting the bus capacity and select their desired seats.
*   **Grace Period:** The system must support a grace period (temporary block) for seats while the user completes the reservation process.
*   **Passenger Details:** When booking multiple seats, the user must provide the Name, Age, and Gender for *each* individual passenger.
*   **Special Considerations (Edge Cases):** The system should optionally handle logic like indicating female-only booked seats in the visual layout.

**Payment & Ticketing**
*   **Payment Gateway:** Can be a dummy gateway or integrated with real providers like Stripe or RazorPay.
*   **Pricing:** The final price shown to users must include a platform "convenience" fee.
*   **Confirmation:** Tickets must be downloadable. Optional email and SMS message confirmations should be supported.

**Booking Management (Post-Booking)**
*   **Travel History:** Logged-in users can view their upcoming, past, and cancelled bookings.
*   **Cancellations:** Users can cancel their tickets up to a strict limit of **24 hours before departure**.
*   **Refund Logic:** Cancellations must trigger business rules governing refund percentages based on the cancellation window.
*   **Real-time Availability:** Cancelled seats must be instantly released and made available to other users.

### 3.2. Bus Operator Features
**Registration & Onboarding**
*   **Approval Workflow:** Operators must register and receive Admin approval before they can log in or operate.
*   **Location Setup:** Operators must establish a presence (provide addresses) at both source and destination locations. These addresses act as the exact boarding and drop-off points.

**Fleet & Bus Management**
*   **Adding Buses:** Operators can add new buses (requires providing the vehicle registration number). *Note: The notes mention adding buses is subject to admin approval.*
*   **Bus Deactivation:** Operators can temporarily or permanently remove their buses from the platform without needing admin approval.
*   **Seat Layouts:** Operators must upload or dynamically generate seat layouts based on capacity. These layouts must be visible to customers during booking.

**Route Management**
*   **Route Selection:** Operators select routes from a master list of sources/destinations created by the Admin.
*   **Point-to-Point Only:** The system **only** supports direct, point-to-point routes. No intermediate pickup or drop-off stops are allowed.
*   **Scheduling:** Multiple buses (even from the same operator) can operate on the same route/destination, with timings determined by the operator.

**Pricing & Analytics**
*   **Seat Pricing:** Operators set a single, flat price per seat (no dynamic pricing models).
*   **Dashboard:** Operators have access to an admin panel to view their bookings and overall revenue.

### 3.3. Administrator Features
**Master Data Management**
*   **Locations:** Admins are solely responsible for adding all valid Source and Destination cities/locations to the platform.

**User & Operator Management**
*   **Operator Approvals:** Admins review and approve new Bus Operators and their subsequent bus additions.
*   **Operator Deactivation:** Admins can disable a bus operator.
    *   *Triggered Actions:* If disabled, both the operator and affected customers must be notified. Booking statuses must be updated, and refund processes communicated.
*   **Customer Oversight:** Admins do not need to manually approve standard customers.

**Platform Settings**
*   **Fee Management:** Admins can adjust the platform (convenience) fee applied to bookings. This can be configured as a fixed amount or a percentage.

---

## 4. Non-Functional Requirements (NFRs)

*   **Security & Data Privacy:**
    *   Passwords must be securely hashed and never stored in clear text.
    *   Strict security checks must be in place to ensure sensitive customer data (like order details and payment info) is not exposed to unauthorized entities.
*   **Localization:** The application architecture must support locale and time zone configurations natively.
*   **Performance:** Performance constraints must be factored into the architecture from the beginning, especially considering database queries, API response times, and real-time seat availability syncing.

## 5. Application Architecture & Deployment

*   **Design Paradigm:** The project emphasizes a database-first design approach, starting with robust PostgreSQL schemas (stored procedures, geospatial queries) before moving to C# backend and Angular frontend.
*   **Integration:** Clear separation of concerns between database provisioning, backend business logic, and front-end requirements.
*   **Cloud Infrastructure (Azure):**
    *   Network isolation using Private and Public subnets.
    *   Internet gateway configurations.
    *   Backend services and databases will be Dockerized and managed via Azure Kubernetes Service (AKS).
*   **Event-Driven:** Utilizing Event Grid for messaging and handling asynchronous operations (like cancellation notifications and seat unblocking).
