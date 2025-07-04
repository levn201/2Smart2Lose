using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KahootTransnetBW.Pages.Viewer
{
    public class FragenAnswerModel : PageModel
    {
        [BindProperty]
        public string vorname { get; set; }

        [BindProperty]
        public string nachname { get; set; }

        [TempData]
        public string UserError { get; set; } 

        public IActionResult OnPost()
        {
            if (NameIn(vorname, nachname))
            {
                return RedirectToPage("PlayQuizz");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Gebe einen Namen ein");
                return Page(); 
            }
        }

        private bool NameIn(string vorname, string nachname)
        {
            return string.IsNullOrWhiteSpace(vorname);
        }



    }
}
