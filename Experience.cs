using System;
using System.Collections.Generic;

namespace MaidAndServantt.Models;

public partial class Experience
{
    public int ExperienceId { get; set; }

    public int? WorkerId { get; set; }

    public string? WorkAt { get; set; }

    public string? ExpDetail { get; set; }

    public string? Duration { get; set; }
}
