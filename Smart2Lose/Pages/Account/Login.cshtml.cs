using log4net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

public class LoginModel : PageModel
{
    private static readonly ILog log =
       LogManager.GetLogger(typeof(LoginModel));


    private readonly SignInManager<IdentityUser> _signInManager;

    public LoginModel(SignInManager<IdentityUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public string ReturnUrl { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }

    public void OnGet(string returnUrl = null)
    {
        ReturnUrl = returnUrl ?? "/Admin/Dashboard";
    }

    public async Task<IActionResult> OnPostAsync(string returnUrl = null)
    {

        log.Info($"Login attempt for user: {Input.Email}");


        returnUrl ??= "/Admin/Dashboard";

        if (!ModelState.IsValid)
            return Page();
        log.Info($"Model state is valid for user: {Input.Email}");

        var result = await _signInManager.PasswordSignInAsync(
            Input.Email,
            Input.Password,
            Input.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            log.Info($"Login successful for user: {Input.Email}");
            return LocalRedirect(returnUrl);
        }

        ModelState.AddModelError(string.Empty, "Login fehlgeschlagen");
        log.Warn($"Login failed for user: {Input.Email}");
        return Page();
        

        // hello leute 
    }
}
