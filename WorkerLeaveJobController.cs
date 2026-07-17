using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using MaidAndServantt.Models;

namespace MaidAndServantt.Controllers
{
    public class WorkerLeaveJobController : Controller
    {
        private readonly FypContext _context;

        public WorkerLeaveJobController(FypContext context)
        {
            _context = context;
        }

        // GET: WorkerLeaveJob/LeaveJob
        [HttpGet]
        public IActionResult LeaveJob(int? workerId)
        {
            // 1. Agar URL query se workerId nahi aya, to session se check karein (Worker flow)
            if (workerId == null)
            {
                workerId = HttpContext.Session.GetInt32("WorkerId");
            }

            // Agar dono jagah se workerId nahi mila, to redirect to Login
            if (workerId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // 2. Worker Profile Info Fetch karein
            var worker = _context.Workers.FirstOrDefault(w => w.WorkerId == workerId);
            if (worker == null)
            {
                // Safe check: Agar Client ne open kiya hai to uske dashboard par bhein, warna worker dashboard par
                if (HttpContext.Session.GetInt32("ClientId") != null)
                {
                    return RedirectToAction("ClientProfile", "Dashboard");
                }
                return RedirectToAction("WorkerDashboard", "WorkerDashboard");
            }

            // Job Category Name nikalein
            var jobRole = (from wc in _context.WorkerCategories
                           join cat in _context.Categories on wc.CategoryId equals cat.CategoryId
                           where wc.WorkerId == workerId
                           select cat.CategoryName).FirstOrDefault() ?? "Worker";

            // 3. Notice Period & Dates Setup
            int totalNoticeDays = 30;
            DateTime suggestedLastDay = DateTime.Today.AddDays(totalNoticeDays);
            int remainingDays = 15;

            // Default values agar resignation pehle se na submit hui ho
            string reasonForLeaving = "";
            string lastWorkingDayStr = suggestedLastDay.ToString("yyyy-MM-dd");

            // Check karein agar is worker ki resignation already submitted hai database mein
            var activeInterview = _context.Interviews.FirstOrDefault(i => i.WorkerId == workerId);
            if (activeInterview != null)
            {
                var existingResignation = _context.Resignations
                    .FirstOrDefault(r => r.InterviewId == activeInterview.InterviewId);

                if (existingResignation != null)
                {
                    reasonForLeaving = existingResignation.ResignationReason ?? "";

                    if (existingResignation.LastWorkingDate != default)
                    {
                        lastWorkingDayStr = existingResignation.LastWorkingDate.ToString("yyyy-MM-dd");
                    }
                }
            }

            // ViewBag values jo view display karne ke liye use karega
            ViewBag.RemainingDays = remainingDays;
            ViewBag.TotalNoticeDays = totalNoticeDays;
            ViewBag.LastWorkingDay = lastWorkingDayStr;
            ViewBag.ReasonForLeaving = reasonForLeaving;

            var model = new LeaveJobViewModel
            {
                WorkerId = worker.WorkerId,
                WorkerName = worker.Name,
                WorkerImage = worker.Picture ?? "/Images/default-avatar.png",
                JobRole = jobRole,
                TotalNoticeDays = totalNoticeDays,
                RemainingDays = remainingDays,
                LastWorkingDay = lastWorkingDayStr,
                Reason = reasonForLeaving // Purani reason load karne ke liye
            };

            return View(model);
        }

        // POST: WorkerLeaveJob/SubmitResignation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitResignation(LeaveJobViewModel model)
        {
            // 🔥 UPDATE 1: Ab Session check flexible hoga. Client ya Worker dono me se kisi ka session hona chahiye.
            int? clientId = HttpContext.Session.GetInt32("ClientId");
            int? workerSessionId = HttpContext.Session.GetInt32("WorkerId");

            if (clientId == null && workerSessionId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Target Worker ID model se uthayenge taake Client flow break na ho
            int targetWorkerId = model.WorkerId;

            if (string.IsNullOrEmpty(model.Reason))
            {
                ModelState.AddModelError("Reason", "Please enter a valid reason for leaving.");

                // Re-fetch basic info to rebuild view if validation fails
                var worker = _context.Workers.FirstOrDefault(w => w.WorkerId == targetWorkerId);
                model.WorkerName = worker?.Name;
                model.WorkerImage = worker?.Picture ?? "/Images/default-avatar.png";

                return View("LeaveJob", model);
            }

            // Find active contract (Interview record) for this worker
            var activeInterview = _context.Interviews.FirstOrDefault(i => i.WorkerId == targetWorkerId);

            if (activeInterview == null)
            {
                TempData["ErrorMessage"] = "No active contract found to submit resignation.";

                if (clientId != null)
                {
                    return RedirectToAction("ClientProfile", "Dashboard");
                }
                return RedirectToAction("WorkerDashboard", "WorkerDashboard");
            }

            // Parse Date to DateOnly format for the Database
            DateOnly parsedLastWorkingDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)); // fallback
            if (DateTime.TryParse(model.LastWorkingDay, out DateTime tempDate))
            {
                parsedLastWorkingDate = DateOnly.FromDateTime(tempDate);
            }

            // Check karein agar resignation pehle se bani hui hai to nayi add karne ke bajaye purani UPDATE karein
            var existingResignation = _context.Resignations.FirstOrDefault(r => r.InterviewId == activeInterview.InterviewId);

            if (existingResignation != null)
            {
                // Update Existing
                existingResignation.ResignationReason = model.Reason;
                existingResignation.LastWorkingDate = parsedLastWorkingDate;
                _context.Resignations.Update(existingResignation);
            }
            else
            {
                // Create New Resignation
                var resignation = new Resignation
                {
                    InterviewId = activeInterview.InterviewId,
                    ResignationReason = model.Reason,
                    LastWorkingDate = parsedLastWorkingDate,
                    SubmittedDate = DateTime.Now
                };
                _context.Resignations.Add(resignation);
            }

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Resignation details updated successfully!";

            // 🔥 UPDATE 2: Role base dynamic redirect
            if (clientId != null)
            {
                // Agar client ne action kiya hai, to client dashboard par bhejein
                return RedirectToAction("ClientProfile", "Dashboard");
            }

            // Agar worker ne khud kiya hai, to worker dashboard par bhejein
            return RedirectToAction("WorkerDashboard", "WorkerDashboard");
        }
    }

    public class LeaveJobViewModel
    {
        public int WorkerId { get; set; }
        public string WorkerName { get; set; }
        public string WorkerImage { get; set; }
        public string JobRole { get; set; }
        public int TotalNoticeDays { get; set; }
        public int RemainingDays { get; set; }
        public string LastWorkingDay { get; set; }
        public string Reason { get; set; }
    }
}