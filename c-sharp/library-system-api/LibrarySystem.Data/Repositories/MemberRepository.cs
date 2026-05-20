using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LibrarySystem.Contracts.Interfaces;
using LibrarySystem.Data.Contexts;
using LibrarySystem.Models.Models;

namespace LibrarySystem.Data.Repositories;

public class MemberRepository : IMemberRepository
{
    private readonly LibraryDbContext _context;

    public MemberRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task AddMember(Member member)
    {
        await _context.Members.AddAsync(member);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Member>> GetAllMembers()
    {
        return await _context.Members.ToListAsync();
    }

    public async Task<Member?> GetMemberById(int id)
    {
        return await _context.Members.FindAsync(id);
    }
}
