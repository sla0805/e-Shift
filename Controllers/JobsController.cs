using eShift.Models;
using eShift.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System;

namespace eShift.Controllers
{
    public class JobsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public JobsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper method to apply filtering and sorting for Jobs
        private IQueryable<Job> ApplyFilteringAndSorting(IQueryable<Job> jobs, string searchString, string sortOrder)
        {
            // 1. Filter (Search)
            if (!string.IsNullOrEmpty(searchString))
            {
                var lowerSearchString = searchString.ToLower();

                jobs = jobs.Where(j =>
                    j.JobId.ToString().Contains(lowerSearchString) ||
                    (j.Customer != null && j.Customer.CustName.ToLower().Contains(lowerSearchString)) ||
                    (j.PickupLocation != null && j.PickupLocation.ToLower().Contains(lowerSearchString)) ||
                    (j.DeliveryLocation != null && j.DeliveryLocation.ToLower().Contains(lowerSearchString)) ||
                    (j.JobStatus != null && j.JobStatus.ToLower().Contains(lowerSearchString))
                );
            }

            // 2. Sort
            switch (sortOrder)
            {
                case "id_desc":
                    jobs = jobs.OrderByDescending(j => j.JobId);
                    break;
                case "CustomerName":
                    jobs = jobs.OrderBy(j => j.Customer == null ? null : j.Customer.CustName);
                    break;
                case "customername_desc":
                    jobs = jobs.OrderByDescending(j => j.Customer == null ? null : j.Customer.CustName);
                    break;
                case "OrderDate":
                    jobs = jobs.OrderBy(j => j.OrderDate);
                    break;
                case "orderdate_desc":
                    jobs = jobs.OrderByDescending(j => j.OrderDate);
                    break;
                case "ScheduledDate":
                    jobs = jobs.OrderBy(j => j.ScheduledDate);
                    break;
                case "scheduleddate_desc":
                    jobs = jobs.OrderByDescending(j => j.ScheduledDate);
                    break;
                case "DeliveryDate":
                    jobs = jobs.OrderBy(j => j.DeliveryDate);
                    break;
                case "deliverydate_desc":
                    jobs = jobs.OrderByDescending(j => j.DeliveryDate);
                    break;
                default: // Default sort: by Job ID Ascending
                    jobs = jobs.OrderBy(j => j.JobId);
                    break;
            }
            return jobs;
        }

        // GET: Jobs
        public async Task<IActionResult> Index(string sortOrder, string searchString, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentSort"] = sortOrder;

            ViewData["IdSortParm"] = String.IsNullOrEmpty(sortOrder) ? "id_desc" : "";
            ViewData["CustomerNameSortParm"] = sortOrder == "CustomerName" ? "customername_desc" : "CustomerName";
            ViewData["OrderDateSortParm"] = sortOrder == "OrderDate" ? "orderdate_desc" : "OrderDate";
            ViewData["ScheduledDateSortParm"] = sortOrder == "ScheduledDate" ? "scheduleddate_desc" : "ScheduledDate";
            ViewData["DeliveryDateSortParm"] = sortOrder == "DeliveryDate" ? "deliverydate_desc" : "DeliveryDate";

            var jobs = from j in _context.Jobs
                       .Include(j => j.Customer) // IMPORTANT: Include the Customer navigation property
                       select j;

            jobs = ApplyFilteringAndSorting(jobs, searchString, sortOrder);

            int pageSize = 7;
            return View(await PaginatedList<eShift.Models.Job>.CreateAsync(jobs.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // GET: Jobs/DownloadCsv
        public async Task<IActionResult> DownloadCsv(string searchString, string sortOrder)
        {
            var jobs = from j in _context.Jobs
                       .Include(j => j.Customer) // IMPORTANT: Include the Customer navigation property
                       select j;

            jobs = ApplyFilteringAndSorting(jobs, searchString, sortOrder);

            var jobList = await jobs.ToListAsync();

            var csvBuilder = new StringBuilder();

            csvBuilder.AppendLine("Job ID,Customer Name,Pickup Location,Delivery Location,Job Status,Order Date,Scheduled Date,Delivery Date");

            foreach (var job in jobList)
            {
                // Access CustName via navigation property (handle null Customer if CustId is nullable)
                string customerName = job.Customer?.CustName ?? "N/A"; // Changed to CustName

                csvBuilder.AppendLine($"{job.JobId}," +
                                      $"{EscapeCsv(customerName)}," +
                                      $"{EscapeCsv(job.PickupLocation)}," +
                                      $"{EscapeCsv(job.DeliveryLocation)}," +
                                      $"{EscapeCsv(job.JobStatus)}," +
                                      $"{job.OrderDate.ToString("yyyy-MM-dd")}," +
                                      $"{job.ScheduledDate.ToString("yyyy-MM-dd")}," +
                                      $"{job.DeliveryDate?.ToString("yyyy-MM-dd") ?? ""}");
            }

            var csvBytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
            var fileName = !string.IsNullOrWhiteSpace(searchString) ? "Searched_Jobs.csv" : "All_Jobs.csv";

            return File(csvBytes, "text/csv", fileName);
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }

        // GET: Jobs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var job = await _context.Jobs.Include(j => j.Customer).FirstOrDefaultAsync(m => m.JobId == id);
            if (job == null) return NotFound();

            return View(job);
        }

        // GET: Jobs/Create
        public IActionResult Create()
        {
            // Populate Customers for dropdown in Create view, using "CustName" for display
            ViewData["CustId"] = new SelectList(_context.Customers, "CustId", "CustName");

            // Define Job Status options for the dropdown
            var jobStatuses = new List<SelectListItem>
            {
                new SelectListItem { Value = "Pending", Text = "Pending" },
                new SelectListItem { Value = "Confirmed", Text = "Confirmed" },
                new SelectListItem { Value = "In Progress", Text = "In Progress" },
                new SelectListItem { Value = "Completed", Text = "Completed" },
                new SelectListItem { Value = "Cancelled", Text = "Cancelled" }
            };
            ViewData["JobStatusOptions"] = jobStatuses; // Pass the list to the view

            return View();
        }

        // POST: Jobs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CustId,PickupLocation,DeliveryLocation,JobStatus,OrderDate,ScheduledDate,DeliveryDate")] Job job)
        {
            if (ModelState.IsValid)
            {
                _context.Add(job);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CustId"] = new SelectList(_context.Customers, "CustId", "CustName", job.CustId);
            // Re-populate Job Status options if model state is invalid
            var jobStatuses = new List<SelectListItem>
            {
                new SelectListItem { Value = "Pending", Text = "Pending" },
                new SelectListItem { Value = "Confirmed", Text = "COnfirmed" },
                new SelectListItem { Value = "In Progress", Text = "In Progress" },
                new SelectListItem { Value = "Completed", Text = "Completed" },
                new SelectListItem { Value = "Cancelled", Text = "Cancelled" }
            };
            ViewData["JobStatusOptions"] = jobStatuses; // Pass the list to the view
            return View(job);
        }

        // GET: Jobs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var job = await _context.Jobs.FindAsync(id);
            if (job == null) return NotFound();

            ViewData["CustId"] = new SelectList(_context.Customers, "CustId", "CustName", job.CustId);

            // Define Job Status options and set the selected value for the dropdown
            var jobStatuses = new List<SelectListItem>
            {
                new SelectListItem { Value = "Pending", Text = "Pending" },
                new SelectListItem { Value = "Confirmed", Text = "Confirmed" },
                new SelectListItem { Value = "In Progress", Text = "In Progress" },
                new SelectListItem { Value = "Completed", Text = "Completed" },
                new SelectListItem { Value = "Cancelled", Text = "Cancelled" }
            };
            // Use SelectList to pre-select the current JobStatus
            ViewData["JobStatusOptions"] = new SelectList(jobStatuses, "Value", "Text", job.JobStatus);

            return View(job);
        }

        // POST: Jobs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("JobId,CustId,PickupLocation,DeliveryLocation,JobStatus,OrderDate,ScheduledDate,DeliveryDate")] Job job)
        {
            if (id != job.JobId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(job);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!JobExists(job.JobId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CustId"] = new SelectList(_context.Customers, "CustId", "CustName", job.CustId);
            // Re-populate Job Status options if model state is invalid
            var jobStatuses = new List<SelectListItem>
            {
                new SelectListItem { Value = "Pending", Text = "Pending" },
                new SelectListItem { Value = "Confirmed", Text = "Confirmed" },
                new SelectListItem { Value = "In Progress", Text = "In Progress" },
                new SelectListItem { Value = "Completed", Text = "Completed" },
                new SelectListItem { Value = "Cancelled", Text = "Cancelled" }
            };
            ViewData["JobStatusOptions"] = new SelectList(jobStatuses, "Value", "Text", job.JobStatus);
            return View(job);
        }

        // GET: Jobs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var job = await _context.Jobs.Include(j => j.Customer).FirstOrDefaultAsync(m => m.JobId == id);
            if (job == null) return NotFound();

            return View(job);
        }

        // POST: Jobs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var job = await _context.Jobs.FindAsync(id);
            if (job != null)
            {
                _context.Jobs.Remove(job);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        //Chart function

        [HttpGet]
        public JsonResult GetJobStatusData(string searchString)
        {
            var jobs = _context.Jobs.AsQueryable();
            jobs = ApplyFilteringAndSorting(jobs, searchString, null);

            var jobStatusCounts = jobs
                .GroupBy(j => j.JobStatus)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                }).ToList();

            return Json(jobStatusCounts);
        }

        private bool JobExists(int id)
        {
            return _context.Jobs.Any(e => e.JobId == id);
        }
    }
}