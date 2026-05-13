# Notification System EF Version
## version 5.0.1
## Project Overview
The Notification System is a console-based application designed to facilitate multi-channel communication between users. The system supports Email and SMS delivery and is engineered using a 3-Tier Architecture to ensure strict separation of concerns, scalability, and robust data integrity.

## Architectural Design
The solution is implemented as an enterprise-grade multi-project structure. This design enforces decoupling between core business rules, data persistence, and user interaction.

### Layers and Project Structure
1.  **Presentation Layer (NotificationSystem.Presentation)**: Manages the user interface, console menu, and input gathering. It communicates exclusively with the Business Layer.
2.  **Business Logic Layer (NotificationSystem.Business)**: Orchestrates the application workflow. It contains the primary logic for notification processing, user authentication, and data validation.
3.  **Data Access Layer (NotificationSystem.Data)**: Responsible for PostgreSQL persistence of notifications and user profiles using Entity Framework Core.
4.  **Contracts Layer (NotificationSystem.Contracts)**: Defines interfaces (INotificationRepository, INotificationSender) to ensure loose coupling.
5.  **Models Layer (NotificationSystem.Models)**: Contains entity definitions (User, Notification, EmailNotification, SmsNotification).
6.  **Shared Layer (NotificationSystem.Shared)**: Houses cross-cutting concerns such as custom exception definitions.
7.  **Infrastructure Adapters (NotificationSystem.Senders)**: Contains concrete implementations of delivery agents (Email and SMS).

---

## Database Design and Schema

The application uses **Entity Framework Core** with **PostgreSQL** for persistent storage. The schema is designed to support inheritance and complex relationships between users.

### 1. Table-Per-Hierarchy (TPH) Inheritance
The system implements the TPH strategy for notifications. Instead of separate tables for Email and SMS, a single `Notifications` table is used with a `NotificationType` discriminator column.

#### **Notifications Table**
| Column | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `Id` | `VARCHAR(8)` | `PRIMARY KEY` | SHA256 deterministic hash. |
| `Message` | `TEXT` | `NOT NULL` | The content of the notification. |
| `SentDate` | `TIMESTAMP` | `NOT NULL` | The UTC time of creation. |
| `SenderId` | `VARCHAR(8)` | `FOREIGN KEY` | References `Users(Id)`. |
| `ReceiverId` | `VARCHAR(8)` | `FOREIGN KEY` | References `Users(Id)`. |
| `NotificationType` | `TEXT` | `DISCRIMINATOR` | "Email" or "SMS". |

### 2. User Management
Users are stored in a dedicated table that handles identity for both senders and receivers.

#### **Users Table**
| Column | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| `Id` | `VARCHAR(8)` | `PRIMARY KEY` | SHA256 hash of the email. |
| `Name` | `VARCHAR(100)` | `NOT NULL` | User's display name. |
| `Email` | `TEXT` | `NOT NULL, UNIQUE` | User's email address. |
| `PhoneNumber` | `VARCHAR(10)` | `NOT NULL` | User's mobile number. |

### 3. Relationships and Constraints
- **One-to-Many (Sender)**: One user can be the sender of many notifications. Linked via `SenderId`.
- **One-to-Many (Receiver)**: One user can be the recipient of many notifications. Linked via `ReceiverId`.
- **Restrict Delete**: Referential integrity is enforced. A user cannot be deleted if they are part of a notification record (`DeleteBehavior.Restrict`).
- **Shared Record Architecture**: Both the sender and receiver point to the same physical record in the `Notifications` table, allowing for single-record source-of-truth management.

---

## Core Business Logic and Technical Implementation

### 1. Identity Management and Authentication
The system identifies users uniquely by their email address. The authentication flow implements the following security protocols:
*   **Deterministic Identifier Generation**: User and Notification IDs are generated using the SHA256 cryptographic hashing algorithm. This produces unique 8-character hexadecimal identifiers based on user input, ensuring consistency across system restarts.
*   **Conflict Detection**: During registration, the system verifies that the provided Email and Phone Number do not already exist in the repository. If a conflict is detected, a NotificationException is thrown.
*   **Integrity Verification**: During login, if a user provides an Email and Phone that match an existing record but the Name differs, the system prompts for a manual resolution to update or preserve the stored profile.

### 2. Notification Processing and Security
Notifications are processed through a strict pipeline to ensure validity and security:
*   **Receiver Verification**: The system enforces a mandatory lookup for receivers. Notifications can only be sent to users already registered in the database to prevent orphaned data.
*   **Single-Record Storage Strategy**: Upon sending a notification, the system generates a single shared record containing both `SenderId` and `ReceiverId`. This eliminates data redundancy. Sent and received views are generated dynamically by filtering the shared records based on the user's role (Sender or Receiver) in the transaction.
*   **Immutability of Received Records**: To ensure audit accuracy, the system prohibits the modification of notifications by the receiver. Only the original sender can modify the message content of a notification they initiated.
*   **Automated Sorting**: The Notification model implements the IComparable interface, enabling the system to sort all notification lists by date in descending order (newest first) by default.

### 3. Validation Protocols
The Business Layer enforces strict data constraints using a dedicated validation utility:
*   **Content Rules**: Messages must have a minimum length of 5 characters and cannot be empty.
*   **Channel Specific Limits**: SMS notifications are strictly capped at 160 characters to align with protocol standards.
*   **Format Verification**: Email addresses are checked for basic structure, and Phone Numbers are validated via Regular Expressions to ensure they are numeric and meet length requirements.

---

## Exception and Error Governance
The system uses a custom exception hierarchy (NotificationException) to manage business rule violations. This strategy ensures that errors are not silently ignored:
*   All validation failures, security mismatches, and unauthorized access attempts (such as editing a received message) throw an explicit exception.
*   The Presentation Layer captures these exceptions and displays formatted error messages to the user, resetting the current operation loop for security.

---

## Execution Output

```text

=== Notification System Simulation ===

--- User Authentication ---
1. Register
2. Login
Choice: 2
Enter Name: Ramu
Enter Email: ramu@gmail.com
Enter Phone Number: 8978675645

Welcome back, Ramu!

--- Main Menu ---
1. Send Email Notification
2. Send SMS Notification
3. Get Received Notifications
4. Get Sent Notifications
5. Filter Notification
6. Delete Notification
7. Edit Notification Message
8. Logout
9. Exit
Choice: 1
Enter Receiver Name: Keerthi
Enter Receiver Email: keerthi@gmail.com
Enter Notification Message: Hey Keerthi, Nice to meet you!

--- [EMAIL SENT] ---
Id: a7dbb1e4
From: Ramu <ramu@gmail.com>
To: Keerthi <keerthi@gmail.com>
Message: Hey Keerthi, Nice to meet you!
--------------------

--- Main Menu ---
1. Send Email Notification
2. Send SMS Notification
3. Get Received Notifications
4. Get Sent Notifications
5. Filter Notification
6. Delete Notification
7. Edit Notification Message
8. Logout
9. Exit
Choice: 4

=== SENT NOTIFICATIONS ===
[a7dbb1e4] EMAIL (ramu@gmail.com) | From: Ramu | To: Keerthi | Date: 13/05/2026 1:16:33 PM
      Message: Hey Keerthi, Nice to meet you!
---------------------------------
==============================

--- Main Menu ---
1. Send Email Notification
2. Send SMS Notification
3. Get Received Notifications
4. Get Sent Notifications
5. Filter Notification
6. Delete Notification
7. Edit Notification Message
8. Logout
9. Exit
Choice: 8
Logged out successfully.

--- User Authentication ---
1. Register
2. Login
Choice: 1
Enter Name: Keerthi
Enter Email: keerthi@gmail.com
Enter Phone Number: 9089786756

[Auth Error] Registration Failed: User already exists with email keerthi@gmail.com.

--- User Authentication ---
1. Register
2. Login
Choice: 2
Enter Name: Keerthi
Enter Email: keerthi@gmail.com
Enter Phone Number: 9089786756

Welcome back, Keerthi!

--- Main Menu ---
1. Send Email Notification
2. Send SMS Notification
3. Get Received Notifications
4. Get Sent Notifications
5. Filter Notification
6. Delete Notification
7. Edit Notification Message
8. Logout
9. Exit
Choice: 3

=== RECEIVED NOTIFICATIONS ===
[a7dbb1e4] EMAIL (ramu@gmail.com) | From: Ramu | To: Keerthi | Date: 13/05/2026 1:16:33 PM
      Message: Hey Keerthi, Nice to meet you!
---------------------------------
==============================

--- Main Menu ---
1. Send Email Notification
2. Send SMS Notification
3. Get Received Notifications
4. Get Sent Notifications
5. Filter Notification
6. Delete Notification
7. Edit Notification Message
8. Logout
9. Exit
Choice: 1
Enter Receiver Name: Ramu
Enter Receiver Email: ramu@gmail.com
Enter Notification Message: Hey ramu, nice to meet you!

--- [EMAIL SENT] ---
Id: c58846a3
From: Keerthi <keerthi@gmail.com>
To: Ramu <ramu@gmail.com>
Message: Hey ramu, nice to meet you!
--------------------

--- Main Menu ---
1. Send Email Notification
2. Send SMS Notification
3. Get Received Notifications
4. Get Sent Notifications
5. Filter Notification
6. Delete Notification
7. Edit Notification Message
8. Logout
9. Exit
Choice: 8

--- User Authentication ---
1. Register
2. Login
Choice: 2
Enter Name: Somu
Enter Email: somu@gmail.com
Enter Phone Number: 7867564534

Welcome back, Somu!

--- Main Menu ---
1. Send Email Notification
2. Send SMS Notification
3. Get Received Notifications
4. Get Sent Notifications
5. Filter Notification
6. Delete Notification
7. Edit Notification Message
8. Logout
9. Exit
Choice: 2
Enter Receiver Name: Keerthi
Enter Receiver Phone Number: 9089786756
Enter Notification Message: Hey Keerthi!

--- [SMS SENT] ---
Id: de578454
From: Somu (7867564534)
To: Keerthi (9089786756)
Message: Hey Keerthi!
------------------

--- Main Menu ---
1. Send Email Notification
2. Send SMS Notification
3. Get Received Notifications
4. Get Sent Notifications
5. Filter Notification
6. Delete Notification
7. Edit Notification Message
8. Logout
9. Exit
Choice: 8
Logged out successfully.

--- User Authentication ---
1. Register
2. Login
Choice: 2
Enter Name: Keerthi
Enter Email: keerthi@gmail.com
Enter Phone Number: 9089786756

Welcome back, Keerthi!

--- Main Menu ---
1. Send Email Notification
2. Send SMS Notification
3. Get Received Notifications
4. Get Sent Notifications
5. Filter Notification
6. Delete Notification
7. Edit Notification Message
8. Logout
9. Exit
Choice: 3

=== RECEIVED NOTIFICATIONS ===
[de578454] SMS (7867564534) | From: Somu | To: Keerthi | Date: 13/05/2026 1:19:17 PM
      Message: Hey Keerthi!
---------------------------------
[a7dbb1e4] EMAIL (ramu@gmail.com) | From: Ramu | To: Keerthi | Date: 13/05/2026 1:16:33 PM
      Message: Hey Keerthi, Nice to meet you!
---------------------------------
==============================

--- Main Menu ---
1. Send Email Notification
2. Send SMS Notification
3. Get Received Notifications
4. Get Sent Notifications
5. Filter Notification
6. Delete Notification
7. Edit Notification Message
8. Logout
9. Exit
Choice: 5

---- Choose Filter Criteria ----:
1. Filter with Email
2. Filter with Contact
Choice: 1
Enter Email: ramu@gmail.com

=== FILTER RESULTS ===
[c58846a3] EMAIL (keerthi@gmail.com) | From: Keerthi | To: Ramu | Date: 13/05/2026 1:18:09 PM
      Message: Hey ramu, nice to meet you!
---------------------------------
[a7dbb1e4] EMAIL (ramu@gmail.com) | From: Ramu | To: Keerthi | Date: 13/05/2026 1:16:33 PM
      Message: Hey Keerthi, Nice to meet you!
---------------------------------
==============================

--- Main Menu ---
1. Send Email Notification
2. Send SMS Notification
3. Get Received Notifications
4. Get Sent Notifications
5. Filter Notification
6. Delete Notification
7. Edit Notification Message
8. Logout
9. Exit
Choice: 6
Enter the Id of the notification to delete: de578454
Notification deleted successfully.

--- Main Menu ---
1. Send Email Notification
2. Send SMS Notification
3. Get Received Notifications
4. Get Sent Notifications
5. Filter Notification
6. Delete Notification
7. Edit Notification Message
8. Logout
9. Exit
Choice: 3

=== RECEIVED NOTIFICATIONS ===
[a7dbb1e4] EMAIL (ramu@gmail.com) | From: Ramu | To: Keerthi | Date: 13/05/2026 1:16:33 PM
      Message: Hey Keerthi, Nice to meet you!
---------------------------------
==============================

--- Main Menu ---
1. Send Email Notification
2. Send SMS Notification
3. Get Received Notifications
4. Get Sent Notifications
5. Filter Notification
6. Delete Notification
7. Edit Notification Message
8. Logout
9. Exit
Choice: 4

=== SENT NOTIFICATIONS ===
[c58846a3] EMAIL (keerthi@gmail.com) | From: Keerthi | To: Ramu | Date: 13/05/2026 1:18:09 PM
      Message: Hey ramu, nice to meet you!
---------------------------------
==============================

--- Main Menu ---
1. Send Email Notification
2. Send SMS Notification
3. Get Received Notifications
4. Get Sent Notifications
5. Filter Notification
6. Delete Notification
7. Edit Notification Message
8. Logout
9. Exit
Choice: 5

---- Choose Filter Criteria ----:
1. Filter with Email
2. Filter with Contact
Choice: 2
Enter Contact: 7867564534

=== FILTER RESULTS ===
No notifications found.

--- Main Menu ---
1. Send Email Notification
2. Send SMS Notification
3. Get Received Notifications
4. Get Sent Notifications
5. Filter Notification
6. Delete Notification
7. Edit Notification Message
8. Logout
9. Exit
Choice: 6
Enter the Id of the notification to delete: 877665

[Error] Action Failed: Notification with Id '877665' not found for your account.

--- Main Menu ---
1. Send Email Notification
2. Send SMS Notification
3. Get Received Notifications
4. Get Sent Notifications
5. Filter Notification
6. Delete Notification
7. Edit Notification Message
8. Logout
9. Exit
Choice: 9
Exiting simulation...

```
