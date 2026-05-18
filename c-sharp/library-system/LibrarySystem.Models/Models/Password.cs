using System;
using System.Collections.Generic;

namespace LibrarySystem.Models;

public partial class Password
{
    public string Userid { get; set; } = null!;

    public string Passwordhash { get; set; } = null!;
}
