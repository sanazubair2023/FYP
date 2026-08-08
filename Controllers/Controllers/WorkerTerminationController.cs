using MaidAndServantt.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MaidAndServantt.Controllers
{
    public class WorkerTerminationController : Controller
    {
        private readonly FypContext _context;

        public WorkerTerminationController(FypContext context)
        {
            _context = context;
        }

       

            [HttpGet]
            public async Task<IActionResult> TerminationDetails(int? workerId)
            {
                try
                {
                    // URL ya Session se Worker ID extract karein
                    int currentWorkerId = workerId ?? HttpContext.Session.GetInt32("WorkerId") ?? 0;

                    if (currentWorkerId == 0)
                    {
                        return RedirectToAction("Login", "Auth");
                    }

                    // Singular DbSets/Tables: _context.Terminations (or _context.Termination)
                    var termination = await (from t in _context.Terminations
                                             join i in _context.Interviews on t.InterviewId equals i.InterviewId
                                             join c in _context.Clients on i.ClientId equals c.ClientId into clientGroup
                                             from c in clientGroup.DefaultIfEmpty()
                                             where i.WorkerId == currentWorkerId
                                             orderby t.TerminatedDate descending
                                             select new WorkerTerminationViewModel
                                             {
                                                 TerminationId = t.TerminationId,
                                                 TerminatedDate = t.TerminatedDate.HasValue
                                                     ? t.TerminatedDate.Value.ToDateTime(TimeOnly.MinValue)
                                                     : DateTime.MinValue,
                                                 TerminatedReason = t.TerminatedReason,
                                                 ClientName = c != null ? c.Name : "Client",
                                                 ClientPicture = c != null ? c.Picture : null,
                                                 Status = i.Status
                                             }).FirstOrDefaultAsync();

                    return View("TerminationDetails", termination);
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = "Error: " + ex.Message;
                    return View("TerminationDetails", null);
                }
            }
        }
    
    public class WorkerTerminationViewModel
    {
        public int TerminationId { get; set; }
        public DateTime TerminatedDate { get; set; }
        public string? TerminatedReason { get; set; }
        public string? ClientName { get; set; }
        public string? ClientPicture { get; set; }
        public string? Status { get; set; }
    }
}