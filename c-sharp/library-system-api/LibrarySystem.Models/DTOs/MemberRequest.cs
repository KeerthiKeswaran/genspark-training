using System;

namespace LibrarySystem.Models.DTOs;

public class MemberRequest
{
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public required string PhoneNumber { get; set; }
    public DateTime MembershipDate { get; set; }
}
