using LibrarySystem.Contracts.Interfaces;
using LibrarySystem.Data;
using LibrarySystem.Models;

namespace LibrarySystem.Business.Services;

public class MemberService : IMemberService
{
    private readonly ILibraryRepository _repository;
    private readonly IAuthService _authService;

    public MemberService(ILibraryRepository repository, IAuthService authService)
    {
        _repository = repository;
        _authService = authService;
    }

    public void AddMember(string name, string phone, string email, string type, string password)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new Exception("Name is required.");
        
        // Basic duplicate check
        if (_repository.GetAllMembers().Any(m => m.Memberphone == phone || m.Memberemail == email))
        {
            throw new Exception("A member with this phone or email already exists.");
        }

        var member = new Member
        {
            Membername = name,
            Memberphone = phone,
            Memberemail = email,
            Membertype = type,
            Memberstatus = "Active"
        };

        _repository.AddMember(member);
        _repository.SaveChanges();

        // 2. Add Password Record
        var passwordRecord = new Password
        {
            Userid = $"mem_{member.Memberid}",
            Passwordhash = _authService.HashPassword(password)
        };
        _repository.AddPasswordRecord(passwordRecord);
        _repository.SaveChanges();
    }

    public IEnumerable<Member> GetAllMembers()
    {
        return _repository.GetAllMembers();
    }

    public IEnumerable<Member> SearchMember(string searchString)
    {
        if (string.IsNullOrWhiteSpace(searchString)) return GetAllMembers();

        return _repository.GetAllMembers()
            .Where(m => m.Membername.Contains(searchString, StringComparison.OrdinalIgnoreCase) || 
                        m.Memberphone.Contains(searchString) || 
                        m.Memberemail.Contains(searchString, StringComparison.OrdinalIgnoreCase));
    }

    public void UpdateMemberType(int memberId, string newType)
    {
        var member = _repository.GetMemberById(memberId);
        if (member == null) throw new Exception("Member not found.");

        member.Membertype = newType;
        _repository.UpdateMember(member);
        _repository.SaveChanges();
    }

    public string UpdateMemberStatus(int memberId)
    {
        var member = _repository.GetMemberById(memberId);
        if (member == null) throw new Exception("Member not found.");

        if (member.Memberstatus == "Active")
        {
            // Call the database function to deactivate
            _repository.DeactivateMember(memberId);
            return $"Member {member.Membername}, status is deactivated";
        }
        else
        {
            // Update the status back to active
            member.Memberstatus = "Active";
            _repository.UpdateMember(member);
            _repository.SaveChanges();
            return $"Member {member.Membername}, status is activated";
        }
    }
}
