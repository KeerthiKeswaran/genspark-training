using LibrarySystem.Contracts.Interfaces;
using LibrarySystem.Data;
using LibrarySystem.Business.Exceptions;

namespace LibrarySystem.Business.Services;

public class LendingService : ILendingService
{
    private readonly ILibraryRepository _repository;

    public LendingService(ILibraryRepository repository)
    {
        _repository = repository;
    }

    public void BorrowBook(int memberId, int bookId, DateTime borrowDate, bool acceptDamaged = false)
    {
        var member = _repository.GetMemberById(memberId);
        if (member == null || member.Memberstatus != "Active")
            throw new LendingException("Member is not active or does not exist.");

        // 1. Check Fines
        decimal totalFines = _repository.GetTotalUnpaidFine(memberId);
        if (totalFines > 500)
            throw new LendingException($"Borrowing blocked. Unpaid fines ({totalFines:C}) exceed the limit of ₹500.");

        // 2. Check Overdue Books
        var activeBorrowings = _repository.GetActiveBorrowingsByMemberId(memberId);
        if (activeBorrowings.Any(b => b.Duedate < DateOnly.FromDateTime(borrowDate)))
            throw new LendingException("Borrowing blocked. Member has overdue books.");

        // 3. Check Borrowing Limit
        var limit = _repository.GetMembershipLimitByType(member.Membertype ?? "Basic");
        if (limit != null && activeBorrowings.Count() >= limit.Maxbooksallowed)
            throw new LendingException($"Borrowing limit reached for {member.Membertype} membership ({limit.Maxbooksallowed} books).");

        // 4. Find Available Copy
        var copies = _repository.GetCopiesByBookId(bookId).Where(c => c.Copystatus == "Available");
        
        var copyToBorrow = copies.FirstOrDefault(c => c.Copycondition == "Good");
        
        if (copyToBorrow == null)
        {
            copyToBorrow = copies.FirstOrDefault(c => c.Copycondition == "Damaged");
            if (copyToBorrow != null && !acceptDamaged)
            {
                throw new LendingException("DAMAGED_COPY_ONLY"); 
            }
        }

        if (copyToBorrow == null)
            throw new LendingException("No copies of this book are currently available.");

        // 5. Execute Transaction
        try
        {
            _repository.BeginTransaction();

            copyToBorrow.Copystatus = "Borrowed";
            _repository.UpdateBookCopy(copyToBorrow);

            var borrowing = new Borrowing
            {
                Memberid = memberId,
                Bookid = bookId,
                Borrowdate = DateOnly.FromDateTime(borrowDate),
                Duedate = DateOnly.FromDateTime(borrowDate.AddDays(limit?.Borrowdurationdays ?? 7)),
                Returnstatus = "Borrowed"
            };
            _repository.AddBorrowing(borrowing);

            _repository.SaveChanges();
            _repository.CommitTransaction();
        }
        catch
        {
            _repository.RollbackTransaction();
            throw;
        }
    }

    public void ReturnBook(int borrowId, DateTime returnDate)
    {
        var borrowing = _repository.GetBorrowingById(borrowId);
        if (borrowing == null) throw new Exception($"Invalid return data: BorrowID {borrowId} not found.");

        // Find the copy
        var copy = _repository.GetCopiesByBookId(borrowing.Bookid ?? 0)
                              .FirstOrDefault(c => string.Equals(c.Copystatus?.Trim(), "Borrowed", StringComparison.OrdinalIgnoreCase));

        if (copy == null) 
        {
            throw new Exception($"No active borrowed copy found for BookID {borrowing.Bookid}. Use Admin Portal to resolve manually.");
        }

        try
        {
            _repository.BeginTransaction();

            // 1. Check if a pending return already exists to prevent duplicates
            var existingPending = _repository.GetPendingReturns().FirstOrDefault(r => r.Borrowid == borrowId);
            if (existingPending != null)
            {
                throw new Exception("A return submission for this book is already pending admin approval.");
            }

            // 2. Mark Copy as Unavailable (Complies with DB check constraint)
            copy.Copystatus = "Unavailable";
            _repository.UpdateBookCopy(copy);

            // 3. DO NOT change borrowing status here (DB constraint only allows Borrowed/Returned)
            // It will remain 'Borrowed' until Admin approves.

            // 4. Create Pending Return Record
            var returnRecord = new Return
            {
                Borrowid = borrowId,
                Actualreturndate = DateOnly.FromDateTime(returnDate),
                Returnapprovalstatus = "Pending",
                Fineamount = 0 
            };
            _repository.AddReturn(returnRecord);

            _repository.SaveChanges();
            _repository.CommitTransaction();
        }
        catch
        {
            _repository.RollbackTransaction();
            throw;
        }
    }

    public IEnumerable<Borrowing> GetActiveBorrowings(int memberId)
    {
        return _repository.GetActiveBorrowingsByMemberId(memberId);
    }

    public Borrowing? GetBorrowingById(int borrowId)
    {
        return _repository.GetBorrowingById(borrowId);
    }
}
