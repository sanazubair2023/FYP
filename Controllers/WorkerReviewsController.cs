using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MaidAndServantt.Models;

namespace MaidAndServantt.Controllers
{
    public class WorkerReviewsController : Controller
    {
        private readonly FypContext _context;

        public WorkerReviewsController(FypContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Reviews(int workerId)
        {
            try
            {
                // 1. Check worker exists
                var worker = await _context.Workers
                    .FirstOrDefaultAsync(w => w.WorkerId == workerId);

                if (worker == null)
                    return NotFound("Worker not found");

                // 2. Fetch reviews directly via Interviews table (Model class touch kiye bina)
                var reviewsQuery = await (from interview in _context.Interviews
                                          join review in _context.Reviews on interview.InterviewId equals review.InterviewId
                                          join client in _context.Clients on interview.ClientId equals client.ClientId into clientJoin
                                          from client in clientJoin.DefaultIfEmpty()
                                          where interview.WorkerId == workerId
                                          select new ReviewItemViewModel
                                          {
                                              Id = review.ReviewId.ToString(),
                                              Name = client != null ? client.Name : "Anonymous",
                                              Rating = Convert.ToDouble(review.Rating ?? 0),
                                              Comment = review.Comment ?? "",
                                              Date = review.ReviewDate.HasValue ? review.ReviewDate.Value.ToString("MMM dd, yyyy") : "N/A",
                                              Duration = "Previous Client"
                                          }).ToListAsync();

                var allReviews = reviewsQuery.OrderByDescending(r => r.Id).ToList();

                // 3. Calculate average rating
                double avgRating = allReviews.Any()
                    ? Math.Round(allReviews.Average(r => r.Rating), 1)
                    : 0.0;

                var viewModel = new WorkerReviewsViewModel
                {
                    WorkerId = worker.WorkerId,
                    WorkerName = worker.Name ?? "Worker",
                    AverageRating = avgRating,
                    ReviewCount = allReviews.Count,
                    Reviews = allReviews
                };

                return View("Reviews", viewModel);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error fetching worker reviews: " + ex.Message);
            }
        }
    }
}

namespace MaidAndServantt.Models
{
    public class WorkerReviewsViewModel
    {
        public int WorkerId { get; set; }
        public string WorkerName { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public List<ReviewItemViewModel> Reviews { get; set; } = new List<ReviewItemViewModel>();
    }

    public class ReviewItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public double Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
    }
}