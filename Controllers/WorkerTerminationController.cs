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
            [HttpGet]
            public IActionResult TerminationDetails()
            {
                int? workerId = HttpContext.Session.GetInt32("WorkerId");
                if (workerId == null)
                {
                    return RedirectToAction("Login", "Auth"); //[cite: 1]
                }

                // 1. Worker profile details fetch
                var worker = _context.Workers.FirstOrDefault(w => w.WorkerId == workerId); //[cite: 1]
                if (worker == null)
                {
                    return NotFound(); //[cite: 1]
                }

                // 2. Job Role fetch
                var jobRole = (from wc in _context.WorkerCategories
                               join cat in _context.Categories on wc.CategoryId equals cat.CategoryId
                               where wc.WorkerId == workerId
                               select cat.CategoryName).FirstOrDefault() ?? "Worker"; //[cite: 1]

                // 3. Worker ki Interviews se Termination aur Client Info JOIN ke sath fetch karein
                var terminationData = (from t in _context.Terminations
                                       join i in _context.Interviews on t.InterviewId equals i.InterviewId
                                       join c in _context.Clients on i.ClientId equals c.ClientId
                                       where i.WorkerId == workerId
                                       orderby t.TerminationId descending
                                       select new
                                       {
                                           Termination = t,
                                           ClientName = c.Name,
                                           ClientPicture = c.Picture
                                       }).FirstOrDefault();

                var model = new TerminationDetailsViewModel
                {
                    WorkerName = worker.Name ?? "Worker", //[cite: 1]
                    WorkerImage = string.IsNullOrEmpty(worker.Picture) ? "/Images/default-avatar.png" : worker.Picture, //[cite: 1]
                    JobRole = jobRole, //[cite: 1]
                    WorkerAddress = worker.Address ?? "N/A", //[cite: 1]
                    WorkerPhone = worker.Phone ?? "N/A" //[cite: 1]
                };

                if (terminationData != null && terminationData.Termination != null) //[cite: 1]
                {
                    var record = terminationData.Termination;

                    model.IsTerminated = true; //[cite: 1]
                    model.Status = "Terminated"; //[cite: 1]
                    model.Reason = string.IsNullOrWhiteSpace(record.TerminatedReason)
                        ? "Contract Terminated by Client."
                        : record.TerminatedReason; //[cite: 1]

                    model.TerminationDate = record.TerminatedDate.HasValue
                        ? record.TerminatedDate.Value.ToString("MMM d, yyyy")
                        : DateTime.Now.ToString("MMM d, yyyy"); //[cite: 1]

                    model.ClientName = terminationData.ClientName ?? "Client / Employer"; //[cite: 1]

                    string clientPic = terminationData.ClientPicture;
                    if (!string.IsNullOrEmpty(clientPic) && !clientPic.StartsWith("/") && !clientPic.StartsWith("http"))
                    {
                        clientPic = "/Images/" + clientPic; //[cite: 1]
                    }
                    model.ClientImage = string.IsNullOrEmpty(clientPic) ? "/Images/default-avatar.png" : clientPic; //[cite: 1]
                }
                else
                {
                    model.IsTerminated = false; //[cite: 1]
                    model.Status = "Active"; //[cite: 1]
                }

                return View(model); //[cite: 1]
            } }
        public class TerminationDetailsViewModel
        {
            public bool IsTerminated { get; set; }
            public string WorkerName { get; set; }
            public string WorkerImage { get; set; }
            public string JobRole { get; set; }
            public string WorkerAddress { get; set; }
            public string WorkerPhone { get; set; }
            public string Status { get; set; }
            public string TerminationDate { get; set; }
            public string Reason { get; set; }
            public string ClientName { get; set; }
            public string ClientImage { get; set; }
        }
    }
