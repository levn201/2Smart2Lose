using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using Smart2Lose.Model;
using Smart2Lose.Helper;


namespace Smart2Lose.Pages
{
    public class IndexModel : PageModel
    {

        public void OnGet()
        {
        }

        public projektName pn = new projektName();

        public string ErrorMessage { get; set; } = string.Empty;

        [BindProperty]
        public SpielDurchlauf session { get; set; } = new SpielDurchlauf();


        // Eingabe Feld => VErweis auf Seiten oder Login ins Spiel
        public IActionResult OnPost()
        {
            try
            {
                // Allgemeine PIN-Prüfung gegen Datenbank
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                string query = "SELECT Join_ID FROM Fragebogen WHERE Join_ID = @joinID";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@joinID", session.GameID);



                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    HttpContext.Session.SetInt32("GameNumber", session.GameID);
                    return RedirectToPage("/1Viewer/NameConfirmation");
                }
                else
                {
                    ErrorMessage = "Ungültiger Game-PIN.";
                    return Page();
                }
            }
            catch (MySqlException ex)
            {
                ErrorMessage = $"MySQL-Fehler: {ex.Message}";
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Allgemeiner Fehler: {ex.Message}";
                return Page();
            }
        }

        //Login zu den Dashboards 
        public IActionResult OnPostLoginRequired()
        {
            return RedirectToPage("/Account/Login");
        }

    }
}
