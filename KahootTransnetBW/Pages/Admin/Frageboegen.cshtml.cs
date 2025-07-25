using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using KahootTransnetBW.Model;

namespace KahootTransnetBW.Pages.Admin
{
    public class FragebögenModel : PageModel
    {
        public void OnGet()
        {
            LadeAlleFrageboegen();
        }
 
        // Liste der Fragebögen
        public class FragebogenViewModel
        {
            public int JoinId { get; set; }
            public string Titel { get; set; }
            public DateTime ErstelltAm { get; set; }
        }
        public List<FragebogenViewModel> Frageboegen { get; set; } = new();

        // Alle Fragebögen werden geladen 
        public void LadeAlleFrageboegen()
        {
            try
            {
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                string query = "SELECT Join_ID, Titel, ErstelltAm FROM Fragebogen;";
                using var cmd = new MySqlCommand(query, connection);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Frageboegen.Add(new FragebogenViewModel
                    {
                        JoinId = reader.GetInt32("Join_ID"),
                        Titel = reader.GetString("Titel"),
                        ErstelltAm = reader.GetDateTime("ErstelltAm")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Laden der Fragebögen: {ex.Message}");
            }
        }

        // Löschen des Fragebogens 
        public IActionResult OnPostLoeschen(int id)
        {
            try
            {
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                string query = "DELETE FROM Fragebogen WHERE Join_ID = @id;";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@id", id);
                int count = cmd.ExecuteNonQuery();

                LadeAlleFrageboegen();

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Löschen: {ex.Message}");
                return StatusCode(500);
            }
        }
    
    }
}
