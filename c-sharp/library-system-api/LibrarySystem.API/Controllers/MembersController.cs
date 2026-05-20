using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using LibrarySystem.Contracts.Interfaces;
using LibrarySystem.Models.DTOs;
using LibrarySystem.Business.Exceptions;

namespace LibrarySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MembersController : ControllerBase
{
    private readonly IMemberService _memberService;

    public MembersController(IMemberService memberService)
    {
        _memberService = memberService;
    }

    [HttpPost]
    public async Task<IActionResult> AddMember([FromBody] MemberRequest memberRequest)
    {
        try
        {
            var result = await _memberService.AddMember(memberRequest);
            return Ok(new { message = "Member added successfully" });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllMembers()
    {
        var result = await _memberService.GetAllMembers();
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetMemberById(int id)
    {
        try
        {
            var result = await _memberService.GetMemberById(id);
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
