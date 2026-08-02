using Microsoft.AspNetCore.Mvc;
using MaidAndServantt.Models;
using Microsoft.EntityFrameworkCore;
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

        // ================= GET: Dashboard/Login =================
        [HttpGet]
        public IActionResult Login()
        {
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

        // ================= GET: Dashboard/InterviewList =================
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
        public IActionResult FinalApprove([FromQuery] int interviewId)
        {
            try
            {
                var interview = _context.Interviews.FirstOrDefault(i => i.InterviewId == interviewId);
                if (interview == null)
                {
                    return Json(new { success = false, message = "Interview record nahi mila." });
                }

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

            ViewBag.PendingInterviews = _context.Interviews
                .Count(i => i.ClientId == clientId && i.Status != null && i.Status.Trim().ToLower() == "pending");

            var currentWorkers = (from i in _context.Interviews
                                  join h in _context.Hirings on i.InterviewId equals h.InterviewId
                                  join w in _context.Workers on i.WorkerId equals w.WorkerId
                                  // 🔥 FIX: Resignations Table Join Karein
                                  join r in _context.Resignations on i.InterviewId equals r.InterviewId into rGroup
                                  from r in rGroup.DefaultIfEmpty()
                                  where i.ClientId == clientId
                                        && h.HiringDecision != null && h.HiringDecision.Trim().ToLower() == "approved"
                                        && h.WorkerDecision != null && h.WorkerDecision.Trim().ToLower() == "accepted"
                                  select new
                                  {
                                      WorkerId = w.WorkerId,
                                      Name = w.Name,
                                      Picture = string.IsNullOrEmpty(w.Picture) ? "/Images/logo.png" : w.Picture,
                                      Address = w.Address ?? "Not Available",
                                      DutyStatus = i.Status != null ? i.Status.Trim() : "On Work",
                                      // 🔥 FIX: Actual ResignationId Select Karein
                                      ResignationId = r != null ? r.ResignationId : 0,
                                      SkillName = (from wc in _context.WorkerCategories
                                                   join s in _context.Skills on wc.SkillsId equals s.SkillsId
                                                   where wc.WorkerId == w.WorkerId
                                                   select s.SkillName).FirstOrDefault()
                                                   ?? (_context.Experiences.Where(e => e.WorkerId == w.WorkerId).Select(e => e.ExpDetail).FirstOrDefault())
                                                   ?? "Helper"
                                  }).ToList();

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

        // ================= GET: Dashboard/SearchWorker =================
        // React Native `GetWorkersForClient` logic ke mutabiq implemented
        [HttpGet]
        public async Task<IActionResult> SearchWorker(
            [FromQuery] List<string>? categories = null,
            [FromQuery] string? category = null,
            [FromQuery] string? searchName = null,
            [FromQuery] string? gender = null,
            [FromQuery] string? city = null,
            [FromQuery] List<string>? subCategories = null)
        {
            LoadUserDetailsToViewBag();

            int? sessionClientId = HttpContext.Session.GetInt32("ClientId");

            // Base Query: Fetch active/available workers
            IQueryable<Worker> query = _context.Workers;

            // 1. Categories Filter (Supports category parameter or categories list)
            var targetCategories = new List<string>();
            if (categories != null && categories.Any())
            {
                targetCategories.AddRange(categories);
            }
            if (!string.IsNullOrEmpty(category) && !targetCategories.Contains(category))
            {
                targetCategories.Add(category);
            }

            if (targetCategories.Any() && !targetCategories.Contains("All", StringComparer.OrdinalIgnoreCase))
            {
                var normalizedCategories = targetCategories
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Select(c => c.Trim())
                    .ToList();

                var categoryIds = normalizedCategories
                    .Where(c => int.TryParse(c, out _))
                    .Select(int.Parse)
                    .ToList();

                var categoryNames = normalizedCategories
                    .Where(c => !int.TryParse(c, out _))
                    .ToList();

                query = query.Where(w => _context.WorkerCategories
                    .Any(wc => wc.WorkerId == w.WorkerId && _context.Categories
                        .Any(c => c.CategoryId == wc.CategoryId &&
                            (categoryIds.Contains(c.CategoryId) || categoryNames.Contains(c.CategoryName)))));
            }

            // 2. Gender Filter
            if (!string.IsNullOrEmpty(gender) && !gender.Equals("Both", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(w => w.Gender == gender);
            }

            // 3. City Filter
            if (!string.IsNullOrEmpty(city))
            {
                query = query.Where(w => w.Address != null && w.Address.Contains(city));
            }

            // 4. Search Name Filter
            if (!string.IsNullOrEmpty(searchName))
            {
                query = query.Where(w => w.Name != null && w.Name.Contains(searchName));
            }

            // 5. Sub-skills (AND Logic - Worker must possess ALL selected sub-skills)
            if (subCategories != null && subCategories.Any())
            {
                foreach (var skillName in subCategories)
                {
                    query = query.Where(w => _context.WorkerCategories
                        .Any(wc => wc.WorkerId == w.WorkerId && _context.Skills
                            .Any(s => s.SkillsId == wc.SkillsId && s.SkillName == skillName)));
                }
            }

            var workerList = await query.ToListAsync();

            // Setup mappings for ViewBag (View Rendering)
            var workerSkillsMap = new Dictionary<int, List<string>>();
            var workerRatingsMap = new Dictionary<int, double>();

            foreach (var w in workerList)
            {
                // Fetch Average Rating dynamically
                var ratings = await (from r in _context.Reviews
                                     join i in _context.Interviews on r.InterviewId equals i.InterviewId
                                     where i.WorkerId == w.WorkerId && r.Rating.HasValue
                                     select (double)r.Rating!.Value).ToListAsync();

                double avgRating = ratings.Any() ? Math.Round(ratings.Average(), 1) : 0.0;
                workerRatingsMap[w.WorkerId] = avgRating;

                // Fetch Categories / Skills
                var catNames = await _context.WorkerCategories
                    .Where(wc => wc.WorkerId == w.WorkerId)
                    .Join(_context.Categories, wc => wc.CategoryId, c => c.CategoryId, (wc, c) => c.CategoryName)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Distinct()
                    .ToListAsync();

                workerSkillsMap[w.WorkerId] = catNames.Any() ? catNames : new List<string> { "General Worker" };
            }

            // Interview Status Map for current logged in Client
            var workerInterviewStatusMap = new Dictionary<int, string>();
            if (sessionClientId.HasValue)
            {
                var clientInterviews = await _context.Interviews
                    .Where(i => i.ClientId == sessionClientId.Value)
                    .ToListAsync();

                foreach (var interview in clientInterviews)
                {
                    if (interview.WorkerId.HasValue)
                    {
                        if (interview.Status != null && interview.Status.Trim().Equals("Approved", StringComparison.OrdinalIgnoreCase))
                        {
                            var isHired = await _context.Hirings.AnyAsync(h => h.InterviewId == interview.InterviewId);
                            workerInterviewStatusMap[interview.WorkerId.Value] = isHired ? "Hired" : "Approved";
                        }
                        else if (interview.Status != null && interview.Status.Trim().Equals("Pending", StringComparison.OrdinalIgnoreCase))
                        {
                            workerInterviewStatusMap[interview.WorkerId.Value] = "Pending";
                        }
                        else
                        {
                            workerInterviewStatusMap[interview.WorkerId.Value] = interview.Status ?? "";
                        }
                    }
                }
            }

            // Pass Data to View via ViewBag
            ViewBag.WorkerInterviewStatusMap = workerInterviewStatusMap;
            ViewBag.WorkerSkillsMap = workerSkillsMap;
            ViewBag.WorkerRatingsMap = workerRatingsMap;
            ViewBag.ActiveCategory = !string.IsNullOrEmpty(category) ? category : "All";
            ViewBag.SearchQuery = searchName;
            ViewBag.SelectedGender = gender;
            ViewBag.SelectedCity = city;

            return View(workerList);
        }
    }
}