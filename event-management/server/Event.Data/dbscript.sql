-- Database: event_management_db




----- Seeding Script ------

-- 1. SEED REGIONS (Mapped to table named "Management")
INSERT INTO "Management" ("Region_Id", "No_Of_Staffs") VALUES
('REG01', 10),
('REG02', 15),
('US-EAST', 8)
ON CONFLICT ("Region_Id") DO NOTHING;

-- 2. SEED TERMS & CONDITIONS (Needed for Register endpoint & Policies)
-- Standard User registration requires consenting to the active terms (ID 10000 by default)
INSERT INTO "TermsAndConditions" ("Terms_Id", "Version", "File_Path", "Type", "Is_Active", "Created_At") VALUES
(10000, 'v1.0', '/docs/policies/terms_v1.md', 'General', TRUE, NOW()),
(10001, 'v1.0', '/docs/policies/refund_v1.md', 'Refund', TRUE, NOW())
ON CONFLICT ("Terms_Id") DO NOTHING;

-- 3. SEED ADMINISTRATORS (Roles: Admin and Finance)
-- Password for ADM01 is 'AdminPassword123!'
-- Password for FIN01 is 'FinancePassword123!'
INSERT INTO "Admins" ("Admin_Id", "Name", "Email", "Password_Hash", "Password_Reset_Token") VALUES
('ADM01', 'System Administrator', 'admin@example.com', '61eJPjKCSth/N5T72cAbXmAqrhLOyHLRuTnmKG1jGO3eu/Fwp2nldfpJ9UOW4S3p', NULL),
('FIN01', 'Finance Executive', 'finance@example.com', 'EBNGxyXSoaYbAAWAjCKcFG4PoXc7YgGT8nZ/QEnU3A8Jnj9lO3gkeLmfVHrx3QWG', NULL)
ON CONFLICT ("Admin_Id") DO NOTHING;

-- 4. SEED PLATFORM SETTINGS (Needed for bookings and payment logic)
INSERT INTO "PlatformSettings" ("Settings_Id", "Staff_Flat_Rate", "Virtual_Event_Activation_Fee", "Physical_Event_Activation_Fee", "Ticket_Commission_Percentage", "Ticket_Fixed_Fee", "Max_Tickets_Per_Booking", "Updated_At", "Updated_By_Admin_Id") VALUES
(10000, 50.00, 25.00, 100.00, 2.50, 1.50, 10, NOW(), 'ADM01')
ON CONFLICT ("Settings_Id") DO NOTHING;

-- 5. SEED VENUES & SEAT CAPACITIES (Needed for physical event creation)
INSERT INTO "Venues" ("Venue_Id", "Name", "Region_Id", "Hourly_Price", "Is_Available") VALUES
(10001, 'Metropolitan Auditorium', 'REG01', 150.00, TRUE),
(10002, 'Silicon Center Hall A', 'REG02', 300.00, TRUE)
ON CONFLICT ("Venue_Id") DO NOTHING;

INSERT INTO "VenueSeatCapacities" ("Venue_Id", "Tier_Name", "Total_Seats") VALUES
(10001, 'General Admission', 500),
(10001, 'VIP Access', 50),
(10002, 'General Admission', 200),
(10002, 'VIP Access', 20)
ON CONFLICT ("Venue_Id", "Tier_Name") DO NOTHING;

-- 6. SEED REGIONAL STAFF (Needed for physical staff allocation checks)
INSERT INTO "Staffs" ("Employee_ID", "Name", "Email", "Region_Id", "Is_Available") VALUES
(10001, 'Alice Smith', 'alice@example.com', 'REG01', TRUE),
(10002, 'Bob Johnson', 'bob@example.com', 'REG01', TRUE),
(10003, 'Charlie Brown', 'charlie@example.com', 'REG02', TRUE)
ON CONFLICT ("Employee_ID") DO NOTHING;


----- Select all tables ------

SELECT schemaname, tablename 
FROM pg_catalog.pg_tables 
WHERE schemaname NOT IN ('pg_catalog', 'information_schema');



------- Clear all records --------

DO $$ 
DECLARE 
    r RECORD;
BEGIN
    -- Loop through all tables in the public schema except the EF Migrations table
    FOR r IN (
        SELECT tablename 
        FROM pg_tables 
        WHERE schemaname = 'public' 
          AND tablename NOT IN ('__EFMigrationsHistory', '__efmigrationshistory')
    ) LOOP
        -- Safe dynamic SQL execution using format()
        EXECUTE format('TRUNCATE TABLE %I CASCADE;', r.tablename);
    END LOOP;
END $$;



--------- Query ----------


Select * from "Users";
Select * from "UserInterestedRegions";
Select * from "TermsAndConditions";
Select * from "Bookings";
Select * from "Venues";
Select * from "Management";
Select * from "Admins";