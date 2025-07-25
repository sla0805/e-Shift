using eShift.Models;
using eShift.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;   // Added for StringBuilder
using System.IO;     // Added for file operations

namespace eShift.Controllers
{
    public class VehiclesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VehiclesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper method to populate Vehicle Types for dropdown
        private void PopulateVehicleTypes(object selectedVehicleType = null)
        {
            var vehicleTypes = new List<string>
            {
                "Truck",
                "Van",
                "Pickup",
                "Motorcycle",
                "Car",
                "Other"
            };

            ViewBag.VehicleTypes = new SelectList(vehicleTypes, selectedVehicleType);
        }

        // Helper method to apply filtering and sorting for Vehicles
        private IQueryable<Vehicle> ApplyFilteringAndSorting(IQueryable<Vehicle> vehicles, string searchString, string sortOrder)
        {
            // 1. Filter (Search)
            if (!string.IsNullOrEmpty(searchString))
            {
                var lowerSearchString = searchString.ToLower();

                vehicles = vehicles.Where(v =>
                    v.VehicleModel.ToLower().Contains(lowerSearchString) ||
                    v.VehicleLicensenum.ToLower().Contains(lowerSearchString) ||
                    v.VehicleType.ToLower().Contains(lowerSearchString) ||
                    v.CapacityKg.ToString().Contains(lowerSearchString) || // Allow searching by capacity
                    v.VehicleId.ToString().Contains(lowerSearchString)); // Allow searching by ID
            }

            // 2. Sort
            switch (sortOrder)
            {
                case "id_desc":
                    vehicles = vehicles.OrderByDescending(v => v.VehicleId);
                    break;
                case "Model":
                    vehicles = vehicles.OrderBy(v => v.VehicleModel);
                    break;
                case "model_desc":
                    vehicles = vehicles.OrderByDescending(v => v.VehicleModel);
                    break;
                case "LicenseNum":
                    vehicles = vehicles.OrderBy(v => v.VehicleLicensenum);
                    break;
                case "licensenum_desc":
                    vehicles = vehicles.OrderByDescending(v => v.VehicleLicensenum);
                    break;
                case "Type":
                    vehicles = vehicles.OrderBy(v => v.VehicleType);
                    break;
                case "type_desc":
                    vehicles = vehicles.OrderByDescending(v => v.VehicleType);
                    break;
                case "Capacity":
                    vehicles = vehicles.OrderBy(v => v.CapacityKg);
                    break;
                case "capacity_desc":
                    vehicles = vehicles.OrderByDescending(v => v.CapacityKg);
                    break;
                default: // Default sort: by ID Ascending
                    vehicles = vehicles.OrderBy(v => v.VehicleId);
                    break;
            }
            return vehicles;
        }

        // GET: Vehicles
        public async Task<IActionResult> Index(string sortOrder, string searchString, int? pageNumber) // Added sortOrder parameter
        {
            // Set search filter to maintain its value in the view
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentSort"] = sortOrder; // Pass current sort to view for icon logic

            // Set sort parameters to toggle on click in the view
            ViewData["IdSortParm"] = String.IsNullOrEmpty(sortOrder) ? "id_desc" : "";
            ViewData["ModelSortParm"] = sortOrder == "Model" ? "model_desc" : "Model";
            ViewData["LicenseNumSortParm"] = sortOrder == "LicenseNum" ? "licensenum_desc" : "LicenseNum";
            ViewData["TypeSortParm"] = sortOrder == "Type" ? "type_desc" : "Type";
            ViewData["CapacitySortParm"] = sortOrder == "Capacity" ? "capacity_desc" : "Capacity";

            var vehicles = from v in _context.Vehicles
                           select v;

            // Apply filtering and sorting using the new helper method
            vehicles = ApplyFilteringAndSorting(vehicles, searchString, sortOrder);

            int pageSize = 10;
            return View(await PaginatedList<eShift.Models.Vehicle>.CreateAsync(vehicles.AsNoTracking(), pageNumber ?? 1, pageSize));

            // Return the filtered and sorted list of vehicles to the view
            return View(await vehicles.ToListAsync());
        }

        // GET: Vehicles/DownloadCsv
        public async Task<IActionResult> DownloadCsv(string searchString, string sortOrder)
        {
            var vehicles = from v in _context.Vehicles
                           select v;

            // Apply search filter and sorting using the helper method
            vehicles = ApplyFilteringAndSorting(vehicles, searchString, sortOrder);

            var vehicleList = await vehicles.ToListAsync();

            var csvBuilder = new StringBuilder();

            // Add CSV header for Vehicle properties
            csvBuilder.AppendLine("Vehicle ID,Vehicle Model,License Number,Vehicle Type,Capacity (kg)");

            // Add CSV data
            foreach (var vehicle in vehicleList)
            {
                // Use the EscapeCsv helper function to properly handle commas and quotes in data
                csvBuilder.AppendLine($"{vehicle.VehicleId}," +
                                      $"{EscapeCsv(vehicle.VehicleModel)}," +
                                      $"{EscapeCsv(vehicle.VehicleLicensenum)}," +
                                      $"{EscapeCsv(vehicle.VehicleType)}," +
                                      $"{vehicle.CapacityKg}"); // CapacityKg is int/double, no need to escape
            }

            var csvBytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
            var fileName = !string.IsNullOrWhiteSpace(searchString) ? "Searched_Vehicles.csv" : "All_Vehicles.csv";

            return File(csvBytes, "text/csv", fileName);
        }

        // Helper to escape values for CSV (crucial for data integrity)
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

        // GET: Vehicles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(m => m.VehicleId == id);
            if (vehicle == null)
            {
                return NotFound();
            }

            return View(vehicle);
        }

        // GET: Vehicles/Create
        public IActionResult Create()
        {
            PopulateVehicleTypes(); // Populate ViewBag for the dropdown
            return View();
        }

        // POST: Vehicles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("VehicleModel,VehicleLicensenum,VehicleType,CapacityKg")] Vehicle vehicle)
        {
            if (ModelState.IsValid)
            {
                _context.Add(vehicle);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            PopulateVehicleTypes(vehicle.VehicleType); // Re-populate for dropdown if validation fails
            return View(vehicle);
        }

        // GET: Vehicles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null)
            {
                return NotFound();
            }
            PopulateVehicleTypes(vehicle.VehicleType); // Populate ViewBag and set selected item
            return View(vehicle);
        }

        // POST: Vehicles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("VehicleId,VehicleModel,VehicleLicensenum,VehicleType,CapacityKg")] Vehicle vehicle)
        {
            if (id != vehicle.VehicleId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(vehicle);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Vehicles.Any(e => e.VehicleId == vehicle.VehicleId))
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
            PopulateVehicleTypes(vehicle.VehicleType); // Re-populate for dropdown if validation fails
            return View(vehicle);
        }

        // GET: Vehicles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vehicle = await _context.Vehicles
                .FirstOrDefaultAsync(m => m.VehicleId == id);
            if (vehicle == null)
            {
                return NotFound();
            }

            return View(vehicle);
        }

        // POST: Vehicles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle != null)
            {
                _context.Vehicles.Remove(vehicle);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}