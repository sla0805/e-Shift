
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using eShift.Models;

namespace eShift.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AccountController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;

        }

        // ✅ List all users with their role status
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var model = new List<UserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                model.Add(new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    IsAdmin = roles.Contains("Admin")
                });
            }

            return View(model);
        }

        //REGISTER function
        [HttpGet]
        [AllowAnonymous] 
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous] 
        public async Task<IActionResult> Register(RegisterViewModel model) 
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Optional: Sign in the user immediately after registration
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "User");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            // If model state is not valid or creation failed, return the view with errors
            return View(model);
        }


        //LOGIN function
        [HttpGet]
        [AllowAnonymous] 
        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid login attempt");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);

                // ✅ Redirect based on role
                if (roles.Contains("Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    return RedirectToAction("Index", "User");
                }
            }

            ModelState.AddModelError("", "Invalid login attempt");
            return View(model);
        }

        // Change Password func
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index", "Home");
            }

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["Message"] = "Password changed successfully!";
                return RedirectToAction("Index", "Admin");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // ✅ Promote user by ID
        [HttpPost]
        public async Task<IActionResult> Promote(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            var result = await _userManager.AddToRoleAsync(user, "Admin");

            if (result.Succeeded)
            {
                TempData["Message"] = $"{user.Email} promoted to Admin.";
            }
            else
            {
                // Handle errors if promotion fails
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                TempData["Error"] = $"Failed to promote {user.Email} to Admin.";
            }

            return RedirectToAction("Index");
        }

        // ✅ Demote Admin to User by ID
        [HttpPost]
        [Authorize(Roles = "Admin")] // Only Admin users can perform demotion
        public async Task<IActionResult> Demote(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return NotFound(); // Or RedirectToAction("Index")
            }

            // --- IMPORTANT: Prevent Demoting the Last Admin ---
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            if (adminUsers.Count == 1 && adminUsers.Any(u => u.Id == user.Id))
            {
                TempData["Error"] = "Cannot demote the last remaining Admin user. Please ensure there's at least one other Admin.";
                return RedirectToAction("Index");
            }
            // --- END of IMPORTANT check ---

            // Check if the user is currently an Admin
            if (!await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = $"{user.Email} is not an Admin.";
                return RedirectToAction("Index");
            }

            // Remove the Admin role
            var result = await _userManager.RemoveFromRoleAsync(user, "Admin");

            if (result.Succeeded)
            {
                // If the user being demoted is the currently logged-in user, sign them out
                // This ensures their claims are updated and they no longer have admin access
                if (user.Id == _userManager.GetUserId(User))
                {
                    await _signInManager.SignOutAsync();
                    TempData["Message"] = "You have successfully demoted yourself. You are now logged out.";
                    return RedirectToAction("Login", "Account"); // Redirect to login page
                }

                TempData["Message"] = $"{user.Email} demoted to regular user successfully.";
            }
            else
            {
                TempData["Error"] = $"Failed to demote {user.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home"); // Redirect to home page after logout
        }
    }
}
