using LibrarySystem.Data;

namespace LibrarySystem.Contracts.Interfaces;

public interface ILendingService
{
    void BorrowBook(int memberId, int bookId, DateTime borrowDate, bool acceptDamaged = false);
    void ReturnBook(int borrowId, DateTime returnDate);
    IEnumerable<Borrowing> GetActiveBorrowings(int memberId);
    Borrowing? GetBorrowingById(int borrowId);
}


