using System;
using System.Collections.Generic;

namespace LibrarySystem.Data;

public partial class Membershiplimit
{
    public string Membertype { get; set; } = null!;

    public int Maxbooksallowed { get; set; }

    public int Borrowdurationdays { get; set; }

    public virtual ICollection<Member> Members { get; set; } = new List<Member>();
}
