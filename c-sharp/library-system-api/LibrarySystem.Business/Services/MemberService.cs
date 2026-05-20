using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibrarySystem.Business.Exceptions;
using LibrarySystem.Contracts.Interfaces;
using LibrarySystem.Models.DTOs;
using LibrarySystem.Models.Models;

namespace LibrarySystem.Business.Services;

public class MemberService : IMemberService
{
    private readonly IMemberRepository _memberRepository;

    public MemberService(IMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public async Task<MemberResponse> AddMember(MemberRequest memberRequest)
    {
        if (string.IsNullOrWhiteSpace(memberRequest.FullName))
        {
            throw new ValidationException("Member full name should not be empty.");
        }

        if (string.IsNullOrWhiteSpace(memberRequest.Email))
        {
            throw new ValidationException("Email should not be empty.");
        }

        if (string.IsNullOrWhiteSpace(memberRequest.PhoneNumber))
        {
            throw new ValidationException("Phone number should not be empty.");
        }

        var member = new Member
        {
            FullName = memberRequest.FullName.Trim(),
            Email = memberRequest.Email.Trim(),
            PhoneNumber = memberRequest.PhoneNumber.Trim(),
            MembershipDate = memberRequest.MembershipDate == default ? DateTime.UtcNow : memberRequest.MembershipDate.ToUniversalTime()
        };

        await _memberRepository.AddMember(member);

        return new MemberResponse
        {
            MemberId = member.MemberId,
            FullName = member.FullName,
            Email = member.Email,
            PhoneNumber = member.PhoneNumber,
            MembershipDate = member.MembershipDate
        };
    }

    public async Task<IEnumerable<MemberResponse>> GetAllMembers()
    {
        var members = await _memberRepository.GetAllMembers();
        return members.Select(m => new MemberResponse
        {
            MemberId = m.MemberId,
            FullName = m.FullName,
            Email = m.Email,
            PhoneNumber = m.PhoneNumber,
            MembershipDate = m.MembershipDate
        });
    }

    public async Task<MemberResponse> GetMemberById(int id)
    {
        var member = await _memberRepository.GetMemberById(id);
        if (member == null)
        {
            throw new NotFoundException($"Member with ID {id} was not found.");
        }

        return new MemberResponse
        {
            MemberId = member.MemberId,
            FullName = member.FullName,
            Email = member.Email,
            PhoneNumber = member.PhoneNumber,
            MembershipDate = member.MembershipDate
        };
    }
}
