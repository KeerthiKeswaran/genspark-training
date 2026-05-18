using LibrarySystem.Data;
using LibrarySystem.Models.Models;
using LibrarySystem.Models;

namespace LibrarySystem.Contracts.Interfaces;

public interface ILibraryRepository
{
    // Members
    void AddMember(Member member);
    Member? GetMemberById(int id);
    IEnumerable<Member> GetAllMembers();
    void UpdateMember(Member member);

    // Authentication
    void AddPasswordRecord(Password password);
    string? GetPasswordHashByUserId(string userId);

    // Admins
    void AddAdmin(Admin admin);
    IEnumerable<Admin> GetAllAdmins();

    // Books
    void AddBook(Book book);
    Book? GetBookById(int id);
    IEnumerable<Book> GetAllBooks();

    // BookCopies
    void AddBookCopy(Bookcopy copy);
    Bookcopy? GetBookCopyById(int id);
    IEnumerable<Bookcopy> GetCopiesByBookId(int bookId);
    void UpdateBookCopy(Bookcopy copy);

    // Borrowings
    void AddBorrowing(Borrowing borrowing);
    Borrowing? GetBorrowingById(int id);
    IEnumerable<Borrowing> GetActiveBorrowingsByMemberId(int memberId);
    IEnumerable<Borrowing> GetAllBorrowings();
    void UpdateBorrowing(Borrowing borrowing);

    // Returns
    void AddReturn(Return returnRecord);
    Return? GetReturnById(int id);
    IEnumerable<Return> GetPendingReturns();
    void UpdateReturn(Return returnRecord);

    // Fines

    void AddFine(Finecalculation fine);
    IEnumerable<Finecalculation> GetUnpaidFinesByMemberId(int memberId);
    void UpdateFine(Finecalculation fine);

    // Categories
    IEnumerable<Bookcategory> GetAllCategories();
    void AddCategory(Bookcategory category);

    // Configuration
    Fineconfiguration? GetFineConfigByType(string type);
    void UpdateFineConfig(Fineconfiguration config);
    IEnumerable<Fineconfiguration> GetFineConfigs();
    Membershiplimit? GetMembershipLimitByType(string type);

    // Persistence
    void SaveChanges();
    
    // Transactions
    void BeginTransaction();
    void CommitTransaction();
    void RollbackTransaction();

    // SQL Functions & Stored Procedures
    decimal GetTotalUnpaidFine(int memberId);
    MemberBorrowingSummaryResult GetMemberBorrowingSummary(int memberId);
    IEnumerable<MostBorrowedBookResult> GetMostBorrowedBooks();
    void DeactivateMember(int memberId);
}




