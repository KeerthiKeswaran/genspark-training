using System;
using System.Collections.Generic;

namespace LibrarySystem.Data;

public partial class Admin
{
    public int Adminid { get; set; }

    public string Adminname { get; set; } = null!;

    public string Adminphone { get; set; } = null!;

    public string Adminemail { get; set; } = null!;
}
