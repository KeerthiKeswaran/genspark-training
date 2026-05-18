## Test Case 1: Standard Borrow and Return
Log in to the Member portal and borrow a book using today's date.
Navigate to View Borrowed Books and submit it for return with a delayed date.
Log in to the Admin portal, go to Return Submissions, and approve it as Good Condition.
Check the Member's Payment Dues to ensure fines were applied.

### Output
==== COMMUNITY LIBRARY SYSTEM ====
1. Admin Side
2. Member Side
3. Exit

Choice: 2

==== MEMBER PORTAL ====
1. Login
2. Register
3. Back
Choice: 1
Enter Username or Email:  (or 0 to cancel): suresh@gmail.com
Enter Password:  (or 0 to cancel): ******

Welcome Suresh!
1. Search & Borrow Books
2. View Borrowed Books (Return)
3. View Payment Dues
4. Logout
5. Exit

Choice: 1

Available Categories: Fiction, Science Fiction, Biography, Technology, History, Mystery, Motivational
Search (Title/Author/Category):  (or 0 to cancel): Biography

--- Search Results ---
ID: 3 | Steve Jobs by Walter Isaacson
ID: 9 | Becoming by Michelle Obama

Enter Book ID to View Details:  (or 0 to cancel): 3

--- Steve Jobs ---
Author: Walter Isaacson
Category: Biography
------------------------------
Description: The authorized biography of the Apple co-founder.
------------------------------
1. Borrow this Book
2. Go Back

Choice: 1
Enter borrow date (dd-mm-yyyy): 10-05-2026

Success! You have borrowed 'Steve Jobs'.

Welcome Suresh!
1. Search & Borrow Books
2. View Borrowed Books (Return)
3. View Payment Dues
4. Logout
5. Exit

Choice: 2
BorrowID: 7 | BookID: 3 | Due: 25/05/2026 | Remarks: No Remarks

Enter Borrow ID to Return:  (or 0 to cancel): 7
Enter return date (dd-mm-yyyy): 10-06-2026

Book submitted for return! Status: Pending Admin Approval.
 Note: Fine will be updated upon late return or damaged condition!

Welcome Suresh!
1. Search & Borrow Books
2. View Borrowed Books (Return)
3. View Payment Dues
4. Logout
5. Exit

Choice: 4

==== COMMUNITY LIBRARY SYSTEM ====
1. Admin Side
2. Member Side
3. Exit

Choice: 1

==== ADMIN PORTAL ====
1. Login
2. Register (Admin)
3. Back

Choice: 1
Enter Admin Username or Email:  (or 0 to cancel): karthik@gmail.com
Enter Password:  (or 0 to cancel): ********

Welcome Karthik! [ADMIN]
------------------------------
1. Dashboard (Reports)
2. List Books
3. Get Overdue Books
4. Member History
5. Members with Pending Fees
6. Return Submissions
7. Configure Fines
8. Update Member Status
9. Logout
10. Exit

Select: 6

==== RETURN SUBMISSIONS ====
ReturnID: 7 | BorrowID: 7 | MemberID: 6 | BookID: 3 | Due: 25/05/2026 | Returned: 10/06/2026

Enter Return ID to process (or 0 to cancel): 7
1. Approve (Good Condition)
2. Approve (Damaged Condition)
3. Reject (Wrong Book / Not Returned)
Choice: 1

Return Approved as Good Condition.

Welcome Karthik! [ADMIN]
------------------------------
1. Dashboard (Reports)
2. List Books
3. Get Overdue Books
4. Member History
5. Members with Pending Fees
6. Return Submissions
7. Configure Fines
8. Update Member Status
9. Logout
10. Exit

Select: 9

==== COMMUNITY LIBRARY SYSTEM ====
1. Admin Side
2. Member Side
3. Exit

Choice: 2

==== MEMBER PORTAL ====
1. Login
2. Register
3. Back
Choice: 1
Enter Username or Email:  (or 0 to cancel): Suresh          
Enter Password:  (or 0 to cancel): ******

Welcome Suresh!
1. Search & Borrow Books
2. View Borrowed Books (Return)
3. View Payment Dues
4. Logout
5. Exit

Choice: 3
FineID: 6 | Amount: ₹160.00 | Status: Unpaid | Reason: Late Return

Enter Fine ID to clear: 0


## Test Case 2: Admin Rejects a Return
Log in as a Member, borrow a book, and submit it for return.
Log in as Admin, go to Return Submissions, and choose to Reject the return with a custom remark.
Log in as the Member, view active borrowed books, and verify the book is still listed with the admin's rejection remark.

### Output
Welcome Suresh!
1. Search & Borrow Books
2. View Borrowed Books (Return)
3. View Payment Dues
4. Logout
5. Exit

Choice: 1

Available Categories: Fiction, Science Fiction, Biography, Technology, History, Mystery, Motivational
Search (Title/Author/Category):  (or 0 to cancel): Technology

--- Search Results ---
ID: 4 | Clean Code by Robert C. Martin
ID: 10 | The Pragmatic Programmer by Andrew Hunt

Enter Book ID to View Details:  (or 0 to cancel): 4

--- Clean Code ---
Author: Robert C. Martin
Category: Technology
------------------------------
Description: A handbook of agile software craftsmanship.
------------------------------
1. Borrow this Book
2. Go Back

Choice: 1
Enter borrow date (dd-mm-yyyy): 09-05-2026

Success! You have borrowed 'Clean Code'.

Welcome Suresh!
1. Search & Borrow Books
2. View Borrowed Books (Return)
3. View Payment Dues
4. Logout
5. Exit

Choice: 2
BorrowID: 8 | BookID: 4 | Due: 24/05/2026 | Remarks: No Remarks

Enter Borrow ID to Return:  (or 0 to cancel): 8
Enter return date (dd-mm-yyyy): 20-05-2026

Book submitted for return! Status: Pending Admin Approval.
 Note: Fine will be updated upon late return or damaged condition!

Welcome Suresh!
1. Search & Borrow Books
2. View Borrowed Books (Return)
3. View Payment Dues
4. Logout
5. Exit

Choice: 4

==== COMMUNITY LIBRARY SYSTEM ====
1. Admin Side
2. Member Side
3. Exit

Choice: 1

==== ADMIN PORTAL ====
1. Login
2. Register (Admin)
3. Back

Choice: 1
Enter Admin Username or Email:  (or 0 to cancel): karthik@gmail.com
Enter Password:  (or 0 to cancel): ********

Welcome Karthik! [ADMIN]
------------------------------
1. Dashboard (Reports)
2. List Books
3. Get Overdue Books
4. Member History
5. Members with Pending Fees
6. Return Submissions
7. Configure Fines
8. Update Member Status
9. Logout
10. Exit

Select: 6

==== RETURN SUBMISSIONS ====
ReturnID: 8 | BorrowID: 8 | MemberID: 6 | BookID: 4 | Due: 24/05/2026 | Returned: 20/05/2026

Enter Return ID to process (or 0 to cancel): 8
1. Approve (Good Condition)
2. Approve (Damaged Condition)
3. Reject (Wrong Book / Not Returned)
Choice: 3
Enter Reason for Rejection:  (or 0 to cancel): Book seems to be different!

Return Rejected. Remark added: Book seems to be different!

Welcome Karthik! [ADMIN]
------------------------------
1. Dashboard (Reports)
2. List Books
3. Get Overdue Books
4. Member History
5. Members with Pending Fees
6. Return Submissions
7. Configure Fines
8. Update Member Status
9. Logout
10. Exit

Select: 9

==== COMMUNITY LIBRARY SYSTEM ====
1. Admin Side
2. Member Side
3. Exit

Choice: 2

==== MEMBER PORTAL ====
1. Login
2. Register
3. Back
Choice: 1
Enter Username or Email:  (or 0 to cancel): Suresh
Enter Password:  (or 0 to cancel): ******

Welcome Suresh!
1. Search & Borrow Books
2. View Borrowed Books (Return)
3. View Payment Dues
4. Logout
5. Exit

Choice: 2
BorrowID: 8 | BookID: 4 | Due: 24/05/2026 | Remarks: Book seems to be different!

Enter Borrow ID to Return:  (or 0 to cancel): 0



## Test Case 3: Damaged Book Fine Verification
Log in as Admin, go to Configure Fines, and change the Damaged Copy fine to 100.
Log in as a Member, borrow a book, and submit it for return.
Log in as Admin and approve the return under Damaged Condition.
Log in as the Member, check Payment Dues, and verify there is an unpaid fine of exactly 100.

### Output

Welcome Suresh!
1. Search & Borrow Books
2. View Borrowed Books (Return)
3. View Payment Dues
4. Logout
5. Exit

Choice: 2
BorrowID: 8 | BookID: 4 | Due: 24/05/2026 | Remarks: Book seems to be different!

Enter Borrow ID to Return:  (or 0 to cancel): 8
Enter return date (dd-mm-yyyy): 20-05-2026

Book submitted for return! Status: Pending Admin Approval.
 Note: Fine will be updated upon late return or damaged condition!

Welcome Suresh!
1. Search & Borrow Books
2. View Borrowed Books (Return)
3. View Payment Dues
4. Logout
5. Exit

Choice: 4

==== COMMUNITY LIBRARY SYSTEM ====
1. Admin Side
2. Member Side
3. Exit

Choice: 1

==== ADMIN PORTAL ====
1. Login
2. Register (Admin)
3. Back

Choice: 1
Enter Admin Username or Email:  (or 0 to cancel): Karthik
Enter Password:  (or 0 to cancel): ********

Welcome Karthik! [ADMIN]
------------------------------
1. Dashboard (Reports)
2. List Books
3. Get Overdue Books
4. Member History
5. Members with Pending Fees
6. Return Submissions
7. Configure Fines
8. Update Member Status
9. Logout
10. Exit

Select: 6

==== RETURN SUBMISSIONS ====
ReturnID: 9 | BorrowID: 8 | MemberID: 6 | BookID: 4 | Due: 24/05/2026 | Returned: 20/05/2026

Enter Return ID to process (or 0 to cancel): 9
1. Approve (Good Condition)
2. Approve (Damaged Condition)
3. Reject (Wrong Book / Not Returned)
Choice: 2

Return Approved as Damaged Condition (Standard fee applied).

Welcome Karthik! [ADMIN]
------------------------------
1. Dashboard (Reports)
2. List Books
3. Get Overdue Books
4. Member History
5. Members with Pending Fees
6. Return Submissions
7. Configure Fines
8. Update Member Status
9. Logout
10. Exit

Select: 9

==== COMMUNITY LIBRARY SYSTEM ====
1. Admin Side
2. Member Side
3. Exit

Choice: 2

==== MEMBER PORTAL ====
1. Login
2. Register
3. Back
Choice: 1
Enter Username or Email:  (or 0 to cancel): Suresh
Enter Password:  (or 0 to cancel): ******

Welcome Suresh!
1. Search & Borrow Books
2. View Borrowed Books (Return)
3. View Payment Dues
4. Logout
5. Exit

Choice: 3
FineID: 6 | Amount: ₹160.00 | Status: Unpaid | Reason: Late Return
FineID: 7 | Amount: ₹100 | Status: Unpaid | Reason: Damaged Book

Enter Fine ID to clear: 0



## Test Case 4: Late Return Fine Calculation
Log in as a Member, borrow a book.
Submit the book for return using future return date beyond due date.
Log in as Admin and approve the return as Good Condition.
Log in as the Member, check Payment Dues, and verify a late return fine is present.

### Output

Welcome Suresh!
1. Search & Borrow Books
2. View Borrowed Books (Return)
3. View Payment Dues
4. Logout
5. Exit

Choice: 1

Available Categories: Fiction, Science Fiction, Biography, Technology, History, Mystery, Motivational
Search (Title/Author/Category):  (or 0 to cancel): Fict

--- Search Results ---
ID: 1 | To Kill a Mockingbird by Harper Lee
ID: 2 | Dune by Frank Herbert
ID: 7 | The Great Gatsby by F. Scott Fitzgerald
ID: 8 | Foundation by Isaac Asimov

Enter Book ID to View Details:  (or 0 to cancel): 7

--- The Great Gatsby ---
Author: F. Scott Fitzgerald
Category: Fiction
------------------------------
Description: A story of wealth, love, and the American Dream.
------------------------------
1. Borrow this Book
2. Go Back

Choice: 1
Enter borrow date (dd-mm-yyyy): 877665
Invalid format provided. Automatically defaulting to today's date.

Success! You have borrowed 'The Great Gatsby'.

Welcome Suresh!
1. Search & Borrow Books
2. View Borrowed Books (Return)
3. View Payment Dues
4. Logout
5. Exit

Choice: 2
BorrowID: 9 | BookID: 6 | Due: 25/05/2026 | Remarks: No Remarks
BorrowID: 10 | BookID: 7 | Due: 02/06/2026 | Remarks: No Remarks

Enter Borrow ID to Return:  (or 0 to cancel): 10
Enter return date (dd-mm-yyyy): 20-07-2026

Book submitted for return! Status: Pending Admin Approval.
 Note: Fine will be updated upon late return or damaged condition!

Welcome Suresh!
1. Search & Borrow Books
2. View Borrowed Books (Return)
3. View Payment Dues
4. Logout
5. Exit

Choice: 4

==== COMMUNITY LIBRARY SYSTEM ====
1. Admin Side
2. Member Side
3. Exit

Choice: 1

==== ADMIN PORTAL ====
1. Login
2. Register (Admin)
3. Back

Choice: 1
Enter Admin Username or Email:  (or 0 to cancel): Karthik
Enter Password:  (or 0 to cancel): ********

Welcome Karthik! [ADMIN]
------------------------------
1. Dashboard (Reports)
2. List Books
3. Get Overdue Books
4. Member History
5. Members with Pending Fees
6. Return Submissions
7. Configure Fines
8. Update Member Status
9. Logout
10. Exit

Select: 6

==== RETURN SUBMISSIONS ====
ReturnID: 11 | BorrowID: 10 | MemberID: 6 | BookID: 7 | Due: 02/06/2026 | Returned: 20/07/2026

Enter Return ID to process (or 0 to cancel): 11
1. Approve (Good Condition)
2. Approve (Damaged Condition)
3. Reject (Wrong Book / Not Returned)
Choice: 1

Return Approved as Good Condition.

Welcome Karthik! [ADMIN]
------------------------------
1. Dashboard (Reports)
2. List Books
3. Get Overdue Books
4. Member History
5. Members with Pending Fees
6. Return Submissions
7. Configure Fines
8. Update Member Status
9. Logout
10. Exit

Select: 9

==== COMMUNITY LIBRARY SYSTEM ====
1. Admin Side
2. Member Side
3. Exit

Choice: 2

==== MEMBER PORTAL ====
1. Login
2. Register
3. Back
Choice: 1
Enter Username or Email:  (or 0 to cancel): Suresh
Enter Password:  (or 0 to cancel): ******

Welcome Suresh!
1. Search & Borrow Books
2. View Borrowed Books (Return)
3. View Payment Dues
4. Logout
5. Exit

Choice: 2
BorrowID: 9 | BookID: 6 | Due: 25/05/2026 | Remarks: No Remarks

Enter Borrow ID to Return:  (or 0 to cancel): 0

Operation canceled. Returning to member menu...

Welcome Suresh!
1. Search & Borrow Books
2. View Borrowed Books (Return)
3. View Payment Dues
4. Logout
5. Exit

Choice: 3
FineID: 6 | Amount: ₹160.00 | Status: Unpaid | Reason: Late Return
FineID: 7 | Amount: ₹100.00 | Status: Unpaid | Reason: Damaged Book
FineID: 8 | Amount: ₹480.00 | Status: Unpaid | Reason: Late Return

Enter Fine ID to clear: 0



## Test Case 5: Overdue Borrowing Block
Verify the system completely blocks the borrow action because the member has an fine amount more than Rs.500.

### Output:
Welcome Suresh!
1. Search & Borrow Books
2. View Borrowed Books (Return)
3. View Payment Dues
4. Logout
5. Exit

Choice: 3
FineID: 6 | Amount: ₹160.00 | Status: Unpaid | Reason: Late Return
FineID: 7 | Amount: ₹100.00 | Status: Unpaid | Reason: Damaged Book
FineID: 8 | Amount: ₹480.00 | Status: Unpaid | Reason: Late Return

Enter Fine ID to clear: 0

Welcome Suresh!
1. Search & Borrow Books
2. View Borrowed Books (Return)
3. View Payment Dues
4. Logout
5. Exit

Choice: 1

Available Categories: Fiction, Science Fiction, Biography, Technology, History, Mystery, Motivational
Search (Title/Author/Category):  (or 0 to cancel): Science

--- Search Results ---
ID: 2 | Dune by Frank Herbert
ID: 8 | Foundation by Isaac Asimov

Enter Book ID to View Details:  (or 0 to cancel): 2

--- Dune ---
Author: Frank Herbert
Category: Science Fiction
------------------------------
Description: A sprawling epic set on the desert planet of Arrakis.
------------------------------
1. Borrow this Book
2. Go Back

Choice: 1
Enter borrow date (dd-mm-yyyy): 
Invalid format provided. Automatically defaulting to today's date.

Error: Borrowing blocked. Unpaid fines (₹740.00) exceed the limit of ₹500.




## Test Case 6: Member Deactivation Check
Log in as Admin and go to Update Member Status.
Select an active member and toggle their status to Inactive.
Go to the Member portal and try to log in using that member's email.
Verify the system denies access with a deactivation message.

### Output
Welcome Karthik! [ADMIN]
------------------------------
1. Dashboard (Reports)
2. List Books
3. Get Overdue Books
4. Member History
5. Members with Pending Fees
6. Return Submissions
7. Configure Fines
8. Update Member Status
9. Logout
10. Exit

Select: 8

--- Update Member Status ---
ID: 1 | Name: Keerthi | Status: Active
ID: 6 | Name: Suresh | Status: Active

Enter Member ID to toggle status (or 0 to cancel): 6

Member Suresh, status is deactivated

Welcome Karthik! [ADMIN]
------------------------------
1. Dashboard (Reports)
2. List Books
3. Get Overdue Books
4. Member History
5. Members with Pending Fees
6. Return Submissions
7. Configure Fines
8. Update Member Status
9. Logout
10. Exit

Select: 9

==== COMMUNITY LIBRARY SYSTEM ====
1. Admin Side
2. Member Side
3. Exit

Choice: 2

==== MEMBER PORTAL ====
1. Login
2. Register
3. Back
Choice: 1
Enter Username or Email:  (or 0 to cancel): Suresh
Enter Password:  (or 0 to cancel): ******

This account has been deactivated, contact the administrator for more details.


## Test Case 7: Basic Membership Limit Enforcement
Register a new member account with the Basic membership type.
Log in and borrow books one by one until you reach the Basic limit.
Try to borrow one additional book.
Verify the system blocks the action because the maximum borrowing limit is reached.

### Output:

==== MEMBER PORTAL ====
1. Login
2. Register
3. Back
Choice: 2
Enter Name:  (or 0 to cancel): ram
Enter Phone:  (or 0 to cancel): 8767654543
Enter Email:  (or 0 to cancel): ram@gmail.com
Available Types: Basic, Student, Premium
Enter Membership Type:  (or 0 to cancel): Basic
Enter Password:  (or 0 to cancel): ram29

Registered Successfully!

==== COMMUNITY LIBRARY SYSTEM ====
1. Admin Side
2. Member Side
3. Exit

Choice: 2

==== MEMBER PORTAL ====
1. Login
2. Register
3. Back
Choice: 1
Enter Username or Email:  (or 0 to cancel): ram
Enter Password:  (or 0 to cancel): *****

Welcome ram!
1. Search & Borrow Books
2. View Borrowed Books (Return)
3. View Payment Dues
4. Logout
5. Exit

Choice: 1

Available Categories: Fiction, Science Fiction, Biography, Technology, History, Mystery, Motivational
Search (Title/Author/Category):  (or 0 to cancel): Bio

--- Search Results ---
ID: 3 | Steve Jobs by Walter Isaacson
ID: 9 | Becoming by Michelle Obama

Enter Book ID to View Details:  (or 0 to cancel): 3

--- Steve Jobs ---
Author: Walter Isaacson
Category: Biography
------------------------------
Description: The authorized biography of the Apple co-founder.
------------------------------
1. Borrow this Book
2. Go Back

Choice: 1
Enter borrow date (dd-mm-yyyy): 
Invalid format provided. Automatically defaulting to today's date.

Success! You have borrowed 'Steve Jobs'.

Welcome ram!
1. Search & Borrow Books
2. View Borrowed Books (Return)
3. View Payment Dues
4. Logout
5. Exit

Choice: 1

Available Categories: Fiction, Science Fiction, Biography, Technology, History, Mystery, Motivational
Search (Title/Author/Category):  (or 0 to cancel): Fic

--- Search Results ---
ID: 1 | To Kill a Mockingbird by Harper Lee
ID: 2 | Dune by Frank Herbert
ID: 7 | The Great Gatsby by F. Scott Fitzgerald
ID: 8 | Foundation by Isaac Asimov

Enter Book ID to View Details:  (or 0 to cancel): 2

--- Dune ---
Author: Frank Herbert
Category: Science Fiction
------------------------------
Description: A sprawling epic set on the desert planet of Arrakis.
------------------------------
1. Borrow this Book
2. Go Back

Choice: 1
Enter borrow date (dd-mm-yyyy): 
Invalid format provided. Automatically defaulting to today's date.

Success! You have borrowed 'Dune'.

Welcome ram!
1. Search & Borrow Books
2. View Borrowed Books (Return)
3. View Payment Dues
4. Logout
5. Exit

Choice: 2
BorrowID: 11 | BookID: 3 | Due: 25/05/2026 | Remarks: No Remarks
BorrowID: 12 | BookID: 2 | Due: 25/05/2026 | Remarks: No Remarks

Enter Borrow ID to Return:  (or 0 to cancel): 0

Operation canceled. Returning to member menu...

Welcome ram!
1. Search & Borrow Books
2. View Borrowed Books (Return)
3. View Payment Dues
4. Logout
5. Exit

Choice: 1

Available Categories: Fiction, Science Fiction, Biography, Technology, History, Mystery, Motivational
Search (Title/Author/Category):  (or 0 to cancel): Tech

--- Search Results ---
ID: 4 | Clean Code by Robert C. Martin
ID: 10 | The Pragmatic Programmer by Andrew Hunt

Enter Book ID to View Details:  (or 0 to cancel): 10

--- The Pragmatic Programmer ---
Author: Andrew Hunt
Category: Technology
------------------------------
Description: Essential advice for software developers.
------------------------------
1. Borrow this Book
2. Go Back

Choice: 1
Enter borrow date (dd-mm-yyyy): 
Invalid format provided. Automatically defaulting to today's date.

Error: Borrowing limit reached for Basic membership (2 books).


## Test Case 8: Dashboard and Summary Integrity
Ensure a member has borrowed several books and has unpaid fines.
Log in as Admin and check the Member History for that specific member.
Verify the summary correctly calculates active borrowings, lifetime borrowed count, and total fines.
Check the main Dashboard to ensure the most borrowed books list reflects the recent borrowing activities.

### Output

elcome Keerthi!
1. Search & Borrow Books
2. View Borrowed Books (Return)
3. View Payment Dues
4. Logout
5. Exit

Choice: 2
BorrowID: 13 | BookID: 9 | Due: 28/05/2026 | Remarks: No Remarks
BorrowID: 14 | BookID: 7 | Due: 28/05/2026 | Remarks: No Remarks

Enter Borrow ID to Return:  (or 0 to cancel): 0

Operation canceled. Returning to member menu...

Welcome Keerthi!
1. Search & Borrow Books
2. View Borrowed Books (Return)
3. View Payment Dues
4. Logout
5. Exit

Choice: 1

Available Categories: Fiction, Science Fiction, Biography, Technology, History, Mystery, Motivational
Search (Title/Author/Category):  (or 0 to cancel): Myst

--- Search Results ---
ID: 6 | The Girl with the Dragon Tattoo by Stieg Larsson

Enter Book ID to View Details:  (or 0 to cancel): 6

--- The Girl with the Dragon Tattoo ---
Author: Stieg Larsson
Category: Mystery
------------------------------
Description: A dark mystery involving a disgraced journalist and a hacker.
------------------------------
1. Borrow this Book
2. Go Back

Choice: 1
Enter borrow date (dd-mm-yyyy): 
Invalid format provided. Automatically defaulting to today's date.

Error: Borrowing blocked. Unpaid fines (₹730.00) exceed the limit of ₹500.

Welcome Keerthi!
1. Search & Borrow Books
2. View Borrowed Books (Return)
3. View Payment Dues
4. Logout
5. Exit

Choice: 4

==== COMMUNITY LIBRARY SYSTEM ====
1. Admin Side
2. Member Side
3. Exit

Choice: 1

==== ADMIN PORTAL ====
1. Login
2. Register (Admin)
3. Back

Choice: 1
Enter Admin Username or Email:  (or 0 to cancel): Karthik
Enter Password:  (or 0 to cancel): ********

Welcome Karthik! [ADMIN]
------------------------------
1. Dashboard (Reports)
2. List Books
3. Get Overdue Books
4. Member History
5. Members with Pending Fees
6. Return Submissions
7. Configure Fines
8. Update Member Status
9. Logout
10. Exit

Select: 4

==== MEMBER LIST ====
ID: 1 | Name: Keerthi | Status: Active
ID: 7 | Name: ram | Status: Active
ID: 6 | Name: Suresh | Status: Active

Enter Member ID to Review Borrowing Summary:  (or 0 to cancel): 1

--- Borrowing Summary for Member ID: 1 ---
Active Borrowings: 2
Total Books Borrowed (Lifetime): 8
Total Unpaid Fines: ₹730.00

Welcome Karthik! [ADMIN]
------------------------------
1. Dashboard (Reports)
2. List Books
3. Get Overdue Books
4. Member History
5. Members with Pending Fees
6. Return Submissions
7. Configure Fines
8. Update Member Status
9. Logout
10. Exit

Select: 5

==== MEMBERS WITH PENDING FEES ====
Keerthi (ID: 1) - Fine: ₹730.00
Suresh (ID: 6) - Fine: ₹740.00

Welcome Karthik! [ADMIN]
------------------------------
1. Dashboard (Reports)
2. List Books
3. Get Overdue Books
4. Member History
5. Members with Pending Fees
6. Return Submissions
7. Configure Fines
8. Update Member Status
9. Logout
10. Exit

Select: 1

==== DASHBOARD ====

[ MOST BORROWED BOOKS ]
- Steve Jobs: 3 borrows
- Dune: 2 borrows
- The Girl with the Dragon Tattoo: 2 borrows
- Becoming: 2 borrows
- Sapiens: 2 borrows
- The Great Gatsby: 2 borrows
- Foundation: 1 borrows
- Clean Code: 1 borrows

[ CURRENTLY BORROWED BOOKS ]
- MemberID: 6 | BookID: 6 | Due: 25/05/2026
- MemberID: 7 | BookID: 3 | Due: 25/05/2026
- MemberID: 7 | BookID: 2 | Due: 25/05/2026
- MemberID: 1 | BookID: 9 | Due: 28/05/2026
- MemberID: 1 | BookID: 7 | Due: 28/05/2026

Welcome Karthik! [ADMIN]
------------------------------
1. Dashboard (Reports)
2. List Books
3. Get Overdue Books
4. Member History
5. Members with Pending Fees
6. Return Submissions
7. Configure Fines
8. Update Member Status
9. Logout
10. Exit

Select:  10