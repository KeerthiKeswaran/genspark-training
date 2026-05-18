using System;
using System.Collections.Generic;

namespace LibrarySystem.Data;

public partial class Book
{
    public int Bookid { get; set; }

    public string Booktitle { get; set; } = null!;

    public string Bookauthor { get; set; } = null!;

    public string? Bookcategory { get; set; }

    public string? Bookcontents { get; set; }

    public int? Categorynumber { get; set; }

    public virtual ICollection<Bookcopy> Bookcopies { get; set; } = new List<Bookcopy>();

    public virtual ICollection<Borrowing> Borrowings { get; set; } = new List<Borrowing>();

    public virtual Bookcategory? CategorynumberNavigation { get; set; }
}
