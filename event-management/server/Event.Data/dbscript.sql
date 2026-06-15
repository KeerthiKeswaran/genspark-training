-- Database Creation Script & Reference Data Seeding

-- 1. DATABASE SYSTEM INITIALIZATION
-- CREATE DATABASE event_management_db;

-- 2. SEED REGIONS (Mapped to table named "Management")
INSERT INTO "Management" ("Region_Id", "Region_Name", "No_Of_Staffs") VALUES
('CHE12', 'Chennai', 10),
('CBE23', 'Coimbatore', 15),
('BNG22', 'Bengaluru', 8)
ON CONFLICT ("Region_Id") DO NOTHING;

-- 3. SEED TERMS & CONDITIONS (Needed for Register endpoint & Policies)
INSERT INTO "TermsAndConditions" ("Terms_Id", "Version", "File_Path", "Type", "Is_Active", "Created_At") VALUES
(10000, 'v1.0', '/docs/policies/terms_v1.md', 'General', TRUE, NOW()),
(10001, 'v1.0', '/docs/policies/refund_v1.md', 'Refund', TRUE, NOW())
ON CONFLICT ("Terms_Id") DO NOTHING;

-- 4. SEED ADMINISTRATORS (Roles: Admin and Finance)
-- Password for ADM01 is 'AdminPassword123!'
-- Password for FIN01 is 'FinancePassword123!'
INSERT INTO "Admins" ("Admin_Id", "Name", "Email", "Password_Hash", "Password_Reset_Token") VALUES
('ADM01', 'System Administrator', 'admin@example.com', '61eJPjKCSth/N5T72cAbXmAqrhLOyHLRuTnmKG1jGO3eu/Fwp2nldfpJ9UOW4S3p', NULL),
('FIN01', 'Finance Executive', 'finance@example.com', 'EBNGxyXSoaYbAAWAjCKcFG4PoXc7YgGT8nZ/QEnU3A8Jnj9lO3gkeLmfVHrx3QWG', NULL)
ON CONFLICT ("Admin_Id") DO NOTHING;

-- 5. SEED STANDARD USER
-- Password for KeerthiKeswaran is 'SecurePassword123!'
INSERT INTO "Users" ("User_Id", "Name", "Email", "Mobile_Number", "Password_Hash", "Consented_Terms_Id", "Has_Marketing_Consent") VALUES
(10000, 'KeerthiKeswaran', 'keshwarankeerthi@gmail.com', '9876543210', '61eJPjKCSth/N5T72cAbXmAqrhLOyHLRuTnmKG1jGO3eu/Fwp2nldfpJ9UOW4S3p', 10000, TRUE)
ON CONFLICT ("User_Id") DO NOTHING;

-- 6. SEED PLATFORM SETTINGS (Needed for bookings and payment logic)
INSERT INTO "PlatformSettings" ("Settings_Id", "Staff_Flat_Rate", "Virtual_Event_Activation_Fee", "Physical_Event_Activation_Fee", "Ticket_Commission_Percentage", "Ticket_Fixed_Fee", "Max_Tickets_Per_Booking", "Updated_At", "Updated_By_Admin_Id") VALUES
(10000, 50.00, 25.00, 100.00, 2.50, 1.50, 10, NOW(), 'ADM01')
ON CONFLICT ("Settings_Id") DO NOTHING;

-- 7. SEED VENUES & SEAT CAPACITIES (Seeded with Tamil Nadu locations & addresses)
INSERT INTO "Venues" ("Venue_Id", "Name", "Region_Id", "Hourly_Price", "Is_Available", "Address") VALUES
(10001, 'Codissia Trade Fair Complex', 'CBE23', 500.00, TRUE, 'Avinashi Road, Civil Aerodrome Post, Coimbatore, Tamil Nadu 641014'),
(10002, 'Chennai Trade Centre', 'CHE12', 800.00, TRUE, 'Mount-Poonamallee Road, Nandambakkam, Chennai, Tamil Nadu 600089')
ON CONFLICT ("Venue_Id") DO NOTHING;

INSERT INTO "VenueSeatCapacities" ("Venue_Id", "Tier_Name", "Total_Seats") VALUES
(10001, 'General Admission', 500),
(10001, 'VIP Access', 50),
(10002, 'General Admission', 200),
(10002, 'VIP Access', 20)
ON CONFLICT ("Venue_Id", "Tier_Name") DO NOTHING;

-- 8. SEED REGIONAL STAFF (Indian Names allocated to Tamil Nadu operational regions)
INSERT INTO "Staffs" ("Employee_ID", "Name", "Email", "Region_Id", "Is_Available") VALUES
(10001, 'Rajesh Kumar', 'rajesh@example.com', 'CHE12', TRUE),
(10002, 'Priya Sharma', 'priya@example.com', 'CHE12', TRUE),
(10003, 'Anish Dev', 'anish@example.com', 'CBE23', TRUE)
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