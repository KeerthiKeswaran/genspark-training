using System;
using System.Collections.Generic;

namespace LibrarySystem.Data;

public partial class Return
{
    public int Returnid { get; set; }

    public int? Borrowid { get; set; }

    public DateOnly? Actualreturndate { get; set; }

    public decimal? Fineamount { get; set; }

    public string? Returnapprovalstatus { get; set; }

    public virtual Borrowing? Borrow { get; set; }
}
