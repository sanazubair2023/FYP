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

    public class ReactExperienceDto
    {
        public int? CategoryId { get; set; }
        public int? SkillsId { get; set; }
        public string? WorkAt { get; set; }
        public string? ExpDetail { get; set; }
        public string? Duration { get; set; }
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
        public IActionResult GetCategories()
        {
            var categories = _context.Categories
                .Select(c => new
                {
                    categoryId = c.CategoryId,
                    categoryName = c.CategoryName
                })
                .ToList();

            return Json(categories);
        }

        [HttpGet]
        public IActionResult GetSkillsByCategory(int categoryId)
        {
            var skills = _context.Skills
                .Where(s => s.CategoryId == categoryId)
                .Select(s => new
                {
                    skillsId = s.SkillsId,
                    skillName = s.SkillName
                })
                .ToList();

            return Json(skills);
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
        public async Task<IActionResult> SaveWorkerSkills([FromBody] JsonElement rawSkillsPayload)
        {
            int? workerId = HttpContext.Session.GetInt32("WorkerId");
            string jsonString = rawSkillsPayload.GetRawText();

            // Store JSON in Session so Signup page can retrieve it during new worker registration
            HttpContext.Session.SetString("TempWorkerSkillsJson", jsonString);

            // If user is NOT logged in (i.e. currently registering on Signup page), signal JS to return back to Signup
            if (workerId == null)
            {
                return Json(new { success = true, isSignupFlow = true, jsonPayload = jsonString });
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

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    bool parsedSuccessfully = false;

                    // Try React Native / Flat Array List Format
                    try
                    {
                        var reactSkills = JsonSerializer.Deserialize<List<ReactExperienceDto>>(jsonString, options);
                        if (reactSkills != null && reactSkills.Count > 0)
                        {
                            var uniqueCategorySkills = new HashSet<(int, int?)>();

                            foreach (var exp in reactSkills)
                            {
                                if (exp.CategoryId.HasValue)
                                {
                                    if (!string.IsNullOrEmpty(exp.WorkAt) || !string.IsNullOrEmpty(exp.ExpDetail) || !string.IsNullOrEmpty(exp.Duration))
                                    {
                                        _context.Experiences.Add(new Experience
                                        {
                                            WorkerId = workerId.Value,
                                            WorkAt = exp.WorkAt,
                                            ExpDetail = exp.ExpDetail,
                                            Duration = exp.Duration
                                        });
                                    }

                                    if (!uniqueCategorySkills.Contains((exp.CategoryId.Value, exp.SkillsId)))
                                    {
                                        uniqueCategorySkills.Add((exp.CategoryId.Value, exp.SkillsId));
                                        _context.WorkerCategories.Add(new WorkerCategory
                                        {
                                            WorkerId = workerId.Value,
                                            CategoryId = exp.CategoryId.Value,
                                            SkillsId = exp.SkillsId
                                        });
                                    }
                                }
                            }
                            parsedSuccessfully = true;
                        }
                    }
                    catch { }

                    // Option B: Web Structured Object Payload Fallback
                    if (!parsedSuccessfully)
                    {
                        try
                        {
                            var skillsData = JsonSerializer.Deserialize<WorkerSkillsSubmitDto>(jsonString, options);
                            if (skillsData != null && (skillsData.PrimaryCategory != null || (skillsData.ExpertiseDetails != null && skillsData.ExpertiseDetails.Count > 0)))
                            {
                                foreach (var expertise in skillsData.ExpertiseDetails ?? new List<SkillExperienceDto>())
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
                                                    _context.WorkerCategories.Add(new WorkerCategory
                                                    {
                                                        WorkerId = workerId.Value,
                                                        CategoryId = catId,
                                                        SkillsId = sId
                                                    });
                                                }
                                            }
                                        }
                                        else
                                        {
                                            _context.WorkerCategories.Add(new WorkerCategory
                                            {
                                                WorkerId = workerId.Value,
                                                CategoryId = catId,
                                                SkillsId = null
                                            });
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                    }

                    await _context.SaveChangesAsync();
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

                if (string.IsNullOrEmpty(SessionSkillsData))
                {
                    SessionSkillsData = HttpContext.Session.GetString("TempWorkerSkillsJson") ?? "";
                }

                string workerDynamicBio = "";
                if (!string.IsNullOrEmpty(SessionSkillsData))
                {
                    try
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var reactSkills = JsonSerializer.Deserialize<List<ReactExperienceDto>>(SessionSkillsData, options);
                        if (reactSkills != null && reactSkills.Count > 0 && reactSkills[0].CategoryId.HasValue)
                        {
                            var pCatName = _context.Categories.Where(c => c.CategoryId == reactSkills[0].CategoryId.Value).Select(c => c.CategoryName).FirstOrDefault();
                            if (!string.IsNullOrEmpty(pCatName)) workerDynamicBio = pCatName;
                        }
                    }
                    catch
                    {
                        try
                        {
                            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            var parsedSkills = JsonSerializer.Deserialize<WorkerSkillsSubmitDto>(SessionSkillsData, options);
                            if (parsedSkills != null && !string.IsNullOrEmpty(parsedSkills.PrimaryCategory))
                            {
                                if (int.TryParse(parsedSkills.PrimaryCategory, out int pCatId))
                                {
                                    var pCatName = _context.Categories.Where(c => c.CategoryId == pCatId).Select(c => c.CategoryName).FirstOrDefault();
                                    if (!string.IsNullOrEmpty(pCatName)) workerDynamicBio = pCatName;
                                }
                            }
                        }
                        catch { }
                    }
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
                    Bio = !string.IsNullOrEmpty(workerDynamicBio) ? workerDynamicBio : "Professional Worker",
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
                            bool parsedSuccessfully = false;

                            try
                            {
                                var reactSkills = JsonSerializer.Deserialize<List<ReactExperienceDto>>(SessionSkillsData, options);
                                if (reactSkills != null && reactSkills.Count > 0)
                                {
                                    var uniqueCategorySkills = new HashSet<(int, int?)>();

                                    foreach (var exp in reactSkills)
                                    {
                                        if (exp.CategoryId.HasValue)
                                        {
                                            _context.Experiences.Add(new Experience
                                            {
                                                WorkerId = newWorkerId,
                                                WorkAt = exp.WorkAt,
                                                ExpDetail = exp.ExpDetail,
                                                Duration = exp.Duration
                                            });

                                            if (!uniqueCategorySkills.Contains((exp.CategoryId.Value, exp.SkillsId)))
                                            {
                                                uniqueCategorySkills.Add((exp.CategoryId.Value, exp.SkillsId));
                                                _context.WorkerCategories.Add(new WorkerCategory
                                                {
                                                    WorkerId = newWorkerId,
                                                    CategoryId = exp.CategoryId.Value,
                                                    SkillsId = exp.SkillsId
                                                });
                                            }
                                        }
                                    }
                                    parsedSuccessfully = true;
                                }
                            }
                            catch { }

                            if (!parsedSuccessfully)
                            {
                                try
                                {
                                    var skillsData = JsonSerializer.Deserialize<WorkerSkillsSubmitDto>(SessionSkillsData, options);
                                    if (skillsData != null && (skillsData.PrimaryCategory != null || skillsData.ExpertiseDetails?.Count > 0))
                                    {
                                        HashSet<int> categoryIdsToAdd = new HashSet<int>();

                                        if (int.TryParse(skillsData.PrimaryCategory, out int pCat)) categoryIdsToAdd.Add(pCat);
                                        if (int.TryParse(skillsData.SecondaryCategory, out int sCat)) categoryIdsToAdd.Add(sCat);

                                        if (skillsData.ExpertiseDetails != null)
                                        {
                                            foreach (var expertise in skillsData.ExpertiseDetails)
                                            {
                                                if (int.TryParse(expertise.CategoryId, out int catId))
                                                {
                                                    categoryIdsToAdd.Add(catId);

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
                                                                _context.WorkerCategories.Add(new WorkerCategory
                                                                {
                                                                    WorkerId = newWorkerId,
                                                                    CategoryId = catId,
                                                                    SkillsId = sId
                                                                });
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        _context.WorkerCategories.Add(new WorkerCategory
                                                        {
                                                            WorkerId = newWorkerId,
                                                            CategoryId = catId,
                                                            SkillsId = null
                                                        });
                                                    }
                                                }
                                            }
                                        }

                                        foreach (var catId in categoryIdsToAdd)
                                        {
                                            bool exists = _context.WorkerCategories.Any(wc => wc.WorkerId == newWorkerId && wc.CategoryId == catId);
                                            if (!exists)
                                            {
                                                _context.WorkerCategories.Add(new WorkerCategory
                                                {
                                                    WorkerId = newWorkerId,
                                                    CategoryId = catId,
                                                    SkillsId = null
                                                });
                                            }
                                        }
                                    }
                                }
                                catch { }
                            }

                            await _context.SaveChangesAsync();
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