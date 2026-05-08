# Notification System Technical Documentation

## Project Overview
The Notification System is a console-based application designed to facilitate multi-channel communication between users. The system supports Email and SMS delivery and is engineered using a 3-Tier Architecture to ensure strict separation of concerns, scalability, and robust data integrity.

## Architectural Design
The solution is implemented as an enterprise-grade multi-project structure. This design enforces decoupling between core business rules, data persistence, and user interaction.

### Layers and Project Structure
1.  **Presentation Layer (NotificationSystem.Presentation)**: Manages the user interface, console menu, and input gathering. It communicates exclusively with the Business Layer.
2.  **Business Logic Layer (NotificationSystem.Business)**: Orchestrates the application workflow. It contains the primary logic for notification processing, user authentication, and data validation.
3.  **Data Access Layer (NotificationSystem.Data)**: Responsible for in-memory persistence of notifications and user profiles.
4.  **Contracts Layer (NotificationSystem.Contracts)**: Defines interfaces (INotificationRepository, INotificationSender) to ensure loose coupling.
5.  **Models Layer (NotificationSystem.Models)**: Contains entity definitions (User, Notification, EmailNotification, SmsNotification).
6.  **Shared Layer (NotificationSystem.Shared)**: Houses cross-cutting concerns such as custom exception definitions.
7.  **Infrastructure Adapters (NotificationSystem.Senders)**: Contains concrete implementations of delivery agents (Email and SMS).

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

## Simulation Execution Output

```text
=== Notification System Simulation ===

--- User Authentication ---
1. Register
2. Login
Choice: 1
Enter Name: keerthi
Enter Email: keerthi@gmail.com
Enter Phone Number: 8978675645

Registration successful! Welcome, keerthi!

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
Enter Email: keeerthi@gmail.com
Enter Phone Number: 8978675645

[Auth Error] Login Failed: Already a user registered with phone 8978675645 but a different email.

--- User Authentication ---
1. Register
2. Login
Choice: 2
Enter Name: keerthi
Enter Email: keerthi@gmail.com
Enter Phone Number: 7867564534

[Auth Error] Login Failed: Already a user registered with email keerthi@gmail.com but a different phone number.

--- User Authentication ---
1. Register
2. Login
Choice: 2
Enter Name: keerthikesh
Enter Email: keerthi@gmail.com
Enter Phone Number: 8978675645

[Conflict] Name mismatch detected.
Stored Name: keerthi
Current Name: keerthikesh
Do you want to continue with the (1) current name or the (2) previous name? 2
Continuing with previous name.

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
Enter Receiver Name: suresh
Enter Receiver Phone Number: 9089786756
Enter Notification Message: Hey suresh!

[Error] Cannot send SMS. The receiver with contact '9089786756' is not registered in the system.

--- Main Menu ---
... [Additional simulation steps truncated] ...

=== RECEIVED NOTIFICATIONS ===
[1f54ae08] EMAIL | From: keerthi | To: suresh | Status: Received | Date: 08/05/2026 7:01:09 PM
      Message: Hey suresh! how are you?
---------------------------------
==============================

--- Main Menu ---
...

Choice: 7
Enter the Id of the notification to edit: 1f54ae08
Enter the new message: Hey hello! 

[Error] Access Denied: You cannot edit a received notification. Only sent messages can be modified.

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
[0a34f8e2] EMAIL | From: suresh | To: keerthi | Status: Sent | Date: 08/05/2026 7:02:22 PM
      Message: Hey Keerthi, I'm fine!
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
Choice: 7
Enter the Id of the notification to edit: 0a34f8e2
Enter the new message: Hey keerthi, good morning!
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
Choice: 2
Enter Name: keerthi 
Enter Email: keerthi@gmail.com
Enter Phone Number: 8978675645

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
[0a34f8e2] EMAIL | From: suresh | To: keerthi | Status: Received | Date: 08/05/2026 7:02:22 PM
      Message: Hey Keerthi, I'm fine!
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
```
