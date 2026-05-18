using System;
using System.Collections.Generic;
using System.IO;
using LibrarySystem.Business.Services;
using Microsoft.Extensions.Configuration;
using LibrarySystem.Contracts.Interfaces;
using LibrarySystem.Data.Contexts;
using LibrarySystem.Data.Repositories;
using LibrarySystem.Data;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Presentation;

class Program
{
    private static IMemberService _memberService = null!;
    private static IBookService _bookService = null!;
    private static ILendingService _lendingService = null!;
    private static IFineService _fineService = null!;
    private static IReportService _reportService = null!;
    private static IAdminService _adminService = null!;
    private static IAuthService _authService = null!;

    private static Member? _currentMember;

    static void Main(string[] args)
    {
        InitializeServices();
        RunMainMenu();
    }

    private static void InitializeServices()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        var configuration = builder.Build();
        
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<LibraryDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        var context = new LibraryDbContext(optionsBuilder.Options);
        var repo = new LibraryRepository(context);
        _authService = new AuthService(repo);
        _memberService = new MemberService(repo, _authService);
        _bookService = new BookService(repo);
        _lendingService = new LendingService(repo);
        _fineService = new FineService(repo);
        _reportService = new ReportService(repo);
        _adminService = new AdminService(repo, _authService);
    }

    private static void RunMainMenu()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
          ██╗     ██╗██████╗ ██████╗  █████╗ ██████╗ ██╗   ██╗
          ██║     ██║██╔══██╗██╔══██╗██╔══██╗██╔══██╗╚██╗ ██╔╝
          ██║     ██║██████╔╝██████╔╝███████║██████╔╝ ╚████╔╝ 
          ██║     ██║██╔══██╗██╔══██╗██╔══██║██╔══██╗  ╚██╔╝  
          ███████╗██║██████╔╝██║  ██║██║  ██║██║  ██║   ██║   
          ╚══════╝╚═╝╚═════╝ ╚═╝  ╚═╝╚═╝  ╚═╝╚═╝  ╚═╝   ╚═╝   ");
        Console.ResetColor();

        while (true)
        {
            Console.WriteLine("\n==== COMMUNITY LIBRARY SYSTEM ====");
            Console.WriteLine("1. Admin Side");
            Console.WriteLine("2. Member Side");
            Console.WriteLine("3. Exit");
            Console.Write("\nChoice: ");

            var choice = Console.ReadLine();
            try {
                switch (choice)
                {
                    case "1": RunAdminFlow(); break;
                    case "2": RunMemberFlow(); break;
                    case "3": Environment.Exit(0); break;
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\nOperation canceled. Returning to main menu...");
            }
        }
    }

    private static void RunAdminFlow()
    {
        Console.WriteLine("\n==== ADMIN PORTAL ====");
        Console.WriteLine("1. Login");
        Console.WriteLine("2. Register (Admin)");
        Console.WriteLine("3. Back");
        Console.Write("\nChoice: ");
        
        var choice = Console.ReadLine();
        if (choice == "1")
        {
            var input = PromptInput("Enter Admin Username or Email: ");
            var admin = _adminService.Login(input);
            if (admin != null)
            {
                var password = PromptPassword("Enter Password: ");
                if (_authService.AuthenticateUser($"adm_{admin.Adminid}", password))
                {
                    RunAdminMenu(admin);
                }
                else
                {
                    Console.WriteLine("\nInvalid credentials!");
                }
            }
            else
            {
                Console.WriteLine("\nAdmin not found!");
            }
        }
        else if (choice == "2")
        {
            var name = PromptInput("Enter Admin Name: ");
            var phone = PromptInput("Enter Phone: ");
            var email = PromptInput("Enter Email: ");
            var password = PromptInput("Enter Password: ");
            try {
                _adminService.RegisterAdmin(name, phone, email, password);
                Console.WriteLine("\nAdmin Registered Successfully!");
            } catch (Exception ex) { 
                Console.WriteLine($"\nError: {ex.Message}"); 
                if (ex.InnerException != null) Console.WriteLine($"Inner: {ex.InnerException.Message}");
            }
        }
    }

    private static void RunAdminMenu(Admin admin)
    {
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nWelcome {admin.Adminname}! [ADMIN]");
            Console.ResetColor();
            Console.WriteLine("------------------------------");
            Console.WriteLine("1. Dashboard (Reports)");
            Console.WriteLine("2. List Books");
            Console.WriteLine("3. Get Overdue Books");
            Console.WriteLine("4. Member History");
            Console.WriteLine("5. Members with Pending Fees");
            Console.WriteLine("6. Return Submissions");
            Console.WriteLine("7. Configure Fines");
            Console.WriteLine("8. Update Member Status");
            Console.WriteLine("9. Logout");
            Console.WriteLine("10. Exit");
            Console.Write("\nSelect: ");

            var choice = Console.ReadLine();
            try 
            {
                switch (choice)
                {
                    case "1": ShowAdminDashboard(); break;
                    case "2": ShowAdminBooks(); break;
                    case "3": ShowOverdueBooks(); break;
                    case "4": ShowMemberHistory(); break;
                    case "5": ShowPendingFinesReport(); break;
                    case "6": HandleReturnSubmissions(); break;
                    case "7": ConfigureFines(); break;
                    case "8": AdminUpdateMemberStatus(); break;
                    case "9": return;
                    case "10": Environment.Exit(0); break;
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\nOperation canceled. Returning to admin menu...");
            }
        }
    }

    private static void AdminUpdateMemberStatus()
    {
        Console.WriteLine("\n--- Update Member Status ---");
        var members = _memberService.GetAllMembers();
        
        if (!members.Any())
        {
            Console.WriteLine("No members found in the system.");
            return;
        }

        foreach (var m in members)
        {
            Console.WriteLine($"ID: {m.Memberid} | Name: {m.Membername} | Status: {m.Memberstatus}");
        }

        Console.Write("\nEnter Member ID to toggle status (or 0 to cancel): ");
        if (int.TryParse(Console.ReadLine(), out int mid) && mid != 0)
        {
            try 
            {
                var msg = _memberService.UpdateMemberStatus(mid);
                Console.WriteLine($"\n{msg}");
            } 
            catch (Exception ex) 
            {
                Console.WriteLine($"\nError: {ex.Message}");
            }
        }
    }

    private static void ShowAdminDashboard()
    {
        Console.WriteLine("\n==== DASHBOARD ====");
        
        Console.WriteLine("\n[ MOST BORROWED BOOKS ]");
        var mostBorrowed = _reportService.GetMostBorrowedBooks();
        foreach(var mb in mostBorrowed) Console.WriteLine($"- {mb.Booktitle}: {mb.Borrowcount} borrows");

        Console.WriteLine("\n[ CURRENTLY BORROWED BOOKS ]");
        var active = _reportService.GetCurrentlyBorrowedBooks();
        foreach(var a in active) Console.WriteLine($"- MemberID: {a.Memberid} | BookID: {a.Bookid} | Due: {a.Duedate}");
    }

    private static void ShowAdminBooks()
    {
        Console.WriteLine("\n==== BOOK CATALOG ====");
        var books = _bookService.GetBooks();
        foreach(var b in books)
        {
            var copiesCount = _bookService.GetCopyCount(b.Bookid);
            Console.WriteLine($"ID: {b.Bookid} | {b.Booktitle} by {b.Bookauthor} [{copiesCount} copies]");
        }

        Console.WriteLine("\n1. Add Book");
        Console.WriteLine("2. Back");
        Console.Write("Choice: ");
        if (Console.ReadLine() == "1")
        {
            var title = PromptInput("Title: ");
            var author = PromptInput("Author: ");
            var category = PromptInput("Category Name: ");
            Console.Write("Initial Copies: ");
            if (!int.TryParse(Console.ReadLine(), out int copies)) copies = 1;

            try
            {
                _bookService.AddBook(title, author, category, copies);
                Console.WriteLine("\nBook and Copies added successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nDatabase Error: {ex.Message}");
            }
        }
    }

    private static void ShowOverdueBooks()
    {
        Console.WriteLine("\n==== OVERDUE BOOKS ====");
        var overdue = _reportService.GetOverdueBooks();
        
        if (!overdue.Any())
        {
            Console.WriteLine("No overdue books found in the system.");
            return;
        }

        foreach(var o in overdue) Console.WriteLine($"BorrowID: {o.Borrowid} | MemberID: {o.Memberid} | BookID: {o.Bookid} | Due: {o.Duedate}");
    }

    private static void ShowMemberHistory()
    {
        Console.WriteLine("\n==== MEMBER LIST ====");
        var members = _memberService.GetAllMembers();
        
        if (!members.Any())
        {
            Console.WriteLine("No members found in the system.");
            return;
        }

        foreach(var m in members) Console.WriteLine($"ID: {m.Memberid} | Name: {m.Membername} | Status: {m.Memberstatus}");

        var midInput = PromptInput("\nEnter Member ID to Review Borrowing Summary: ");
        if (int.TryParse(midInput, out int mid))
        {
            var summary = _reportService.GetMemberBorrowingHistory(mid);
            
            Console.WriteLine($"\n--- Borrowing Summary for Member ID: {mid} ---");
            Console.WriteLine($"Active Borrowings: {summary.active_loans}");
            Console.WriteLine($"Total Books Borrowed (Lifetime): {summary.total_borrowed}");
            Console.WriteLine($"Total Unpaid Fines: ₹{summary.unpaid_fines:F2}");
        }
    }

    private static void ShowPendingFinesReport()
    {
        Console.WriteLine("\n==== MEMBERS WITH PENDING FEES ====");
        var members = _reportService.GetMembersWithPendingFines();
        
        if (!members.Any())
        {
            Console.WriteLine("No members with pending fees found.");
            return;
        }

        foreach(var m in members) 
        {
            decimal fine = _fineService.GetTotalUnpaidFine(m.Memberid);
            Console.WriteLine($"{m.Membername} (ID: {m.Memberid}) - Fine: ₹{fine}");
        }
    }

    private static void HandleReturnSubmissions()
    {
        Console.WriteLine("\n==== RETURN SUBMISSIONS ====");
        var pending = _adminService.GetPendingReturns();
        
        if (!pending.Any())
        {
            Console.WriteLine("No pending return submissions.");
            return;
        }

        foreach(var r in pending)
        {
            var b = _lendingService.GetBorrowingById(r.Borrowid ?? 0);
            Console.WriteLine($"ReturnID: {r.Returnid} | BorrowID: {r.Borrowid} | MemberID: {b?.Memberid} | BookID: {b?.Bookid} | Due: {b?.Duedate} | Returned: {r.Actualreturndate}");
        }

        Console.Write("\nEnter Return ID to process (or 0 to cancel): ");
        if (int.TryParse(Console.ReadLine(), out int rid) && rid != 0)
        {
            Console.WriteLine("1. Approve (Good Condition)");
            Console.WriteLine("2. Approve (Damaged Condition)");
            Console.WriteLine("3. Reject (Wrong Book / Not Returned)");
            Console.Write("Choice: ");
            var choice = Console.ReadLine();

            try {
                if (choice == "1")
                {
                    _adminService.ApproveReturn(rid, "Good");
                    Console.WriteLine("\nReturn Approved as Good Condition.");
                }
                else if (choice == "2")
                {
                    _adminService.ApproveReturn(rid, "Damaged");
                    Console.WriteLine("\nReturn Approved as Damaged Condition (Standard fee applied).");
                }
                else if (choice == "3")
                {
                    var remark = PromptInput("Enter Reason for Rejection: ");
                    _adminService.RejectReturn(rid, remark);
                    Console.WriteLine($"\nReturn Rejected. Remark added: {remark}");
                }
                else
                {
                    Console.WriteLine("\nInvalid option selected. The return remains Pending.");
                }
            } catch (Exception ex) { Console.WriteLine($"\nError: {ex.Message}"); }
        }
    }

    private static void ConfigureFines()
    {
        Console.WriteLine("\n==== CONFIGURE FINES ====");
        var configs = _adminService.GetFineConfiguration();
        foreach (var cfg in configs)
        {
            Console.WriteLine($"Current {cfg.Finetype}: ₹{cfg.Amount}");
        }

        Console.WriteLine("\n1. Late Fee Configure");
        Console.WriteLine("2. Damaged Copy Configure");
        Console.WriteLine("3. Back");
        Console.Write("Choice: ");
        var c = Console.ReadLine();
        if (c == "3") return;

        string type = c == "1" ? "LateReturn" : "Damaged";
        Console.Write($"Enter new amount for {type}: ");
        if (decimal.TryParse(Console.ReadLine(), out decimal amt))
        {
            _adminService.UpdateFineAmount(type, amt);
            Console.WriteLine("Updated Successfully!");
        }
    }

    private static void RunMemberFlow()
    {
        Console.WriteLine("\n==== MEMBER PORTAL ====");
        Console.WriteLine("1. Login");
        Console.WriteLine("2. Register");
        Console.WriteLine("3. Back");
        Console.Write("Choice: ");
        var choice = Console.ReadLine();
        if (choice == "1")
        {
            var input = PromptInput("Enter Username or Email: ");
            
            var member = _memberService.GetAllMembers().FirstOrDefault(m => m.Memberphone == input || m.Memberemail == input || m.Membername == input);
            if (member != null) 
            { 
                var password = PromptPassword("Enter Password: ");
                if (!_authService.AuthenticateUser($"mem_{member.Memberid}", password))
                {
                    Console.WriteLine("\nInvalid credentials!");
                    return;
                }

                if (member.Memberstatus == "Inactive")
                {
                    Console.WriteLine("\nThis account has been deactivated, contact the administrator for more details.");
                    return;
                }
                _currentMember = member; 
                RunMemberMenu(); 
            }
            else 
            { 
                Console.WriteLine("\nMember not found."); 
            }
        }
        else if (choice == "2")
        {
            var n = PromptInput("Enter Name: ");
            var p = PromptInput("Enter Phone: ");
            var e = PromptInput("Enter Email: ");
            Console.WriteLine("Available Types: Basic, Student, Premium");
            var t = PromptInput("Enter Membership Type: ");
            var pw = PromptInput("Enter Password: ");
            try { 
                _memberService.AddMember(n, p, e, t, pw); 
                Console.WriteLine("\nRegistered Successfully!"); 
            }
            catch (Exception ex) { Console.WriteLine($"\nError: {ex.Message}"); }
        }
    }

    private static void RunMemberMenu()
    {
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\nWelcome {_currentMember?.Membername}!");
            Console.ResetColor();
            Console.WriteLine("1. Search & Borrow Books");
            Console.WriteLine("2. View Borrowed Books (Return)");
            Console.WriteLine("3. View Payment Dues");
            Console.WriteLine("4. Logout");
            Console.WriteLine("5. Exit");
            Console.Write("\nChoice: ");

            var choice = Console.ReadLine();
            try 
            {
                switch (choice)
                {
                    case "1": MemberSearchAndBorrow(); break;
                    case "2": MemberViewBorrowed(); break;
                    case "3": MemberViewFines(); break;
                    case "4": return;
                    case "5": Environment.Exit(0); break;
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\nOperation canceled. Returning to member menu...");
            }
        }
    }

    private static void MemberSearchAndBorrow()
    {
        var cats = _bookService.GetCategories();
        Console.WriteLine("\nAvailable Categories: " + string.Join(", ", cats.Select(c => c.Categoryname)));
        
        var term = PromptInput("Search (Title/Author/Category): ");
        var books = _bookService.SearchBooks(term).ToList();
        
        if (!books.Any())
        {
            Console.WriteLine("\nNo Books available.");
            return;
        }

        Console.WriteLine("\n--- Search Results ---");
        foreach(var b in books) 
            Console.WriteLine($"ID: {b.Bookid} | {b.Booktitle} by {b.Bookauthor}");

        var midInput = PromptInput("\nEnter Book ID to View Details: ");
        if (int.TryParse(midInput, out int bid) && bid != 0)
        {
            var book = _bookService.GetBookById(bid);
            if (book != null)
            {
                Console.WriteLine($"\n--- {book.Booktitle} ---");
                Console.WriteLine($"Author: {book.Bookauthor}");
                Console.WriteLine($"Category: {book.CategorynumberNavigation?.Categoryname ?? "N/A"}");
                Console.WriteLine("------------------------------");
                Console.WriteLine($"Description: {book.Bookcontents ?? "No description available."}");
                Console.WriteLine("------------------------------");
                
                Console.WriteLine("1. Borrow this Book");
                Console.WriteLine("2. Go Back");
                Console.Write("\nChoice: ");
                
                if (Console.ReadLine() == "1")
                {
                    Console.Write("Enter borrow date (dd-mm-yyyy): ");
                    if (!DateTime.TryParseExact(Console.ReadLine(), "dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime borrowDate))
                    {
                        Console.WriteLine("Invalid format provided. Automatically defaulting to today's date.");
                        borrowDate = DateTime.Now;
                    }

                    try {
                        _lendingService.BorrowBook(_currentMember?.Memberid ?? 0, bid, borrowDate);
                        Console.WriteLine($"\nSuccess! You have borrowed '{book.Booktitle}'.");
                    }
                    catch (Exception ex) when (ex.Message == "DAMAGED_COPY_ONLY") {
                        Console.Write("\nOnly a damaged copy is available. Borrow it? (y/n): ");
                        if ((Console.ReadLine() ?? "").ToLower() == "y") {
                            _lendingService.BorrowBook(_currentMember?.Memberid ?? 0, bid, borrowDate, true);
                            Console.WriteLine("Borrowed successfully!");
                        }
                    }
                    catch (Exception ex) { Console.WriteLine($"\nError: {ex.Message}"); }
                }
            }
            else Console.WriteLine("Book not found.");
        }
    }

    private static void MemberViewBorrowed()
    {
        var borrowed = _lendingService.GetActiveBorrowings(_currentMember?.Memberid ?? 0);
        
        if (!borrowed.Any())
        {
            Console.WriteLine("\nYou have no active borrowed books.");
            return;
        }

        foreach(var b in borrowed) 
        {
            string remarkText = string.IsNullOrEmpty(b.Remarks) ? "No Remarks" : b.Remarks;
            Console.WriteLine($"BorrowID: {b.Borrowid} | BookID: {b.Bookid} | Due: {b.Duedate} | Remarks: {remarkText}");
        }
        
        var bridInput = PromptInput("\nEnter Borrow ID to Return: ");
        if (int.TryParse(bridInput, out int brid))
        {
            var bRecord = _lendingService.GetBorrowingById(brid);
            if (bRecord == null) return;
            
            Console.Write("Enter return date (dd-mm-yyyy): ");
            if (!DateTime.TryParseExact(Console.ReadLine(), "dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime returnDate))
            {
                Console.WriteLine("Invalid format provided. Automatically defaulting to today's date.");
                returnDate = DateTime.Now;
            }
            
            try {
                _lendingService.ReturnBook(brid, returnDate);
                Console.WriteLine("\nBook submitted for return! Status: Pending Admin Approval.\n Note: Fine will be updated upon late return or damaged condition!");
            } catch (DbUpdateException dbEx) {
                Console.WriteLine("\nDatabase Error: Could not save the return record.");
                if (dbEx.InnerException != null) Console.WriteLine($"Detail: {dbEx.InnerException.Message}");
            } catch (Exception ex) { 
                Console.WriteLine($"\nError: {ex.Message}"); 
            }
        }
    }

    private static void MemberViewFines()
    {
        var fines = _fineService.ViewPendingFines(_currentMember?.Memberid ?? 0);
        
        if (!fines.Any())
        {
            Console.WriteLine("\nNo payment dues found. You are all clear!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        foreach(var f in fines) 
            Console.WriteLine($"FineID: {f.Fineid} | Amount: ₹{f.Fineamount} | Status: {f.Finestatus} | Reason: {f.Remarks ?? "N/A"}");
        
        Console.Write("\nEnter Fine ID to clear: ");
        if (int.TryParse(Console.ReadLine(), out int fid))
        {
            var fRecord = fines.FirstOrDefault(f => f.Fineid == fid);
            if (fRecord != null) {
                _fineService.PayFine(fid, fRecord.Fineamount);
                Console.WriteLine("Thanks for paying! Your due has been cleared.");
            }
        }
    }

    private static string PromptInput(string prompt)
    {
        while (true)
        {
            Console.Write($"{prompt} (or 0 to cancel): ");
            var input = Console.ReadLine();
            
            if (input == "0") throw new OperationCanceledException("Operation canceled by user.");
            
            if (!string.IsNullOrWhiteSpace(input))
            {
                return input;
            }
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("WARNING : Input cannot be empty. Please try again.");
            Console.ResetColor();
        }
    }

    private static string PromptPassword(string prompt)
    {
        while (true)
        {
            Console.Write($"{prompt} (or 0 to cancel): ");
            var pwd = string.Empty;
            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (pwd.Length > 0)
                    {
                        pwd = pwd.Substring(0, pwd.Length - 1);
                        Console.Write("\b \b");
                    }
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    pwd += key.KeyChar;
                    Console.Write("*");
                }
            }

            if (pwd == "0") throw new OperationCanceledException("Operation canceled by user.");
            
            if (!string.IsNullOrWhiteSpace(pwd))
            {
                return pwd;
            }
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("WARNING : Password cannot be empty. Please try again.");
            Console.ResetColor();
        }
    }
}
