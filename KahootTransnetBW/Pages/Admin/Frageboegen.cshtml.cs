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
            //countResults();
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

                string query = "SELECT Join_ID, Titel, ErstelltAm FROM Fragebogen ORDER BY Join_ID ASC;";
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

                string query = "DELETE FROM Fragebogen WHERE Join_ID = @id;" + //Löschen aus der Titel und Fragebogen Datenbank 
                                "DELETE FROM Fragen WHERE FragebogenID = @id;" + 
                                "DELETE FROM playerpoints WHERE GAMEPIN = @id";

                using var cmd = new MySqlCommand(query,connection);
                cmd.Parameters.AddWithValue("@id", id);
                int count = cmd.ExecuteNonQuery();

                LadeAlleFrageboegen();

                connection.Close();

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Löschen: {ex.Message}");
                return StatusCode(500);
            }
        }




        // IN ARBEIT 
        //----------





        // Anschauen des Fragebogens
        public IActionResult OnPostView(int id)
        {
            return Page();
        }

        // Bearbeiten des Fragebogens 
        public IActionResult OnPostEdit(int id)
        {
            return Page();
        }








        // Für die noch freien Plätze in der Card
        public int countResults()
        {
            var db = new SQLconnection.DatenbankZugriff();
            using var connection = db.GetConnection();
            connection.Open();

            string query = @"SELECT COUNT(*) FROM PlayerPoints WHERE GamePin = @ID;";
            using var cmd = new MySqlCommand(query, connection);

            foreach (var fragebogen in Frageboegen)
            {
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@ID", fragebogen.JoinId);

                var result = Convert.ToInt32(cmd.ExecuteScalar());
                // weiterverarbeiten...
                return result;
            }

            return 0;
        }

        // Hier wird geckeckt ob du dieses Quizz erstellt hast
        public void checkCreater()
        {

        }

    }
}
