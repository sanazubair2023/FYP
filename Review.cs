using System;
using System.Collections.Generic;

namespace MaidAndServantt.Models;

public partial class Review
{
    public int ReviewId { get; set; }

    public int? InterviewId { get; set; }

    public int? Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime? ReviewDate { get; set; }
}
