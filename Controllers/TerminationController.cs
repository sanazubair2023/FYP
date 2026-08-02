using MaidAndServantt.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace MaidAndServantt.Controllers
{
    public class TerminationController : Controller
    {
        private readonly FypContext _context;

        public TerminationController(FypContext context)
        {
            _context = context;
        }

        // GET: /Termination/Terminate?interviewId=23
        [HttpGet]
        public IActionResult Terminate(int interviewId)
        {
            var interview = _context.Interviews.FirstOrDefault(i => i.InterviewId == interviewId);
            if (interview == null)
            {
                interview = _context.Interviews.FirstOrDefault(i => i.WorkerId == interviewId);
            }

            if (interview == null)
            {
                return NotFound($"Active contract record not found for ID: {interviewId}");
            }

            var worker = _context.Workers.FirstOrDefault(w => w.WorkerId == interview.WorkerId);
            if (worker == null)
            {
                return NotFound("Worker profile missing in Database.");
            }

            string finalSkill = "Professional Maid";
            var skillQuery = from wc in _context.WorkerCategories
                             join c in _context.Categories on wc.CategoryId equals c.CategoryId
                             where wc.WorkerId == worker.WorkerId
                             select c.CategoryName;

            if (skillQuery.Any())
            {
                finalSkill = skillQuery.FirstOrDefault() ?? "Professional Maid";
            }

            var viewModel = new TerminationViewModel
            {
                InterviewId = interview.InterviewId,
                WorkerName = worker.Name ?? "Unknown Worker",
                WorkerImage = string.IsNullOrEmpty(worker.Picture) ? "https://cdn-icons-png.flaticon.com/512/3135/3135715.png" : worker.Picture,
                WorkerSkill = finalSkill
            };

            return View(viewModel);
        }

        // POST: /Termination/Terminate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Terminate(TerminationViewModel model)
        {
            // Bypass checking for system-populated dynamic properties
            ModelState.Remove("WorkerName");
            ModelState.Remove("WorkerSkill");
            ModelState.Remove("WorkerImage");

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Parsing date for DateOnly property
                    DateOnly parsedDate = DateOnly.FromDateTime(DateTime.Now);
                    if (!string.IsNullOrEmpty(model.LastWorkingDay))
                    {
                        DateOnly.TryParse(model.LastWorkingDay, out parsedDate);
                    }

                    // 2. Map and Save into 'Termination' Table
                    var termination = new Termination
                    {
                        InterviewId = model.InterviewId,
                        TerminatedReason = model.TerminatedReason,
                        TerminatedDate = parsedDate
                    };
                    _context.Terminations.Add(termination);

                    // 3. Map and Save into 'Review' Table
                    var review = new Review
                    {
                        InterviewId = model.InterviewId,
                        Rating = model.Rating,
                        Comment = model.Comment ?? "Contract Terminated",
                        ReviewDate = DateTime.Now
                    };
                    _context.Reviews.Add(review);

                    // 4. Update core interview status
                    var interview = _context.Interviews.FirstOrDefault(i => i.InterviewId == model.InterviewId);
                    if (interview != null)
                    {
                        interview.Status = "Terminated";
                    }

                    _context.SaveChanges();
                    return RedirectToAction("JobRequests", "WorkerDecision");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Failed to save in DB: {ex.Message}");
                }
            }

            return View(model);
        }
    } // <-- TerminationController class yahan band ho gayi
} // <-- Controllers Namespace yahan band ho gaya


// AB YAHAN SE MODELS NAMESPACE ALAG SE SHURU HOGA TAQI VIEW ISKO DEKH SAKE
namespace MaidAndServantt.Models
{
    public class TerminationViewModel
    {
        public int InterviewId { get; set; }
        public string WorkerName { get; set; } = null!;
        public string WorkerSkill { get; set; } = null!;
        public string? WorkerImage { get; set; }

        // Save fields
        public string TerminatedReason { get; set; } = null!;
        public string LastWorkingDay { get; set; } = null!; // HTML Date Input bind string

        // Star Rating fields
        public int Rating { get; set; } = 5; // Default 5 star
        public string? Comment { get; set; }
    }
}