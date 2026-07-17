using System;
using System.Collections.Generic;

namespace MaidAndServantt.Models;

public partial class Worker
{
    public int WorkerId { get; set; }

    public string Name { get; set; } = null!;

    public string Cnic { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public decimal? Salary { get; set; }

    public string? Address { get; set; }

    public string? Picture { get; set; }

    public bool? AvailableStatus { get; set; }

    public string? Gender { get; set; }

    public string Password { get; set; } = null!;

    public int? Age { get; set; }

    public string? Bio { get; set; }

    public string? Number { get; set; }
}
