using Microsoft.AspNetCore.Mvc;
using MaidAndServantt.Models;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using System.IO;

namespace MaidAndServantt.Controllers
{
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class DashboardController : Controller
    {
        private readonly FypContext _context;

        public DashboardController(FypContext context)
        {
            _context = context;
        }

        // ================= GET: Dashboard/Logout =================
        [HttpGet]
        public IActionResult Login()
        {
            // Session clear karega taake logged in user ki credentials destroy ho jayein
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }

        // ================= GET: Dashboard/EditProfile =================
        [HttpGet]
        public IActionResult EditProfile()
        {
            int? clientId = HttpContext.Session.GetInt32("ClientId");
            if (clientId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var client = _context.Clients.FirstOrDefault(c => c.ClientId == clientId);
            if (client == null)
            {
                return NotFound();
            }

            ViewBag.IsEditMode = true;
            return View("~/Views/Auth/Signup.cshtml", client);
        }

        // ================= POST: Dashboard/EditProfile =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(Client updatedClient, IFormFile? ClientPicture)
        {
            int? currentClientId = HttpContext.Session.GetInt32("ClientId");
            if (currentClientId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            ModelState.Remove("Password");

            if (!ModelState.IsValid)
            {
                ViewBag.IsEditMode = true;
                return View("~/Views/Auth/Signup.cshtml", updatedClient);
            }

            var existingClient = await _context.Clients.FindAsync(currentClientId);
            if (existingClient == null)
            {
                return NotFound();
            }

            existingClient.Name = updatedClient.Name;
            existingClient.Phone = updatedClient.Phone;
            existingClient.Address = updatedClient.Address;
            existingClient.Email = updatedClient.Email;

            if (ClientPicture != null && ClientPicture.Length > 0)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ClientPicture.FileName);
                using (var fileStream = new FileStream(Path.Combine(uploadsFolder, uniqueFileName), FileMode.Create))
                {
                    await ClientPicture.CopyToAsync(fileStream);
                }
                existingClient.Picture = "/Images/" + uniqueFileName;
            }

            try
            {
                _context.Clients.Update(existingClient);
                await _context.SaveChangesAsync();
                return RedirectToAction("SearchWorker", "Dashboard");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Database update failed: " + ex.Message);
                ViewBag.IsEditMode = true;
                return View("~/Views/Auth/Signup.cshtml", updatedClient);
            }
        }

        // GET: /Dashboard/ActiveRequest
        [HttpGet]
        public IActionResult InterviewList(string searchTerm, string filterStatus = "All")
        {
            int? clientId = HttpContext.Session.GetInt32("ClientId");
            if (clientId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var query = from i in _context.Interviews
                        join w in _context.Workers on i.WorkerId equals w.WorkerId
                        where i.ClientId == clientId
                        select new
                        {
                            InterviewId = i.InterviewId,
                            WorkerName = w.Name,
                            WorkerDecision = i.WorkerDecision,
                            Status = i.Status,
                            WorkerImage = w.Picture,
                            SkillName = (from wc in _context.WorkerCategories
                                         join s in _context.Skills on wc.SkillsId equals s.SkillsId
                                         where wc.WorkerId == w.WorkerId
                                         select s.SkillName).FirstOrDefault()
                        };

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(x => x.WorkerName.ToLower().Contains(searchTerm) ||
                                         (x.SkillName != null && x.SkillName.ToLower().Contains(searchTerm)));
            }

            if (filterStatus == "Pending")
            {
                query = query.Where(x => x.Status == "Pending");
            }
            else if (filterStatus == "Approved")
            {
                query = query.Where(x => x.Status == "Approved");
            }

            ViewBag.SearchTerm = searchTerm;
            ViewBag.CurrentFilter = filterStatus;
            ViewBag.Interviews = query.ToList();

            return View("~/Views/Dashboard/InterviewList.cshtml");
        }

        [HttpPost]
        // [ValidateAntiForgeryToken]  <-- ISKO REMOVE / COMMENT KAR DEIN temporarily!
        public IActionResult FinalApprove([FromQuery] int interviewId) // FromQuery explicitly map karega URL se
        {
            try
            {
                var interview = _context.Interviews.FirstOrDefault(i => i.InterviewId == interviewId);

                if (interview == null)
                {
                    return Json(new { success = false, message = "Interview record nahi mila." });
                }

                // Status updating logic
                interview.Status = "Approved";

                var alreadyHired = _context.Hirings.Any(h => h.InterviewId == interviewId);
                if (!alreadyHired)
                {
                    var hiring = new Hiring
                    {
                        InterviewId = interview.InterviewId,
                        WorkerDecision = "Pending",
                        HiringDecision = "Pending",
                        HiringDate = DateTime.Now
                    };
                    _context.Hirings.Add(hiring);
                }

                _context.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                var errorMsg = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = errorMsg });
            }
        }

        [HttpPost]
        public IActionResult FinalReject(int interviewId)
        {
            var interview = _context.Interviews.FirstOrDefault(i => i.InterviewId == interviewId);
            if (interview != null)
            {
                interview.Status = "Rejected";
                _context.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult DeleteRequest(int interviewId)
        {
            var interview = _context.Interviews.FirstOrDefault(i => i.InterviewId == interviewId);
            if (interview != null)
            {
                _context.Interviews.Remove(interview);
                _context.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpGet]
        public IActionResult BookInterview(int workerId)
        {
            int? clientId = HttpContext.Session.GetInt32("ClientId");
            if (clientId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var client = _context.Clients.FirstOrDefault(c => c.ClientId == clientId);

            var interviewModel = new Interview
            {
                WorkerId = workerId,
                ClientId = clientId.Value,
                Address = client?.Address
            };

            return View("dateandtime", interviewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmInterview(Interview model, string customTime)
        {
            model.Status = "Pending";
            ModelState.Remove("customTime");

            // ================= FIXED TIME PARSING SYSTEM =================
            if (model.InterviewDate.HasValue && !string.IsNullOrEmpty(customTime))
            {
                try
                {
                    string cleanTime = customTime.Trim();

                    if (TimeSpan.TryParse(cleanTime, out TimeSpan parsedSpan))
                    {
                        model.InterviewDate = model.InterviewDate.Value.Date + parsedSpan;
                    }
                    else if (DateTime.TryParse(cleanTime, out DateTime parsedDateTime))
                    {
                        model.InterviewDate = model.InterviewDate.Value.Date
                                                          .AddHours(parsedDateTime.Hour)
                                                          .AddMinutes(parsedDateTime.Minute);
                    }
                    else
                    {
                        var parts = cleanTime.Split(' ');
                        string timePart = parts[0];
                        string amPmPart = parts.Length > 1 ? parts[1].ToUpper() : "AM";

                        var timeComponents = timePart.Split(':');
                        int hours = int.Parse(timeComponents[0]);
                        int minutes = int.Parse(timeComponents[1]);

                        if (amPmPart == "PM" && hours < 12) hours += 12;
                        else if (amPmPart == "AM" && hours == 12) hours = 0;

                        model.InterviewDate = model.InterviewDate.Value.Date.AddHours(hours).AddMinutes(minutes);
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Time process karne mein masla aya: " + ex.Message);
                    return View("dateandtime", model);
                }
            }

            try
            {
                _context.Interviews.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Interview scheduled successfully!";
                return RedirectToAction("SearchWorker", "Dashboard");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Database Error: " + ex.Message);
                return View("dateandtime", model);
            }
        }

        [HttpGet]
        public IActionResult ClientProfile()
        {
            int? clientId = HttpContext.Session.GetInt32("ClientId");
            if (clientId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var client = _context.Clients.FirstOrDefault(c => c.ClientId == clientId);
            if (client == null)
            {
                return NotFound();
            }

            // Pending interviews count
            ViewBag.PendingInterviews = _context.Interviews
                .Count(i => i.ClientId == clientId && i.Status != null && i.Status.Trim().ToLower() == "pending");

            // 🔥 UPDATED LOGIC: Ab data Hiring table ke basis par fetch hoga jahan dono decisions 'approved' hain
            var currentWorkers = (from i in _context.Interviews
                                  join h in _context.Hirings on i.InterviewId equals h.InterviewId
                                  join w in _context.Workers on i.WorkerId equals w.WorkerId
                                  where i.ClientId == clientId
                                        && h.HiringDecision != null && h.HiringDecision.Trim().ToLower() == "approved"
                                        && h.WorkerDecision != null && h.WorkerDecision.Trim().ToLower() == "accepted"
                                  select new
                                  {
                                      WorkerId = w.WorkerId,
                                      Name = w.Name,
                                      Picture = string.IsNullOrEmpty(w.Picture) ? "/Images/logo.png" : w.Picture,
                                      Address = w.Address ?? "Not Available",
                                      // Contract current status handling (e.g., if worker resigned later or currently on work)
                                      DutyStatus = i.Status != null ? i.Status.Trim() : "On Work",
                                      SkillName = (from wc in _context.WorkerCategories
                                                   join s in _context.Skills on wc.SkillsId equals s.SkillsId
                                                   where wc.WorkerId == w.WorkerId
                                                   select s.SkillName).FirstOrDefault()
                                                   ?? (_context.Experiences.Where(e => e.WorkerId == w.WorkerId).Select(e => e.ExpDetail).FirstOrDefault())
                                                   ?? "Helper"
                                  }).ToList();

            // Active screen filtering rules
            var activeList = currentWorkers
                .Where(w => w.DutyStatus.Equals("approved", StringComparison.OrdinalIgnoreCase) ||
                            w.DutyStatus.Equals("resigned", StringComparison.OrdinalIgnoreCase) ||
                            w.DutyStatus.Equals("on work", StringComparison.OrdinalIgnoreCase))
                .ToList();

            ViewBag.CurrentWorkers = activeList;
            ViewBag.ActiveWorkersCount = activeList.Count;

            return View(client);
        }

        private void LoadUserDetailsToViewBag()
        {
            var loggedInName = HttpContext.Session.GetString("UserName");
            var loggedInPic = HttpContext.Session.GetString("UserPicture");

            if (string.IsNullOrEmpty(loggedInName) || string.IsNullOrEmpty(loggedInPic))
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                var userRole = HttpContext.Session.GetString("UserRole");

                if (userId.HasValue && !string.IsNullOrEmpty(userRole))
                {
                    if (userRole == "Client")
                    {
                        var client = _context.Clients.FirstOrDefault(c => c.ClientId == userId.Value);
                        if (client != null)
                        {
                            loggedInName = client.Name;
                            loggedInPic = client.Picture;
                            HttpContext.Session.SetString("UserName", client.Name);
                            HttpContext.Session.SetString("UserPicture", string.IsNullOrEmpty(client.Picture) ? "/Images/logo.png" : client.Picture);
                        }
                    }
                    else if (userRole == "Worker")
                    {
                        var worker = _context.Workers.FirstOrDefault(w => w.WorkerId == userId.Value);
                        if (worker != null)
                        {
                            loggedInName = worker.Name;
                            loggedInPic = worker.Picture;
                            HttpContext.Session.SetString("UserName", worker.Name);
                            HttpContext.Session.SetString("UserPicture", string.IsNullOrEmpty(worker.Picture) ? "/Images/logo.png" : worker.Picture);
                        }
                    }
                }
            }

            ViewBag.ClientName = !string.IsNullOrEmpty(loggedInName) ? loggedInName : "Guest";
            ViewBag.ClientPicture = !string.IsNullOrEmpty(loggedInPic) ? loggedInPic : "/Images/logo.png";
        }

        [HttpGet]
        public IActionResult SearchWorker(string category, string searchName, string gender, string city, List<string> subCategories)
        {
            LoadUserDetailsToViewBag();

            // ================= LIVE DATABASE COUNTER INJECTION =================
            int? sessionWorkerId = HttpContext.Session.GetInt32("UserId");
            string sessionUserRole = HttpContext.Session.GetString("UserRole");

            // Agar logged-in banda Client hai, toh hum ClientId use karenge session se counter filter ke liye
            int? sessionClientId = HttpContext.Session.GetInt32("ClientId");

            if (sessionWorkerId.HasValue && sessionUserRole == "Worker")
            {
                // Aapke screenshot ke mutabiq database mein status 'Approved' hai, isliye hum Pending aur Approved dono count kar rahe hain
                ViewBag.InterviewCount = _context.Interviews
                    .Count(i => i.WorkerId == sessionWorkerId.Value && (i.Status == "Pending" || i.Status == "Approved"));

                ViewBag.JobNotificationCount = _context.Hirings
                    .Join(_context.Interviews,
                          h => h.InterviewId,
                          i => i.InterviewId,
                          (h, i) => new { HiringRecord = h, InterviewRecord = i })
                    .Count(joined => joined.InterviewRecord.WorkerId == sessionWorkerId.Value &&
                                     (joined.HiringRecord.WorkerDecision == "Pending" || joined.HiringRecord.WorkerDecision == "Approved"));
            }
            else if (sessionClientId.HasValue && sessionUserRole == "Client")
            {
                // Agar Client dashboard par dekh raha hai, toh uske specific counts yahan handle honge
                ViewBag.InterviewCount = _context.Interviews
                    .Count(i => i.ClientId == sessionClientId.Value && (i.Status == "Pending" || i.Status == "Approved"));

                ViewBag.JobNotificationCount = _context.Hirings
                    .Join(_context.Interviews,
                          h => h.InterviewId,
                          i => i.InterviewId,
                          (h, i) => new { HiringRecord = h, InterviewRecord = i })
                    .Count(joined => joined.InterviewRecord.ClientId == sessionClientId.Value &&
                                     (joined.HiringRecord.HiringDecision == "Pending" || joined.HiringRecord.HiringDecision == "Approved"));
            }
            else
            {
                ViewBag.InterviewCount = 0;
                ViewBag.JobNotificationCount = 0;
            }

            var workerQuery = _context.Workers.Where(w => w.AvailableStatus == true || w.AvailableStatus == null).AsQueryable();

            if (!string.IsNullOrEmpty(gender))
            {
                workerQuery = workerQuery.Where(w => w.Gender != null && w.Gender.ToLower() == gender.ToLower());
            }

            if (!string.IsNullOrEmpty(city))
            {
                workerQuery = workerQuery.Where(w => w.Address != null && w.Address.Contains(city));
            }

            if (!string.IsNullOrEmpty(searchName))
            {
                workerQuery = workerQuery.Where(w => w.Name != null && w.Name.Contains(searchName));
            }

            var activeWorkers = workerQuery.ToList();
            var workerCategories = _context.WorkerCategories.ToList();
            var categoriesList = _context.Categories.ToList();
            var skillsList = _context.Skills.ToList();

            List<Worker> filteredWorkers = new List<Worker>();
            Dictionary<int, List<string>> workerSkillsMap = new Dictionary<int, List<string>>();
            Dictionary<int, double> workerRatingsMap = new Dictionary<int, double>();

            foreach (var worker in activeWorkers)
            {
                var matchingBridges = workerCategories.Where(wc => wc.WorkerId == worker.WorkerId).ToList();

                var actualSkillNames = (from wc in matchingBridges
                                        join s in skillsList on wc.SkillsId equals s.SkillsId
                                        select s.SkillName).ToList();

                var actualCategoryNames = (from wc in matchingBridges
                                           join c in categoriesList on wc.CategoryId equals c.CategoryId
                                           select c.CategoryName).Distinct().ToList();

                if (!actualCategoryNames.Any())
                {
                    actualCategoryNames.Add("General Worker");
                }

                bool matchesCategory = true;
                if (!string.IsNullOrEmpty(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    matchesCategory = actualCategoryNames.Any(cName =>
                        cName.Replace(" ", "").Equals(category.Replace(" ", ""), StringComparison.OrdinalIgnoreCase) ||
                        (cName.Contains("Cook", StringComparison.OrdinalIgnoreCase) && category.Contains("Cook", StringComparison.OrdinalIgnoreCase)) ||
                        ((cName.Contains("Maid", StringComparison.OrdinalIgnoreCase) || cName.Contains("Clean", StringComparison.OrdinalIgnoreCase)) && category.Contains("Clean", StringComparison.OrdinalIgnoreCase)) ||
                        (cName.Contains("Driver", StringComparison.OrdinalIgnoreCase) && category.Contains("Driv", StringComparison.OrdinalIgnoreCase)));
                }

                bool matchesSubCategory = true;
                if (subCategories != null && subCategories.Any())
                {
                    var specificRequestedSkills = subCategories.Where(sc => !sc.EndsWith("All")).ToList();

                    if (specificRequestedSkills.Any())
                    {
                        matchesSubCategory = actualSkillNames.Any(sName =>
                            specificRequestedSkills.Contains(sName, StringComparer.OrdinalIgnoreCase));
                    }
                }

                if (matchesCategory && matchesSubCategory)
                {
                    filteredWorkers.Add(worker);
                    workerSkillsMap[worker.WorkerId] = actualCategoryNames;

                    var workerReviews = (from r in _context.Reviews
                                         join i in _context.Interviews on r.InterviewId equals i.InterviewId
                                         where i.WorkerId == worker.WorkerId && r.Rating != null
                                         select (double)r.Rating.Value).ToList();

                    double averageRating = workerReviews.Any() ? Math.Round(workerReviews.Average(), 1) : 0.0;
                    workerRatingsMap[worker.WorkerId] = averageRating;
                }
            }

            if (!filteredWorkers.Any() && (string.IsNullOrEmpty(category) || category.Equals("All", StringComparison.OrdinalIgnoreCase)) && string.IsNullOrEmpty(searchName))
            {
                foreach (var w in activeWorkers)
                {
                    filteredWorkers.Add(w);
                    if (!workerSkillsMap.ContainsKey(w.WorkerId))
                        workerSkillsMap[w.WorkerId] = new List<string> { "Professional Helper" };

                    if (!workerRatingsMap.ContainsKey(w.WorkerId))
                    {
                        var fallbackReviews = (from r in _context.Reviews
                                               join i in _context.Interviews on r.InterviewId equals i.InterviewId
                                               where i.WorkerId == w.WorkerId && r.Rating != null
                                               select (double)r.Rating.Value).ToList();

                        workerRatingsMap[w.WorkerId] = fallbackReviews.Any() ? Math.Round(fallbackReviews.Average(), 1) : 0.0;
                    }
                }
            }

            // ================= NEW ADDED CODE START =================
            // ... (Upar ka saara code bilkul theek chal raha hai)

            // ================= CURRENT CLIENT INTERVIEW STATUS LOGIC =================
            var workerInterviewStatusMap = new Dictionary<int, string>();
            if (sessionClientId.HasValue)
            {
                var clientInterviews = _context.Interviews
                    .Where(i => i.ClientId == sessionClientId.Value)
                    .ToList();

                foreach (var interview in clientInterviews)
                {
                    // Is if-condition ko check karein ke brackets sahi open aur close hain
                    if (interview.WorkerId.HasValue)
                    {
                        if (interview.Status != null && interview.Status.Trim().Equals("Approved", StringComparison.OrdinalIgnoreCase))
                        {
                            var isHired = _context.Hirings.Any(h => h.InterviewId == interview.InterviewId);
                            if (isHired)
                            {
                                workerInterviewStatusMap[interview.WorkerId.Value] = "Hired";
                            }
                            else
                            {
                                workerInterviewStatusMap[interview.WorkerId.Value] = "Approved";
                            }
                        }
                        else if (interview.Status != null && interview.Status.Trim().Equals("Pending", StringComparison.OrdinalIgnoreCase))
                        {
                            workerInterviewStatusMap[interview.WorkerId.Value] = "Pending";
                        }
                        else
                        {
                            workerInterviewStatusMap[interview.WorkerId.Value] = interview.Status ?? "";
                        }
                    } // <--- Yeh bracket interview.WorkerId.HasValue ka band ho raha hai
                } // <--- Yeh bracket foreach loop ka band ho raha hai
            } // <--- Yeh bracket if (sessionClientId.HasValue) ka band ho raha hai

            ViewBag.WorkerInterviewStatusMap = workerInterviewStatusMap;

            ViewBag.WorkerSkillsMap = workerSkillsMap;
            ViewBag.WorkerRatingsMap = workerRatingsMap;
            ViewBag.ActiveCategory = string.IsNullOrEmpty(category) ? "All" : category;
            ViewBag.SearchQuery = searchName;
            ViewBag.SelectedGender = gender;
            ViewBag.SelectedCity = city;
            ViewBag.SelectedCategories = !string.IsNullOrEmpty(category) ? new List<string> { category } : new List<string>();
            ViewBag.SelectedSubs = subCategories ?? new List<string>();

            ViewBag.Categories = categoriesList;
            ViewBag.Skills = skillsList;
            ViewBag.Cities = _context.Workers
                                   .Select(w => w.Address)
                                   .ToList()
                                   .Where(a => !string.IsNullOrEmpty(a))
                                   .Distinct()
                                   .ToList();

            return View(filteredWorkers); // <--- Yeh line is bracket se pehle honi chahiye!
        } // <--- Yeh SearchWorker method ka aakhri closed bracket hai
    }
}