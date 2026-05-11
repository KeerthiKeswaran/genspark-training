# Notification System (ADO.Net Version)

## Project Overview
This is the updated version of the existing project, now featuring a robust persistence layer using **ADO.NET** and **PostgreSQL**. The application has been migrated from in-memory storage to a relational database to support data persistence and more complex querying.

## Architectural Design
The solution is implemented as an enterprise-grade multi-project structure. This design enforces decoupling between core business rules, data persistence, and user interaction.

### Layers and Project Structure
1.  **Presentation Layer (NotificationSystem.Presentation)**: Manages the user interface, console menu, and input gathering. It communicates exclusively with the Business Layer.
2.  **Business Logic Layer (NotificationSystem.Business)**: Orchestrates the application workflow. It contains the primary logic for notification processing, user authentication, and data validation.
3.  **Data Access Layer (NotificationSystem.Data)**: This is the updated version of the existing project, now utilizing **ADO.NET** to manage persistent storage in a **PostgreSQL** database.
4.  **Contracts Layer (NotificationSystem.Contracts)**: Defines interfaces (INotificationRepository, INotificationSender) to ensure loose coupling.
5.  **Models Layer (NotificationSystem.Models)**: Contains entity definitions (User, Notification, EmailNotification, SmsNotification).
6.  **Shared Layer (NotificationSystem.Shared)**: Houses cross-cutting concerns such as custom exception definitions.
7.  **Infrastructure Adapters (NotificationSystem.Senders)**: Contains concrete implementations of delivery agents (Email and SMS).

---

## Setup and Configuration

### 1. Database Setup
Ensure you have **PostgreSQL** installed and running. Create a database with your choice and execute the following SQL commands to set up the necessary tables:

```sql
CREATE TABLE users(
    id UUID PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    phone_number VARCHAR(20)
);

CREATE TABLE notifications (
    id UUID PRIMARY KEY,
    message TEXT NOT NULL,
    sent_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    type VARCHAR(10) NOT NULL CHECK (type IN ('Email', 'SMS')),
    status INT NOT NULL CHECK (status IN (0,1)), -- (0 - Sent, 1 - Received)
    sender_id UUID REFERENCES users(id) ON DELETE SET NULL,
    receiver_id UUID REFERENCES users(id) ON DELETE SET NULL
);
```

### 2. Configuration (`appsettings.json`)
The application uses an `appsettings.json` file located in the `NotificationSystem.Data` project for database connectivity. Update the connection string with your PostgreSQL credentials:

```json
{
  "ConnectionStrings": {
    "GensparkDb": "Host=localhost;Port=5432;Database=your_db_name;Username=YOUR_USERNAME;Password=YOUR_PASSWORD"
  }
}
```

### 3. Running the Application
From the root directory, use the following command to start the simulation:
```bash
dotnet run --project NotificationSystem.Presentation
```

---

## Core Business Logic and Technical Implementation

### 1. Identity Management and Authentication
The system identifies users uniquely by their email address. The authentication flow implements the following security protocols:
*   **Deterministic UUID Generation**: User IDs are generated as deterministic 36-character **UUIDs** (using MD5 hashing of the email). This ensures that a user's unique identifier remains consistent across sessions while complying with relational database standards.
*   **Conflict Detection**: During registration, the system verifies that the provided Email and Phone Number do not already exist in the repository. If a conflict is detected, a NotificationException is thrown.
*   **Integrity Verification**: During login, if a user provides an Email and Phone that match an existing record but the Name differs, the system prompts for a manual resolution to update or preserve the stored profile.

### 2. Notification Processing and Security
Notifications are processed through a strict pipeline to ensure validity and security:
*   **Receiver Verification**: The system enforces a mandatory lookup for receivers. Notifications can only be sent to users already registered in the database to prevent orphaned data.
*   **Object Mirroring (Mirror Copy Strategy)**: Upon sending a notification, the system generates two distinct objects using a cloning mechanism. One record is marked as "Sent" for the sender, and another is marked as "Received" for the receiver. This decoupling allows users to manage their notification history independently.
*   **Immutability of Received Records**: To ensure audit accuracy, the system prohibits the modification of notifications marked with a "Received" status. Only "Sent" records may be edited by the original sender.
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


## Data Verification

### Users Table
| id | username | email | phone_number |
| :--- | :--- | :--- | :--- |
| 66c59cc9-40dc-b658-effb-af3c6075d650 | keerthi | keerthi@gmail.com | 9089786756 |
| ef66dbc6-defe-5f06-f0dd-93ead0b3deed | karthik | karthik@gmail.com | 8978675645 |
| 3832050c-5c5d-7270-75ae-715504fc0fca | ramu | ramu@gmail.com | 7867564534 |

### Notifications Table
| id | message | sent_at | type | status | sender_id | receiver_id | deleted_by_sender | deleted_by_receiver |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| 8ae6a14e... | Hey Keerthi, I'm Ramu! Nice to meet you. | 2026-05-11 16:41:16 | Email | 0 | 3832050c... | 66c59cc9... | false | false |
| fafb8629... | Hey Karthik, Good Evening! Ramu here. | 2026-05-11 16:41:42 | SMS | 0 | 3832050c... | ef66dbc6... | false | false |
| 761f410a... | Hey ramu, glad to get a notif from you! | 2026-05-11 16:43:39 | SMS | 0 | ef66dbc6... | 3832050c... | false | false |
| 1b3eed03... | Hey sorry, had a typo before! Hope... | 2026-05-11 16:44:54 | Email | 0 | ef66dbc6... | 66c59cc9... | false | false |
| 3aff5c77... | Hey ramu, what a surprise! | 2026-05-11 16:46:48 | SMS | 0 | 66c59cc9... | 3832050c... | false | false |
| 4beb205c... | Hey karthik, yeah i received it! Chill! | 2026-05-11 16:47:07 | Email | 0 | 66c59cc9... | ef66dbc6... | false | false |
| 910ec3b2... | hey keerthi, hape u gat the txt from ram? | 2026-05-11 17:12:15 | Email | 0 | ef66dbc6... | 66c59cc9... | false | true |


## Simulation Execution Output

```text
=== Notification System Simulation ===

--- User Authentication ---
1. Register
2. Login
Choice: 2
Enter Name: %                                                                        
keerthikeswaran@FVFG90BGQ05F-keerthikeswaran notification-system-psql % clear
keerthikeswaran@FVFG90BGQ05F-keerthikeswaran notification-system-psql % dotnet run --project NotificationSystem.Presentation
=== Notification System Simulation ===

--- User Authentication ---
1. Register
2. Login
Choice: 1
Enter Name: ramu
Enter Email: ramu@gmail.com
Enter Phone Number: 7867564534

Registration successful! Welcome, ramu!

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
Enter Receiver Name: keerthi
Enter Receiver Email: keerthi@gmail.com
Enter Notification Message: Hey Keerthi, I'm Ramu! Nice to meet you.

--- [EMAIL SENT] ---
Id: 8ae6a14e-f9bd-4e33-aa8f-f485459a656c
From: ramu <ramu@gmail.com>
To: keerthi <keerthi@gmail.com>
Message: Hey Keerthi, I'm Ramu! Nice to meet you.
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
Choice: 2
Enter Receiver Name: karthik
Enter Receiver Phone Number: 8978675645
Enter Notification Message: Hey karthik, this is me ramu!

--- [SMS SENT] ---
Id: fafb8629-01a9-472f-9cf6-825f88e91efc
From: ramu (7867564534)
To: karthik (8978675645)
Message: Hey karthik, this is me ramu!
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
Choice: 7
Enter the Id of the notification to edit: fafb8629-01a9-472f-9cf6-825f88e91efc
Enter the new message: Hey Karthik, Good Evening! Ramu here.
Notification updated successfully.

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
Enter Name: karthik
Enter Email: karthik@gmail.com
Enter Phone Number: 8978675645
Registration Failed: User already exists with email karthik@gmail.com.

--- User Authentication ---
1. Register
2. Login
Choice: 2
Enter Name: karthik
Enter Email: karthik@gmail.com
Enter Phone Number: 8978675645

Welcome back, karthik!

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
[fafb8629-01a9-472f-9cf6-825f88e91efc] SMS (7867564534) | From: ramu | To: karthik | Status: Received | Date: 11/05/2026 4:41:42 PM
      Message: Hey Karthik, Good Evening! Ramu here.
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
Choice: 2
Enter Receiver Name: ramu
Enter Receiver Phone Number: 7867564534
Enter Notification Message: Hey ramu, glad to get a notif from you!

--- [SMS SENT] ---
Id: 761f410a-ef18-43b9-aada-3de7f10405c8
From: karthik (8978675645)
To: ramu (7867564534)
Message: Hey ramu, glad to get a notif from you!
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
Choice: 1
Enter Receiver Name: keerthi
Enter Receiver Email: keerthi@gmail.com
Enter Notification Message: hey keerthi, hape u gat the txt from ram?

--- [EMAIL SENT] ---
Id: 02d6cdcb-2a11-4eab-a883-d6c022315f5c
From: karthik <karthik@gmail.com>
To: keerthi <keerthi@gmail.com>
Message: hey keerthi, hape u gat the txt from ram?
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
Choice: 1
Enter Receiver Name: keerthi
Enter Receiver Email: keerthi@gmail.com
Enter Notification Message: Hey sorry, had a typo before! Hope u got the notif from ramu right?

--- [EMAIL SENT] ---
Id: 1b3eed03-63c6-45f9-b855-872294d709ac
From: karthik <karthik@gmail.com>
To: keerthi <keerthi@gmail.com>
Message: Hey sorry, had a typo before! Hope u got the notif from ramu right?
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
Logged out successfully.

--- User Authentication ---
1. Register
2. Login
Choice: 2
Enter Name: keerthi
Enter Email: keerthi@gmail.com
Enter Phone Number: 9089786756

Welcome back, keerthi!

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
[1b3eed03-63c6-45f9-b855-872294d709ac] EMAIL (karthik@gmail.com) | From: karthik | To: keerthi | Status: Received | Date: 11/05/2026 4:44:54 PM
      Message: Hey sorry, had a typo before! Hope u got the notif from ramu right?
---------------------------------
[02d6cdcb-2a11-4eab-a883-d6c022315f5c] EMAIL (karthik@gmail.com) | From: karthik | To: keerthi | Status: Received | Date: 11/05/2026 4:44:24 PM
      Message: hey keerthi, hape u gat the txt from ram?
---------------------------------
[8ae6a14e-f9bd-4e33-aa8f-f485459a656c] EMAIL (ramu@gmail.com) | From: ramu | To: keerthi | Status: Received | Date: 11/05/2026 4:41:16 PM
      Message: Hey Keerthi, I'm Ramu! Nice to meet you.
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
Enter the Id of the notification to delete: 02d6cdcb-2a11-4eab-a883-d6c022315f5c
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
Choice: 2
Enter Receiver Name: ramu
Enter Receiver Phone Number: 7867564534
Enter Notification Message: Hey ramu, what a surprise!            

--- [SMS SENT] ---
Id: 3aff5c77-1b1b-45a6-b83a-be40d3f6920f
From: keerthi (9089786756)
To: ramu (7867564534)
Message: Hey ramu, what a surprise!
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
Choice: 1
Enter Receiver Name: karthik
Enter Receiver Email: karthik@gmail.com
Enter Notification Message: Hey karthik, yeah i received it! Chill!

--- [EMAIL SENT] ---
Id: 4beb205c-e6c6-4756-a7a5-2f87d7d3631a
From: keerthi <keerthi@gmail.com>
To: karthik <karthik@gmail.com>
Message: Hey karthik, yeah i received it! Chill!
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
Choice: 5

---- Choose Filter Criteria ----:
1. Filter with Email
2. Filter with Contact
Choice: 1
Enter Email: karthik@gmail.com

=== FILTER RESULTS ===
[4beb205c-e6c6-4756-a7a5-2f87d7d3631a] EMAIL (keerthi@gmail.com) | From: keerthi | To: karthik | Status: Sent | Date: 11/05/2026 4:47:07 PM
      Message: Hey karthik, yeah i received it! Chill!
---------------------------------
[1b3eed03-63c6-45f9-b855-872294d709ac] EMAIL (karthik@gmail.com) | From: karthik | To: keerthi | Status: Received | Date: 11/05/2026 4:44:54 PM
      Message: Hey sorry, had a typo before! Hope u got the notif from ramu right?
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
[3aff5c77-1b1b-45a6-b83a-be40d3f6920f] SMS (9089786756) | From: keerthi | To: ramu | Status: Sent | Date: 11/05/2026 4:46:48 PM
      Message: Hey ramu, what a surprise!
---------------------------------
[8ae6a14e-f9bd-4e33-aa8f-f485459a656c] EMAIL (ramu@gmail.com) | From: ramu | To: keerthi | Status: Received | Date: 11/05/2026 4:41:16 PM
      Message: Hey Keerthi, I'm Ramu! Nice to meet you.
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
Choice: 2
Enter Name: karthik
Enter Email: karthik@gmail.com
Enter Phone Number: 8978675645

Welcome back, karthik!

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
[910ec3b2-c83f-460a-8428-bb8cddf561ff] EMAIL (karthik@gmail.com) | From: karthik | To: keerthi | Status: Sent | Date: 11/05/2026 4:44:24 PM
      Message: hey keerthi, hape u gat the txt from ram?
---------------------------------
[1b3eed03-63c6-45f9-b855-872294d709ac] EMAIL (karthik@gmail.com) | From: karthik | To: keerthi | Status: Sent | Date: 11/05/2026 4:44:54 PM
      Message: Hey sorry, had a typo before! Hope u got the notif from ramu right?
---------------------------------
[761f410a-ef18-43b9-aada-3de7f10405c8] SMS (8978675645) | From: karthik | To: ramu | Status: Sent | Date: 11/05/2026 4:43:39 PM
      Message: Hey ramu, glad to get a notif from you!
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
Choice: 9
Exiting simulation...
```
