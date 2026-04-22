# Bus Booking App - System Workflow and Design

This document explains the complete system flow, architecture, and design of the Bus Booking Application. It is intended to build a clear mental model of the system before coding begins.

## 1. System Design Diagram

The following text-based diagram illustrates the flow of requests and data across the system components:

```text
[Client] (Angular Web App)
   │
   ├── 1. HTTP Request (Search, Book, Pay)
   ▼
[API Gateway] (Azure Internet Gateway)
   │
   ├── 2. Route Request
   ▼
[Backend Server] (.NET C# Web APIs)
   │
   ├── 3. Read/Write Data               ├── 4. Publish Event (e.g., Ticket Booked)
   ▼                                    ▼
[Database] (PostgreSQL)            [Message Broker] (Azure Event Grid)
                                        │
                                        ├── 5. Trigger Worker
                                        ▼
                                   [Notification Service] (Background Worker)
                                        │
                                        ├── 6. Send SMS / Email
                                        ▼
                                   [External Provider] (Twilio / SendGrid)
```

## 2. Core Components and Interaction

*   **Client (Angular):** The user-facing front end. It renders the UI, validates user input, handles client-side routing, and communicates with the Backend Server via RESTful APIs.
*   **API Gateway (Azure):** The entry point for all client requests. It handles routing, basic security (like CORS), and passes requests to the appropriate backend containers managed by Azure Kubernetes Service (AKS).
*   **Backend Server (.NET C# Web API):** The core business logic engine. It authenticates users, processes search queries, handles the seat reservation grace period, calculates pricing, and securely interacts with the Database.
*   **Database (PostgreSQL):** The persistent storage layer. It stores all structured data: user profiles, bus routes, seat layouts, booking statuses, and payment records. It is responsible for transactional integrity (ensuring two people don't book the same seat).
*   **Message Broker (Azure Event Grid):** An asynchronous event router. When important actions happen (like a successful booking or a bus cancellation), the Backend publishes an event here.
*   **Notification Service:** A decoupled background worker that listens to the Message Broker and handles sending emails or SMS messages without slowing down the user's web request.

## 3. Step-by-Step System Flow (Customer Booking Journey)

1.  **Search:** The user (guest or logged in) enters a source, destination, and date on the Client.
2.  **Fetch Buses:** The Client sends a search request to the Backend. The Backend queries the Database for scheduled buses matching the criteria and returns them along with available seat counts.
3.  **Seat Selection:** The user selects a specific bus. The Client requests the seat layout. The user clicks on desired seats.
4.  **Grace Period Lock:** The Client sends a request to lock the selected seats. The Backend creates a temporary lock in the Database with an expiration timestamp. The seats are now "Blocked" for other users.
5.  **Authentication:** The user is prompted to log in (if they haven't already).
6.  **Passenger Details:** The user inputs the name, age, and gender for each passenger.
7.  **Payment Initiation:** The user confirms the booking. The Client calculates the total (including platform fees) and sends the payment payload to the Backend.
8.  **Payment Processing:** The Backend interacts with the Payment Gateway (Stripe/RazorPay) to process the funds.
9.  **Booking Confirmation:** Upon successful payment, the Backend updates the seat lock in the Database to a permanent "Booked" state. It generates a ticket ID.
10. **Event Dispatch:** The Backend publishes a `BookingConfirmed` event to the Message Broker.
11. **Notification:** The Notification Service picks up the event and sends an email/SMS with the ticket details to the user.
12. **Result Delivery:** The Backend responds to the Client with the confirmed ticket details, and the Client displays a downloadable confirmation page to the user.

## 4. State Management

*   **Users:** Stored in the Database. Includes profile data, role (Admin, Operator, Customer), and hashed passwords.
*   **Sessions:** Managed via JWT (JSON Web Tokens). Upon login, the Backend generates a token. The Client stores it (e.g., in Local Storage) and sends it in the header of subsequent requests to prove identity.
*   **Bus and Route State:** Stored in the Database. Includes active/inactive status. Governed by Admin and Bus Operator configurations.
*   **Seat State:** Managed in the Database. A seat for a specific journey can have three states:
    *   `Available`: Open for booking.
    *   `Blocked`: Temporarily reserved (Grace Period). Linked to a timestamp and user session.
    *   `Booked`: Permanently reserved. Linked to a paid booking record.

## 5. Data Flow

*   **Search Phase:**
    *   *Format:* JSON
    *   *Flow:* Client `{"source": "A", "destination": "B", "date": "..."}` → Backend → SQL Query → DB → `[{bus details, available seats}]` → Client.
*   **Reservation Phase:**
    *   *Format:* JSON
    *   *Flow:* Client `{"busId": 1, "seatIds": [12, 13]}` → Backend → DB (Update seat state to Blocked) → Client (Timer starts).
*   **Payment Phase:**
    *   *Format:* Secure JSON payload (Tokenized)
    *   *Flow:* Client (Payment Token) → Backend → Stripe API → Success Response → DB (Update seat state to Booked, insert Booking record) → Event Grid (Dispatch Event) → Client (Ticket Data).

## 6. Edge Cases and Failure Scenarios

*   **Grace Period Expiration:** If the user takes too long to pay, the temporary lock timestamp expires. A background job (or a check upon next access) reverts the seat state from `Blocked` to `Available`. The user is notified that their session expired.
*   **Concurrent Booking Attempt:** If two users click the same seat at the exact same millisecond, the Database handles this via Transaction Isolation (or optimistic concurrency). The first transaction locks the row; the second fails and the second user is shown a "Seat unavailable" message.
*   **Payment Failure:** If the payment gateway declines the card, the Backend immediately releases the `Blocked` seats back to `Available` and informs the Client to show an error message.
*   **Operator Deactivates a Bus:** If an operator cancels a route, the Backend finds all `Booked` seats for that journey. It automatically triggers refund transactions through the payment gateway, updates booking states to `Cancelled`, and publishes events to Event Grid to send apology/refund emails to affected users.
*   **Network Delay during Payment:** The system must implement idempotency keys for payment requests. If the user's connection drops right after clicking "Pay", they can safely retry without being charged twice.

## 7. System Mapping (Steps → Modules → Tech)

| System Step | Module / Function | Technology Used |
| :--- | :--- | :--- |
| **Search Buses** | `SearchModule.GetAvailableBuses()` | Angular (UI), .NET API, PostgreSQL (Geospatial/Relational Queries) |
| **View Seat Layout** | `BusModule.GetSeatLayout()` | Angular (UI generation), .NET API, PostgreSQL |
| **Lock Seats** | `BookingModule.LockSeats()` | .NET API, PostgreSQL (Row-level locking / Transactions) |
| **User Authentication** | `AuthModule.Login()` | Angular, .NET API (JWT Generation), PostgreSQL (Password Verification) |
| **Process Payment** | `PaymentModule.ProcessCharge()` | .NET API, Stripe/RazorPay SDK |
| **Confirm Booking** | `BookingModule.ConfirmBooking()` | .NET API, PostgreSQL (State update to 'Booked') |
| **Send Notifications** | `NotificationModule.HandleEvent()` | Azure Event Grid, .NET Background Worker, External SMTP/SMS API |
| **Cancel Booking** | `BookingModule.CancelBooking()` | Angular, .NET API, PostgreSQL (Refund logic & state reversion) |
