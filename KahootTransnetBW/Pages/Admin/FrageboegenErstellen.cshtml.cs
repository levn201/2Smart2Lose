using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;
using KahootTransnetBW.Model;


namespace KahootTransnetBW.Pages.Admin
{
    public class FrageboegenErstellenModel : PageModel
    {
        public void OnGet()
        {
        }

        public int FragenCount = 0;

        public int RandomNum()
        {
            Random random = new Random();
            return random.Next(1000, 10000);
        }

        [BindProperty]
        public string Titel { get; set; }
        public string loadError { get; set; }
        public string TitelError { get; set; }

        public IActionResult OnPost()
        {
            if (string.IsNullOrWhiteSpace(Titel))
            {
                TitelError = "Gebe einen Titel ein.";
                return Page();
            }

            try
            {

                int JoinNumber = RandomNum();

                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                string query = "INSERT INTO Fragebogen (Titel, Join_ID) VALUES (@titel, @Join_ID)";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@titel", Titel);
                cmd.Parameters.AddWithValue("@Join_ID", JoinNumber);

                cmd.ExecuteNonQuery();

                TitelError = "Erfolgreich eingeschrieben";
                return Page();
            }
            catch (MySqlException ex)
            {
                TitelError = $"MySQL-Fehler: {ex.Message}";
                return Page();
            }
            catch (Exception ex)
            {
                TitelError = $"Allgemeiner Fehler: {ex.Message}";
                return Page();
            }
        }

    }
}
