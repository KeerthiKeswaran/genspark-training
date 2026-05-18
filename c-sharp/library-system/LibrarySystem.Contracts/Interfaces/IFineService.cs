using LibrarySystem.Data;

namespace LibrarySystem.Contracts.Interfaces;

public interface IFineService
{
    IEnumerable<Finecalculation> ViewPendingFines(int memberId);
    void PayFine(int fineId, decimal amount);
    IEnumerable<Finecalculation> ViewFineHistory(int memberId);
    decimal GetTotalUnpaidFine(int memberId); 
}
