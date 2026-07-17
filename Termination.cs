using System;
using System.Collections.Generic;

namespace MaidAndServantt.Models;

public partial class Termination
{
    public int TerminationId { get; set; }

    public int? InterviewId { get; set; }

    public DateOnly? TerminatedDate { get; set; }

    public string? TerminatedReason { get; set; }
}
