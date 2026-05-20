using System.Collections.Generic;
using System.Threading.Tasks;
using LibrarySystem.Models.Models;

namespace LibrarySystem.Contracts.Interfaces;

public interface IMemberRepository
{
    Task AddMember(Member member);
    Task<IEnumerable<Member>> GetAllMembers();
    Task<Member?> GetMemberById(int id);
}
