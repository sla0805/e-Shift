using eShift.Data;
using eShift.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System;

namespace eShift.Controllers
{
    public class LoadsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LoadsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper method to populate dropdowns for Jobs and Products
        private async Task PopulateRelatedEntitiesAsync(object selectedJob = null, object selectedProduct = null)
        {
            // For Jobs: Display JobId and Pickup/Delivery locations
            var jobs = await _context.Jobs
                                     .OrderBy(j => j.JobId)
                                     .Select(j => new
                                     {
                                         j.JobId,
                                         DisplayName = $"Job {j.JobId}: {j.PickupLocation} to {j.DeliveryLocation} ({j.ScheduledDate.ToShortDateString()})"
                                     })
                                     .ToListAsync();
            ViewBag.Jobs = new SelectList(jobs, "JobId", "DisplayName", selectedJob);

            // For Products: Display ProductId and ProductName
            var products = await _context.Products
                                         .OrderBy(p => p.ProductName)
                                         .Select(p => new
                                         {
                                             p.ProductId,
                                             DisplayName = p.ProductName
                                         })
                                         .ToListAsync();
            ViewBag.Products = new SelectList(products, "ProductId", "DisplayName", selectedProduct);
        }

        // Helper method to apply filtering and sorting for Loads
        private IQueryable<Load> ApplyFilteringAndSorting(IQueryable<Load> loads, string searchString, string sortOrder)
        {
            // 1. Filter (Search)
            if (!string.IsNullOrEmpty(searchString))
            {
                var lowerSearchString = searchString.ToLower();

                loads = loads.Where(l =>
                    // These checks are correct, as they are for string conversion and comparison
                    (l.Job != null && l.Job.JobId.ToString().Contains(lowerSearchString)) ||
                    (l.Product != null && l.Product.ProductName.ToLower().Contains(lowerSearchString))
                );
            }

            // 2. Sort
            switch (sortOrder)
            {
                case "job_desc":
                    // FIX: Provide a default value (0 or int.MinValue) if Job is null for int JobId.
                    // This ensures the expression always yields a non-nullable int for OrderBy.
                    loads = loads.OrderByDescending(l => l.Job == null ? int.MinValue : l.Job.JobId);
                    break;
                case "Job": // Ascending Job ID
                    loads = loads.OrderBy(l => l.Job == null ? int.MaxValue : l.Job.JobId); // Use MaxValue to place nulls at end for ASC
                    break;
                case "product_desc":
                    // FIX: Provide an empty string if Product is null for string ProductName.
                    // This ensures the expression always yields a non-null string for OrderBy.
                    loads = loads.OrderByDescending(l => l.Product == null ? "" : l.Product.ProductName);
                    break;
                case "Product": // Ascending Product Name
                    loads = loads.OrderBy(l => l.Product == null ? "" : l.Product.ProductName);
                    break;
                case "loadweight_desc":
                    loads = loads.OrderByDescending(l => l.LoadWeightKg);
                    break;
                case "LoadWeight": // Ascending Load Weight
                    loads = loads.OrderBy(l => l.LoadWeightKg);
                    break;
                default: // Default sort by LoadId (or primary key)
                    loads = loads.OrderBy(l => l.LoadId);
                    break;
            }
            return loads;
        }

        // GET: Loads
        public async Task<IActionResult> Index(string sortOrder, string searchString, int? pageNumber)
        {
            // Set current filter and sort for view
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentSort"] = sortOrder;

            // Setup sort parameters for view (toggle between asc/desc)
            ViewData["JobSortParm"] = sortOrder == "Job" ? "job_desc" : "Job";
            ViewData["ProductSortParm"] = sortOrder == "Product" ? "product_desc" : "Product";
            ViewData["LoadWeightSortParm"] = sortOrder == "LoadWeight" ? "loadweight_desc" : "LoadWeight";


            // Eager load related Job and Product data for display and filtering/sorting
            IQueryable<Load> loads = _context.Loads
                                .Include(l => l.Job)
                                .Include(l => l.Product);

            // Apply filtering and sorting (THIS LINE WILL NOW BE FIXED)
            loads = ApplyFilteringAndSorting(loads, searchString, sortOrder);

            int pageSize = 7; // Define your page size
            return View(await PaginatedList<Load>.CreateAsync(loads.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // GET: Loads/DownloadCsv
        public async Task<IActionResult> DownloadCsv(string searchString, string sortOrder)
        {
            IQueryable<Load> loads = _context.Loads
                                .Include(l => l.Job)
                                .Include(l => l.Product);

            // Apply the same filtering and sorting as the Index view (THIS LINE WILL NOW BE FIXED)
            loads = ApplyFilteringAndSorting(loads, searchString, sortOrder);

            var loadList = await loads.ToListAsync();

            var csvBuilder = new StringBuilder();

            // Add CSV Header
            csvBuilder.AppendLine("Load ID,Job ID,Product Name,Product Quantity,Container,Load Weight (kg),Comment");

            // Add Data Rows
            foreach (var load in loadList)
            {
                string jobId = load.Job?.JobId.ToString() ?? "N/A";
                string productName = load.Product?.ProductName ?? "N/A";

                // FIX FOR ProductContainer: It's an int, so directly call .ToString()
                csvBuilder.AppendLine($"{load.LoadId}," +
                                      $"{EscapeCsv(jobId)}," +
                                      $"{EscapeCsv(productName)}," +
                                      $"{load.ProductQuantity}," +
                                      $"{EscapeCsv(load.ProductContainer.ToString())}," + // <-- FIXED: ProductContainer is an int, so .ToString() is needed
                                      $"{load.LoadWeightKg}," +
                                      $"{EscapeCsv(load.Comment)}");
            }

            var csvBytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
            var fileName = !string.IsNullOrWhiteSpace(searchString) ? "Searched_Loads.csv" : "All_Loads.csv";

            return File(csvBytes, "text/csv", fileName);
        }

        // Helper method to escape values for CSV
        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }
            // Check if the value contains special characters that require quoting
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                // Double any existing quotes and enclose the value in quotes
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }

        // GET: Loads/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Eager load related Job and Product data for display
            var load = await _context.Loads
                .Include(l => l.Job)
                .Include(l => l.Product)
                .FirstOrDefaultAsync(m => m.LoadId == id);

            if (load == null)
            {
                return NotFound();
            }

            return View(load);
        }

        // GET: Loads/Create
        public async Task<IActionResult> Create()
        {
            await PopulateRelatedEntitiesAsync(); // Populate dropdowns
            return View();
        }

        // POST: Loads/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // MODIFICATION: Added "ProductContainer" and ensured "ProductQuantity" is present in [Bind]
        public async Task<IActionResult> Create([Bind("JobId,ProductId,ProductQuantity,ProductContainer,LoadWeightKg,Comment")] Load load)
        {
            if (ModelState.IsValid)
            {
                _context.Add(load);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await PopulateRelatedEntitiesAsync(load.JobId, load.ProductId); // Re-populate dropdowns if validation fails
            return View(load);
        }

        // GET: Loads/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var load = await _context.Loads.FindAsync(id);
            if (load == null)
            {
                return NotFound();
            }
            await PopulateRelatedEntitiesAsync(load.JobId, load.ProductId);
            return View(load);
        }

        // POST: Loads/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        // MODIFICATION: Added "ProductContainer" and ensured "ProductQuantity" is present in [Bind]
        public async Task<IActionResult> Edit(int id, [Bind("LoadId,JobId,ProductId,ProductQuantity,ProductContainer,LoadWeightKg,Comment")] Load load)
        {
            if (id != load.LoadId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(load);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Loads.Any(e => e.LoadId == load.LoadId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            await PopulateRelatedEntitiesAsync(load.JobId, load.ProductId); // Re-populate dropdowns if validation fails
            return View(load);
        }

        // GET: Loads/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Eager load related Job and Product data for display
            var load = await _context.Loads
                .Include(l => l.Job)
                .Include(l => l.Product)
                .FirstOrDefaultAsync(m => m.LoadId == id);
            if (load == null)
            {
                return NotFound();
            }

            return View(load);
        }

        // POST: Loads/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var load = await _context.Loads.FindAsync(id);
            if (load != null)
            {
                _context.Loads.Remove(load);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}