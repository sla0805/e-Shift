using eShift.Models;
using eShift.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Required for SelectList
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Text;   // Added for StringBuilder
using System.IO;     // Added for file operations
using System;      // Added for String.IsNullOrEmpty, etc.

namespace eShift.Controllers
{
    public class TransportAssignmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransportAssignmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper method to populate all dropdowns
        private async Task PopulateRelatedEntitiesAsync(object selectedJob = null, object selectedDriver = null, object selectedAssistant = null, object selectedVehicle = null)
        {
            // Jobs (displaying ID and locations for clarity)
            var jobs = await _context.Jobs
                                     .OrderBy(j => j.JobId)
                                     .Select(j => new {
                                         j.JobId,
                                         DisplayName = $"Job {j.JobId}: {j.PickupLocation} to {j.DeliveryLocation} ({j.ScheduledDate.ToShortDateString()})"
                                     })
                                     .ToListAsync();
            ViewBag.Jobs = new SelectList(jobs, "JobId", "DisplayName", selectedJob);

            // Drivers (displaying full name)
            var drivers = await _context.Drivers
                                        .OrderBy(d => d.DriverName)
                                        .Select(d => new {
                                            d.DriverId,
                                            DisplayName = d.DriverName
                                        })
                                        .ToListAsync();
            ViewBag.Drivers = new SelectList(drivers, "DriverId", "DisplayName", selectedDriver);

            // Assistants (displaying full name)
            var assistants = await _context.Assistants
                                           .OrderBy(a => a.AssistantName)
                                           .Select(a => new {
                                               a.AssistantId,
                                               DisplayName = a.AssistantName
                                           })
                                           .ToListAsync();
            ViewBag.Assistants = new SelectList(assistants, "AssistantId", "DisplayName", selectedAssistant);

            // Vehicles (displaying model and license number)
            var vehicles = await _context.Vehicles
                                         .OrderBy(v => v.VehicleModel)
                                         .Select(v => new {
                                             v.VehicleId,
                                             DisplayName = $"{v.VehicleModel} ({v.VehicleLicensenum})"
                                         })
                                         .ToListAsync();
            ViewBag.Vehicles = new SelectList(vehicles, "VehicleId", "DisplayName", selectedVehicle);
        }

        // Helper method to apply filtering and sorting for TransportAssignments
        private IQueryable<TransportAssignment> ApplyFilteringAndSorting(IQueryable<TransportAssignment> transportAssignments, string searchString, string sortOrder)
        {
            // 1. Filter (Search)
            if (!string.IsNullOrEmpty(searchString))
            {
                var lowerSearchString = searchString.ToLower();

                transportAssignments = transportAssignments.Where(t =>
                    // Search by Transport ID
                    t.TransportId.ToString().Contains(lowerSearchString) ||
                    // Search by Job ID or Job locations
                    (t.Job != null && (t.Job.JobId.ToString().Contains(lowerSearchString) ||
                                       t.Job.PickupLocation.ToLower().Contains(lowerSearchString) ||
                                       t.Job.DeliveryLocation.ToLower().Contains(lowerSearchString))) ||
                    // Search by Driver Name
                    (t.Driver != null && t.Driver.DriverName.ToLower().Contains(lowerSearchString)) ||
                    // Search by Assistant Name (if Assistant is nullable, handle null check)
                    (t.Assistant != null && t.Assistant.AssistantName.ToLower().Contains(lowerSearchString)) ||
                    // Search by Vehicle Model or License Number
                    (t.Vehicle != null && (t.Vehicle.VehicleModel.ToLower().Contains(lowerSearchString) ||
                                          t.Vehicle.VehicleLicensenum.ToLower().Contains(lowerSearchString)))
                );
            }

            // 2. Sort
            switch (sortOrder)
            {
                case "transportid_desc":
                    transportAssignments = transportAssignments.OrderByDescending(t => t.TransportId);
                    break;
                case "Job": // Ascending Job ID (nulls at end)
                    // If Job is null, treat its JobId as int.MaxValue to push it to the end in ascending sort
                    transportAssignments = transportAssignments.OrderBy(t => t.Job == null ? int.MaxValue : t.Job.JobId);
                    break;
                case "job_desc": // Descending Job ID (nulls at end)
                    // If Job is null, treat its JobId as int.MinValue to push it to the end in descending sort
                    transportAssignments = transportAssignments.OrderByDescending(t => t.Job == null ? int.MinValue : t.Job.JobId);
                    break;
                case "Driver": // Ascending Driver Name (nulls at start for strings)
                    transportAssignments = transportAssignments.OrderBy(t => t.Driver == null ? "" : t.Driver.DriverName);
                    break;
                case "driver_desc": // Descending Driver Name (nulls at end for strings)
                    transportAssignments = transportAssignments.OrderByDescending(t => t.Driver == null ? "" : t.Driver.DriverName);
                    break;
                case "Assistant": // Ascending Assistant Name
                    transportAssignments = transportAssignments.OrderBy(t => t.Assistant == null ? "" : t.Assistant.AssistantName);
                    break;
                case "assistant_desc": // Descending Assistant Name
                    transportAssignments = transportAssignments.OrderByDescending(t => t.Assistant == null ? "" : t.Assistant.AssistantName);
                    break;
                case "Vehicle": // Ascending Vehicle Model
                    transportAssignments = transportAssignments.OrderBy(t => t.Vehicle == null ? "" : t.Vehicle.VehicleModel);
                    break;
                case "vehicle_desc": // Descending Vehicle Model
                    transportAssignments = transportAssignments.OrderByDescending(t => t.Vehicle == null ? "" : t.Vehicle.VehicleModel);
                    break;
                default: // Default sort: by TransportId Ascending
                    transportAssignments = transportAssignments.OrderBy(t => t.TransportId);
                    break;
            }
            return transportAssignments;
        }


        // GET: TransportAssignments
        public async Task<IActionResult> Index(string sortOrder, string searchString, int? pageNumber)
        {
            // Set current filter and sort for view
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentSort"] = sortOrder;

            // Setup sort parameters for view (toggle between asc/desc)
            ViewData["TransportIdSortParm"] = String.IsNullOrEmpty(sortOrder) ? "transportid_desc" : "";
            ViewData["JobSortParm"] = sortOrder == "Job" ? "job_desc" : "Job";
            // Adding sort parameters for other fields for future expansion or if needed in the view
            ViewData["DriverSortParm"] = sortOrder == "Driver" ? "driver_desc" : "Driver";
            ViewData["AssistantSortParm"] = sortOrder == "Assistant" ? "assistant_desc" : "Assistant";
            ViewData["VehicleSortParm"] = sortOrder == "Vehicle" ? "vehicle_desc" : "Vehicle";


            // Eager load related Job, Driver, Assistant, and Vehicle data
            IQueryable<TransportAssignment> transportAssignments = _context.TransportAssignments
                .Include(t => t.Job)
                .Include(t => t.Driver)
                .Include(t => t.Assistant)
                .Include(t => t.Vehicle);

            // Apply filtering and sorting
            transportAssignments = ApplyFilteringAndSorting(transportAssignments, searchString, sortOrder);

            // Set a page size for pagination
            int pageSize = 7; // You can adjust this value as needed
            return View(await PaginatedList<TransportAssignment>.CreateAsync(transportAssignments.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // GET: TransportAssignments/DownloadCsv
        public async Task<IActionResult> DownloadCsv(string searchString, string sortOrder)
        {
            IQueryable<TransportAssignment> transportAssignments = _context.TransportAssignments
                .Include(t => t.Job)
                .Include(t => t.Driver)
                .Include(t => t.Assistant)
                .Include(t => t.Vehicle);

            // Apply the same filtering and sorting as the Index view
            transportAssignments = ApplyFilteringAndSorting(transportAssignments, searchString, sortOrder);

            var assignmentList = await transportAssignments.ToListAsync();

            var csvBuilder = new StringBuilder();

            // Add CSV Header
            csvBuilder.AppendLine("Transport ID,Job ID,Pickup Location,Delivery Location,Driver Name,Assistant Name,Vehicle Model,Vehicle License Number");

            // Add Data Rows
            foreach (var assignment in assignmentList)
            {
                // Use null-conditional operator (?.) to safely access properties of related entities
                // and provide a default "N/A" if the related entity is null.
                string jobId = assignment.Job?.JobId.ToString() ?? "N/A";
                string pickupLocation = assignment.Job?.PickupLocation ?? "N/A";
                string deliveryLocation = assignment.Job?.DeliveryLocation ?? "N/A";
                string driverName = assignment.Driver?.DriverName ?? "N/A";
                string assistantName = assignment.Assistant?.AssistantName ?? "N/A"; // Assistant might be nullable
                string vehicleModel = assignment.Vehicle?.VehicleModel ?? "N/A";
                string vehicleLicenseNum = assignment.Vehicle?.VehicleLicensenum ?? "N/A";

                csvBuilder.AppendLine($"{assignment.TransportId}," +
                                      $"{EscapeCsv(jobId)}," +
                                      $"{EscapeCsv(pickupLocation)}," +
                                      $"{EscapeCsv(deliveryLocation)}," +
                                      $"{EscapeCsv(driverName)}," +
                                      $"{EscapeCsv(assistantName)}," +
                                      $"{EscapeCsv(vehicleModel)}," +
                                      $"{EscapeCsv(vehicleLicenseNum)}");
            }

            var csvBytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
            // Adjust the filename based on whether a search filter was applied
            var fileName = !string.IsNullOrWhiteSpace(searchString) ? "Searched_TransportAssignments.csv" : "All_TransportAssignments.csv";

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
            // These characters are comma, double quote, and newline.
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                // To escape a double quote within a CSV field, you double it.
                // Then, the entire field is enclosed in double quotes.
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }


        // GET: TransportAssignments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transportAssignment = await _context.TransportAssignments
                .Include(t => t.Job)
                .Include(t => t.Driver)
                .Include(t => t.Assistant)
                .Include(t => t.Vehicle)
                .FirstOrDefaultAsync(m => m.TransportId == id);

            if (transportAssignment == null)
            {
                return NotFound();
            }

            return View(transportAssignment);
        }

        // GET: TransportAssignments/Create
        public async Task<IActionResult> Create()
        {
            await PopulateRelatedEntitiesAsync(); // Populate all dropdowns
            return View();
        }

        // POST: TransportAssignments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("JobId,DriverId,AssistantId,VehicleId")] TransportAssignment transportAssignment)
        {
            if (ModelState.IsValid)
            {
                _context.Add(transportAssignment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            // Re-populate dropdowns if validation fails
            await PopulateRelatedEntitiesAsync(transportAssignment.JobId, transportAssignment.DriverId, transportAssignment.AssistantId, transportAssignment.VehicleId);
            return View(transportAssignment);
        }

        // GET: TransportAssignments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transportAssignment = await _context.TransportAssignments.FindAsync(id);
            if (transportAssignment == null)
            {
                return NotFound();
            }
            // Populate dropdowns and set selected items
            await PopulateRelatedEntitiesAsync(transportAssignment.JobId, transportAssignment.DriverId, transportAssignment.AssistantId, transportAssignment.VehicleId);
            return View(transportAssignment);
        }

        // POST: TransportAssignments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TransportId,JobId,DriverId,AssistantId,VehicleId")] TransportAssignment transportAssignment)
        {
            if (id != transportAssignment.TransportId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(transportAssignment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.TransportAssignments.Any(e => e.TransportId == transportAssignment.TransportId))
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
            // Re-populate dropdowns if validation fails
            await PopulateRelatedEntitiesAsync(transportAssignment.JobId, transportAssignment.DriverId, transportAssignment.AssistantId, transportAssignment.VehicleId);
            return View(transportAssignment);
        }

        // GET: TransportAssignments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Eager load all related entities for display before deleting
            var transportAssignment = await _context.TransportAssignments
                .Include(t => t.Job)
                .Include(t => t.Driver)
                .Include(t => t.Assistant)
                .Include(t => t.Vehicle)
                .FirstOrDefaultAsync(m => m.TransportId == id);

            if (transportAssignment == null)
            {
                return NotFound();
            }

            return View(transportAssignment);
        }

        // POST: TransportAssignments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var transportAssignment = await _context.TransportAssignments.FindAsync(id);
            if (transportAssignment != null)
            {
                _context.TransportAssignments.Remove(transportAssignment);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}