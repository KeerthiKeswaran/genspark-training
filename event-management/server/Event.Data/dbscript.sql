-- Database Creation Script & Reference Data Seeding

-- 1. DATABASE SYSTEM INITIALIZATION
-- CREATE DATABASE event_management_db;

-- 2. SEED REGIONS (Mapped to table named "Management")
INSERT INTO "Management" ("Region_Id", "Region_Name", "No_Of_Staffs") VALUES
('MDU45', 'Madurai', 12),
('TRY78', 'Trichy', 10),
('HYD99', 'Hyderabad', 25),
('KOC44', 'Kochi', 6),
('SLM33', 'Salem', 5),
('TNV55', 'Tirunelveli', 4),
('VLR11', 'Vellore', 8),
('PUD04', 'Puducherry', 7)
ON CONFLICT ("Region_Id") DO NOTHING;

Select * from "Management";

-- 3. SEED TERMS & CONDITIONS (Needed for Register endpoint & Policies)
INSERT INTO "TermsAndConditions" ("Terms_Id", "Version", "File_Path", "Type", "Is_Active", "Created_At") VALUES
(10001, 'v1.0', 'assets/policies/10001.md', 'General', TRUE, NOW()),
(10002, 'v1.0', 'assets/policies/10002.md', 'Event', TRUE, NOW())
ON CONFLICT ("Terms_Id") DO NOTHING;

Select * from "TermsAndConditions";

-- 4. SEED ADMINISTRATORS (Roles: Admin and Finance)
-- Password for ADM01 is 'AdminPassword123!'
-- Password for FIN01 is 'FinancePassword123!'
INSERT INTO "Admins" ("Admin_Id", "Name", "Email", "Password_Hash", "Password_Reset_Token") VALUES
('ADM01', 'System Administrator', 'keerthiipsedits@gmail.com', '61eJPjKCSth/N5T72cAbXmAqrhLOyHLRuTnmKG1jGO3eu/Fwp2nldfpJ9UOW4S3p', NULL),
('FIN01', 'Finance Executive', 'fuelgrad@gmail.com', 'EBNGxyXSoaYbAAWAjCKcFG4PoXc7YgGT8nZ/QEnU3A8Jnj9lO3gkeLmfVHrx3QWG', NULL)
ON CONFLICT ("Admin_Id") DO NOTHING;

Select * from "Admins";

-- 5. SEED STANDARD USER
-- Password for KeerthiKeswaran is 'SecurePassword123!'
INSERT INTO "Users" ("User_Id", "Name", "Email", "Mobile_Number", "Password_Hash", "Has_Marketing_Consent", "Consented_Terms_Id", "Status") VALUES
(10000, 'KeerthiKeswaran', 'keshwarankeerthi@gmail.com', '9876543210', '61eJPjKCSth/N5T72cAbXmAqrhLOyHLRuTnmKG1jGO3eu/Fwp2nldfpJ9UOW4S3p', false, 10001, "Active")
ON CONFLICT ("User_Id") DO NOTHING;

Select * from "Users";

-- 6. SEED PLATFORM SETTINGS (Needed for bookings and payment logic)
INSERT INTO "PlatformSettings" (
    "Settings_Id", 
    "Staff_Flat_Rate", 
    "Virtual_Event_Activation_Fee", 
    "Physical_Event_Activation_Fee", 
    "Ticket_Commission_Percentage", 
    "Ticket_Fixed_Fee", 
    "Max_Tickets_Per_Booking", 
    "Updated_At", 
    "Updated_By_Admin_Id"
) VALUES (
    10000, 
    500.00,    -- Staff_Flat_Rate (₹500 flat fee per allocated staff member)
    500.00,    -- Virtual_Event_Activation_Fee (₹500 to publish/host virtual event)
    2000.00,   -- Physical_Event_Activation_Fee (₹2000 base fee to publish physical event)
    3.50,      -- Ticket_Commission_Percentage (3.5% commission cut per ticket sold)
    20.00,     -- Ticket_Fixed_Fee (₹20 flat booking convenience fee per ticket)
    10,        -- Max_Tickets_Per_Booking (Maximum of 10 tickets per single purchase)
    NOW(), 
    'ADM01'
);

DELETE FROM "PlatformSettings" WHERE "Settings_Id" = 10000;

Select * from "PlatformSettings";

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

Select * from "Venues";
Select * from "VenueSeatCapacities";

-- 8. SEED REGIONAL STAFF (Indian Names allocated to Tamil Nadu operational regions)
INSERT INTO "Staffs" ("Employee_ID", "Name", "Email", "Region_Id", "IsAllocated") VALUES
(10004, 'Arun Karthik', 'arun.k@example.com', 'CHE12', TRUE),
(10005, 'Meera Krishnan', 'meera.k@example.com', 'CHE12', FALSE),
(10006, 'Sanjay Viswanathan', 'sanjay.v@example.com', 'CBE23', TRUE),
(10007, 'Deepika Rangan', 'deepika.r@example.com', 'CBE23', FALSE),
(10008, 'Vijay Chandran', 'vijay.c@example.com', 'MDU45', TRUE),
(10009, 'Kavitha Murugan', 'kavitha.m@example.com', 'MDU45', TRUE),
(10010, 'Rahul Subramanian', 'rahul.s@example.com', 'TRY78', FALSE),
(10011, 'Sneha Gopal', 'sneha.g@example.com', 'TRY78', TRUE),
(10012, 'Karthik Raja', 'karthik.r@example.com', 'CHE12', TRUE),
(10013, 'Aishwarya Srinivasan', 'aishwarya.s@example.com', 'CHE12', FALSE),
(10014, 'Manoj Prabakar', 'manoj.p@example.com', 'CBE23', TRUE),
(10015, 'Divya Bharathi', 'divya.b@example.com', 'CBE23', TRUE),
(10016, 'Hariharan Natarajan', 'hari.n@example.com', 'MDU45', FALSE),
(10017, 'Nandhini Devi', 'nandhini.d@example.com', 'MDU45', TRUE),
(10018, 'Prakash Rajan', 'prakash.r@example.com', 'TRY78', TRUE),
(10019, 'Gayathri Ram', 'gayathri.r@example.com', 'TRY78', FALSE),
(10020, 'Suresh Kumar', 'suresh.k@example.com', 'CHE12', TRUE),
(10021, 'Shalini Venkatesh', 'shalini.v@example.com', 'CHE12', TRUE),
(10022, 'Balaji Sundaram', 'balaji.s@example.com', 'CBE23', FALSE),
(10023, 'Ramya Pandian', 'ramya.p@example.com', 'CBE23', TRUE),
(10024, 'Saravanan Muthu', 'saravanan.m@example.com', 'MDU45', TRUE),
(10025, 'Janani Sankar', 'janani.s@example.com', 'MDU45', FALSE),
(10026, 'Vigneshwaran Palani', 'vignesh.p@example.com', 'TRY78', TRUE),
(10027, 'Sandhya Mani', 'sandhya.m@example.com', 'TRY78', TRUE),
(10028, 'Dinesh Karthik', 'dinesh.k@example.com', 'CHE12', FALSE)
ON CONFLICT ("Employee_ID") DO NOTHING;


Select * from "Staffs";

Update "Staffs"
Set "IsAllocated" = false;


-- 9. SEED EVENT & EVENT TICKET TIERS - PHYSICAL (Chennai Tech Summit)
INSERT INTO "Events" (
    "Event_Id",
    "Organizer_Id",
    "Venue_Id",
    "Event_Type",
    "Title",
    "Description_Url",
    "Image_Url",
    "Date_Time",
    "Duration_Hours",
    "Status",
    "Requires_Staff",
    "Virtual_Url",
    "Virtual_Password_Hash"
) VALUES (
    10001,
    11992, -- References seeded standard user KeerthiKeswaran (10000)
    10002, -- References Chennai Trade Centre (10002) in region CHE12
    'Physical',
    'Chennai International Tech Summit 2026',
    'https://example.com/events/chennai-tech-summit-desc',
    'https://example.com/images/chennai-tech-summit.jpg',
    NOW() + INTERVAL '10 days', -- Scheduled in the future (required by logic)
    6.5, -- 6 hours and 30 minutes duration
    'Live',
    TRUE,
    NULL,
    NULL
)
ON CONFLICT ("Event_Id") DO NOTHING;

INSERT INTO "EventTicketTiers" (
    "Event_Id",
    "Tier_Name",
    "Price",
    "Tickets_Sold"
) VALUES 
(10001, 'General Admission', 150.00, 0),
(10001, 'VIP Access', 500.00, 0)
ON CONFLICT ("Event_Id", "Tier_Name") DO NOTHING;

Select * From "Events";
Select * from "EventTicketTiers";



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
Select * from "Events";
Select * from "PlatformSettings";
Select * from "Transactions";
Select * from "EventReports";
Select * from "EventTicketTiers";
Select * from "VenueSeatCapacities";
Select * from "AdminActions";
Select * from "OrganizerUpfrontPayments";
Select * from "SupportTickets";
Update "AdminActions"
Set "ActionStatus" = 'Pending'
Where "ActionId" = 10001;


Select * from "OrganizerUpfrontPayments";


----- Deleting -----

Delete from "OrganizerUpfrontPayments"
Where "Event_Id" = 11330;
Delete from "Transactions"
Where "Related_Id" = 11330;
Delete from "Events"
Where "Event_Id" = 11330;