using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using KahootTransnetBW.Model;


namespace KahootTransnetBW.Pages
{
    public class IndexModel : PageModel
    {

        public void OnGet()
        {

        }

        [BindProperty]
        public string GamePin { get; set; }

        public string ErrorMessage { get; set; }


        // Eingabe Feld => VErweis auf Seiten oder Login ins Spiel
        public IActionResult OnPost()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(GamePin))
                {
                    ErrorMessage = "Der Game-PIN darf nicht leer sein.";
                    return Page();
                }

                // Spezialfälle zuerst prüfen
                switch (GamePin)
                {
                    case "111":
                        return RedirectToPage("/Admin/DatabaseCheck");
                    case "2":
                        return RedirectToPage("/Admin/FrageboegenErstellen");
                    case "3":
                        return RedirectToPage("/Admin/Frageerstellen");
                }

                // Allgemeine PIN-Prüfung gegen Datenbank
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                string query = "SELECT Join_ID FROM Fragebogen WHERE Join_ID = @joinID";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@joinID", GamePin);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return RedirectToPage("/1Viewer/Playground");
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

        


        //Anmelde Fenster 
        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        //Login zu den Dashboards 
        public IActionResult OnPostLoginRequired()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
                {
                    ErrorMessage = "Benutzername und Passwort dürfen nicht leer sein.";
                    return Page();
                }

                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                string query = "SELECT * FROM DasboardUser WHERE Username = @username AND Password = @password";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@username", Username);
                cmd.Parameters.AddWithValue("@password", Password);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    string role = reader.GetString("Role");

                    if (role == "Creater")
                    {
                        return RedirectToPage("/Creater/DashboardCreater"); //Ansicht für Creater Ohne User USW 
                    }
                    else
                    {
                        return RedirectToPage("/Admin/Dashboard"); // Admin Bereich mit vollen berechtig
                    }
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

            // ?? Fallback für alle anderen Fälle
            return Page();
        }



    }

}
