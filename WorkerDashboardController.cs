using Microsoft.AspNetCore.Mvc;
using MaidAndServantt.Models;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;            // Task use karne k liye
using Microsoft.EntityFrameworkCore;    // FirstOrDefaultAsync k liye
using System;

namespace MaidAndServantt.Controllers
{
    public class WorkerDashboardController : Controller
    {
        private readonly FypContext _context;

        // Constructor: Database context inject ho rha hai
        public WorkerDashboardController(FypContext context)
        {
            _context = context;
        }

        // ================= NEWLY ADDED LOGOUT METHOD =================
        [HttpGet]
        public IActionResult Login()
        {
            // Session clear karna
            HttpContext.Session.Clear();

            // Cookies aur sign-out handling (agar use ho rahi ho to safe rahegi)
            Response.Cookies.Delete(".AspNetCore.Session");

            // User ko Auth controller ke Login page par redirect karna
            return RedirectToAction("Login", "Auth");
        }
        // =============================================================

        [HttpGet]
        public IActionResult ActiveRequests()
        {
            // 1. Session se logged-in worker ki ID lena
            int? workerId = HttpContext.Session.GetInt32("WorkerId");
            if (workerId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // 2. Query: Sirf wahi requests layen jo main status 'Pending' hon 
            //    aur worker ne abhi tak un par koi decision (Accept/Reject) na liya ho (WorkerDecision == null)
            var pendingRequests = (from i in _context.Interviews
                                   join c in _context.Clients on i.ClientId equals c.ClientId
                                   where i.WorkerId == workerId
                                      && i.Status == "Pending"
                                      && (i.WorkerDecision == null || i.WorkerDecision == "")
                                   select new
                                   {
                                       InterviewId = i.InterviewId,
                                       ClientName = c.Name,
                                       Address = i.Address // Fallback hata kar exact database column fetch ho rha hai
                                   }).ToList();

            // 3. Data ko dynamic list ki shakal mein ViewBag ke zariye View ko pass karna
            ViewBag.Requests = pendingRequests;

            return View();
        }

        // POST: /WorkerDashboard/UpdateStatus
        [HttpPost]
        public IActionResult UpdateStatus(int interviewId, string status)
        {
            var interview = _context.Interviews.FirstOrDefault(i => i.InterviewId == interviewId);
            if (interview != null)
            {
                // FIX: Status ko hamesha "Pending" hi rakhein ge, isko change nahi karna
                interview.Status = "Pending";

                // FIX: Sirf worker ka decision (Approved/Rejected) save ho rha hai
                interview.WorkerDecision = status;

                _context.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpGet]
        public IActionResult WorkerDashboard()
        {
            int? workerId = HttpContext.Session.GetInt32("WorkerId");
            if (workerId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var worker = _context.Workers.FirstOrDefault(w => w.WorkerId == workerId);
            if (worker == null)
            {
                return NotFound();
            }

            // 1. Worker matching skills
            var workerSkills = (from wc in _context.WorkerCategories
                                join s in _context.Skills on wc.SkillsId equals s.SkillsId
                                where wc.WorkerId == workerId
                                select s.SkillName)
                                .Distinct()
                                .ToList();

            ViewBag.Skills = workerSkills;

            // Interview Counter
            ViewBag.PendingInterviews = _context.Interviews
                .Count(i => i.WorkerId == workerId &&
                           (i.Status == "Pending" || i.Status == "Approved"));

            // Job Notifications Counter
            ViewBag.JobNotifications = _context.Hirings
                .Join(_context.Interviews,
                      h => h.InterviewId,
                      i => i.InterviewId,
                      (h, i) => new { HiringRecord = h, InterviewRecord = i })
                      .Count(joined => joined.InterviewRecord.WorkerId == workerId &&
                                       (joined.HiringRecord.WorkerDecision == "Pending" ||
                                        joined.HiringRecord.WorkerDecision == "Approved" ||
                                        joined.HiringRecord.WorkerDecision == null));

            // ================= DEBUGGABLE RATING CALCULATION =================
            double calculatedRating = 0.0;
            string debugMessage = "";
            int totalReviewsFound = 0;

            try
            {
                // Step A: Pehle check karein ke is WorkerId ke liye koi Interview record database mein hai bhi ya nahi
                var workerInterviews = _context.Interviews.Where(i => i.WorkerId == workerId).Select(i => i.InterviewId).ToList();

                if (!workerInterviews.Any())
                {
                    debugMessage = $"No Interviews found in database for Worker ID: {workerId}";
                }
                else
                {
                    // Step B: Ab un Interviews ke aganst Reviews search karein
                    var reviews = _context.Reviews
                        .Where(r => r.InterviewId != null && workerInterviews.Contains(r.InterviewId.Value))
                        .ToList();

                    totalReviewsFound = reviews.Count;

                    if (!reviews.Any())
                    {
                        debugMessage = $"Interviews found, but no matching Reviews exist in DB for Worker ID: {workerId}";
                    }
                    else
                    {
                        var ratings = reviews
                            .Where(r => r.Rating != null)
                            .Select(r => (double)r.Rating.Value)
                            .ToList();

                        if (ratings.Any())
                        {
                            calculatedRating = Math.Round(ratings.Average(), 1);
                            debugMessage = $"Success! Found {ratings.Count} ratings.";
                        }
                        else
                        {
                            debugMessage = $"Reviews exist, but all Ratings are NULL in database.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                debugMessage = $"Error: {ex.Message}";
                calculatedRating = 0.0;
            }

            // View ko variables pass karna
            ViewBag.Rating = calculatedRating;
            ViewBag.DebugRatingMessage = debugMessage;
            ViewBag.TotalReviewsFound = totalReviewsFound;
            // =====================================================================

            ViewBag.ExperienceYears = _context.Experiences.Where(e => e.WorkerId == workerId).Count();
            ViewBag.ExperienceList = _context.Experiences.Where(e => e.WorkerId == workerId).ToList();

            return View(worker);
        }

        // POST: /WorkerDashboard/UpdateDutyStatus
        [HttpPost]
        public IActionResult UpdateDutyStatus(bool status)
        {
            int? workerId = HttpContext.Session.GetInt32("WorkerId");
            if (workerId == null)
            {
                return Json(new { success = false, message = "Session expired." });
            }

            var worker = _context.Workers.FirstOrDefault(w => w.WorkerId == workerId);
            if (worker != null)
            {
                worker.AvailableStatus = status;
                _context.SaveChanges();
                return Json(new { success = true, message = "Web state status synchronized." });
            }

            return Json(new { success = false, message = "Worker profile not found." });
        }

        // URL: /WorkerDashboard/WorkerProfile?workerId=21
        public async Task<IActionResult> WorkerProfile(int workerId)
        {
            var worker = await _context.Workers
                .FirstOrDefaultAsync(w => w.WorkerId == workerId);

            if (worker == null)
            {
                return NotFound();
            }

            // Loading required properties for WorkerProfile UI View Render
            var matchingBridges = await _context.WorkerCategories
                .Where(wc => wc.WorkerId == workerId)
                .ToListAsync();

            var skillsIds = matchingBridges.Select(wc => wc.SkillsId).ToList();
            var categoryIds = matchingBridges.Select(wc => wc.CategoryId).Distinct().ToList();

            ViewBag.SubSkills = await _context.Skills
                .Where(s => skillsIds.Contains(s.SkillsId))
                .Select(s => s.SkillName)
                .ToListAsync();

            ViewBag.MainCategories = await _context.Categories
                .Where(c => categoryIds.Contains(c.CategoryId))
                .Select(c => c.CategoryName)
                .ToListAsync();

            var reviewsQuery = from r in _context.Reviews
                               join i in _context.Interviews on r.InterviewId equals i.InterviewId
                               where i.WorkerId == workerId
                               select r;

            var workerReviews = await reviewsQuery.ToListAsync();
            ViewBag.WorkerReviews = workerReviews;
            ViewBag.TotalReviewsCount = workerReviews.Count;

            var validRatings = workerReviews.Where(r => r.Rating != null).Select(r => (double)r.Rating.Value).ToList();
            ViewBag.AverageRating = validRatings.Any() ? Math.Round(validRatings.Average(), 1) : 0.0;

            // 🔥 Database se actual Experience ki sequence list load ho rahi hai timeline display k liye
            var workerExperiences = await _context.Experiences
                .Where(e => e.WorkerId == workerId)
                .ToListAsync();

            ViewBag.WorkerExperiences = workerExperiences;
            ViewBag.ExperienceYears = workerExperiences.Count;

            // ✨ DYNAMIC EXPERIENCE TEXT GENERATOR FOR UI GRID (Multiple Records Solution)
            string displayExperience = "Experienced";
            if (workerExperiences.Any())
            {
                // Saare experience records mein se valid years (jaise 2023, 2024) parse kar rahe hain
                var validYears = workerExperiences
                    .Select(e => int.TryParse(e.Duration, out int y) && y > 1900 ? y : (int?)null)
                    .Where(y => y.HasValue)
                    .Select(y => y.Value)
                    .ToList();

                if (validYears.Any())
                {
                    // Sabse minimum (oldest) saal nikal rahe hain taake total span sahi calculate ho
                    int oldestStartYear = validYears.Min();
                    int currentYear = DateTime.Now.Year; // Yeh automatically 2026 uthaye ga
                    int totalYears = currentYear - oldestStartYear;

                    if (totalYears <= 0)
                    {
                        displayExperience = "Less than 1 Year";
                    }
                    else
                    {
                        displayExperience = totalYears == 1 ? "1 Year" : totalYears + " Years";
                    }
                }
                else
                {
                    // Fallback: Agar kisi record mein direct plain text save ho (e.g. "2 Years")
                    var primaryExp = workerExperiences.First().Duration;
                    if (!string.IsNullOrEmpty(primaryExp))
                    {
                        displayExperience = primaryExp.Contains("Year", StringComparison.OrdinalIgnoreCase)
                            ? primaryExp
                            : primaryExp + " Years";
                    }
                }
            }

            // Yeh view bag variable ab grid box mein bilkul perfect "3 Years" show karega
            ViewBag.DisplayExperienceText = displayExperience;

            return View(worker);
        }
    }
}