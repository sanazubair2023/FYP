using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MaidAndServantt.Models;

namespace MaidAndServantt.Controllers
{
    public class ResignationDetailController : Controller
    {
        private readonly FypContext _context;

        public ResignationDetailController(FypContext context)
        {
            _context = context;
        }

        // 1. Resignation Detail Page Load Karna (GET)
        [HttpGet]
        public async Task<IActionResult> ResignationDetail(int id)
        {
            try
            {
                var resignationData = await (from r in _context.Resignations
                                             join i in _context.Interviews on r.InterviewId equals i.InterviewId
                                             join w in _context.Workers on i.WorkerId equals w.WorkerId
                                             where r.ResignationId == id
                                             select new
                                             {
                                                 ResignationId = r.ResignationId,
                                                 InterviewId = i.InterviewId,
                                                 WorkerId = w.WorkerId,
                                                 WorkerName = w.Name,
                                                 WorkerAvatar = w.Picture,
                                                 Reason = r.ResignationReason,
                                                 SubmittedDate = r.SubmittedDate,
                                                 LastWorkingDate = r.LastWorkingDate,
                                                 InterviewStatus = i.Status
                                             }).FirstOrDefaultAsync();

                if (resignationData == null)
                {
                    ViewBag.Error = "Resignation record not found.";
                    return View("Error");
                }

                var workerRole = await _context.WorkerCategories
                    .Where(wc => wc.WorkerId == resignationData.WorkerId)
                    .Join(_context.Categories, wc => wc.CategoryId, c => c.CategoryId, (wc, c) => c.CategoryName)
                    .FirstOrDefaultAsync() ?? "Worker";

                var submitted = resignationData.SubmittedDate ?? DateTime.Now.AddDays(-15);
                var lastDayRaw = resignationData.LastWorkingDate;
                var lastDay = lastDayRaw.ToDateTime(TimeOnly.MinValue);

                int totalNoticeDays = (lastDay - submitted).Days;
                if (totalNoticeDays <= 0) totalNoticeDays = 30;

                int remainingDays = (lastDay - DateTime.Now).Days;
                if (remainingDays < 0) remainingDays = 0;

                double progress = 1.0 - ((double)remainingDays / totalNoticeDays);
                if (progress > 1) progress = 1;
                if (progress < 0) progress = 0;

                bool hasClientReview = await _context.Reviews.AnyAsync(r => r.InterviewId == resignationData.InterviewId);
                bool isConfirmed = resignationData.InterviewStatus == "Resigned" || hasClientReview;

                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.InterviewId == resignationData.InterviewId);

                var viewModel = new ResignationDetailViewModel
                {
                    ResignationId = resignationData.ResignationId,
                    InterviewId = resignationData.InterviewId,
                    WorkerName = resignationData.WorkerName ?? "Unknown",
                    WorkerRole = workerRole,
                    WorkerAvatar = resignationData.WorkerAvatar,
                    Reason = resignationData.Reason,
                    LastWorkingDate = lastDayRaw.ToString("MMM dd, yyyy"),
                    TotalNoticeDays = totalNoticeDays,
                    RemainingDays = remainingDays,
                    ProgressPercentage = (int)Math.Round(progress * 100),
                    IsConfirmed = isConfirmed,
                    Rating = existingReview?.Rating ?? 3,
                    Comment = existingReview?.Comment ?? ""
                };

                // FIX: "ResignationDetails" path pass kiya hai taake aap ki file name match ho sake
                return View("ResignationDetail", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error loading resignation details: " + ex.Message;
                return View("Error");
            }
        }

        // 2. Resignation Confirm Karna (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmResignation(ResignationDetailViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Comment))
            {
                ModelState.AddModelError("Comment", "Please enter some remarks before confirming.");
                // FIX: Here also targeting "ResignationDetails"
                return View("ResignationDetail", model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var interview = await _context.Interviews.FindAsync(model.InterviewId);
                if (interview == null)
                {
                    TempData["ErrorMessage"] = "Interview record not found.";
                    return RedirectToAction("ResignationDetail", new { id = model.ResignationId });
                }

                if (interview.Status == "Resigned")
                {
                    TempData["ErrorMessage"] = "This resignation has already been confirmed.";
                    return RedirectToAction("ResignationDetail", new { id = model.ResignationId });
                }

                // Review Entry Save Karna
                var review = new Review
                {
                    InterviewId = model.InterviewId,
                    Rating = model.Rating,
                    Comment = model.Comment,
                    ReviewDate = DateTime.Now
                };
                _context.Reviews.Add(review);

                // Status Update
                interview.Status = "Resigned";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Resignation successfully confirmed!";
                return RedirectToAction("ResignationDetail", new { id = model.ResignationId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Error processing confirmation: " + ex.Message;
                return RedirectToAction("ResignationDetail", new { id = model.ResignationId });
            }
        }

        // 3. Get List of Resignations
        [HttpGet]
        public async Task<IActionResult> GetResignationDetails(int resignationId = 0)
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrEmpty(userIdStr) && HttpContext.Session.GetInt32("ClientId").HasValue)
                {
                    userIdStr = HttpContext.Session.GetInt32("ClientId").ToString();
                }

                if (string.IsNullOrEmpty(userIdStr))
                {
                    return RedirectToAction("Login", "Account");
                }

                int clientId = int.Parse(userIdStr);

                var query = from r in _context.Resignations
                            join i in _context.Interviews on r.InterviewId equals i.InterviewId
                            join w in _context.Workers on i.WorkerId equals w.WorkerId
                            where i.ClientId == clientId
                            select new
                            {
                                ResignationId = r.ResignationId,
                                WorkerId = i.WorkerId,
                                WorkerName = w.Name,
                                Reason = r.ResignationReason,
                                LastWorkingDate = r.LastWorkingDate,
                                SubmittedDate = r.SubmittedDate
                            };

                if (resignationId > 0)
                {
                    query = query.Where(r => r.ResignationId == resignationId);
                }

                var data = await query.OrderByDescending(r => r.SubmittedDate).ToListAsync();

                var viewModel = new List<ResignationListItemViewModel>();

                foreach (var item in data)
                {
                    var workerRole = await _context.WorkerCategories
                        .Where(wc => wc.WorkerId == item.WorkerId)
                        .Join(_context.Categories, wc => wc.CategoryId, c => c.CategoryId, (wc, c) => c.CategoryName)
                        .FirstOrDefaultAsync() ?? "Worker";

                    viewModel.Add(new ResignationListItemViewModel
                    {
                        ResignationId = item.ResignationId,
                        WorkerName = item.WorkerName ?? "Unknown Worker",
                        WorkerRole = workerRole,
                        Reason = item.Reason,
                        LastWorkingDate = item.LastWorkingDate.ToString("MMM dd, yyyy"),
                        SubmittedDate = item.SubmittedDate.HasValue ? item.SubmittedDate.Value.ToString("MMM dd, yyyy") : "N/A"
                    });
                }

                return View("Resignation", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error loading resignations: " + ex.Message;
                return View("Resignation", new List<ResignationListItemViewModel>());
            }
        }

        // ViewModels
        public class ResignationDetailViewModel
        {
            public int ResignationId { get; set; }
            public int InterviewId { get; set; }
            public string WorkerName { get; set; }
            public string WorkerRole { get; set; }
            public string WorkerAvatar { get; set; }
            public string Reason { get; set; }
            public string LastWorkingDate { get; set; }
            public int TotalNoticeDays { get; set; }
            public int RemainingDays { get; set; }
            public int ProgressPercentage { get; set; }
            public bool IsConfirmed { get; set; }

            public int Rating { get; set; }
            public string Comment { get; set; }
        }

        public class ResignationListItemViewModel
        {
            public int ResignationId { get; set; }
            public string WorkerName { get; set; }
            public string WorkerRole { get; set; }
            public string Reason { get; set; }
            public string LastWorkingDate { get; set; }
            public string SubmittedDate { get; set; }
        }
    }
}