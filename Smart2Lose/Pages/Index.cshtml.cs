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
                // testSwtche um schenller auf seiten zu kommen
                switch (session.GameID)
                {
                    case 111:
                        return RedirectToPage("/Admin/DatabaseCheck");
                    case 2:
                        return RedirectToPage("/Admin/FrageboegenErstellen");
                    case 3:
                        return RedirectToPage("/Account/Login");
                    case 123:
                        return RedirectToPage("/Account/Register");
                    case 6:
                        return RedirectToPage("/Account/CreateUser");
                    case 7:
                        return RedirectToPage("/Account/ManageUsers");
                }

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





        [BindProperty]
        public User loginU { get; set; } = new User();

        //Login zu den Dashboards 
        public IActionResult OnPostLoginRequired()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(loginU.Username) || string.IsNullOrWhiteSpace(loginU.Password))
                {
                    ErrorMessage = "Benutzername und Passwort dürfen nicht leer sein.";
                    return Page();
                }

                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                string query = "SELECT * FROM DasboardUser WHERE Username = @username AND Password = @password";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@username", loginU.Username);
                cmd.Parameters.AddWithValue("@password", loginU.Password);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    string role = reader.GetString("Role");
                    EditerEvaluation.SetEditor(HttpContext, loginU.Username, role);
                    return RedirectToPage("/Admin/Dashboard");
                }
                else
                {
                    ErrorMessage = "Falscher Benutzername oder Passwort.";
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



    }

}
