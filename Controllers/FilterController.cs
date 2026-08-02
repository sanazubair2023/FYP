using MaidAndServantt.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MaidAndServantt.Controllers
{
    [Route("Filter")]
    public class FilterController : Controller
    {
        // Database context variable
        private readonly FypContext db;

        // Constructor: Iske zariye ASP.NET Core khud database provider configure karega
        public FilterController(FypContext context)
        {
            db = context;
        }

        // GET: /Filter/Filter ya sirf /Filter
        // GET: /Filter
        [HttpGet]
        [Route("")]
        [Route("Filter")]
        public ActionResult Filter(string gender, List<string> category, string city, List<string> subCategories)
        {
            // Load dropdown selections from the database
            ViewBag.Categories = db.Categories.ToList();
            ViewBag.Skills = db.Skills.ToList();
            ViewBag.Cities = db.Workers
                               .Select(w => w.Address)
                               .ToList()
                               .Where(a => !string.IsNullOrEmpty(a))
                               .Distinct()
                               .ToList();

            // Pass the parameters back to preserve form state[cite: 4]
            ViewBag.SelectedGender = gender;
            ViewBag.SelectedCity = city;
            ViewBag.SelectedCategories = category ?? new List<string>();
            ViewBag.SelectedSubs = subCategories ?? new List<string>();

            return View();
        }
        //[HttpGet]
        //[Route("")]
        //[Route("Filter")]
        //public ActionResult Filter(string gender, List<string> category, string city, List<string> subCategories, string activeCategory = null, string searchName = null)
        //{
        //    // ==========================================
        //    // DATABASE SE DATA FETCH KARKE VIEWBAGS MEIN DALNA
        //    // ==========================================
        //    ViewBag.Categories = db.Categories.ToList();
        //    ViewBag.Skills = db.Skills.ToList();

        //    // FIX: Pehle data memory me load kiya taake 'text' data type compatibility ka issue khatam ho sake
        //    ViewBag.Cities = db.Workers
        //                       .Select(w => w.Address)
        //                       .ToList() // Pehle memory me le aaye
        //                       .Where(a => !string.IsNullOrEmpty(a))
        //                       .Distinct()
        //                       .ToList();

        //    // Initial Query: Sirf available workers fetch karein
        //    var workerQuery = db.Workers.Where(w => w.AvailableStatus == true);

        //    // 1. Gender Filtering
        //    if (!string.IsNullOrEmpty(gender))
        //    {
        //        workerQuery = workerQuery.Where(w => w.Gender == gender);
        //    }

        //    // 2. City Filtering (Address column me search karega)
        //    if (!string.IsNullOrEmpty(city))
        //    {
        //        // SQL 'text' datatype par direct .Contains chalane ke liye isko string compare kiya
        //        workerQuery = workerQuery.Where(w => w.Address != null && w.Address.Contains(city));
        //    }

        //    // Data ko memory me le kar aate hain taake Bio string parsing lag sake
        //    var workersList = workerQuery.ToList();

        //    // 3. Main Categories Filtering (Bio text search combination)
        //    if (category != null && category.Any())
        //    {
        //        workersList = workersList.Where(w =>
        //            category.Any(c => w.Bio != null && w.Bio.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0)
        //        ).ToList();
        //    }

        //    // 4. Skills Options / Sub-Categories Filtering
        //    if (subCategories != null && subCategories.Any())
        //    {
        //        workersList = workersList.Where(w =>
        //            subCategories.Any(sc => w.Bio != null && w.Bio.IndexOf(sc, StringComparison.OrdinalIgnoreCase) >= 0)
        //        ).ToList();
        //    }

        //    // State ko store rakhne ke liye ViewBags taake page refresh par selections gayab na hon
        //    ViewBag.SelectedGender = gender;
        //    ViewBag.SelectedCity = city;
        //    ViewBag.SelectedCategories = category ?? new List<string>();
        //    ViewBag.SelectedSubs = subCategories ?? new List<string>();

        //    // Layout dynamics tracking parameters
        //    ViewBag.ActiveCategory = activeCategory ?? "All";
        //    ViewBag.SearchQuery = searchName ?? "";

        //    return View(workersList);
        //}
    }
}