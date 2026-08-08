using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public async Task<IActionResult> LeaveJob()
        {
            int? workerId = HttpContext.Session.GetInt32("WorkerId");

            if (workerId == null)
            {
                TempData["ErrorMessage"] = "Worker identity not found. Please re-login.";
                return RedirectToAction("Login", "Account");
            }

            var activeJobQuery = await (from i in _context.Interviews
                                        join c in _context.Clients on i.ClientId equals c.ClientId into clientGroup
                                        from c in clientGroup.DefaultIfEmpty()
                                        where i.WorkerId == workerId &&
                                              i.WorkerDecision != "Rejected" &&
                                              i.Status != "Rejected" &&
                                              i.Status != "JobRejected" &&
                                              i.Status != "Completed" &&
                                              i.Status != "Terminated" &&
                                              i.Status != "Resigned"
                                        select new
                                        {
                                            InterviewId = i.InterviewId,
                                            EmployerName = c != null ? c.Name : "Unknown Employer",
                                            EmployerAddress = c != null ? c.Address : "N/A"
                                        }).ToListAsync();

            var activeJob = activeJobQuery
                .OrderByDescending(x => x.InterviewId)
                .Select(x => new ResignationViewModel
                {
                    InterviewId = x.InterviewId,
                    EmployerName = x.EmployerName,
                    EmployerAddress = x.EmployerAddress,
                    LastWorkingDate = DateTime.Now.AddDays(7)
                })
                .FirstOrDefault();

            if (activeJob == null)
            {
                TempData["ErrorMessage"] = "No active job found to resign from.";
                // FIX: Added Controller Name "WorkerDashboard"
                return RedirectToAction("WorkerDashboard", "WorkerDashboard");
            }

            return View(activeJob);
        }

        // POST: WorkerLeaveJob/LeaveJob
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LeaveJob(ResignationViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.ResignationReason))
            {
                ModelState.AddModelError("ResignationReason", "Please provide a reason for resignation.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var interview = await _context.Interviews
                    .FirstOrDefaultAsync(i => i.InterviewId == model.InterviewId);

                if (interview == null)
                {
                    TempData["ErrorMessage"] = "Job record not found.";
                    // FIX: Added Controller Name "WorkerDashboard"
                    return RedirectToAction("WorkerDashboard", "WorkerDashboard");
                }

                var alreadyResigned = _context.Resignations
                    .Any(r => r.InterviewId == model.InterviewId);

                if (alreadyResigned)
                {
                    ModelState.AddModelError("", "You have already submitted a resignation for this job.");
                    return View(model);
                }

                // 1. Insert Resignation Record
                var resignation = new Resignation
                {
                    InterviewId = model.InterviewId,
                    ResignationReason = model.ResignationReason,
                    LastWorkingDate = DateOnly.FromDateTime(model.LastWorkingDate),
                    SubmittedDate = DateTime.Now
                };
                _context.Resignations.Add(resignation);

                // 2. Update Interview Status
                interview.Status = "ResignationPending";

                // 3. Update Worker Status
                var worker = await _context.Workers.FindAsync(interview.WorkerId);
                if (worker != null)
                {
                    worker.AvailableStatus = true;
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Resignation submitted successfully.";
                // FIX: Added Controller Name "WorkerDashboard"
                return RedirectToAction("WorkerDashboard", "WorkerDashboard");
            }
            catch (Exception ex)
            {
                var finalMsg = ex.InnerException?.InnerException?.Message
                               ?? ex.InnerException?.Message
                               ?? ex.Message;
                ModelState.AddModelError("", "DB Error: " + finalMsg);
                return View(model);
            }
        }

        public IActionResult Dashboard()
        {
            return View();
        }

    }
    public class ResignationViewModel
    {
        public int InterviewId { get; set; }
        public string? EmployerName { get; set; }
        public string? EmployerAddress { get; set; }
        public string ResignationReason { get; set; } = string.Empty;
        public DateTime LastWorkingDate { get; set; }
    }
}