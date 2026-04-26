# Auth Module - Implementation Details

The Auth Module is the foundational security layer of the Bus Booking Application, handling user identity, role-based access control, and secure session management.

## 1. Technical Stack
- **Backend**: .NET 10.0, Entity Framework Core (PostgreSQL)
- **Security**: BCrypt.Net (Password Hashing), System.IdentityModel.Tokens.Jwt (JWT)
- **Frontend**: Angular 18+, Reactive Forms, Signals, RxJS

---

## 2. Backend Implementation Logic

### Data Modeling (TPT Inheritance)
We use **Table-per-Type (TPT)** inheritance to represent different user roles while maintaining data integrity and specific metadata.
- **`Users` (Base Table)**: Stores common fields like `Email`, `PasswordHash`, `FullName`, `Phone`, and `Role`.
- **`Admins` (Extension Table)**: Stores administrative metadata (`IsSuperAdmin`, `Department`).
- **`BusOperators` (Extension Table)**: Stores operator-specific data (`CompanyName`, `IsApproved`, `Address`).

### Security Workflow
1. **Registration**:
   - Accepts `RegisterRequest` DTO.
   - Checks for duplicate emails.
   - Hashes password using **BCrypt** with a high cost factor.
   - Instantiates the correct entity type (User, Admin, or BusOperator) based on the `Role` enum.
   - Saves to PostgreSQL (EF Core handles the primary key relationship across tables).

2. **Authentication (Login)**:
   - Validates email existence.
   - Verifies the provided password against the stored BCrypt hash.
   - Generates a **JWT Token** containing claims: `NameIdentifier` (ID), `Email`, and `Role`.
   - Returns an `AuthResponse` DTO containing the token and user profile.

---

## 3. Frontend Implementation Logic

### State Management
- **AuthService**: Uses a `BehaviorSubject` to broadcast the current user's state across the application.
- **LocalStorage**: Persists the user profile and token for session continuity (with SSR compatibility checks).
- **Angular Signals**: Used in components (like `HomeComponent`) for reactive and efficient UI updates.

### Interceptors & Guards
- **`AuthInterceptor`**: A functional interceptor that automatically clones outgoing HTTP requests and injects the `Authorization: Bearer <token>` header if a token exists.
- **`AuthGuard`**: A route guard that prevents unauthorized access to the Home/Dashboard pages, redirecting unauthenticated users to the Login screen.

### UI Architecture (Minimalist Design)
- **Reactive Forms**: Used for both Login and Registration with real-time validation.
- **Monochrome Theme**: A high-contrast "Black & White" design system implemented with vanilla CSS and Inter typography.
- **Dynamic Initial Generation**: The profile circle automatically extracts initials from the user's full name.

---

## 4. Folder Structure (Vertical Slice)

### Backend (`/server/Features/Auth`)
- `AuthController.cs`: API Endpoints (Register/Login).
- `AuthService.cs`: Hashing and Token generation logic.
- `AuthDtos.cs`: Immutable records for data transfer.

### Frontend (`/client/src/app/features/auth`)
- `login/`: LoginComponent (UI & Logic).
- `register/`: RegisterComponent (UI & Logic).
- `/core/services/auth.service.ts`: Core singleton for auth logic.
- `/core/guards/auth.guard.ts`: Routing security.

---

## 5. API Contracts

### `POST /api/auth/register`
**Request:**
```json
{
  "fullName": "John Doe",
  "email": "john@example.com",
  "phone": "+1234567890",
  "password": "securepassword",
  "role": 0 // 0: Customer, 1: Operator, 2: Admin
}
```

### `POST /api/auth/login`
**Response:**
```json
{
  "token": "eyJhbG...",
  "fullName": "John Doe",
  "email": "john@example.com",
  "role": 0
}
```
