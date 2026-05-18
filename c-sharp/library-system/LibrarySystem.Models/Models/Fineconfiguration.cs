using System;
using System.Collections.Generic;

namespace LibrarySystem.Data;

public partial class Fineconfiguration
{
    public int Fineconfigid { get; set; }

    public string Finetype { get; set; } = null!;

    public decimal Amount { get; set; }
}
