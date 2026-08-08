using MaidAndServantt.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http; // 🟢 Session use karne ke liye zaroori hai
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MaidAndServantt.Controllers
{
    public class AcceptRequestController : Controller
    {
        private readonly FypContext _context;

        public AcceptRequestController(FypContext context)
        {
            _context = context;
        }

        // GET: AcceptRequest/Dashboard
        // GET: AcceptRequest/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // 1. Session se logged-in worker ki ID lein
                int? workerId = HttpContext.Session.GetInt32("WorkerId");
                if (workerId == null)
                {
                    return RedirectToAction("Login", "Auth");
                }

                // 2. FIXED QUERY: Database state ("Approved") ke mutabik query fetch mapping
                var acceptedInterviews = await (from i in _context.Interviews
                                                join c in _context.Clients on i.ClientId equals c.ClientId
                                                where (i.WorkerDecision == "Accepted" || i.WorkerDecision == "Approved") && i.WorkerId == workerId
                                                orderby i.InterviewDate descending
                                                select new AcceptedInterviewViewModel
                                                {
                                                    InterviewId = i.InterviewId,
                                                    InterviewDate = i.InterviewDate,
                                                    Address = i.Address ?? "N/A",
                                                    ClientName = c.Name,
                                                    ClientPhone = c.Phone ?? "N/A",
                                                    ClientPicture = c.Picture
                                                }).ToListAsync();

                return View("AcceptRequest", acceptedInterviews);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error loading database records: " + ex.Message;
                return View("AcceptRequest", new List<AcceptedInterviewViewModel>());
            }
        }
    }
}

namespace MaidAndServantt.Models
{
    public class AcceptedInterviewViewModel
    {
        public int InterviewId { get; set; }
        public DateTime? InterviewDate { get; set; }
        public string Address { get; set; }
        public string Service { get; set; } = "Interview Request";
        public string ClientName { get; set; }
        public string ClientPhone { get; set; }
        public string ClientPicture { get; set; }
    }
}