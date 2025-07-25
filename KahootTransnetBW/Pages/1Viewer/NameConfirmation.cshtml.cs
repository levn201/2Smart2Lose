using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KahootTransnetBW.Pages._1Viewer
{
    public class NameConfirmationModel : PageModel
    {
        public void OnGet()
        {
        }


        public string nickname { get; set; }
        public string ErrorMessage { get; set; }



    }
}
