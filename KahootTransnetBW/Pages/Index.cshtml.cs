using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using KahootTransnetBW.Model;


namespace KahootTransnetBW.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }

        // Um die bestehenden codes durchzugehen und zu Überprüfen 
        [BindProperty]
        public string GamePin { get; set; }
        public string ErrorMessage { get; set; }

        public IActionResult OnPost()
        {
            if (GamePin == "111")
            {
                return RedirectToPage("/Admin/DatabaseCheck");
            }
            else if (GamePin == "2")
            {
                return RedirectToPage("/Admin/FrageboegenErstellen");
            }
            else
            {
                ErrorMessage = "Ungültiger Game-PIN.";
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
