using System;
using System.Collections.Generic;

namespace LibrarySystem.Data;

public partial class Bookcopy
{
    public int Copyid { get; set; }

    public int? Bookid { get; set; }

    public string? Copystatus { get; set; }

    public string? Copycondition { get; set; }

    public virtual Book? Book { get; set; }
}
