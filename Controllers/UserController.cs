using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using eShift.Models; // Your Customer model namespace
using eShift.Data; // Replace with your actual DbContext namespace, e.g., eShift.Data
using System;
using System.Linq; // For .FirstOrDefault()
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace eShift.Controllers // Make sure this namespace matches your project
{
    [Authorize] // Only authenticated users can access this controller
    public class UserController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context; 

        public UserController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }


        public IActionResult Index()
        {
            ViewData["Title"] = "User Dashboard";
            return View();
        }

        // GET: /UserDashboard/UpdateAccountInfo
        // Displays the form for updating account info and becoming a customer
        public async Task<IActionResult> UpdateAccountInfo()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                // This shouldn't happen if user is [Authorize], but good safeguard
                return RedirectToAction("Login", "Account");
            }

            var identityUser = await _userManager.GetUserAsync(User); // Get the logged-in IdentityUser
            if (identityUser == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            // Try to find an existing customer profile linked to this IdentityUser
            var customer = _context.Customers.FirstOrDefault(c => c.IdentityUserId == userId);

            // Directly use the Customer model for the view
            if (customer == null)
            {
                // If no customer profile exists yet, create a new Customer instance
                // and pre-fill with IdentityUser's email and current date for display
                customer = new Customer
                {
                    CustEmail = identityUser.Email,
                    CustRegisterDate = DateTime.Now // Set a default for display, will be set on POST for creation
                };
            }

            ViewData["Title"] = "Update Account Info";
            return View(customer); // Pass the Customer object directly to the view
        }

        //Change password func
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["Message"] = "Password changed successfully.";
                return RedirectToAction("Index", "User"); 
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // POST: /UserDashboard/UpdateAccountInfo
        // Handles the form submission to create/update customer details
        [HttpPost]
        [ValidateAntiForgeryToken] // Important for security
        public async Task<IActionResult> UpdateAccountInfo([Bind("CustId,CustName,CustAddress,CustPhone,CustEmail")] Customer model) // Directly bind to Customer model
        {
            ModelState.Remove("IdentityUserId"); 
            ModelState.Remove("IdentityUser");

            // Declare userId and identityUser ONCE at the beginning of the POST method
            var userId = _userManager.GetUserId(User);
            var identityUser = await _userManager.GetUserAsync(User); // This is the single declaration

            if (identityUser == null || userId == null)
            {
                TempData["Error"] = "User not found or not logged in. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            // --- IMPORTANT: Ensure the submitted CustEmail matches the logged-in IdentityUser's email for security ---
            // This is a critical check if CustEmail is part of your form input
            if (model.CustEmail != identityUser.Email)
            {
                ModelState.AddModelError("CustEmail", "The customer email must match your registered account email.");
                // If validation fails due to email mismatch, ensure other fields are still bound
                // and CustRegisterDate is set correctly before returning the view.
                if (model.CustId == 0) // If it's a new entry
                {
                    model.CustRegisterDate = DateTime.Now;
                }
                ViewData["Title"] = "Update Account Info";
                return View(model);
            }
            // --- End Email Security Check ---

            if (!ModelState.IsValid)
            {
                // If validation fails (for other fields), ensure CustRegisterDate is set correctly for display
                if (model.CustId == 0)
                {
                    model.CustRegisterDate = DateTime.Now; // For new customer, retain display value
                }
                ViewData["Title"] = "Update Account Info";
                return View(model);
            }

            // Try to find an existing customer profile
            var customer = _context.Customers.FirstOrDefault(c => c.IdentityUserId == userId);

            if (customer == null)
            {
                // CREATE NEW CUSTOMER PROFILE
                customer = new Customer
                {
                    IdentityUserId = userId, // Link to the IdentityUser
                    CustName = model.CustName,
                    CustAddress = model.CustAddress,
                    CustPhone = model.CustPhone,
                    CustEmail = model.CustEmail,
                    CustRegisterDate = DateTime.Now // Set registration date upon creation
                };
                _context.Customers.Add(customer);
                TempData["Message"] = "Your customer profile has been created successfully!";
            }
            else
            {
                // UPDATE EXISTING CUSTOMER PROFILE
                
                customer.CustName = model.CustName;
                customer.CustAddress = model.CustAddress;
                customer.CustPhone = model.CustPhone;
                customer.CustEmail = model.CustEmail; 
                _context.Customers.Update(customer); // Mark entity as modified
                TempData["Message"] = "Your account information has been updated successfully!";
            }

            try
            {
                await _context.SaveChangesAsync();
                
                TempData["Message"] = "Data saved to database!"; // Add this for testing feedback
            }
            catch (Exception ex)
            {
                
                TempData["Error"] = $"An error occurred while saving: {ex.Message}";
                
                return View(model); // Stay on the page with an error message
            }


            return RedirectToAction("Index"); // Redirect back to the user dashboard
        }

        // GET Create Job
        public IActionResult CreateJob()
        {
            var job = new Job
            {
                OrderDate = DateTime.Now,
                ScheduledDate = DateTime.Now.AddDays(1)
            };
            return View(job);
        }

        // POST Create Job
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateJob(Job job)
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.IdentityUserId == userId);

            if (customer == null)
            {
                TempData["Error"] = "Customer profile not found. Please update your customer info first to make your delivery.";
                return RedirectToAction("UpdateAccountInfo");
            }

            try
            {
                job.CustId = customer.CustId;
                job.JobStatus = "Pending";

                _context.Jobs.Add(job);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Job created successfully.";
                return RedirectToAction("AddLoad", new { jobId = job.JobId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while saving the job: " + ex.Message;
                return View(job);
            }
        }






        // GET Add Load
        [HttpGet]
        public async Task<IActionResult> AddLoad(int jobId)
        {
            var productList = await _context.Products
                .Select(p => new SelectListItem
                {
                    Value = p.ProductId.ToString(),
                    Text = p.ProductName
                }).ToListAsync();

            var existingLoads = await _context.Loads
                .Where(l => l.JobId == jobId)
                .Include(l => l.Product)
                .ToListAsync();

           // ViewBag.JobId = jobId;
            ViewBag.ProductList = productList;
            ViewBag.ExistingLoads = existingLoads;

            return View(new Load { JobId = jobId });

    }

        // POST Add Load
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLoad(Load load)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ProductList = await _context.Products
                    .Select(p => new SelectListItem
                    {
                        Value = p.ProductId.ToString(),
                        Text = p.ProductName
                    }).ToListAsync();

                ViewBag.ExistingLoads = await _context.Loads
                    .Where(l => l.JobId == load.JobId)
                    .Include(l => l.Product)
                    .ToListAsync();

                TempData["Error"] = "Please correct the errors in the form.";
                return View(load);
            }

            try
            {
                _context.Loads.Add(load);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Load added successfully.";
            }
            catch (DbUpdateException dbEx)
            {
                TempData["Error"] = "Database error: " + dbEx.Message;
                // Log exception here if needed
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Unexpected error: " + ex.Message;
            }

            // After adding, redirect to the same page to add more loads
            return RedirectToAction("AddLoad", new { jobId = load.JobId });
        }


        public IActionResult FinishDelivery(int jobId)
        {
            TempData["Message"] = "Delivery created successfully!";
            return RedirectToAction("Index");
        }

        // GET: ViewMyJobs
        [HttpGet]
        public async Task<IActionResult> ViewMyJobs()
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.IdentityUserId == userId);

            if (customer == null)
            {
                TempData["Error"] = "Customer profile not found.";
                return RedirectToAction("UpdateAccountInfo");
            }

            var jobsWithLoads = await _context.Jobs
                .Where(j => j.CustId == customer.CustId)
                .Include(j => j.Loads)
                .ThenInclude(l => l.Product)
                .ToListAsync();

            return View(jobsWithLoads);
        }

        // GET: Edit Job
        [HttpGet]
        public async Task<IActionResult> EditJob(int id)
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.IdentityUserId == userId);

            if (customer == null)
            {
                TempData["Error"] = "Customer profile not found. Please update your account info first.";
                return RedirectToAction("UpdateAccountInfo");
            }

            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.JobId == id && j.CustId == customer.CustId);

            if (job == null)
            {
                return NotFound();
            }

            if (job.JobStatus != "Pending")
            {
                TempData["Error"] = "You can only edit jobs with Pending status.";
                return RedirectToAction("ViewMyJobs");
            }

            return View("CreateJob", job);  // Reuse CreateJob view for editing
        }

        // POST: Edit Job
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditJob(Job job)
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.IdentityUserId == userId);

            if (customer == null)
            {
                TempData["Error"] = "Customer profile not found. Please update your account info first.";
                return RedirectToAction("UpdateAccountInfo");
            }

            if (job.CustId != customer.CustId)
            {
                return Unauthorized();
            }

            if (job.JobStatus != "Pending")
            {
                TempData["Error"] = "You can only edit jobs with Pending status.";
                return RedirectToAction("ViewMyJobs");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Jobs.Update(job);
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Job updated successfully!";
                    return RedirectToAction("ViewMyJobs");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error updating job: " + ex.Message);
                }
            }

            return View("CreateJob", job);
        }
        //Delete Job
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteJob(int id)
        {
            var job = await _context.Jobs.Include(j => j.Loads).FirstOrDefaultAsync(j => j.JobId == id);

            if (job == null)
            {
                TempData["Error"] = "Job not found.";
                return RedirectToAction("ViewMyJobs");
            }

            // Remove all related loads first (if needed)
            if (job.Loads != null)
            {
                _context.Loads.RemoveRange(job.Loads);
            }

            _context.Jobs.Remove(job);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Job deleted successfully.";
            return RedirectToAction("ViewMyJobs");
        }



        // GET: Edit Load
        [HttpGet]
        public async Task<IActionResult> EditLoad(int id)
        {
            var load = await _context.Loads.Include(l => l.Job).FirstOrDefaultAsync(l => l.LoadId == id);
            if (load == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.IdentityUserId == userId);

            if (customer == null || load.Job.CustId != customer.CustId)
            {
                return Unauthorized();
            }

            if (load.Job.JobStatus != "Pending")
            {
                TempData["Error"] = "You can only edit loads of jobs with Pending status.";
                return RedirectToAction("ViewMyJobs");
            }

            // Prepare product list for dropdown
            ViewBag.ProductList = await _context.Products
                .Select(p => new SelectListItem
                {
                    Value = p.ProductId.ToString(),
                    Text = p.ProductName
                }).ToListAsync();

            return View("AddLoad", load);  // Reuse AddLoad view for editing
        }

        // POST: Edit Load
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLoad(Load load)
        {
            var existingLoad = await _context.Loads.Include(l => l.Job).FirstOrDefaultAsync(l => l.LoadId == load.LoadId);
            if (existingLoad == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.IdentityUserId == userId);

            if (customer == null || existingLoad.Job.CustId != customer.CustId)
            {
                return Unauthorized();
            }

            if (existingLoad.Job.JobStatus != "Pending")
            {
                TempData["Error"] = "You can only edit loads of jobs with Pending status.";
                return RedirectToAction("ViewMyJobs");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update load properties
                    existingLoad.ProductId = load.ProductId;
                    existingLoad.ProductQuantity = load.ProductQuantity;
                    existingLoad.ProductContainer = load.ProductContainer;
                    existingLoad.LoadWeightKg = load.LoadWeightKg;
                    existingLoad.Comment = load.Comment;

                    _context.Loads.Update(existingLoad);
                    await _context.SaveChangesAsync();

                    TempData["Message"] = "Load updated successfully!";
                    return RedirectToAction("ViewMyJobs");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error updating load: " + ex.Message);
                }
            }

            ViewBag.ProductList = await _context.Products
                .Select(p => new SelectListItem
                {
                    Value = p.ProductId.ToString(),
                    Text = p.ProductName
                }).ToListAsync();

            return View("AddLoad", load);
        }

        // GET: User/DeleteLoad/5
        [HttpGet]
        public async Task<IActionResult> DeleteLoad(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var load = await _context.Loads
                .Include(l => l.Product)
                .FirstOrDefaultAsync(m => m.LoadId == id);

            if (load == null)
            {
                return NotFound();
            }

            return View(load);
        }

        // POST: User/DeleteLoad/5
        [HttpPost, ActionName("DeleteLoad")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var load = await _context.Loads.FindAsync(id);
            if (load != null)
            {
                _context.Loads.Remove(load);
                await _context.SaveChangesAsync();
            }

            // Redirect to the job editing page or list of jobs
            return RedirectToAction("ViewMyJobs");
        }

    }

}
