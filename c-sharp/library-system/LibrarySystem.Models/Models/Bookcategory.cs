using System;
using System.Collections.Generic;

namespace LibrarySystem.Data;

public partial class Bookcategory
{
    public int Categorynumber { get; set; }

    public string Categoryname { get; set; } = null!;

    public virtual ICollection<Book> Books { get; set; } = new List<Book>();
}
