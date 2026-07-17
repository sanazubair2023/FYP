using System;
using System.Collections.Generic;

namespace MaidAndServantt.Models;

public partial class WorkerCategory
{
    public int? WorkerId { get; set; }

    public int? CategoryId { get; set; }

    public int? SkillsId { get; set; }
}
