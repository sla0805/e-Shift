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
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper method to populate Product Types for dropdown
        private void PopulateProductTypes(object selectedProductType = null)
        {
            var productTypes = new List<string>
            {
                "Electronic Appliances",
                "Clothing",
                "Decorations",
                "Furniture",
                "Kitchenware",
                "Outdoor Equipment",
                "Other"
            };

            ViewBag.ProductTypes = new SelectList(productTypes, selectedProductType);
        }

        // Helper method to apply filtering and sorting for Products
        private IQueryable<Product> ApplyFilteringAndSorting(IQueryable<Product> products, string searchString, string sortOrder)
        {
            // 1. Filter (Search)
            if (!string.IsNullOrEmpty(searchString))
            {
                var lowerSearchString = searchString.ToLower();

                products = products.Where(p =>
                    p.ProductName.ToLower().Contains(lowerSearchString) ||
                    (p.ProductType != null && p.ProductType.ToLower().Contains(lowerSearchString)) ||
                    p.ProductId.ToString().Contains(lowerSearchString) // Allow searching by Product ID
                );
            }

            // 2. Sort
            switch (sortOrder)
            {
                case "id_desc":
                    products = products.OrderByDescending(p => p.ProductId);
                    break;
                case "Name":
                    products = products.OrderBy(p => p.ProductName);
                    break;
                case "name_desc":
                    products = products.OrderByDescending(p => p.ProductName);
                    break;
                case "Type":
                    products = products.OrderBy(p => p.ProductType);
                    break;
                case "type_desc":
                    products = products.OrderByDescending(p => p.ProductType);
                    break;
                default: // Default sort: by ID Ascending
                    products = products.OrderBy(p => p.ProductId);
                    break;
            }
            return products;
        }


        // GET: Products
        public async Task<IActionResult> Index(string sortOrder, string searchString, int? pageNumber) // Added sortOrder and pageNumber
        {
            // Set search filter to maintain its value in the view
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentSort"] = sortOrder; // Pass current sort to view for icon logic

            // Set sort parameters to toggle on click in the view
            ViewData["IdSortParm"] = String.IsNullOrEmpty(sortOrder) ? "id_desc" : "";
            ViewData["NameSortParm"] = sortOrder == "Name" ? "name_desc" : "Name";
            ViewData["TypeSortParm"] = sortOrder == "Type" ? "type_desc" : "Type";

            var products = from p in _context.Products
                           select p;

            // Apply filtering and sorting using the new helper method
            products = ApplyFilteringAndSorting(products, searchString, sortOrder);
   
            int pageSize = 10;
            return View(await PaginatedList<eShift.Models.Product>.CreateAsync(products.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // DownloadCsv
        public async Task<IActionResult> DownloadCsv(string searchString, string sortOrder)
        {
            var products = from p in _context.Products
                           select p;

            // Apply search filter and sorting using the helper method
            products = ApplyFilteringAndSorting(products, searchString, sortOrder);

            var productList = await products.ToListAsync();

            var csvBuilder = new StringBuilder();

            // Add CSV header for Product properties
            csvBuilder.AppendLine("Product ID,Product Name,Product Type");

            // Add CSV data
            foreach (var product in productList)
            {
                // Use the EscapeCsv helper function to properly handle commas and quotes in data
                csvBuilder.AppendLine($"{product.ProductId}," +
                                      $"{EscapeCsv(product.ProductName)}," +
                                      $"{EscapeCsv(product.ProductType)}");
            }

            var csvBytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
            var fileName = !string.IsNullOrWhiteSpace(searchString) ? "Searched_Products.csv" : "All_Products.csv";

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

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            PopulateProductTypes(); // Populate ViewBag for the dropdown
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Create([Bind("ProductName,ProductType")] Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index)); // Redirect to the list view
            }
            PopulateProductTypes(product.ProductType); // Re-populate for dropdown if validation fails
            return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id); // Find the product by ID
            if (product == null)
            {
                return NotFound();
            }
            PopulateProductTypes(product.ProductType); // Populate ViewBag and set selected item
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Edit(int id, [Bind("ProductId,ProductName,ProductType")] Product product)
        {
            if (id != product.ProductId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product); // Update the product record
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException) // Handle concurrency issues
                {
                    if (!_context.Products.Any(e => e.ProductId == product.ProductId))
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
            PopulateProductTypes(product.ProductType); // Re-populate for dropdown if validation fails
            return View(product);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id); // Find the product to delete
            if (product != null)
            {
                _context.Products.Remove(product); // Remove the product
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index)); // Redirect to the list view
        }
        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}