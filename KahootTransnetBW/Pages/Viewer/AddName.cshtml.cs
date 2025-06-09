using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KahootTransnetBW.Pages.Viewer
{
    public class FragenAnswerModel : PageModel
    {
        [BindProperty]
        public string participant { get; set; }

        [BindProperty]
        public string UserError { get; set; }
        public void OnGet()
        {
        }


    }
}
