using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using KahootTransnetBW.Model;
using Microsoft.AspNetCore.Http;


namespace KahootTransnetBW.Pages
{
    public class IndexModel : PageModel
    {

        public void OnGet()
        {
            HttpContext.Session.SetString("projectName", ProjektName);
        }

        [BindProperty]
        public int GamePin { get; set; }

        public string ErrorMessage { get; set; }

        public string ProjektName = "2Smart2Lose";




        // Eingabe Feld => VErweis auf Seiten oder Login ins Spiel
        public IActionResult OnPost()
        {
            try
            {
                // testSwtche um schenller auf seiten zu kommen
                switch (GamePin)
                {
                    case 111:
                        return RedirectToPage("/Admin/DatabaseCheck");
                    case 2:
                        return RedirectToPage("/Admin/FrageboegenErstellen");
                    case 3:
                        return RedirectToPage("/Admin/Frageerstellen");
                    case 123:
                        return RedirectToPage("/testPages/startPage");
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
                    HttpContext.Session.SetInt32("GameNumber", GamePin);
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
                        HttpContext.Session.SetString("createrName", Username);
                        return RedirectToPage("/Creater/DashboardCreater"); //Ansicht für Creater Ohne User USW 
                    }
                    else
                    {
                        HttpContext.Session.SetString("createrName", Username);
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
        }



    }

}
