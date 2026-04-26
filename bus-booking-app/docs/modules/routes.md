# Routing & Navigation Architecture

This document outlines the navigation architecture of the Bus Booking System. It defines the contract for data transfer between modules, the role of query parameters in state persistence, and the end-to-end data flow for a booking journey.

---

## Section 1 — Routing Philosophy

The system prioritizes **Query Parameters** over complex local state for the following reasons:

*   **Deep Linking**: Users can share or bookmark a specific search or booking stage.
*   **State Persistence**: Refreshing the browser does not lose the current selection (e.g., source, destination, or selected seats).
*   **State Transfer**: Routing acts as a formal contract between decoupled components. One component "emits" state into the URL, and the next "consumes" it.

### Path vs. Query Parameters:
*   **Path Parameters (`/:id`)**: Used for resource identification (e.g., a specific trip ID).
*   **Query Parameters (`?key=value`)**: Used for filters, transient selection data, and non-sensitive state metadata.

---

## Section 2 — High-Level Navigation Flow

The booking journey is a linear progression where each step enriches the navigational state:

`Landing Page → Search Results → Seat Selection → Passenger Details → Payment Gateway → Success/History`

---

## Section 3 — Route Definitions

### 1. Search Results
*   **URL**: `/search?from=CityA&to=CityB&date=2026-04-24`
*   **Path**: `/search`
*   **Query Params**:
    *   `from` (string): Source city name.
    *   `to` (string): Destination city name.
    *   `date` (ISO string): The date of travel.
*   **Purpose**: To fetch and display available buses meeting the criteria.

### 2. Seat Selection
*   **URL**: `/booking/seats/UUID`
*   **Path**: `/booking/seats/:id`
*   **Path Param**: `id` (UUID): The unique ID of the schedule/trip.
*   **Purpose**: To visualize the bus layout and allow seat blocking.

### 3. Passenger Details
*   **URL**: `/booking/passengers?journeyId=UUID&seats=A1,A2&price=1200`
*   **Path**: `/booking/passengers`
*   **Query Params**:
    *   `journeyId` (UUID): Reference to the selected trip.
    *   `seats` (string): Comma-separated list of selected seat numbers.
    *   `price` (number): Base ticket price for the selected seats.
*   **Purpose**: Collect passenger names, ages, and gender for the specific seats.

### 4. Payment Page
*   **URL**: `/booking/payment?bookingId=UUID`
*   **Path**: `/booking/payment`
*   **Query Params**:
    *   `bookingId` (UUID): The preliminary booking record ID created after passenger submission.
*   **Purpose**: Finalize the financial transaction for a specific booking.

---

## Section 4 — Query Parameter Contract

| Parameter | Type | Required | Validation |
| :--- | :--- | :--- | :--- |
| `from` / `to` | String | Yes | Must be non-empty; must exist in City database. |
| `date` | ISO Date | Yes | Must be today or in the future. |
| `journeyId` | UUID | Yes | Must be a valid, non-cancelled trip. |
| `seats` | String | Yes | Must be a comma-separated list of valid seat numbers for the bus type. |

**Handling Missing Params**: If critical parameters are missing (e.g., `journeyId` on the passenger page), the system redirects the user back to the `/search` page with an error toast.

---

## Section 5 — Frontend Implementation

*   **Lazy Loading**: The `/admin`, `/operator`, and `/booking` modules are lazy-loaded to optimize initial bundle size.
*   **Route Guards**:
    *   `authGuard`: Prevents access to booking and profile pages for unauthenticated users.
    *   `rootGuard`: Redirects the root path (`/`) to the appropriate dashboard based on the user's role (Customer/Operator/Admin).

### Reading Parameters:
Components use `ActivatedRoute` to consume the contract:
```typescript
this.route.queryParams.subscribe(params => {
  this.from = params['from'];
  this.to = params['to'];
});
```

---

## Section 6 — Data Flow Across Pages

1.  **Search**: Collects `from`, `to`, `date` → Navigates to `/search` with these as query params.
2.  **Results**: Reads params → Calls `SearchService.getBuses(from, to, date)` → User clicks "Select Seats".
3.  **Seats**: Component reads `:id` from path → Displays layout → User selects seats → Navigates to `/booking/passengers` passing `journeyId`, `seats`, and `price` via query params.
4.  **Passengers**: Reads selection → Renders form → On submit, creates a "Pending" booking in DB → Receives `bookingId` → Navigates to `/booking/payment?bookingId=...`.
5.  **Payment**: Reads `bookingId` → Fetches final amount including platform fees → On success, redirects to History.

---

## Section 7 — State vs URL

*   **URL (Query Params)**: Stores public, non-sensitive state that should survive a refresh (Search criteria, seat selection).
*   **Angular Services**: Stores sensitive user data (Auth tokens, Profile details) and large objects that are too bulky for the URL.
*   **Safety**: **Prices** are passed in the URL for UI convenience, but the **Backend re-calculates all prices** based on the `journeyId` during the final confirmation to prevent price tampering.

---

## Section 8 — Backend Mapping

| Frontend Route | Backend API Endpoint |
| :--- | :--- |
| `/search` | `GET /api/Search?from=...&to=...&date=...` |
| `/booking/seats/:id` | `GET /api/Booking/layout/{id}` |
| `/booking/passengers` | `POST /api/Booking/lock-seats` (for blocking) |
| `/booking/payment` | `POST /api/Booking/confirm` |

---

## Section 9 — Validation & Edge Cases

*   **Direct URL Access**: If a user pastes a payment URL without an active session or valid `bookingId`, the `authGuard` and component-level null checks will force a redirect to `/login` or `/search`.
*   **Back Navigation**: If a user goes back from Payment to Passengers, the query params persist, allowing them to re-submit or modify details easily.
*   **Refresh**: Since all state is in the URL, the page reloads and re-fetches the latest data from the backend (e.g., checking if the seats are still locked).

---

## Section 10 — Route Mapping Table

| Route Path | Component | Primary Params | Backend API |
| :--- | :--- | :--- | :--- |
| `/search` | `BusListingComponent` | `from`, `to`, `date` | `api/Search` |
| `/booking/seats/:id` | `SeatLayoutComponent` | `id` (Path) | `api/Booking/layout` |
| `/booking/passengers` | `PassengerFormComponent`| `journeyId`, `seats` | `api/Booking/lock-seats` |
| `/booking/history` | `HistoryComponent` | None | `api/Booking/history` |
| `/admin/operators` | `OperatorApprovalsComponent`| None | `api/Admin/operators` |

---

## Section 11 — Security Considerations

*   **Param Tampering**: All ID-based parameters (UUIDs) are validated on the backend to ensure they belong to the requesting user (where applicable).
*   **Sensitive Data**: No passwords, personal contact details, or payment tokens are ever stored in the URL.
*   **Validation**: Every "POST" request that relies on a `journeyId` from the URL performs a fresh database lookup for pricing and availability, ignoring any "price" parameter passed from the client.
