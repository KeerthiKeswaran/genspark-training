using LibrarySystem.Data;
using LibrarySystem.Models.Models;

namespace LibrarySystem.Contracts.Interfaces;

public interface IReportService
{
    IEnumerable<Borrowing> GetCurrentlyBorrowedBooks();
    IEnumerable<Borrowing> GetOverdueBooks();
    IEnumerable<Member> GetMembersWithPendingFines();
    MemberBorrowingSummaryResult GetMemberBorrowingHistory(int memberId);
    IEnumerable<MostBorrowedBookResult> GetMostBorrowedBooks();
}
