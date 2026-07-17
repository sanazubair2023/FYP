using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MaidAndServantt.Models;

namespace MaidAndServantt.Controllers
{
    public class JobConfirmationController : Controller
    {
        private readonly FypContext _context;

        public JobConfirmationController(FypContext context)
        {
            _context = context;
        }

        // 1. GET: Main View Screen
        [HttpGet]
        public async Task<IActionResult> JobConfirmation()
{
    int? workerId = HttpContext.Session.GetInt32("WorkerId");
    if (workerId == null)
    {
        return RedirectToAction("Login", "Auth");
    }

    try
    {
        // 1. Database se data anonymous object mein select karein (With clientImage)
        var jobs = await _context.Hirings
            .Include(h => h.Interview)
            .Where(h => h.Interview.WorkerId == workerId)
            .Select(h => new
            {
                id = h.InterviewId, 
                clientName = _context.Clients.Where(c => c.ClientId == h.Interview.ClientId).Select(c => c.Name).FirstOrDefault() ?? "Client",
                status = h.Interview.Status,
                date = h.HiringDate != null ? h.HiringDate.Value.ToString("dd-MM-yyyy") : "Pending",
                role = _context.WorkerCategories.Where(wc => wc.WorkerId == h.Interview.WorkerId).Join(_context.Categories, wc => wc.CategoryId, c => c.CategoryId, (wc, c) => c.CategoryName).FirstOrDefault() ?? "Worker",
                address = h.Address ?? "Pending",
                hiringDecision = h.HiringDecision ?? "Pending",
                workerDecision = h.WorkerDecision ?? "Pending",
                
                // ERROR RESOLVED: Yeh property yahan missing thi jiski wajah se loop crash ho raha tha
                clientImage = _context.Clients.Where(c => c.ClientId == h.Interview.ClientId).Select(c => c.Picture).FirstOrDefault()
            })
            .ToListAsync();

        // 2. Map anonymous list to JobConfirmationViewModel
        var mappedJobs = jobs.Select(item =>
        {
            string type, msg, displayStatus;

            if (item.workerDecision == "Rejected")
            {
                type = "rejected";
                msg = "Thank you for your time. Job offer declined.";
                displayStatus = "Rejected";
            }
            else if (item.status == "Terminated")
            {
                type = "terminated";
                msg = "Your contract has been terminated by the client.";
                displayStatus = "Terminated";
            }
            else if (item.hiringDecision == "Accepted")
            {
                type = "final";
                msg = "Congratulations! You are officially hired. Welcome aboard!";
                displayStatus = "Hired";
            }
            else if (item.workerDecision == "Accepted")
            {
                type = "accepted";
                msg = "Job offer accepted. Awaiting client response.";
                displayStatus = "Accepted";
            }
            else
            {
                type = "offered";
                msg = "Great interview! We'd like to proceed with a contract.";
                displayStatus = "Pending";
            }

            return new JobConfirmationViewModel
            {
                Id = item.id ?? 0, // int? to int fix
                ClientName = item.clientName,
                Status = displayStatus,
                Date = item.date,
                Role = item.role,
                Address = item.address,
                Message = msg,
                Type = type,
                ClientImage = item.clientImage // Ab yeh compiler ko mil jayega bina kisi error ke!
            };
        }).ToList();

        return View(mappedJobs);
    }
    catch (Exception ex)
    {
        ViewBag.Error = "Error loading data: " + ex.Message;
        return View(new List<JobConfirmationViewModel>());
    }
}

        // 2. POST: Worker Accept Offer (AJAX)
        [HttpPost]
        public async Task<IActionResult> AcceptJob(int id)
        {
            try
            {
                var hiring = await _context.Hirings.FirstOrDefaultAsync(h => h.InterviewId == id);
                if (hiring == null) return Json(new { success = false, message = "Record not found." });

                hiring.WorkerDecision = "Accepted";
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Job offer accepted! Awaiting client confirmation." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // 3. POST: Worker Reject Offer (AJAX)
        [HttpPost]
        public async Task<IActionResult> RejectJob(int id)
        {
            try
            {
                var hiring = await _context.Hirings.Include(h => h.Interview).FirstOrDefaultAsync(h => h.InterviewId == id);
                if (hiring == null) return Json(new { success = false, message = "Record not found." });

                hiring.WorkerDecision = "Rejected";
                if (hiring.Interview != null)
                {
                    hiring.Interview.Status = "Rejected";
                }
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Job offer declined successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // 4. POST: Dismiss/Delete Record (AJAX)
        [HttpPost]
        public async Task<IActionResult> DeleteJob(int id)
        {
            try
            {
                var hiring = await _context.Hirings.FirstOrDefaultAsync(h => h.InterviewId == id);
                if (hiring == null) return Json(new { success = false, message = "Record not found." });

                _context.Hirings.Remove(hiring);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Job request removed." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    public class JobConfirmationViewModel
    {
        public int Id { get; set; }
        public string ClientName { get; set; } = "Client";
        public string Status { get; set; } = "Pending";
        public string Date { get; set; } = "Pending";
        public string Role { get; set; } = "Worker";
        public string Address { get; set; } = "Pending";
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? ClientImage { get; set; }
    }
}