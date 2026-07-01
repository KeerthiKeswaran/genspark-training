-- Database Creation Script & Reference Data Seeding

-- 1. DATABASE SYSTEM INITIALIZATION
-- CREATE DATABASE event_management_db;

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
Select * from "Bookings" Where "Booking_Status" = 'Payment Pending';
Select * from "BookingDetails";
Select * from "Venues";
Select * from "Management";
Select * from "Admins";
Select * from "Events" Where "Status" = 'Activation Pending';
Select * from "PlatformSettings";
Select * from "Transactions";
Select * from "EventReports";
Select * from "EventTicketTiers";
Select * from "VenueSeatCapacities";
Select * from "AdminActions";
Select * from "OrganizerUpfrontPayments";
Select * from "SupportTickets";
Select * from "TermsAndConditions";

Update "AdminActions"
Set "ActionStatus" = 'Pending'
Where "ActionId" = 10001;


Select * from "OrganizerUpfrontPayments";

Update "Events"
Set "Image_Url" = 'https://www.stayvista.com/blog/wp-content/uploads/2026/04/fevestival.png'
Where "Event_Id" = 10018;
----- Deleting -----

Update "PlatformSettings"
Set "Staff_Flat_Rate" = '200';

Delete from "OrganizerUpfrontPayments"
Where "Event_Id" = 11330;
Delete from "Transactions"
Where "Related_Id" = 11330;
Delete from "Events"
Where "Event_Id" = 11330;