using eShift.Models;
using eShift.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // Added for List
using System.Text; // Added for StringBuilder
using System.IO; // Added for StringWriter

namespace eShift.Controllers
{
    public class AssistantsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AssistantsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper method to apply filtering and sorting for Assistants
        private IQueryable<Assistant> ApplyFilteringAndSorting(IQueryable<Assistant> assistants, string sortOrder, string searchString)
        {
            // 1. Filter (Search)
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var lowerSearchString = searchString.ToLower();

                assistants = assistants.Where(a =>
                    a.AssistantName.ToLower().Contains(lowerSearchString) ||
                    a.AssistantPhone.ToLower().Contains(lowerSearchString) ||
                    a.AssistantId.ToString().Contains(lowerSearchString)); // Convert ID to string for search
            }

            // 2. Sort
            switch (sortOrder)
            {
                case "id_desc": // Sort by ID Descending
                    assistants = assistants.OrderByDescending(a => a.AssistantId);
                    break;
                case "Name": // Sort by Name Ascending
                    assistants = assistants.OrderBy(a => a.AssistantName);
                    break;
                case "name_desc": // Sort by Name Descending
                    assistants = assistants.OrderByDescending(a => a.AssistantName);
                    break;
                default: // Default sort: by ID Ascending
                    assistants = assistants.OrderBy(a => a.AssistantId);
                    break;
            }
            return assistants;
        }

        //Search, sort function
        public async Task<IActionResult> Index(string sortOrder, string searchString, int? pageNumber)
        {
            // Set search filter to maintain its value in the view
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentSort"] = sortOrder; // Pass current sort to view for icon logic

            // Set sort parameters to toggle on click in the view
            ViewData["IdSortParm"] = String.IsNullOrEmpty(sortOrder) ? "id_desc" : ""; // Sort by ID
            ViewData["NameSortParm"] = sortOrder == "Name" ? "name_desc" : "Name"; // Sort by Name

            var assistants = from a in _context.Assistants
                             select a;

            assistants = ApplyFilteringAndSorting(assistants, sortOrder, searchString);

            int pageSize = 10;
            return View(await PaginatedList<eShift.Models.Assistant>.CreateAsync(assistants.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // GET: Assistants/DownloadCsv
        public async Task<IActionResult> DownloadCsv(string searchString, string sortOrder)
        {
            var assistants = from a in _context.Assistants
                             select a;

            // Apply search filter and sorting using the helper method
            assistants = ApplyFilteringAndSorting(assistants, sortOrder, searchString);

            var assistantList = await assistants.ToListAsync();

            var csvBuilder = new StringBuilder();

            // Add CSV header for Assistant properties
            csvBuilder.AppendLine("Assistant ID,Assistant Name,Phone Number");

            // Add CSV data
            foreach (var assistant in assistantList)
            {
                // Use the EscapeCsv helper function to properly handle commas and quotes in data
                csvBuilder.AppendLine($"{assistant.AssistantId}," +
                                      $"{EscapeCsv(assistant.AssistantName)}," +
                                      $"{EscapeCsv(assistant.AssistantPhone)}");
            }

            var csvBytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
            var fileName = !string.IsNullOrWhiteSpace(searchString) ? "Searched_Assistants.csv" : "All_Assistants.csv";

            return File(csvBytes, "text/csv", fileName);
        }

        // Helper to escape values for CSV
        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }
            // If the value contains a comma, double quote, or newline, enclose it in double quotes
            // and escape any existing double quotes by doubling them.
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }


        // GET: Assistants/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var assistant = await _context.Assistants
                .FirstOrDefaultAsync(m => m.AssistantId == id);
            if (assistant == null)
            {
                return NotFound();
            }

            return View(assistant);
        }

        // GET: Assistants/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Assistants/Create
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Create([Bind("AssistantName,AssistantPhone")] Assistant assistant)
        {
            if (ModelState.IsValid)
            {
                _context.Add(assistant); // Add the new assistant (ID will be assigned by DB on SaveChanges)
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index)); // Redirect to the list view
            }
            return View(assistant); // If model is invalid, return to the form with errors
        }

        // GET: Assistants/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var assistant = await _context.Assistants.FindAsync(id); // Find the assistant by ID
            if (assistant == null)
            {
                return NotFound();
            }
            return View(assistant);
        }

        // POST: Assistants/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Edit(int id, [Bind("AssistantId,AssistantName,AssistantPhone")] Assistant assistant)
        {
            if (id != assistant.AssistantId) // Check if the ID in the URL matches the ID in the form
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(assistant); // Update the assistant record
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Assistants.Any(e => e.AssistantId == assistant.AssistantId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index)); // Redirect to the list view
            }
            return View(assistant); // If model is invalid, return to the form with errors
        }

        // GET: Assistants/Delete/5
        public async Task<IActionResult> Delete(int? id) // ID is int?
        {
            if (id == null)
            {
                return NotFound();
            }

            var assistant = await _context.Assistants
                .FirstOrDefaultAsync(m => m.AssistantId == id);
            if (assistant == null)
            {
                return NotFound();
            }

            return View(assistant);
        }

        // POST: Assistants/Delete/5 (Handles the actual deletion after confirmation)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var assistant = await _context.Assistants.FindAsync(id); // Find the assistant to delete
            if (assistant != null)
            {
                _context.Assistants.Remove(assistant); // Remove the assistant
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index)); // Redirect to the list view
        }
    }
}