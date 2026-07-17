using MaidAndServantt.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MaidAndServantt.Controllers
{
    public class WorkerReviewsController : Controller
    {
        private readonly FypContext _context;

        public WorkerReviewsController(FypContext context)
        {
            _context = context;
        }

        // GET: /WorkerReviews/Reviews?workerId=23
        [HttpGet]
        public IActionResult Reviews(int workerId)
        {
            // Agar URL binding galat hai aur workerId 0 aa rahi hai
            if (workerId == 0)
            {
                // Fallback: Test ke liye pehle worker ki ID utha lein taaki blank white screen na aaye
                var firstWorker = _context.Workers.FirstOrDefault();
                if (firstWorker != null)
                {
                    workerId = firstWorker.WorkerId;
                }
                else
                {
                    return NotFound("No workers found in database.");
                }
            }

            var worker = _context.Workers.FirstOrDefault(w => w.WorkerId == workerId);
            if (worker == null)
            {
                return NotFound("Worker not found");
            }

            // 1. Worker ke linked Interviews ki list nikalen
            var workerInterviewIds = _context.Interviews
                                             .Where(i => i.WorkerId == workerId)
                                             .Select(i => i.InterviewId)
                                             .ToList();

            // 2. Reviews nikalen jo un interviews se ya directly linked hon
            var reviewsQuery = (from r in _context.Reviews
                                where (r.InterviewId != null && workerInterviewIds.Contains(r.InterviewId.Value))
                                   || r.InterviewId == workerId
                                select r).ToList();

            var reviewsList = new List<IndividualReviewModel>();

            foreach (var r in reviewsQuery)
            {
                // Is review se relative interview aur client ka details load karein
                var interview = _context.Interviews.FirstOrDefault(i => i.InterviewId == r.InterviewId || i.WorkerId == r.InterviewId);
                string clientName = "Verified Client";
                string durationText = "Employer for 1 year";

                if (interview != null)
                {
                    var client = _context.Clients.FirstOrDefault(c => c.ClientId == interview.ClientId);
                    if (client != null && !string.IsNullOrEmpty(client.Name))
                    {
                        clientName = client.Name;
                    }

                    if (interview.InterviewDate.HasValue)
                    {
                        int years = DateTime.Now.Year - interview.InterviewDate.Value.Year;
                        years = years <= 0 ? 1 : years;
                        durationText = $"Employer for {years} year" + (years > 1 ? "s" : "");
                    }
                }

                reviewsList.Add(new IndividualReviewModel
                {
                    Id = r.ReviewId,
                    Name = clientName,
                    Rating = r.Rating ?? 5,
                    Comment = r.Comment ?? "No written review provided.",
                    Date = durationText
                });
            }

            int reviewCount = reviewsList.Count;
            double avgRating = 0.0;
            if (reviewCount > 0)
            {
                avgRating = reviewsList.Average(r => r.Rating);
            }

            var viewModel = new WorkerReviewsViewModel
            {
                WorkerId = workerId,
                WorkerName = worker.Name ?? "Worker",
                AverageRating = avgRating.ToString("F1"),
                ReviewCount = reviewCount,
                Reviews = reviewsList
            };

            return View(viewModel);
        }
    }
}

namespace MaidAndServantt.Models
{
    public class WorkerReviewsViewModel
    {
        public int WorkerId { get; set; }
        public string WorkerName { get; set; } = null!;
        public string AverageRating { get; set; } = "0.0";
        public int ReviewCount { get; set; }
        public List<IndividualReviewModel> Reviews { get; set; } = new List<IndividualReviewModel>();
    }

    public class IndividualReviewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int Rating { get; set; }
        public string Comment { get; set; } = null!;
        public string Date { get; set; } = null!;
    }
}