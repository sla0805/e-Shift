using eShift.Models;
using eShift.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // Required for List
using System.IO; // Required for MemoryStream and StreamWriter
using System.Text; // Required for StringBuilder


public class CustomersController : Controller
{
    private readonly ApplicationDbContext _context;

    public CustomersController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string sortOrder, string searchString, int? pageNumber)
    {
        ViewData["CurrentFilter"] = searchString;
        ViewData["CurrentSort"] = sortOrder; // Pass current sort to view for icon logic

        ViewData["IdSortParm"] = String.IsNullOrEmpty(sortOrder) ? "id_desc" : "";
        ViewData["NameSortParm"] = sortOrder == "Name" ? "name_desc" : "Name";
        ViewData["EmailSortParm"] = sortOrder == "Email" ? "email_desc" : "Email";
        ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";

        var customers = from c in _context.Customers
                        select c;

        // 1. Filter (Search)
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var lowerSearchString = searchString.ToLower();

            customers = customers.Where(c =>
                c.CustName.ToLower().Contains(lowerSearchString) ||
                c.CustAddress.ToLower().Contains(lowerSearchString) ||
                c.CustPhone.ToLower().Contains(lowerSearchString) ||
                c.CustEmail.ToLower().Contains(lowerSearchString) ||
                c.CustId.ToString().Contains(lowerSearchString)); // Convert ID to string for search
        }

        // 2. Sort
        switch (sortOrder)
        {
            case "id_desc": // Sort by ID Descending
                customers = customers.OrderByDescending(c => c.CustId);
                break;
            case "Name": // Sort by Name Ascending
                customers = customers.OrderBy(c => c.CustName);
                break;
            case "name_desc": // Sort by Name Descending
                customers = customers.OrderByDescending(c => c.CustName);
                break;
            case "Email": // Sort by Email Ascending
                customers = customers.OrderBy(c => c.CustEmail);
                break;
            case "email_desc": // Sort by Email Descending
                customers = customers.OrderByDescending(c => c.CustEmail);
                break;
            case "Date": // Sort by RegisterDate Ascending
                customers = customers.OrderBy(c => c.CustRegisterDate);
                break;
            case "date_desc": // Sort by RegisterDate Descending
                customers = customers.OrderByDescending(c => c.CustRegisterDate);
                break;
            default: // Default sort: by ID Ascending
                customers = customers.OrderBy(c => c.CustId);
                break;
        }
        int pageSize = 5;
        return View(await PaginatedList<eShift.Models.Customer>.CreateAsync(customers.AsNoTracking(), pageNumber ?? 1, pageSize));

        // 3. Execute query and return to view
        return View(await customers.ToListAsync());
    }

    // New action to download CSV
    public async Task<IActionResult> DownloadCsv(string searchString, string sortOrder)
    {
        var customers = from c in _context.Customers
                        select c;

        // Apply search filter if provided
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var lowerSearchString = searchString.ToLower();
            customers = customers.Where(c =>
                c.CustName.ToLower().Contains(lowerSearchString) ||
                c.CustAddress.ToLower().Contains(lowerSearchString) ||
                c.CustPhone.ToLower().Contains(lowerSearchString) ||
                c.CustEmail.ToLower().Contains(lowerSearchString) ||
                c.CustId.ToString().Contains(lowerSearchString));
        }

        // Apply sorting if provided (same logic as Index)
        switch (sortOrder)
        {
            case "id_desc":
                customers = customers.OrderByDescending(c => c.CustId);
                break;
            case "Name":
                customers = customers.OrderBy(c => c.CustName);
                break;
            case "name_desc":
                customers = customers.OrderByDescending(c => c.CustName);
                break;
            case "Email":
                customers = customers.OrderBy(c => c.CustEmail);
                break;
            case "email_desc":
                customers = customers.OrderByDescending(c => c.CustEmail);
                break;
            case "Date":
                customers = customers.OrderBy(c => c.CustRegisterDate);
                break;
            case "date_desc":
                customers = customers.OrderByDescending(c => c.CustRegisterDate);
                break;
            default:
                customers = customers.OrderBy(c => c.CustId);
                break;
        }

        var customerList = await customers.ToListAsync();

        var csvBuilder = new StringBuilder();

        // Add CSV header
        csvBuilder.AppendLine("Customer ID,Name,Address,Phone,Email,Register Date");

        // Add CSV data
        foreach (var customer in customerList)
        {
            csvBuilder.AppendLine($"{customer.CustId},\"{customer.CustName.Replace("\"", "\"\"")}\",\"{customer.CustAddress.Replace("\"", "\"\"")}\",\"{customer.CustPhone.Replace("\"", "\"\"")}\",\"{customer.CustEmail.Replace("\"", "\"\"")}\",{customer.CustRegisterDate:yyyy-MM-dd}");
        }

        var csvBytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
        var fileName = !string.IsNullOrWhiteSpace(searchString) ? "Searched_Customers.csv" : "All_Customers.csv";

        return File(csvBytes, "text/csv", fileName);
    }
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var customer = await _context.Customers
            //.Include(c => c.Jobs) 
                  //  .ThenInclude(j => j.TransportAssignment)
            .FirstOrDefaultAsync(m => m.CustId == id);

        if (customer == null) return NotFound();

        return View(customer);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CustName,CustAddress,CustPhone,CustEmail,CustRegisterDate")] Customer customer)
    {
        if (ModelState.IsValid)
        {
            _context.Add(customer);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(customer);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();

        return View(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("CustId,CustName,CustAddress,CustPhone,CustEmail,CustRegisterDate")] Customer customer)
    {
        if (id != customer.CustId) return NotFound();

        if (ModelState.IsValid)
        {
            _context.Update(customer);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(customer);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var customer = await _context.Customers
            .FirstOrDefaultAsync(m => m.CustId == id);
        if (customer == null) return NotFound();

        return View(customer);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}