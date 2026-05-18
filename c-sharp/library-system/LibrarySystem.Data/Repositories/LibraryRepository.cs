using LibrarySystem.Contracts.Interfaces;
using LibrarySystem.Data.Contexts;
using LibrarySystem.Data;
using LibrarySystem.Models;
using LibrarySystem.Models.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using LibrarySystem.Data.Exceptions;


namespace LibrarySystem.Data.Repositories;

public class LibraryRepository : ILibraryRepository
{
    private readonly LibraryDbContext _context;
    private IDbContextTransaction? _transaction;

    public LibraryRepository(LibraryDbContext context)
    {
        _context = context;
    }

    // Members
    public void AddMember(Member member) => _context.Members.Add(member);
    public Member? GetMemberById(int id) => _context.Members.Find(id);
    public IEnumerable<Member> GetAllMembers() => _context.Members.ToList();
    public void UpdateMember(Member member) => _context.Members.Update(member);

    // Authentication
    public void AddPasswordRecord(Password password) => _context.Passwords.Add(password);
    public string? GetPasswordHashByUserId(string userId) => 
        _context.Passwords.FirstOrDefault(p => p.Userid == userId)?.Passwordhash;

    public void AddAdmin(Admin admin) => _context.Admins.Add(admin);
    public IEnumerable<Admin> GetAllAdmins() => _context.Admins.ToList();

    // Books
    public void AddBook(Book book) => _context.Books.Add(book);
    public Book? GetBookById(int id) => _context.Books.Find(id);
    public IEnumerable<Book> GetAllBooks() => _context.Books.ToList();

    // BookCopies
    public void AddBookCopy(Bookcopy copy) => _context.Bookcopies.Add(copy);
    public Bookcopy? GetBookCopyById(int id) => _context.Bookcopies.Find(id);
    public IEnumerable<Bookcopy> GetCopiesByBookId(int bookId) => 
        _context.Bookcopies.Where(c => c.Bookid == bookId).ToList();
    public void UpdateBookCopy(Bookcopy copy) => _context.Bookcopies.Update(copy);

    // Borrowings
    public void AddBorrowing(Borrowing borrowing) => _context.Borrowings.Add(borrowing);
    public Borrowing? GetBorrowingById(int id) => _context.Borrowings.Find(id);
    public IEnumerable<Borrowing> GetActiveBorrowingsByMemberId(int memberId) => 
        _context.Borrowings.Where(b => b.Memberid == memberId && b.Returnstatus == "Borrowed").ToList();
    public IEnumerable<Borrowing> GetAllBorrowings() => _context.Borrowings.ToList();
    public void UpdateBorrowing(Borrowing borrowing) => _context.Borrowings.Update(borrowing);

    // Returns
    public void AddReturn(Return returnRecord) => _context.Returns.Add(returnRecord);
    public Return? GetReturnById(int id) => _context.Returns.Find(id);
    public IEnumerable<Return> GetPendingReturns() => _context.Returns.Where(r => r.Returnapprovalstatus == "Pending").ToList();
    public void UpdateReturn(Return returnRecord) => _context.Returns.Update(returnRecord);

    // Fines

    public void AddFine(Finecalculation fine) => _context.Finecalculations.Add(fine);
    public IEnumerable<Finecalculation> GetUnpaidFinesByMemberId(int memberId) => 
        _context.Finecalculations.Include(f => f.Borrow).Where(f => f.Borrow != null && f.Borrow.Memberid == memberId && f.Finestatus == "Unpaid").ToList();
    public void UpdateFine(Finecalculation fine) => _context.Finecalculations.Update(fine);

    // Categories
    public IEnumerable<Bookcategory> GetAllCategories() => _context.Bookcategories.ToList();
    public void AddCategory(Bookcategory category) => _context.Bookcategories.Add(category);

    // Configuration
    public Fineconfiguration? GetFineConfigByType(string type) => 
        _context.Fineconfigurations.FirstOrDefault(c => c.Finetype == type);
    public void UpdateFineConfig(Fineconfiguration config) => _context.Fineconfigurations.Update(config);
    public IEnumerable<Fineconfiguration> GetFineConfigs() => _context.Fineconfigurations.ToList();
    public Membershiplimit? GetMembershipLimitByType(string type) => 
        _context.Membershiplimits.Find(type);

    // Persistence
    public void SaveChanges() => _context.SaveChanges();

    // Transactions
    public void BeginTransaction() => _transaction = _context.Database.BeginTransaction();
    public void CommitTransaction()
    {
        _transaction?.Commit();
        _transaction?.Dispose();
        _transaction = null;
    }
    public void RollbackTransaction()
    {
        _transaction?.Rollback();
        _transaction?.Dispose();
        _transaction = null;
    }

    // SQL Functions & Stored Procedures
    public decimal GetTotalUnpaidFine(int memberId)
    {
        return _context.Database
            .SqlQueryRaw<decimal>("SELECT calculate_member_fine({0})", memberId)
            .AsEnumerable()
            .FirstOrDefault();
    }

    public MemberBorrowingSummaryResult GetMemberBorrowingSummary(int memberId)
    {
        return _context.Database
            .SqlQueryRaw<MemberBorrowingSummaryResult>("SELECT * FROM get_member_borrowing_summary({0})", memberId)
            .AsEnumerable()
            .FirstOrDefault() ?? new MemberBorrowingSummaryResult(0, 0, 0);
    }

    public IEnumerable<MostBorrowedBookResult> GetMostBorrowedBooks()
    {
        return _context.Database
            .SqlQueryRaw<MostBorrowedBookResult>("SELECT b_title AS Booktitle, borrow_count AS Borrowcount FROM get_most_borrowed_books()")
            .ToList();
    }

    public void DeactivateMember(int memberId)
    {
        _context.Database.ExecuteSqlRaw("CALL deactivate_member({0})", memberId);
        
        // Force reload the entity from the database
        var trackedMember = _context.Members.Local.FirstOrDefault(m => m.Memberid == memberId);
        if (trackedMember != null)
        {
            _context.Entry(trackedMember).Reload();
        }
    }
}




