using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Smart2Lose.Model;

namespace Smart2Lose.Pages.testPages
{
    public class secondPageModel : PageModel
    {
        public int testNumber { get; set; } // Verwende einen nicht-nullbaren Typ, um null-Werte zu vermeiden
        public string testTitle { get; set; }

        public void OnGet()
        {
            loadSessions();
        }



        //Packe es in eine Methode um den OnGet nicht so zu mülle 
        public void loadSessions()
        {
            // mit ?? checkt man ob der wert 0 oder nichtss ist 
            testNumber = HttpContext.Session.GetInt32("Number") ?? 0;
            testTitle = HttpContext.Session.GetString("Title") ?? "";
        }
    }
}
