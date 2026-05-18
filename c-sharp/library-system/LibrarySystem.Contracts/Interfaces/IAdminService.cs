using LibrarySystem.Data;

namespace LibrarySystem.Contracts.Interfaces;

public interface IAdminService
{
    void UpdateFineAmount(string fineType, decimal newAmount);
    IEnumerable<Fineconfiguration> GetFineConfiguration();
    void RegisterAdmin(string name, string phone, string email, string password);
    Admin? Login(string input);
    IEnumerable<Return> GetPendingReturns();
    void ApproveReturn(int returnId, string finalCondition);
    void RejectReturn(int returnId, string remark);
}
