using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using MaidAndServantt.Models;
using Microsoft.EntityFrameworkCore;

namespace MaidAndServantt.Controllers
{
    // ================= DATA TRANSFER OBJECTS (DTOs) =================
    public class ExperienceSlotDto
    {
        public string? WorkingSince { get; set; }
        public string? WorkedAt { get; set; }
        public string? Description { get; set; }
    }

    public class SkillExperienceDto
    {
        public string? CategoryId { get; set; }
        public List<string>? SubSkills { get; set; } = new List<string>();
        public List<ExperienceSlotDto>? Slots { get; set; } = new List<ExperienceSlotDto>();
    }

    public class WorkerSkillsSubmitDto
    {
        public string? PrimaryCategory { get; set; }
        public string? SecondaryCategory { get; set; }
        public List<SkillExperienceDto>? ExpertiseDetails { get; set; } = new List<SkillExperienceDto>();
    }

    // ================= AUTH CONTROLLER =================
    public class AuthController : Controller
    {
        private readonly FypContext _context;

        public AuthController(FypContext context)
        {
            _context = context;
        }

        // ================= GET ACTIONS =================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Signup()
        {
            return View();
        }

        [HttpGet]
        public IActionResult AddSkills()
        {
            int? workerId = HttpContext.Session.GetInt32("WorkerId");

            ViewBag.PrimaryCategoryId = 0;
            ViewBag.SecondaryCategoryId = 0;
            ViewBag.SavedSkillIds = new List<int>();

            if (workerId != null)
            {
                var existingExperience = _context.Experiences.FirstOrDefault(e => e.WorkerId == workerId);
                if (existingExperience != null)
                {
                    ViewBag.WorkedAt = existingExperience.WorkAt;
                    ViewBag.ExpDetail = existingExperience.ExpDetail;
                    ViewBag.Duration = existingExperience.Duration;
                }

                var savedCategories = _context.WorkerCategories
                                              .Where(wc => wc.WorkerId == workerId)
                                              .Select(wc => wc.CategoryId)
                                              .Distinct()
                                              .ToList();

                if (savedCategories.Count > 0) ViewBag.PrimaryCategoryId = savedCategories[0];
                if (savedCategories.Count > 1) ViewBag.SecondaryCategoryId = savedCategories[1];

                var savedSkills = _context.WorkerCategories
                                    .Where(wc => wc.WorkerId == workerId && wc.SkillsId != null)
                                    .Select(wc => wc.SkillsId.Value)
                                    .ToList();

                ViewBag.SavedSkillIds = savedSkills;
            }

            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.Skills = _context.Skills.ToList();
            return View();
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }

        // ================= POST ACTIONS =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string LoginIdentifier, string Password, string Role, bool RememberMe = false)
        {
            if (string.IsNullOrEmpty(LoginIdentifier) || string.IsNullOrEmpty(Password) || string.IsNullOrEmpty(Role))
            {
                ModelState.AddModelError("", "All fields are required.");
                return View();
            }

            if (Role == "Client")
            {
                var emailExists = _context.Clients.Any(c => c.Email == LoginIdentifier);
                if (!emailExists)
                {
                    ModelState.AddModelError("", $"Database Error: Client with Email '{LoginIdentifier}' not found.");
                    return View();
                }

                var client = _context.Clients.FirstOrDefault(c => c.Email == LoginIdentifier && c.Password == Password);
                if (client != null)
                {
                    HttpContext.Session.SetString("UserRole", "Client");
                    HttpContext.Session.SetInt32("UserId", client.ClientId);
                    HttpContext.Session.SetInt32("ClientId", client.ClientId);

                    return RedirectToAction("SearchWorker", "Dashboard");
                }
                else
                {
                    ModelState.AddModelError("", "Database Error: Password does not match in database.");
                    return View();
                }
            }
            else if (Role == "Worker")
            {
                string cleanCnic = LoginIdentifier.Replace("-", "").Trim();

                var cnicExists = _context.Workers.Any(w => w.Cnic == cleanCnic);
                if (!cnicExists)
                {
                    ModelState.AddModelError("", $"Database Error: Worker with CNIC '{cleanCnic}' not found.");
                    return View();
                }

                var worker = _context.Workers.FirstOrDefault(w => w.Cnic == cleanCnic && w.Password == Password);
                if (worker != null)
                {
                    HttpContext.Session.SetString("UserRole", "Worker");
                    HttpContext.Session.SetInt32("UserId", worker.WorkerId);
                    HttpContext.Session.SetInt32("WorkerId", worker.WorkerId);

                    return RedirectToAction("WorkerDashboard", "WorkerDashboard");
                }
                else
                {
                    ModelState.AddModelError("", "Database Error: Worker Password does not match.");
                    return View();
                }
            }

            ModelState.AddModelError("", "Invalid Role Selected.");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadTemporaryPicture(IFormFile file)
        {
            if (file == null || file.Length == 0) return Json(new { success = false, message = "No file uploaded" });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var permittedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            if (!permittedExtensions.Contains(ext)) return Json(new { success = false, message = "Invalid image extension" });

            string tempFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "Temp");
            if (!Directory.Exists(tempFolder)) Directory.CreateDirectory(tempFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + ext;
            string filePath = Path.Combine(tempFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return Json(new { success = true, filePath = "/Images/Temp/" + uniqueFileName });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveWorkerSkills([FromBody] WorkerSkillsSubmitDto skillsData)
        {
            int? workerId = HttpContext.Session.GetInt32("WorkerId");
            if (workerId == null)
            {
                return Json(new { success = false, message = "Session expired. Please log in again." });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var existingWorkerCats = _context.WorkerCategories.Where(wc => wc.WorkerId == workerId);
                    _context.WorkerCategories.RemoveRange(existingWorkerCats);

                    var existingExp = _context.Experiences.Where(e => e.WorkerId == workerId);
                    _context.Experiences.RemoveRange(existingExp);

                    await _context.SaveChangesAsync();

                    if (skillsData != null && skillsData.ExpertiseDetails != null)
                    {
                        foreach (var expertise in skillsData.ExpertiseDetails)
                        {
                            if (int.TryParse(expertise.CategoryId, out int catId))
                            {
                                if (expertise.Slots != null && expertise.Slots.Count > 0)
                                {
                                    foreach (var slot in expertise.Slots)
                                    {
                                        _context.Experiences.Add(new Experience
                                        {
                                            WorkerId = workerId.Value,
                                            WorkAt = slot.WorkedAt,
                                            ExpDetail = slot.Description,
                                            Duration = slot.WorkingSince
                                        });
                                    }
                                }

                                if (expertise.SubSkills != null && expertise.SubSkills.Count > 0)
                                {
                                    foreach (var subSkillStr in expertise.SubSkills)
                                    {
                                        if (int.TryParse(subSkillStr, out int sId))
                                        {
                                            _context.Database.ExecuteSqlRaw(
                                                "INSERT INTO Worker_Category (Worker_id, Category_id, Skills_Id) VALUES ({0}, {1}, {2})",
                                                workerId.Value, catId, sId
                                            );
                                        }
                                    }
                                }
                                else
                                {
                                    _context.Database.ExecuteSqlRaw(
                                        "INSERT INTO Worker_Category (Worker_id, Category_id, Skills_Id) VALUES ({0}, {1}, NULL)",
                                        workerId.Value, catId
                                    );
                                }
                            }
                        }
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                    return Json(new { success = true, redirectUrl = Url.Action("WorkerDashboard", "WorkerDashboard") });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Error saving skills: " + ex.Message });
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            string Role,
            string ClientName, string ClientEmail, string ClientPhone, string ClientAddress, string ClientPassword, string ClientConfirmPassword, IFormFile ClientPicture,
            string WorkerName, int? WorkerAge, string WorkerPhone, string WorkerCNIC, decimal? WorkerSalary, string WorkerGender, string WorkerEmail, string WorkerAddress, string WorkerPassword, string WorkerConfirmPassword, IFormFile WorkerPicture,
            string SessionSkillsData,
            string TempPicturePath)
        {
            // ================= CLIENT REGISTRATION LOGIC =================
            if (Role == "Client")
            {
                if (ClientPassword != ClientConfirmPassword)
                {
                    ModelState.AddModelError("", "Client Password and Confirm Password do not match.");
                    return View("Signup");
                }

                if (_context.Clients.Any(c => c.Email == ClientEmail))
                {
                    ModelState.AddModelError("", "This Email is already registered as a Client.");
                    return View("Signup");
                }

                string clientPicturePath = string.Empty;

                if (ClientPicture != null && ClientPicture.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "Clients");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ClientPicture.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await ClientPicture.CopyToAsync(fileStream);
                    }

                    clientPicturePath = "/Images/Clients/" + uniqueFileName;
                }
                else if (!string.IsNullOrEmpty(TempPicturePath))
                {
                    string relativePath = TempPicturePath.TrimStart('~', '/').Replace("/", Path.DirectorySeparatorChar.ToString());
                    string sourcePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);

                    if (System.IO.File.Exists(sourcePath))
                    {
                        string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(sourcePath);
                        string destFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "Clients");
                        if (!Directory.Exists(destFolder)) Directory.CreateDirectory(destFolder);

                        string destPath = Path.Combine(destFolder, uniqueFileName);
                        System.IO.File.Move(sourcePath, destPath);

                        clientPicturePath = "/Images/Clients/" + uniqueFileName;
                    }
                }

                if (string.IsNullOrEmpty(clientPicturePath))
                {
                    clientPicturePath = "/Images/default-avatar.png";
                }

                var newClient = new Client
                {
                    Name = ClientName,
                    Email = ClientEmail,
                    Phone = ClientPhone,
                    Address = ClientAddress,
                    Password = ClientPassword,
                    Picture = clientPicturePath
                };

                try
                {
                    _context.Clients.Add(newClient);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Login", "Auth");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Failed to register Client: " + ex.Message);
                    return View("Signup");
                }
            }
            // ================= WORKER REGISTRATION LOGIC =================
            else if (Role == "Worker")
            {
                if (WorkerPassword != WorkerConfirmPassword)
                {
                    ModelState.AddModelError("", "Worker Password and Confirm Password do not match.");
                    return View("Signup");
                }

                string cleanCnic = WorkerCNIC?.Replace("-", "").Trim() ?? "";

                if (_context.Workers.Any(w => w.Cnic == cleanCnic))
                {
                    ModelState.AddModelError("", "This CNIC is already registered.");
                    return View("Signup");
                }

                string workerPicturePath = string.Empty;

                if (WorkerPicture != null && WorkerPicture.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "Workers");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(WorkerPicture.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await WorkerPicture.CopyToAsync(fileStream);
                    }
                    workerPicturePath = "/Images/Workers/" + uniqueFileName;
                }
                else if (!string.IsNullOrEmpty(TempPicturePath))
                {
                    string relativePath = TempPicturePath.TrimStart('~', '/').Replace("/", Path.DirectorySeparatorChar.ToString());
                    string sourcePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);

                    if (System.IO.File.Exists(sourcePath))
                    {
                        string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(sourcePath);
                        string destFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "Workers");
                        if (!Directory.Exists(destFolder)) Directory.CreateDirectory(destFolder);

                        string destPath = Path.Combine(destFolder, uniqueFileName);
                        System.IO.File.Move(sourcePath, destPath);
                        workerPicturePath = "/Images/Workers/" + uniqueFileName;
                    }
                }

                if (string.IsNullOrEmpty(workerPicturePath))
                {
                    workerPicturePath = "/Images/default-avatar.png";
                }

                var newWorker = new Worker
                {
                    Name = WorkerName,
                    Age = WorkerAge,
                    Phone = WorkerPhone,
                    Cnic = cleanCnic,
                    Salary = WorkerSalary,
                    Gender = WorkerGender,
                    Address = WorkerAddress,
                    Password = WorkerPassword,
                    AvailableStatus = true,
                    Picture = workerPicturePath,
                    Bio = "Professional Servant / Maid",
                    Number = "1"
                };

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        _context.Workers.Add(newWorker);
                        await _context.SaveChangesAsync();

                        int newWorkerId = newWorker.WorkerId;

                        if (!string.IsNullOrEmpty(SessionSkillsData))
                        {
                            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            var skillsData = JsonSerializer.Deserialize<WorkerSkillsSubmitDto>(SessionSkillsData, options);

                            if (skillsData != null && skillsData.ExpertiseDetails != null)
                            {
                                foreach (var expertise in skillsData.ExpertiseDetails)
                                {
                                    if (int.TryParse(expertise.CategoryId, out int catId))
                                    {
                                        if (expertise.Slots != null && expertise.Slots.Count > 0)
                                        {
                                            foreach (var slot in expertise.Slots)
                                            {
                                                _context.Experiences.Add(new Experience
                                                {
                                                    WorkerId = newWorkerId,
                                                    WorkAt = slot.WorkedAt,
                                                    ExpDetail = slot.Description,
                                                    Duration = slot.WorkingSince
                                                });
                                            }
                                        }

                                        if (expertise.SubSkills != null && expertise.SubSkills.Count > 0)
                                        {
                                            foreach (var subSkillStr in expertise.SubSkills)
                                            {
                                                if (int.TryParse(subSkillStr, out int sId))
                                                {
                                                    _context.Database.ExecuteSqlRaw(
                                                        "INSERT INTO Worker_Category (Worker_id, Category_id, Skills_Id) VALUES ({0}, {1}, {2})",
                                                        newWorkerId, catId, sId
                                                    );
                                                }
                                            }
                                        }
                                        else
                                        {
                                            _context.Database.ExecuteSqlRaw(
                                                "INSERT INTO Worker_Category (Worker_id, Category_id, Skills_Id) VALUES ({0}, {1}, NULL)",
                                                newWorkerId, catId
                                            );
                                        }
                                    }
                                }
                                await _context.SaveChangesAsync();
                            }
                        }

                        await transaction.CommitAsync();
                        return RedirectToAction("Login", "Auth");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", "Database transaction failed: " + ex.Message);
                        return View("Signup");
                    }
                }
            }
            return View("Signup");
        }
    }
}