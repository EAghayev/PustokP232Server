using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pustok.DAL;
using Pustok.Models;
using Pustok.ViewModels;
using System.Security.Policy;

namespace Pustok.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly PustokDbContext _context;

        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, RoleManager<IdentityRole> roleManager, PustokDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
        }

        //public async Task<IActionResult> CreateRole()
        //{
        //    IdentityRole role1 = new IdentityRole { Name = "Member" };
        //    IdentityRole role2 = new IdentityRole { Name = "Admin" };
        //    IdentityRole role3 = new IdentityRole { Name = "SuperAdmin" };

        //    await _roleManager.CreateAsync(role1);
        //    await _roleManager.CreateAsync(role2);
        //    await _roleManager.CreateAsync(role3);

        //    return Ok();
        //}
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(UserLoginViewModel loginVM,string returnUrl)
        {
            AppUser user = await _userManager.FindByNameAsync(loginVM.UserName);

            if (user == null || user.IsAdmin)
            {
                ModelState.AddModelError("", "UserName or Password is incorrect!");
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(user, loginVM.Password, loginVM.RememberMe, false);

            if(!result.Succeeded)
            {
                ModelState.AddModelError("", "UserName or Password is incorrect!");
                return View();
            }

            return returnUrl!=null?Redirect(returnUrl):RedirectToAction("index", "home");
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(UserRegisterViewModel registerVM)
        {
            if (_userManager.Users.Any(x => x.NormalizedEmail == registerVM.Email.ToUpper()))
            {
                ModelState.AddModelError("", "Email is already taken");
                return View();
            }
              

            AppUser appUser = new AppUser
            {
                UserName = registerVM.UserName,
                FullName = registerVM.FullName
            };

            var result = await _userManager.CreateAsync(appUser, registerVM.Password);

            if (!result.Succeeded)
            {
                foreach (var item in result.Errors)
                    ModelState.AddModelError("", item.Description);

                return View();
            }

            await _userManager.AddToRoleAsync(appUser, "Member");

            await _signInManager.SignInAsync(appUser, false);

            return RedirectToAction("index", "home");
        }

        
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("index", "home");
        }

        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(UserForgotViewModel forgotVM)
        {
            AppUser user = await _userManager.FindByEmailAsync(forgotVM.Email);

            if (user == null || user.IsAdmin) return View("error");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var url = Url.Action("verify", "account", new { email = forgotVM.Email, token = token },Request.Scheme);

            return Json(new { url = url });
        }


        public async Task<IActionResult> Verify(string email,string token)
        {
            AppUser user = await _userManager.FindByEmailAsync(email);

            var result = await _userManager.VerifyUserTokenAsync(user, _userManager.Options.Tokens.PasswordResetTokenProvider, "ResetPassword", token);

            if (result)
            {
                TempData["Email"] = email;
                TempData["Token"] = token;
                return RedirectToAction("ResetPassword");
            }

            return RedirectToAction("index");
        }

        public IActionResult ResetPassword()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ResetPassword(UserResetPasswordViewModel resetVM)
        {
            AppUser user = await _userManager.FindByEmailAsync(resetVM.Email);

            var result = await _userManager.ResetPasswordAsync(user, resetVM.Token, resetVM.Password);

            if (!result.Succeeded)
            {
                return View("Error");
            }

            return RedirectToAction("login");
        }


        [Authorize(Roles ="Member")]
        public async Task<IActionResult> Profile()
        {
            AppUser user = await _userManager.FindByNameAsync(User.Identity.Name);

            ProfileViewModel profileVM = new ProfileViewModel
            {
                User = new UserUpdateViewModel
                {
                    FullName = user.FullName,
                    Email = user.Email,
                    UserName = user.UserName
                },
                Orders = _context.Orders.Include(x=>x.OrderItems).ThenInclude(x=>x.Book).Where(x=>x.AppUserId == user.Id).ToList()
            };

            return View(profileVM);
        }

        [Authorize(Roles = "Member")]
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(UserUpdateViewModel updateVM)
        {
            ProfileViewModel profileVM = new ProfileViewModel
            {
                User = updateVM,
            };

            if (!ModelState.IsValid)
                return View("Profile", profileVM);


            AppUser user = await _userManager.FindByNameAsync(User.Identity.Name);

        profileVM.Orders = _context.Orders.Include(x => x.OrderItems).ThenInclude(x => x.Book).Where(x => x.AppUserId == user.Id).ToList();

            if (updateVM.Password != null)
            {
                var passwordResult = await _userManager.ChangePasswordAsync(user, updateVM.CurrentPassword, updateVM.Password);

                if(!passwordResult.Succeeded)
                {
                    foreach (var item in passwordResult.Errors)
                        ModelState.AddModelError("", item.Description);

                    return View("Profile", profileVM);
                }
            }

            user.FullName = updateVM.FullName;
            user.Email = updateVM.Email;
            user.UserName = updateVM.UserName;

            var result = await _userManager.UpdateAsync(user);

            if(!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError("", err.Description);

                return View("Profile", profileVM);
            }

            //change password

            await _signInManager.SignInAsync(user, true);
            return RedirectToAction("profile");
        }
    }
}
