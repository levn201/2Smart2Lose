//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Internal;
//using Microsoft.AspNetCore.Mvc;
//using Smart2Lose.Model;


//namespace Smart2Lose.Data
//{
//    public class AccountController : Controller
//    {
//        private readonly UserManager<IdentityUser> _userManager;
//        private readonly SignInManager<IdentityUser> _signInManager;

//        public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
//        {
//            _userManager = userManager;
//            _signInManager = signInManager;
//        }
//        [HttpGet]
//        public IActionResult Register()
//        {
//            return View();
//        }

//        [HttpPost]
//        public async Task<IActionResult> Register(Register model)
//        {
//            if (!ModelState.IsValid)
//                return View(model);

//            var user = new IdentityUser
//            {
//                UserName = model.Email,
//                Email = model.Email
//            };

//            var result = await _userManager.CreateAsync(user, model.Password);

//            if (result.Succeeded)
//            {
//                await _userManager.AddToRoleAsync(user, "User");
//                await _signInManager.SignInAsync(user, false);
//                return RedirectToAction("Index", "Home");
//            }

//            foreach (var error in result.Errors)
//                ModelState.AddModelError(string.Empty, error.Description);

//            return View(model);
//        }








//    }
//}
