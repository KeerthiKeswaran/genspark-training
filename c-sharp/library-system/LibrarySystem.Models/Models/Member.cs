using System;
using System.Collections.Generic;

namespace LibrarySystem.Data;

public partial class Member
{
    public int Memberid { get; set; }

    public string Membername { get; set; } = null!;

    public string Memberphone { get; set; } = null!;

    public string Memberemail { get; set; } = null!;

    public string? Membertype { get; set; }

    public string? Memberstatus { get; set; }

    public virtual ICollection<Borrowing> Borrowings { get; set; } = new List<Borrowing>();

    public virtual Membershiplimit? MembertypeNavigation { get; set; }
}
