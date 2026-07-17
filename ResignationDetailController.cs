using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MaidAndServantt.Models; // Apne project ka sahi namespace use karein
using System.Linq;

namespace MaidAndServantt.Controllers
{
    public class ResignationDetailController : Controller
    {
        private readonly FypContext _context; // Apne DbContext ka naam likhein

        public ResignationDetailController(FypContext context)
        {
            _context = context;
        }
        // GET: ResignationDetail/ClientResignationsList
        public IActionResult ClientResignationsList()
        {
            // 1. Client ka session check karein
            int? clientId = HttpContext.Session.GetInt32("ClientId");
            if (clientId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // 2. Database se un sab workers ki list nikalen jinhon ne is Client ke under resign kiya hai
            var clientResignations = (from r in _context.Resignations
                                      join i in _context.Interviews on r.InterviewId equals i.InterviewId
                                      join w in _context.Workers on i.WorkerId equals w.WorkerId
                                      where i.ClientId == clientId.Value
                                      select new
                                      {
                                          WorkerId = w.WorkerId
                                      }).ToList();

            var firstResignedWorker = clientResignations.FirstOrDefault();

            if (firstResignedWorker != null)
            {
                // Pehle milne wale resigned worker ka ID lekar view action par bhej dein
                return RedirectToAction("ClientViewResignationDetail", "ResignationDetail", new { workerId = firstResignedWorker.WorkerId });
            }

            // Fallback: Agar kisi worker ne abhi tak resign nahi kiya
            TempData["ErrorMessage"] = "Abhi tak kisi worker ne resignation submit nahi ki.";
            return RedirectToAction("ClientProfile", "Dashboard");
        }

        // GET: ResignationDetail/ClientViewResignationDetail?workerId=5
        public IActionResult ClientViewResignationDetail(int workerId)
        {
            // Client ka session verification
            int? clientId = HttpContext.Session.GetInt32("ClientId");
            if (clientId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // 1. Worker fetch karein
            var worker = _context.Workers.FirstOrDefault(w => w.WorkerId == workerId);
            if (worker == null)
            {
                TempData["ErrorMessage"] = "Worker record database mein nahi mila.";
                return RedirectToAction("ClientProfile", "Dashboard");
            }

            // ViewModel populate karein (Resignation_2.cshtml ke liye)
            var viewModel = new LeaveJobViewModel
            {
                WorkerId = worker.WorkerId,
                WorkerName = worker.Name,
                WorkerImage = string.IsNullOrEmpty(worker.Picture) ? "/Images/default-avatar.png" : worker.Picture,
                JobRole = (from wc in _context.WorkerCategories
                           join cat in _context.Categories on wc.CategoryId equals cat.CategoryId
                           where wc.WorkerId == workerId
                           select cat.CategoryName).FirstOrDefault() ?? "Helper"
            };

            // 2. Database se is worker ki Resignation details nikalen
            var resignation = (from r in _context.Resignations
                               join i in _context.Interviews on r.InterviewId equals i.InterviewId
                               where i.WorkerId == workerId
                               select r).FirstOrDefault();

            if (resignation != null)
            {
                int totalNoticeDays = 30;
                int remainingDays = resignation.LastWorkingDate.DayNumber - DateOnly.FromDateTime(DateTime.Now).DayNumber;
                if (remainingDays < 0) remainingDays = 0;

                ViewBag.TotalNoticeDays = totalNoticeDays;
                ViewBag.RemainingDays = remainingDays;
                ViewBag.LastWorkingDay = resignation.LastWorkingDate.ToString("MMM dd, yyyy");
                ViewBag.ReasonForLeaving = resignation.ResignationReason;
            }
            else
            {
                // Fallback agar resignation table entry pending ho
                ViewBag.TotalNoticeDays = 30;
                ViewBag.RemainingDays = 15;
                ViewBag.LastWorkingDay = DateOnly.FromDateTime(DateTime.Now.AddDays(15)).ToString("MMM dd, yyyy");
                ViewBag.ReasonForLeaving = "This worker has indicated an intent to resign. Official notice pending database confirmation.";
            }

            // Resignation_2 View load karein
            return View("Resignation_2", viewModel);
        }
        // POST: Submit Resignation
        [HttpPost]
        public IActionResult SubmitResignation(string remarks)
        {
            var workerIdStr = HttpContext.Session.GetString("WorkerId");
            if (string.IsNullOrEmpty(workerIdStr)) return RedirectToAction("Login", "Auth");

            int workerId = int.Parse(workerIdStr);

            // Interview table ke rastay se is worker ki resignation entry find karein
            var resignation = _context.Resignations
                                      .FirstOrDefault(r => _context.Interviews
                                                                   .Where(i => i.WorkerId == workerId)
                                                                   .Select(i => (int?)i.InterviewId)
                                                                   .Contains(r.InterviewId));

            if (resignation != null)
            {
                // Kyunki model mein Remarks nahi hai, aap user ke likhay hue remarks ko 
                // ResignationReason field ke sath concatenate ya assign kar sakti hain:
                if (!string.IsNullOrEmpty(remarks))
                {
                    resignation.ResignationReason += " | Worker Remarks: " + remarks;
                }

                _context.SaveChanges();
            }

            return RedirectToAction("Index", "WorkerDashboard");
        }
    }
}