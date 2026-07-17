using System;
using System.Collections.Generic;

namespace MaidAndServantt.Models;

public partial class Interview
{
    public int InterviewId { get; set; }

    public int? ClientId { get; set; }

    public int? WorkerId { get; set; }

    public DateTime? InterviewDate { get; set; }

    public string? Address { get; set; }

    public string? Status { get; set; }

    public string? WorkerDecision { get; set; }

    public virtual ICollection<Hiring> Hirings { get; set; } = new List<Hiring>();
}
