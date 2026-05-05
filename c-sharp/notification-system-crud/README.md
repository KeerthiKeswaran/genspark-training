# Notification System CRUD

## Overview
A C# console application demonstrating a complete CRUD (Create, Read, Update, Delete) workflow for a notification system. This application allows users to register, send Email and SMS notifications, and manage their notification history. The project emphasizes clean architecture, separation of concerns, and advanced Object-Oriented Programming (OOP) concepts.

## Features
- User Session Management: Deterministic login based on email hashes.
- Send Notifications: Simulates sending both Email and SMS messages.
- Read History: View sent and received messages separately.
- Search: Filter notifications by email or contact number.
- Update & Delete: Edit or remove past notifications.
- Data Isolation: Users only have access to their own notification data.

## Advanced OOP Concepts Implemented

This project acts as a practical implementation of several advanced C# OOP features to ensure maintainability and robustness.

### 1. Prepping of Models
Models (`User`, `Notification`, `EmailNotification`, `SmsNotification`) are designed with strong encapsulation and validation. Constructors enforce that critical data (like sender and receiver objects) must be provided upon instantiation. This prevents the creation of invalid state objects and ensures data integrity from the moment an object is created.

### 2. IComparable
The `Notification` base class implements the `IComparable<Notification>` interface. The `CompareTo` method is overridden to compare notifications based on their `SentDate`. This allows lists of notifications to be automatically sorted in descending order (newest first) simply by calling the `.Sort()` method in the service layer before displaying them.

### 3. IEquatable
The `User` class implements `IEquatable<User>` to provide a type-safe and high-performance way to compare two user objects. Instead of relying on default memory reference checks, the custom `Equals(User? other)` method evaluates equality strictly based on the user's unique `Id`. 

### 4. Partial Classes
To adhere to the Single Responsibility Principle, the `NotificationService` is implemented as a `partial class`. This allows a previously monolithic service class to be cleanly split across multiple physical files without breaking functionality:
- `NotificationService.cs`: Contains core setup and helper logic.
- `NotificationService.Messaging.cs`: Handles notification generation and sending logic.
- `NotificationService.Crud.cs`: Manages all read, search, update, and delete operations.

### 5. Indexers
The `NotificationRepository` class utilizes a C# Indexer (`this[string userId]`). This feature allows the repository instance to be queried using an array-like syntax. Instead of calling `_notificationRepository.GetNotifications(user.Id)`, the service layer simply calls `_notificationRepository[user.Id]`, resulting in much cleaner, more intuitive data access.

## Architecture
The application follows a standard layered architecture:
- Models: Defines the data structures and inheritance hierarchies.
- Interfaces: Contracts for the services and repositories to ensure loose coupling.
- Repositories: In-memory data storage using a generic `Dictionary` to segregate data by user IDs.
- Services: Contains the core business logic, hashing, and orchestration.
- Program.cs: The entry point managing the interactive console-based user interface.

## How to Run
Navigate to the root directory of the project and execute:
```bash
dotnet run
```
