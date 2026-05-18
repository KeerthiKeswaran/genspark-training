using LibrarySystem.Contracts.Interfaces;
using LibrarySystem.Data;
using LibrarySystem.Models.Models;

namespace LibrarySystem.Business.Services;

public class ReportService : IReportService
{
    private readonly ILibraryRepository _repository;

    public ReportService(ILibraryRepository repository)
    {
        _repository = repository;
    }

    public IEnumerable<Borrowing> GetCurrentlyBorrowedBooks()
    {
        return _repository.GetAllBorrowings().Where(b => b.Returnstatus == "Borrowed");
    }

    public IEnumerable<Borrowing> GetOverdueBooks()
    {
        return _repository.GetAllBorrowings()
            .Where(b => b.Returnstatus == "Borrowed" && b.Duedate < DateOnly.FromDateTime(DateTime.Now));
    }

    public IEnumerable<Member> GetMembersWithPendingFines()
    {
        return _repository.GetAllMembers()
            .Where(m => _repository.GetTotalUnpaidFine(m.Memberid) > 0);
    }

    public MemberBorrowingSummaryResult GetMemberBorrowingHistory(int memberId)
    {
        return _repository.GetMemberBorrowingSummary(memberId);
    }

    public IEnumerable<MostBorrowedBookResult> GetMostBorrowedBooks()
    {
        return _repository.GetMostBorrowedBooks();
    }
}
