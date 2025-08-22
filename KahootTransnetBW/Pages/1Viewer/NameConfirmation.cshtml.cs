using KahootTransnetBW.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KahootTransnetBW.Pages._1Viewer
{
    public class NameConfirmationModel : PageModel
    {
        public void OnGet()
        {

        }

        [BindProperty] // Dies hinzufügen
        public string Nickname { get; set; }


        public string ErrorMessage { get; set; }

        // Entweder den Handler im Formular entfernen oder diese Methode umbenennen:
        public IActionResult OnPostLogName()
        {
            if (string.IsNullOrEmpty(Nickname))
            {
                ErrorMessage = "Bitte gebe einen Namen ein";
                return Page(); // Zeigt die gleiche Seite mit Fehler an
            }

            //HttpContext.Session.SetString("Name", Nickname);
            return RedirectToPage("/1Viewer/Playground");
        }
    }
}
