using System;
using System.Collections.Generic;

namespace LibrarySystem.Data;

public partial class Finecalculation
{
    public int Fineid { get; set; }

    public int? Borrowid { get; set; }

    public decimal Fineamount { get; set; }

    public string? Finestatus { get; set; }

    public string? Remarks { get; set; }

    public virtual Borrowing? Borrow { get; set; }
}
