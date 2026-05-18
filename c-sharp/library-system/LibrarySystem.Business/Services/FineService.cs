using LibrarySystem.Contracts.Interfaces;
using LibrarySystem.Data;

namespace LibrarySystem.Business.Services;

public class FineService : IFineService
{
    private readonly ILibraryRepository _repository;

    public FineService(ILibraryRepository repository)
    {
        _repository = repository;
    }

    public IEnumerable<Finecalculation> ViewPendingFines(int memberId)
    {
        return _repository.GetUnpaidFinesByMemberId(memberId);
    }

    public void PayFine(int fineId, decimal amount)
    {
        var allUnpaid = _repository.GetAllBorrowings().SelectMany(b => _repository.GetUnpaidFinesByMemberId(b.Memberid ?? 0));
        var targetFine = allUnpaid.FirstOrDefault(f => f.Fineid == fineId);


        if (targetFine == null) throw new Exception("Fine record not found or already paid.");

        if (amount < targetFine.Fineamount)
        {
            targetFine.Fineamount -= amount; // Partial payment
        }
        else
        {
            targetFine.Finestatus = "Paid";
        }

        _repository.UpdateFine(targetFine);
        _repository.SaveChanges();
    }

    public IEnumerable<Finecalculation> ViewFineHistory(int memberId)
    {
        return _repository.GetAllBorrowings()
            .Where(b => b.Memberid == memberId)
            .SelectMany(b => _repository.GetUnpaidFinesByMemberId(memberId)) // This is logic heavy, usually repo does it.
            .ToList();
    }

    public decimal GetTotalUnpaidFine(int memberId)
    {
        return _repository.GetTotalUnpaidFine(memberId);
    }
}
