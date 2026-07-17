using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using MaidAndServantt.Models;

namespace MaidAndServantt.Controllers
{
    public class WorkerTerminationController : Controller
    {
        private readonly FypContext _context;

        public WorkerTerminationController(FypContext context)
        {
            _context = context;
        }

        // GET: WorkerTermination/TerminationDetails
        [HttpGet]
        public IActionResult TerminationDetails()
        {
            int? workerId = HttpContext.Session.GetInt32("WorkerId");
            if (workerId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // 1. Dynamic Job Role (Category Name) database se fetch karna
            var jobRole = (from wc in _context.WorkerCategories
                           join cat in _context.Categories on wc.CategoryId equals cat.CategoryId
                           where wc.WorkerId == workerId
                           select cat.CategoryName).FirstOrDefault() ?? "Worker";

            // 2. Dynamic Experience Years database se calculate karna
            var expCount = _context.Experiences.Count(e => e.WorkerId == workerId);
            string experienceText = expCount == 1 ? "1 Year" : expCount > 1 ? $"{expCount} Years" : "No Experience Listed";

            // 3. Database se dynamic Termination, Client aur Worker details query karna
            var queryResult = (from t in _context.Terminations
                               join i in _context.Interviews on t.InterviewId equals i.InterviewId
                               join c in _context.Clients on i.ClientId equals c.ClientId
                               join w in _context.Workers on i.WorkerId equals w.WorkerId
                               where i.WorkerId == workerId
                               orderby t.TerminationId descending // Latest record lane k liye
                               select new
                               {
                                   WorkerName = w.Name,
                                   WorkerImage = w.Picture ?? "/Images/default-avatar.png",
                                   WorkerAddress = w.Address ?? "N/A",
                                   WorkerPhone = w.Phone ?? "N/A",

                                   Status = "Terminated",
                                   RawDate = t.TerminatedDate,
                                   Reason = t.TerminatedReason ?? "Termination updated by admin.",

                                   ClientName = c.Name,
                                   ClientAddress = c.Address ?? "N/A",
                                   ClientImage = c.Picture ?? "/Images/default-avatar.png"
                               }).FirstOrDefault();

            var model = new TerminationDetailsViewModel();

            if (queryResult != null)
            {
                // Agar database mein mukammal contract terminated data mil jata hai
                model.WorkerName = queryResult.WorkerName;
                model.WorkerImage = queryResult.WorkerImage;
                model.JobRole = jobRole;
                model.WorkerAddress = queryResult.WorkerAddress;
                model.WorkerPhone = queryResult.WorkerPhone;
                model.WorkerExperience = experienceText;
                model.Status = queryResult.Status;

                model.TerminationDate = queryResult.RawDate.HasValue
                    ? queryResult.RawDate.Value.ToString("dd-MM-yyyy")
                    : DateTime.Now.ToString("dd-MM-yyyy");

                model.Reason = queryResult.Reason;
                model.ClientName = queryResult.ClientName;
                model.ClientAddress = queryResult.ClientAddress;
                model.ClientImage = queryResult.ClientImage;
            }
            else
            {
                // Fallback: Agar termination entry nahi hai, to worker ki active/latest interview aur client details fetch karwayen
                var latestInterview = (from i in _context.Interviews
                                       join c in _context.Clients on i.ClientId equals c.ClientId
                                       join w in _context.Workers on i.WorkerId equals w.WorkerId
                                       where i.WorkerId == workerId
                                       orderby i.InterviewId descending
                                       select new { Worker = w, Client = c }).FirstOrDefault();

                if (latestInterview != null)
                {
                    model.WorkerName = latestInterview.Worker.Name;
                    model.WorkerImage = latestInterview.Worker.Picture ?? "/Images/default-avatar.png";
                    model.JobRole = jobRole;
                    model.WorkerAddress = latestInterview.Worker.Address ?? "N/A";
                    model.WorkerPhone = latestInterview.Worker.Phone ?? "N/A";
                    model.WorkerExperience = experienceText;
                    model.Status = "Contract Ended";
                    model.TerminationDate = DateTime.Now.ToString("dd-MM-yyyy");
                    model.Reason = "Service closed or contract completed by client.";
                    model.ClientName = latestInterview.Client.Name;
                    model.ClientAddress = latestInterview.Client.Address ?? "N/A";
                    model.ClientImage = latestInterview.Client.Picture ?? "/Images/default-avatar.png";
                }
                else
                {
                    // Nihayat basic fallback agar worker ka koi record hi mojood nahi
                    var worker = _context.Workers.FirstOrDefault(w => w.WorkerId == workerId);
                    model.WorkerName = worker?.Name ?? "Worker";
                    model.WorkerImage = worker?.Picture ?? "/Images/default-avatar.png";
                    model.JobRole = jobRole;
                    model.WorkerAddress = worker?.Address ?? "N/A";
                    model.WorkerPhone = worker?.Phone ?? "N/A";
                    model.WorkerExperience = experienceText;
                    model.Status = "Inactive / Pending Record";
                    model.TerminationDate = DateTime.Now.ToString("dd-MM-yyyy");
                    model.Reason = "No termination history exists in database.";
                    model.ClientName = "No Associated Client";
                    model.ClientAddress = "N/A";
                    model.ClientImage = "/Images/default-avatar.png";
                }
            }

            return View(model);
        }
    }

    public class TerminationDetailsViewModel
    {
        public string WorkerName { get; set; }
        public string WorkerImage { get; set; }
        public string JobRole { get; set; }
        public string WorkerAddress { get; set; }
        public string WorkerPhone { get; set; }
        public string WorkerExperience { get; set; }
        public string Status { get; set; }
        public string TerminationDate { get; set; }
        public string Reason { get; set; }
        public string ClientName { get; set; }
        public string ClientAddress { get; set; }
        public string ClientImage { get; set; }
    }
}