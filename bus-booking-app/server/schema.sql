CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE "Users" (
    "Id" uuid NOT NULL,
    "FullName" text NOT NULL,
    "Email" text NOT NULL,
    "Phone" text NOT NULL,
    "PasswordHash" text NOT NULL,
    "Role" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);

CREATE TABLE "Admins" (
    "Id" uuid NOT NULL,
    "IsSuperAdmin" boolean NOT NULL,
    "CreatedByAdminId" uuid,
    "Department" text NOT NULL,
    CONSTRAINT "PK_Admins" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Admins_Users_Id" FOREIGN KEY ("Id") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "BusOperators" (
    "Id" uuid NOT NULL,
    "CompanyName" text NOT NULL,
    "IsApproved" boolean NOT NULL,
    "Address" text NOT NULL,
    CONSTRAINT "PK_BusOperators" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_BusOperators_Users_Id" FOREIGN KEY ("Id") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260422093433_InitialCreate', '9.0.2');

CREATE TABLE "Buses" (
    "Id" uuid NOT NULL,
    "BusNumber" text NOT NULL,
    "BusType" text NOT NULL,
    "TotalSeats" integer NOT NULL,
    "OperatorId" uuid NOT NULL,
    CONSTRAINT "PK_Buses" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Buses_BusOperators_OperatorId" FOREIGN KEY ("OperatorId") REFERENCES "BusOperators" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Routes" (
    "Id" uuid NOT NULL,
    "Source" text NOT NULL,
    "Destination" text NOT NULL,
    "DistanceKm" double precision NOT NULL,
    CONSTRAINT "PK_Routes" PRIMARY KEY ("Id")
);

CREATE TABLE "Schedules" (
    "Id" uuid NOT NULL,
    "BusId" uuid NOT NULL,
    "RouteId" uuid NOT NULL,
    "DepartureTime" timestamp with time zone NOT NULL,
    "ArrivalTime" timestamp with time zone NOT NULL,
    "Price" numeric NOT NULL,
    "AvailableSeats" integer NOT NULL,
    CONSTRAINT "PK_Schedules" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Schedules_Buses_BusId" FOREIGN KEY ("BusId") REFERENCES "Buses" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Schedules_Routes_RouteId" FOREIGN KEY ("RouteId") REFERENCES "Routes" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Buses_OperatorId" ON "Buses" ("OperatorId");

CREATE INDEX "IX_Schedules_BusId" ON "Schedules" ("BusId");

CREATE INDEX "IX_Schedules_RouteId" ON "Schedules" ("RouteId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260422105601_AddBusSearchTables', '9.0.2');

ALTER TABLE "Buses" ADD "AssignedRouteId" uuid;

CREATE INDEX "IX_Buses_AssignedRouteId" ON "Buses" ("AssignedRouteId");

ALTER TABLE "Buses" ADD CONSTRAINT "FK_Buses_Routes_AssignedRouteId" FOREIGN KEY ("AssignedRouteId") REFERENCES "Routes" ("Id");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260422111028_AddAssignedRouteToBus', '9.0.2');

CREATE INDEX "IX_Schedules_DepartureTime" ON "Schedules" ("DepartureTime");

CREATE INDEX "IX_Routes_Destination" ON "Routes" ("Destination");

CREATE INDEX "IX_Routes_Source" ON "Routes" ("Source");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260422111414_AddSearchIndexes', '9.0.2');

ALTER TABLE "Schedules" ADD "Status" integer NOT NULL DEFAULT 0;

ALTER TABLE "Buses" ADD "LayoutConfig" text;

CREATE TABLE "Bookings" (
    "Id" uuid NOT NULL,
    "CustomerId" uuid NOT NULL,
    "JourneyId" uuid NOT NULL,
    "Status" integer NOT NULL,
    "TotalAmount" numeric NOT NULL,
    "PlatformFee" numeric NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_Bookings" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Bookings_Schedules_JourneyId" FOREIGN KEY ("JourneyId") REFERENCES "Schedules" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Bookings_Users_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "SeatLocks" (
    "Id" uuid NOT NULL,
    "JourneyId" uuid NOT NULL,
    "SeatNumber" text NOT NULL,
    "LockedByUserId" uuid NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_SeatLocks" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SeatLocks_Schedules_JourneyId" FOREIGN KEY ("JourneyId") REFERENCES "Schedules" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_SeatLocks_Users_LockedByUserId" FOREIGN KEY ("LockedByUserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Passengers" (
    "Id" uuid NOT NULL,
    "BookingId" uuid NOT NULL,
    "SeatNumber" text NOT NULL,
    "Name" text NOT NULL,
    "Age" integer NOT NULL,
    "Gender" integer NOT NULL,
    CONSTRAINT "PK_Passengers" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Passengers_Bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES "Bookings" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Payments" (
    "Id" uuid NOT NULL,
    "BookingId" uuid NOT NULL,
    "TransactionId" text,
    "Amount" numeric NOT NULL,
    "Status" integer NOT NULL,
    "ProcessedAt" timestamp with time zone,
    CONSTRAINT "PK_Payments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Payments_Bookings_BookingId" FOREIGN KEY ("BookingId") REFERENCES "Bookings" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Bookings_CustomerId" ON "Bookings" ("CustomerId");

CREATE INDEX "IX_Bookings_JourneyId" ON "Bookings" ("JourneyId");

CREATE INDEX "IX_Passengers_BookingId" ON "Passengers" ("BookingId");

CREATE UNIQUE INDEX "IX_Payments_BookingId" ON "Payments" ("BookingId");

CREATE UNIQUE INDEX "IX_SeatLocks_JourneyId_SeatNumber" ON "SeatLocks" ("JourneyId", "SeatNumber");

CREATE INDEX "IX_SeatLocks_LockedByUserId" ON "SeatLocks" ("LockedByUserId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260423120521_AddBookingModule', '9.0.2');

ALTER TABLE "Bookings" ADD "BoardingHubId" uuid;

ALTER TABLE "Bookings" ADD "DroppingHubId" uuid;

CREATE TABLE "Cities" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "State" text NOT NULL,
    CONSTRAINT "PK_Cities" PRIMARY KEY ("Id")
);

CREATE TABLE "Hubs" (
    "Id" uuid NOT NULL,
    "Name" text NOT NULL,
    "CityId" uuid NOT NULL,
    "Type" integer NOT NULL,
    CONSTRAINT "PK_Hubs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Hubs_Cities_CityId" FOREIGN KEY ("CityId") REFERENCES "Cities" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Bookings_BoardingHubId" ON "Bookings" ("BoardingHubId");

CREATE INDEX "IX_Bookings_DroppingHubId" ON "Bookings" ("DroppingHubId");

CREATE INDEX "IX_Hubs_CityId" ON "Hubs" ("CityId");

ALTER TABLE "Bookings" ADD CONSTRAINT "FK_Bookings_Hubs_BoardingHubId" FOREIGN KEY ("BoardingHubId") REFERENCES "Hubs" ("Id") ON DELETE RESTRICT;

ALTER TABLE "Bookings" ADD CONSTRAINT "FK_Bookings_Hubs_DroppingHubId" FOREIGN KEY ("DroppingHubId") REFERENCES "Hubs" ("Id") ON DELETE RESTRICT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260423143242_AddLocationEntities', '9.0.2');

DROP INDEX "IX_SeatLocks_LockedByUserId";

DROP INDEX "IX_Bookings_CustomerId";

CREATE INDEX "IX_SeatLocks_LockedByUserId_JourneyId" ON "SeatLocks" ("LockedByUserId", "JourneyId");

CREATE INDEX "IX_Bookings_CustomerId_JourneyId" ON "Bookings" ("CustomerId", "JourneyId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260423154224_AddBookingIndices', '9.0.2');

CREATE TABLE "PlatformSettings" (
    "Id" uuid NOT NULL,
    "Key" text NOT NULL,
    "Value" text NOT NULL,
    CONSTRAINT "PK_PlatformSettings" PRIMARY KEY ("Id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260423194529_AddPlatformSettings', '9.0.2');

CREATE TABLE "GlobalConfigurations" (
    "Id" uuid NOT NULL,
    "PlatformFeeType" text NOT NULL,
    "PlatformFeeValue" numeric NOT NULL,
    "OperatorCommissionPercentage" numeric NOT NULL,
    "LastUpdated" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_GlobalConfigurations" PRIMARY KEY ("Id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260424030456_AddGlobalConfiguration', '9.0.2');

ALTER TABLE "Hubs" ADD "OperatorId" uuid;

CREATE INDEX "IX_Hubs_OperatorId" ON "Hubs" ("OperatorId");

ALTER TABLE "Hubs" ADD CONSTRAINT "FK_Hubs_BusOperators_OperatorId" FOREIGN KEY ("OperatorId") REFERENCES "BusOperators" ("Id");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260424044714_SyncModelChanges', '9.0.2');

CREATE TABLE "Notifications" (
    "Id" uuid NOT NULL,
    "Message" text NOT NULL,
    "Title" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "IsRead" boolean NOT NULL,
    "UserId" uuid NOT NULL,
    "Type" text NOT NULL,
    "RelatedEntityId" text,
    CONSTRAINT "PK_Notifications" PRIMARY KEY ("Id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260424050710_AddNotifications', '9.0.2');

ALTER TABLE "Buses" ADD "IsApproved" boolean NOT NULL DEFAULT FALSE;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260424053828_AddBusApproval', '9.0.2');

ALTER TABLE "Schedules" ADD "BoardingHubIds" uuid[];

ALTER TABLE "Schedules" ADD "DroppingHubIds" uuid[];

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260424062739_AddScheduleHubs', '9.0.2');

COMMIT;

