using System;
using System.Collections.Generic;

namespace MaidAndServantt.Models;

public partial class Hiring
{
    public int HiringId { get; set; }

    public int? InterviewId { get; set; }

    public string? WorkerDecision { get; set; }

    public string? HiringDecision { get; set; }

    public virtual Interview? Interview { get; set; }

    public DateTime? HiringDate { get; set; } = default(DateTime?);
    public string? Address { get; set; }
}
