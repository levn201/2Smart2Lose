using Smart2Lose.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Smart2Lose.Pages.testPages
{
    public class startPageModel : PageModel
    {
        [BindProperty]
        public int testNumber { get; set; } 


        [BindProperty]
        public string testTitle { get; set; }

        public void OnGet()
        {
        }



        [BindProperty]
        public string ComboResult { get; set; }

        public void OnCombo()
        {
            // Hier landet der ausgewählte Wert nach Submit
            var selectedValue = ComboResult;
            // z.B. Debug-Ausgabe
            Console.WriteLine($"Gewählt: {selectedValue}");
        }

        public IActionResult OnPost()
        {
            HttpContext.Session.SetString("Title", testTitle);
            HttpContext.Session.SetInt32("Number", testNumber);
            return RedirectToPage("/testPages/secondPage");
        }
    }
}
