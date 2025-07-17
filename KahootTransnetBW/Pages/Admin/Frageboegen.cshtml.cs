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

        // Titel und ID werden eingelesen als übersicht 
        // ==> Wenn man auf das feld klickt soll man auf eine seite gehen wo man die fragen nochaml anschauen kann und lösungen schonmal sieht 
        // Admin User Klasse
        public class FragebogenViewModel
        {
            public int JoinId { get; set; }
            public string Titel { get; set; }
            public DateTime ErstelltAm { get; set; }
        }

        public List<FragebogenViewModel> Frageboegen { get; set; } = new();

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

    }
}
