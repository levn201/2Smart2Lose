using KahootTransnetBW.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KahootTransnetBW.Pages.testPages
{
    public class startPageModel : PageModel
    {
        [BindProperty]
        public int testNumber { get; set; } // [BindProperty] sorgt dafür, dass der Wert gebunden wird

        [BindProperty]
        public string testTitle { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            HttpContext.Session.SetString("Title", testTitle);
            HttpContext.Session.SetInt32("Number", testNumber);
            return RedirectToPage("/testPages/secondPage");
        }
    }
}
