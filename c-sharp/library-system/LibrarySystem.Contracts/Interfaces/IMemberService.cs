using LibrarySystem.Data;

namespace LibrarySystem.Contracts.Interfaces;

public interface IMemberService
{
    void AddMember(string name, string phone, string email, string type, string password);
    IEnumerable<Member> GetAllMembers();
    IEnumerable<Member> SearchMember(string searchString);
    void UpdateMemberType(int memberId, string newType);
    string UpdateMemberStatus(int memberId);
}
