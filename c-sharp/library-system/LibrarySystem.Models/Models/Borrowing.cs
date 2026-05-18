using System;
using System.Collections.Generic;

namespace LibrarySystem.Data;

public partial class Borrowing
{
    public int Borrowid { get; set; }

    public int? Memberid { get; set; }

    public int? Bookid { get; set; }

    public DateOnly? Borrowdate { get; set; }

    public DateOnly Duedate { get; set; }

    public string? Returnstatus { get; set; }

    public string? Remarks { get; set; }

    public virtual Book? Book { get; set; }

    public virtual ICollection<Finecalculation> Finecalculations { get; set; } = new List<Finecalculation>();

    public virtual Member? Member { get; set; }

    public virtual ICollection<Return> Returns { get; set; } = new List<Return>();
}
