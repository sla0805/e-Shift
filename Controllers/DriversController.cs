using eShift.Models;
using eShift.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Text;

public class DriversController : Controller
{
    private readonly ApplicationDbContext _context;

    public DriversController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Drivers
    public async Task<IActionResult> Index(string sortOrder, string searchString, int? pageNumber)
    {
        ViewData["CurrentFilter"] = searchString;
        ViewData["CurrentSort"] = sortOrder; // Pass current sort to view for icon logic

        ViewData["IdSortParm"] = String.IsNullOrEmpty(sortOrder) ? "id_desc" : "";
        ViewData["NameSortParm"] = sortOrder == "Name" ? "name_desc" : "Name";

        var drivers = from d in _context.Drivers
                      select d;

        // 1. Filter (Search)
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var lowerSearchString = searchString.ToLower();

            drivers = drivers.Where(d =>
                d.DriverName.ToLower().Contains(lowerSearchString) ||
                d.DriverLicensenum.ToLower().Contains(lowerSearchString) || // Assuming DriverLicensenum is a string
                d.DriverPhone.ToLower().Contains(lowerSearchString) ||
                d.DriverId.ToString().Contains(lowerSearchString)); // Convert ID to string for search
        }

        // 2. Sort
        switch (sortOrder)
        {
            case "id_desc": // Sort by ID Descending
                drivers = drivers.OrderByDescending(d => d.DriverId);
                break;
            case "Name": // Sort by Name Ascending
                drivers = drivers.OrderBy(d => d.DriverName);
                break;
            case "name_desc": // Sort by Name Descending
                drivers = drivers.OrderByDescending(d => d.DriverName);
                break;
            default: // Default sort: by ID Ascending
                drivers = drivers.OrderBy(d => d.DriverId);
                break;
        }

        int pageSize = 10;
        return View(await PaginatedList<eShift.Models.Driver>.CreateAsync(drivers.AsNoTracking(), pageNumber ?? 1, pageSize));

        // 3. Execute query and return to view
        return View(await drivers.ToListAsync());
    }


    public async Task<IActionResult> DownloadCsv(string searchString, string sortOrder)
    {
        var drivers = from d in _context.Drivers
                      select d;

        // Apply search filter if provided (using Driver properties)
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var lowerSearchString = searchString.ToLower();
            drivers = drivers.Where(d =>
                d.DriverName.ToLower().Contains(lowerSearchString) ||
                d.DriverLicensenum.ToLower().Contains(lowerSearchString) ||
                d.DriverPhone.ToLower().Contains(lowerSearchString) ||
                d.DriverId.ToString().Contains(lowerSearchString)); // Convert ID to string for search
        }

        // Apply sorting if provided (same logic as Index method for drivers)
        switch (sortOrder)
        {
            case "id_desc": // Sort by ID Descending
                drivers = drivers.OrderByDescending(d => d.DriverId);
                break;
            case "Name": // Sort by Name Ascending
                drivers = drivers.OrderBy(d => d.DriverName);
                break;
            case "name_desc": // Sort by Name Descending
                drivers = drivers.OrderByDescending(d => d.DriverName);
                break;
            default: // Default sort: by ID Ascending
                drivers = drivers.OrderBy(d => d.DriverId);
                break;
        }

        var driverList = await drivers.ToListAsync();

        var csvBuilder = new StringBuilder();

        // Add CSV header (updated for Driver properties)
        csvBuilder.AppendLine("Driver ID,Driver Name,License Number,Phone Number");

        // Add CSV data (updated for Driver properties and escaping)
        foreach (var driver in driverList)
        {
            // Use the EscapeCsv helper function to properly handle commas and quotes in data
            csvBuilder.AppendLine($"{driver.DriverId}," +
                                  $"{EscapeCsv(driver.DriverName)}," +
                                  $"{EscapeCsv(driver.DriverLicensenum)}," +
                                  $"{EscapeCsv(driver.DriverPhone)}");
        }

        var csvBytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
        var fileName = !string.IsNullOrWhiteSpace(searchString) ? "Searched_Drivers.csv" : "All_Drivers.csv";

        return File(csvBytes, "text/csv", fileName);
    }
    // Helper to escape values for CSV (same as in the previous response, crucial for data integrity)
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

    // GET: Drivers/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var driver = await _context.Drivers
            .FirstOrDefaultAsync(m => m.DriverId == id); 
        if (driver == null)
        {
            return NotFound();
        }

        return View(driver);
    }

    // GET: Drivers/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Drivers/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("DriverName,DriverLicensenum,DriverPhone")] Driver driver) 
    {
        if (ModelState.IsValid)
        {
            _context.Add(driver);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(driver);
    }

    // GET: Drivers/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var driver = await _context.Drivers.FindAsync(id);
        if (driver == null)
        {
            return NotFound();
        }
        return View(driver);
    }

    // POST: Drivers/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("DriverId,DriverName,DriverLicensenum,DriverPhone")] Driver driver) 
    {
        if (id != driver.DriverId) 
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(driver);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Drivers.Any(e => e.DriverId == driver.DriverId)) 
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
        return View(driver);
    }

    // GET: Drivers/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var driver = await _context.Drivers
            .FirstOrDefaultAsync(m => m.DriverId == id); 
        if (driver == null)
        {
            return NotFound();
        }

        return View(driver);
    }

    // POST: Drivers/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var driver = await _context.Drivers.FindAsync(id);
        if (driver != null)
        {
            _context.Drivers.Remove(driver);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}