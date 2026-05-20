using System.Collections.Generic;
using System.Threading.Tasks;
using LibrarySystem.Models.DTOs;

namespace LibrarySystem.Contracts.Interfaces;

public interface IMemberService
{
    Task<MemberResponse> AddMember(MemberRequest memberRequest);
    Task<IEnumerable<MemberResponse>> GetAllMembers();
    Task<MemberResponse> GetMemberById(int id);
}
