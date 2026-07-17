using MaidAndServantt.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MaidAndServantt.Controllers
{
    public class WorkerDecisionController : Controller
    {
        private readonly FypContext _context;

        public WorkerDecisionController(FypContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult JobRequests(string searchTerm)
        {
            int? clientId = HttpContext.Session.GetInt32("ClientId");
            if (clientId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // JOIN Query combining Interview, Worker and Hiring records
            var query = from i in _context.Interviews
                        join w in _context.Workers on i.WorkerId equals w.WorkerId
                        join h in _context.Hirings on i.InterviewId equals h.InterviewId into hiringJoin
                        from h in hiringJoin.DefaultIfEmpty() // Left Join to catch initial requests too
                        where i.ClientId == clientId
                        select new WorkerDecisionViewModel
                        {
                            InterviewId = i.InterviewId,
                            WorkerId = w.WorkerId,
                            WorkerName = w.Name,
                            WorkerImage = string.IsNullOrEmpty(w.Picture) ? "/Images/logo.png" : w.Picture,
                            Address = w.Address ?? "Not Available",
                            InterviewStatus = i.Status ?? "Pending",
                            WorkerDecision = h != null ? (h.WorkerDecision ?? "Pending") : (i.WorkerDecision ?? "Pending"),
                            HiringDecision = h != null ? (h.HiringDecision ?? "Pending") : "Pending", // Pull exact database value
                            InterviewDate = i.InterviewDate != null ? i.InterviewDate.Value.ToString("dd-MM-yyyy") : "12-01-2026",
                            WorkerSkill = (from wc in _context.WorkerCategories
                                           join c in _context.Categories on wc.CategoryId equals c.CategoryId
                                           where wc.WorkerId == w.WorkerId
                                           select c.CategoryName).FirstOrDefault() ?? "General Worker"
                        };

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(w => w.WorkerName.Contains(searchTerm) || w.WorkerSkill.Contains(searchTerm));
            }

            return View(query.ToList());
        }

        [HttpPost]
        public IActionResult AcceptWorkerDecision(int interviewId)
        {
            // Update both Interview and Hiring record states
            var interview = _context.Interviews.FirstOrDefault(i => i.InterviewId == interviewId);
            if (interview != null)
            {
                interview.Status = "Approved";

                var hiring = _context.Hirings.FirstOrDefault(h => h.InterviewId == interviewId);
                if (hiring != null)
                {
                    hiring.HiringDecision = "Approved";
                }

                _context.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Record not found." });
        }

        [HttpPost]
        public IActionResult RejectWorkerDecision(int interviewId)
        {
            var interview = _context.Interviews.FirstOrDefault(i => i.InterviewId == interviewId);
            if (interview != null)
            {
                interview.Status = "Rejected";

                var hiring = _context.Hirings.FirstOrDefault(h => h.InterviewId == interviewId);
                if (hiring != null)
                {
                    hiring.HiringDecision = "Rejected";
                    hiring.WorkerDecision = "Rejected";
                }

                _context.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Record not found." });
        }
    }

    public class WorkerDecisionViewModel
    {
        public int InterviewId { get; set; }
        public int WorkerId { get; set; }
        public string WorkerName { get; set; } = null!;
        public string WorkerSkill { get; set; } = null!;
        public string? WorkerImage { get; set; }
        public string InterviewStatus { get; set; } = null!;
        public string WorkerDecision { get; set; } = null!;
        public string HiringDecision { get; set; } = null!;
        public string? Address { get; set; }
        public string InterviewDate { get; set; } = null!;
    }
}