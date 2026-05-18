-- 1. Book Categories
CREATE TABLE BookCategories (
    CategoryNumber SERIAL PRIMARY KEY,
    NoOfBooksAvailable INT DEFAULT 0
);


-- 2. Membership Limits (Configuration Table)
CREATE TABLE MembershipLimits (
    MemberType VARCHAR(20) PRIMARY KEY, -- Basic, Premium, Student
    MaxBooksAllowed INT NOT NULL,
    BorrowDurationDays INT NOT NULL
);

-- 3. Fine Configuration (Global Settings)
CREATE TABLE FineConfiguration (
    FineConfigId SERIAL PRIMARY KEY,
    FineType VARCHAR(50) UNIQUE NOT NULL,
    Amount DECIMAL(10, 2) NOT NULL
);


-- 4. Members & Admin
CREATE TABLE Members (
    MemberId SERIAL PRIMARY KEY,
    MemberName VARCHAR(100) NOT NULL,
    MemberPhone VARCHAR(15) UNIQUE NOT NULL,
    MemberEmail VARCHAR(100) UNIQUE NOT NULL,
    MemberType VARCHAR(20) REFERENCES MembershipLimits(MemberType),
    MemberStatus VARCHAR(20) CHECK (MemberStatus IN ('Active', 'Inactive')) DEFAULT 'Active'
);

CREATE TABLE Admins (
    AdminID SERIAL PRIMARY KEY,
    AdminName VARCHAR(100) NOT NULL,
    AdminPhone VARCHAR(15) UNIQUE NOT NULL,
    AdminEmail VARCHAR(100) UNIQUE NOT NULL
);

-- 4.5 Passwords (Authentication)
CREATE TABLE Passwords (
    UserId VARCHAR(50) PRIMARY KEY,
    PasswordHash TEXT NOT NULL
);

-- 5. Books
CREATE TABLE Books (
    BookId SERIAL PRIMARY KEY,
    BookTitle VARCHAR(200) NOT NULL,
    BookAuthor VARCHAR(100) NOT NULL,
    BookCategory VARCHAR(50),
    BookContents TEXT,
    CategoryNumber INT REFERENCES BookCategories(CategoryNumber)
);

-- 6. Book Copies
CREATE TABLE BookCopies (
    CopyId SERIAL PRIMARY KEY,
    BookId INT REFERENCES Books(BookId) ON DELETE CASCADE,
    CopyStatus VARCHAR(20) CHECK (CopyStatus IN ('Available', 'Borrowed', 'Unavailable')) DEFAULT 'Available',
    CopyCondition VARCHAR(20) CHECK (CopyCondition IN ('Good', 'Damaged')) DEFAULT 'Good'
);

-- 7. Borrowings
CREATE TABLE Borrowings (
    BorrowId SERIAL PRIMARY KEY,
    MemberId INT REFERENCES Members(MemberId),
    BookId INT REFERENCES Books(BookId),
    BorrowDate DATE DEFAULT CURRENT_DATE,
    DueDate DATE NOT NULL,
    ReturnStatus VARCHAR(20) CHECK (ReturnStatus IN ('Borrowed', 'Returned')) DEFAULT 'Borrowed',
    Remarks VARCHAR(200)
);


-- 8. Returns
CREATE TABLE Returns (
    ReturnId SERIAL PRIMARY KEY,
    BorrowId INT REFERENCES Borrowings(BorrowId),
    ActualReturnDate DATE DEFAULT CURRENT_DATE,
    FineAmount DECIMAL(10, 2) DEFAULT 0.00,
    ReturnApprovalStatus VARCHAR(20) DEFAULT 'Pending'
);

-- 9. Fine Calculation (Tracking)
CREATE TABLE FineCalculation (
    FineId SERIAL PRIMARY KEY,
    BorrowId INT REFERENCES Borrowings(BorrowId),
    FineAmount DECIMAL(10, 2) NOT NULL,
    FineStatus VARCHAR(20) CHECK (FineStatus IN ('Paid', 'Unpaid')) DEFAULT 'Unpaid',
    Remarks VARCHAR(200)
);

-- Seed Data for Business Rules
INSERT INTO MembershipLimits (MemberType, MaxBooksAllowed, BorrowDurationDays) VALUES
('Basic', 2, 7),
('Student', 3, 10),
('Premium', 5, 15);

-- Seed Data for Fine Configurations

INSERT INTO FineConfiguration (FineType, Amount) VALUES
('LateReturn', 10.00),
('Damaged', 50.00);

-- Seed Data for Book Categories
INSERT INTO BookCategories (CategoryName) VALUES
('Fiction'),
('Science Fiction'),
('Biography'),
('Technology'),
('History'),
('Mystery');

-- Seed Data for Books
-- Assuming CategoryNumbers 1-6 map to the categories inserted above
INSERT INTO Books (BookTitle, BookAuthor, BookCategory, BookContents, CategoryNumber) VALUES
('To Kill a Mockingbird', 'Harper Lee', 'Fiction', 'A classic novel about racial injustice in the American South.', 1),
('Dune', 'Frank Herbert', 'Science Fiction', 'A sprawling epic set on the desert planet of Arrakis.', 2),
('Steve Jobs', 'Walter Isaacson', 'Biography', 'The authorized biography of the Apple co-founder.', 3),
('Clean Code', 'Robert C. Martin', 'Technology', 'A handbook of agile software craftsmanship.', 4),
('Sapiens', 'Yuval Noah Harari', 'History', 'A brief history of humankind.', 5),
('The Girl with the Dragon Tattoo', 'Stieg Larsson', 'Mystery', 'A dark mystery involving a disgraced journalist and a hacker.', 6),
('The Great Gatsby', 'F. Scott Fitzgerald', 'Fiction', 'A story of wealth, love, and the American Dream.', 1),
('Foundation', 'Isaac Asimov', 'Science Fiction', 'The start of a grand space opera series.', 2),
('Becoming', 'Michelle Obama', 'Biography', 'The memoir of the former First Lady.', 3),
('The Pragmatic Programmer', 'Andrew Hunt', 'Technology', 'Essential advice for software developers.', 4);

Select * from BookCopies;


-- Seed Data for Book Copies
INSERT INTO BookCopies (BookId, CopyStatus, CopyCondition) VALUES
(1, 'Available', 'Good'), (1, 'Available', 'Good'), (1, 'Available', 'Good'), -- 3 copies of Book 1
(2, 'Available', 'Good'), (2, 'Available', 'Good'), (2, 'Available', 'Good'), (2, 'Available', 'Good'), (2, 'Available', 'Good'), -- 5 copies of Book 2
(3, 'Available', 'Good'), (3, 'Available', 'Good'), -- 2 copies of Book 3
(4, 'Available', 'Good'), (4, 'Available', 'Good'), (4, 'Available', 'Good'), (4, 'Available', 'Good'), -- 4 copies of Book 4
(5, 'Available', 'Good'), (5, 'Available', 'Good'), (5, 'Available', 'Good'), -- 3 copies of Book 5
(6, 'Available', 'Good'), (6, 'Available', 'Good'), -- 2 copies of Book 6
(7, 'Available', 'Good'), (7, 'Available', 'Good'), (7, 'Available', 'Good'), -- 3 copies of Book 7
(8, 'Available', 'Good'), (8, 'Available', 'Good'), (8, 'Available', 'Good'), (8, 'Available', 'Good'), -- 4 copies of Book 8
(9, 'Available', 'Good'), (9, 'Available', 'Good'), -- 2 copies of Book 9
(10, 'Available', 'Good'), (10, 'Available', 'Good'), (10, 'Available', 'Good'), (10, 'Available', 'Good'), (10, 'Available', 'Good'); -- 5 copies of Book 10




-- 10. PostgreSQL Functions

-- Returns total unpaid fine for a member
CREATE OR REPLACE FUNCTION calculate_member_fine(p_member_id INT) 
RETURNS DECIMAL(10,2) AS $$
BEGIN
    RETURN (
        SELECT COALESCE(SUM(f.FineAmount), 0)
        FROM FineCalculation f
        JOIN Borrowings b ON f.BorrowId = b.BorrowId
        WHERE b.MemberId = p_member_id AND f.FineStatus = 'Unpaid'
    );
END;
$$ LANGUAGE plpgsql;


-- Returns summary of borrowing for a member
CREATE OR REPLACE FUNCTION get_member_borrowing_summary(p_member_id INT)
RETURNS TABLE(active_loans BIGINT, total_borrowed BIGINT, unpaid_fines DECIMAL) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        COUNT(CASE WHEN b.ReturnStatus = 'Borrowed' THEN 1 END)::BIGINT,
        COUNT(*)::BIGINT,
        calculate_member_fine(p_member_id)
    FROM Borrowings b
    WHERE b.MemberId = p_member_id;
END;
$$ LANGUAGE plpgsql;

-- Returns top borrowed books
CREATE OR REPLACE FUNCTION get_most_borrowed_books()
RETURNS TABLE(b_title VARCHAR, borrow_count BIGINT) AS $$
BEGIN
    RETURN QUERY
    SELECT b.BookTitle, COUNT(br.BorrowId) as b_count
    FROM Books b
    JOIN Borrowings br ON b.BookId = br.BookId
    GROUP BY b.BookTitle
    ORDER BY b_count DESC;
END;
$$ LANGUAGE plpgsql;


-- 11. Stored Procedures

-- Deactivate a member
CREATE OR REPLACE PROCEDURE deactivate_member(p_member_id INT)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE Members SET MemberStatus = 'Inactive' WHERE MemberId = p_member_id;
END;
$$;


-- Test and Debug

Select * from FineCalculation;

Select * from FineConfiguration;

Select * from Returns;

Select * from Borrowings;

Select * from BookCopies;

Select * from MembershipLimits;

Select * from BookCategories;

Select* from Members;

Select * from Books;

Select * from Members;


ALTER TABLE finecalculation ADD COLUMN remarks VARCHAR(200);

Update finecalculation
set remarks = 'Late Return'
where fineid = 1;

Update finecalculation
set remarks = 'Late Return and Damaged'
where fineid = 2;