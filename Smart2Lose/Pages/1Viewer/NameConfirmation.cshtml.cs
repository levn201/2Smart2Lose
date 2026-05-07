using Smart2Lose.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Smart2Lose.Pages._1Viewer
{
    public class NameConfirmationModel : PageModel
    {
        public void OnGet()
        {

        }

        public projektName pn = new projektName();

        [BindProperty] 
        public string Nickname { get; set; }
        public string ErrorMessage { get; set; }


        public IActionResult OnPostLogName()
        {
            if (string.IsNullOrWhiteSpace(Nickname))
            {
                ErrorMessage = "Bitte gib einen Namen ein.";
                return Page();
            }

            if (Nickname.Length < 2)
            {
                ErrorMessage = "Der Name muss mindestens 2 Zeichen lang sein.";
                return Page();
            }

            if (Nickname.Length > 20)
            {
                ErrorMessage = "Der Name darf maximal 20 Zeichen lang sein.";
                return Page();
            }

            HttpContext.Session.SetString("Name", Nickname.Trim());
            return RedirectToPage("/1Viewer/Playground");
        }
    }
}
