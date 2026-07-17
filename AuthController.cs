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
            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.Skills = _context.Skills.ToList();
            return View();
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

            // Client Login Logic
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
            // Worker Login Logic
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
                    // Session keys set karna jo worker dashboard ke liye zaroori hain
                    HttpContext.Session.SetString("UserRole", "Worker");
                    HttpContext.Session.SetInt32("UserId", worker.WorkerId);
                    HttpContext.Session.SetInt32("WorkerId", worker.WorkerId); // 🔥 Yeh line dashboard error se bachayegi

                    // 🔥 Worker Dashboard controller aur uske action par redirect
                    return RedirectToAction("WorkerDashboard", "WorkerDashboard");
                }
                else
                {
                    ModelState.AddModelError("", "Database Error: Worker Password does not match.");
                    return View();
                }
            }

            // Fallback error
            ModelState.AddModelError("", "Invalid Role Selected.");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadTemporaryPicture(IFormFile file)
        {
            if (file == null || file.Length == 0) return Json(new { success = false });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var permittedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            if (!permittedExtensions.Contains(ext)) return Json(new { success = false });

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

                var newClient = new Client
                {
                    Name = ClientName,
                    Email = ClientEmail,
                    Phone = ClientPhone,
                    Address = ClientAddress,
                    Password = ClientPassword
                };

                if (ClientPicture != null && ClientPicture.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ClientPicture.FileName);
                    using (var fileStream = new FileStream(Path.Combine(uploadsFolder, uniqueFileName), FileMode.Create))
                    {
                        await ClientPicture.CopyToAsync(fileStream);
                    }
                    newClient.Picture = "/Images/" + uniqueFileName;
                }
                else
                {
                    newClient.Picture = "/Images/default-avatar.png";
                }

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

                if (_context.Workers.Any(w => w.Cnic == WorkerCNIC))
                {
                    ModelState.AddModelError("", "This CNIC is already registered.");
                    return View("Signup");
                }

                var newWorker = new Worker
                {
                    Name = WorkerName,
                    Age = WorkerAge,
                    Phone = WorkerPhone,
                    Cnic = WorkerCNIC,
                    Salary = WorkerSalary,
                    Gender = WorkerGender,
                    Address = WorkerAddress,
                    Password = WorkerPassword,
                    AvailableStatus = true,
                    Bio = "Professional Servant / Maid",
                    Number = "1"
                };

                if (WorkerPicture != null && WorkerPicture.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(WorkerPicture.FileName);
                    using (var fileStream = new FileStream(Path.Combine(uploadsFolder, uniqueFileName), FileMode.Create))
                    {
                        await WorkerPicture.CopyToAsync(fileStream);
                    }
                    newWorker.Picture = "/Images/" + uniqueFileName;
                }
                else if (!string.IsNullOrEmpty(TempPicturePath))
                {
                    string sourcePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", TempPicturePath.TrimStart('/'));
                    if (System.IO.File.Exists(sourcePath))
                    {
                        string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(sourcePath);
                        string destFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images");
                        if (!Directory.Exists(destFolder)) Directory.CreateDirectory(destFolder);

                        string destPath = Path.Combine(destFolder, uniqueFileName);
                        System.IO.File.Move(sourcePath, destPath);
                        newWorker.Picture = "/Images/" + uniqueFileName;
                    }
                }
                else
                {
                    newWorker.Picture = "/Images/default-avatar.png";
                }

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
                                        if (expertise.Slots != null)
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
                                        else
                                        {
                                            _context.Database.ExecuteSqlRaw(
                                                "INSERT INTO Worker_Category (Worker_id, Category_id, Skills_Id) VALUES ({0}, {1}, NULL)",
                                                newWorkerId, catId
                                            );
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