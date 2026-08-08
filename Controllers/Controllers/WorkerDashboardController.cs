using Microsoft.AspNetCore.Mvc;
using MaidAndServantt.Models;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;            // Task use karne k liye
using Microsoft.EntityFrameworkCore;    // FirstOrDefaultAsync k liye
using System;
using System.IO;

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

        // ================= NEW WORKER EDIT PROFILE ACTION =================
        [HttpGet]
        public IActionResult EditWorkerProfile()
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

            // React Native implementation style variables ko replicate karne ke liye settings
            ViewBag.IsEditMode = true;
            ViewBag.UserRole = "Worker";

            // Experience aur work description records view layer k liye pass karna
            ViewBag.ExperienceList = _context.Experiences.Where(e => e.WorkerId == workerId).ToList();

            // Signup view ko as a base canvas profile dynamic model provide karna
            return View("~/Views/Auth/Signup.cshtml", worker);
        }

        // ================= POST: UPDATE WORKER PROFILE (React Native Architecture Match) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateWorker(
            int WorkerId,
            string WorkerName,
            int? WorkerAge,
            string WorkerPhone,
            string WorkerCNIC,
            decimal? WorkerSalary,
            string WorkerGender,
            string WorkerEmail,
            string WorkerAddress,
            string WorkerPassword,
            string ConfirmPassword,
            IFormFile WorkerPicture,
            string experiencesJson) // Pure string base JSON input mapping
        {
            // Database se matching structural primary key entry track karna
            var workerInDb = _context.Workers.FirstOrDefault(w => w.WorkerId == WorkerId);
            if (workerInDb == null)
            {
                ModelState.AddModelError("", "Worker profile not found in database.");
                return View("~/Views/Auth/Signup.cshtml", workerInDb);
            }

            // Passwords mismatch checking state 
            if (!string.IsNullOrEmpty(WorkerPassword) && WorkerPassword != ConfirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match!");
                ViewBag.IsEditMode = true;
                ViewBag.UserRole = "Worker";
                // Validation fail par experience list reload
                ViewBag.ExperienceList = _context.Experiences.Where(e => e.WorkerId == WorkerId).ToList();
                return View("~/Views/Auth/Signup.cshtml", workerInDb);
            }

            // Form bindings aur data state assignment values mapping
            workerInDb.Name = WorkerName;
            workerInDb.Age = WorkerAge;
            workerInDb.Phone = WorkerPhone;
            workerInDb.Cnic = WorkerCNIC;
            workerInDb.Salary = WorkerSalary;
            workerInDb.Gender = WorkerGender;
            //  workerInDb.WorkerEmail = WorkerEmail;
            workerInDb.Address = WorkerAddress;

            // React Native requirement: Agar field empty string nahi hai tabhi save/overwrite karein
            if (!string.IsNullOrEmpty(WorkerPassword))
            {
                workerInDb.Password = WorkerPassword;
            }

            // Image handling processing blocks
            if (WorkerPicture != null && WorkerPicture.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(WorkerPicture.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await WorkerPicture.CopyToAsync(fileStream);
                }
                workerInDb.Picture = "/Images/" + uniqueFileName;
            }

            try
            {
                _context.SaveChanges();
                return RedirectToAction("WorkerDashboard");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Failed to update profile data parameters: " + ex.Message);
                ViewBag.IsEditMode = true;
                ViewBag.UserRole = "Worker";
                ViewBag.ExperienceList = _context.Experiences.Where(e => e.WorkerId == WorkerId).ToList();
                return View("~/Views/Auth/Signup.cshtml", workerInDb);
            }
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

                // NEW LOGIC: Agar button se 'Approved' aaya hai, toh DB mein 'Accepted' save hoga
                if (status == "Approved" || status == "Accepted")
                {
                    interview.WorkerDecision = "Accepted";
                }
                else
                {
                    interview.WorkerDecision = status; // e.g. "Rejected"
                }

                _context.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
        [HttpGet]
        public IActionResult WorkerDashboard()
        {
            // Session se Worker ID extract karein (Dono Session keys check kar rahe hain)
            int? workerId = HttpContext.Session.GetInt32("WorkerId") ?? HttpContext.Session.GetInt32("UserId");

            if (workerId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var worker = _context.Workers.FirstOrDefault(w => w.WorkerId == workerId);
            if (worker == null)
            {
                return NotFound();
            }

            // 1. Worker Skills Fetch
            var workerSkills = (from wc in _context.WorkerCategories
                                join s in _context.Skills on wc.SkillsId equals s.SkillsId
                                where wc.WorkerId == workerId
                                select s.SkillName)
                                .Distinct()
                                .ToList();

            ViewBag.Skills = workerSkills;

            // 2. Counters
            ViewBag.PendingInterviews = _context.Interviews
                .Count(i => i.WorkerId == workerId &&
                            i.Status == "Pending" &&
                            (i.WorkerDecision == null || i.WorkerDecision == ""));

            ViewBag.JobNotifications = _context.Hirings
                .Join(_context.Interviews,
                      h => h.InterviewId,
                      i => i.InterviewId,
                      (h, i) => new { HiringRecord = h, InterviewRecord = i })
                      .Count(joined => joined.InterviewRecord.WorkerId == workerId &&
                                       (joined.HiringRecord.WorkerDecision == "Pending" ||
                                        joined.HiringRecord.WorkerDecision == "Approved" ||
                                        joined.HiringRecord.WorkerDecision == null));

            // 3. REVIEWS & RATING CALCULATION (UPDATED & FIXED LINQ QUERY)
            var reviewsData = (from r in _context.Reviews
                               join i in _context.Interviews on r.InterviewId equals i.InterviewId
                               join c in _context.Clients on i.ClientId equals c.ClientId into clientJoin
                               from c in clientJoin.DefaultIfEmpty()
                               where i.WorkerId == workerId.Value
                               select new
                               {
                                   ClientName = c != null ? c.Name : "Client",
                                   Rating = r.Rating,
                                   Comment = r.Comment,
                                   ReviewDate = r.ReviewDate
                               })
                               .OrderByDescending(r => r.ReviewDate) // Latest reviews tops par aayenge
                               .ToList();

            if (reviewsData.Any())
            {
                var validRatings = reviewsData
                    .Where(r => r.Rating.HasValue && r.Rating.Value > 0)
                    .Select(r => (double)r.Rating.Value)
                    .ToList();

                ViewBag.Rating = validRatings.Any() ? Math.Round(validRatings.Average(), 1) : 0.0;
                ViewBag.TotalReviewsFound = validRatings.Count;
                ViewBag.FirstReview = reviewsData.FirstOrDefault(); // Sabse recent single review
                ViewBag.RecentReviews = reviewsData;                // Puri review list UI iterations ke liye
            }
            else
            {
                ViewBag.Rating = 0.0;
                ViewBag.TotalReviewsFound = 0;
                ViewBag.FirstReview = null;
                ViewBag.RecentReviews = new List<dynamic>();
            }

            // 4. Experience Details
            ViewBag.ExperienceYears = _context.Experiences.Where(e => e.WorkerId == workerId).Count();
            ViewBag.ExperienceList = _context.Experiences.Where(e => e.WorkerId == workerId).ToList();

            return View(worker);
        }
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

            // ================= DIRECT RATING & REVIEWS FETCH (UPDATED) =================
            var workerReviews = await (from r in _context.Reviews
                                       join i in _context.Interviews on r.InterviewId equals i.InterviewId
                                       where i.WorkerId == workerId
                                       select r).ToListAsync();

            ViewBag.WorkerReviews = workerReviews;
            ViewBag.TotalReviewsCount = workerReviews.Count;

            var validRatings = workerReviews.Where(r => r.Rating != null && r.Rating > 0).Select(r => (double)r.Rating.Value).ToList();
            ViewBag.AverageRating = validRatings.Any() ? Math.Round(validRatings.Average(), 1) : 0.0;

            // Database se actual Experience ki sequence list load ho rahi hai timeline display k liye
            var workerExperiences = await _context.Experiences
                .Where(e => e.WorkerId == workerId)
                .ToListAsync();

            ViewBag.WorkerExperiences = workerExperiences;
            ViewBag.ExperienceYears = workerExperiences.Count;

            // DYNAMIC EXPERIENCE TEXT GENERATOR FOR UI GRID (Multiple Records Solution)
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
                    int currentYear = DateTime.Now.Year;
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