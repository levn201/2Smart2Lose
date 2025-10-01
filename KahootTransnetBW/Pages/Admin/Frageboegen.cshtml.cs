using KahootTransnetBW.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using static KahootTransnetBW.Pages._1Viewer.PlaygroundModel;

namespace KahootTransnetBW.Pages.Admin
{
    public class FragebögenModel : PageModel
    {
        public void OnGet()
        {   
            WebsiteName = HttpContext.Session.GetString("projectName") ?? "";
            LadeAlleFrageboegen();
        }

        public int GamePin { get; set; }
        public int countPlayer { get; set; }
        public string WebsiteName { get; set; }



        // DB Fragebogen TAbelle
        public class FragebogenViewModel
        {
            public int JoinId { get; set; }
            public string Titel { get; set; }
            public string Autor { get; set; }
            public string Kategorie { get; set; }

            public DateTime ErstelltAm { get; set; }
        }
        public List<FragebogenViewModel> Frageboegen { get; set; } = new();

        // DB Fragen Tabelle
        public class FragenChecknerModel 
            { 
                public string DB_Fragestellung { get; set; } 
                public string DB_Antwort1 { get; set; } 
                public bool DB_IstAntwort1Richtig { get; set; } 
                public string DB_Antwort2 { get; set; } 
                public bool DB_IstAntwort2Richtig { get; set; } 
                public string DB_Antwort3 { get; set; } 
                public bool DB_IstAntwort3Richtig { get; set; } 
                public string DB_Antwort4 { get; set; } 
                public bool DB_IstAntwort4Richtig { get; set; } 
            }
        public List<FragenChecknerModel> FragenChecken { get; set; } = new();




        // Alle Fragebögen werden geladen
        public void LadeAlleFrageboegen()
        {
            try
            {
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                string query = "SELECT Join_ID, Titel, Autor, Kategorie, ErstelltAm FROM Fragebogen ORDER BY Join_ID ASC;";
                using var cmd = new MySqlCommand(query, connection);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Frageboegen.Add(new FragebogenViewModel
                    {
                        JoinId = reader.GetInt32("Join_ID"),
                        Titel = reader.GetString("Titel"),
                        Autor = reader.GetString("Autor"),
                        Kategorie = reader.GetString("Kategorie"),
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

        // Anschauen Button 
        public IActionResult OnPostView(int id)
        {
            countResults(id);
            try
            {
                GamePin = id;
                FragenChecken.Clear();

                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                const string query = @"
            SELECT 
                Fragestellung,
                Antwort1, IstAntwort1Richtig,
                Antwort2, IstAntwort2Richtig,
                Antwort3, IstAntwort3Richtig,
                Antwort4, IstAntwort4Richtig
            FROM Fragen
            WHERE FragebogenID = @ID
            ORDER BY ID;";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@ID", GamePin);

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    FragenChecken.Add(new FragenChecknerModel
                    {
                        DB_Fragestellung = reader.GetString("Fragestellung"),
                        DB_Antwort1 = reader.GetString("Antwort1"),
                        DB_IstAntwort1Richtig = reader.GetBoolean("IstAntwort1Richtig"),
                        DB_Antwort2 = reader.GetString("Antwort2"),
                        DB_IstAntwort2Richtig = reader.GetBoolean("IstAntwort2Richtig"),
                        DB_Antwort3 = reader.GetString("Antwort3"),
                        DB_IstAntwort3Richtig = reader.GetBoolean("IstAntwort3Richtig"),
                        DB_Antwort4 = reader.GetString("Antwort4"),
                        DB_IstAntwort4Richtig = reader.GetBoolean("IstAntwort4Richtig")
                    });
                }

                // Setze das Flag für das Popup erst NACH dem Laden der Daten
                ViewData["ShowViewPopup"] = true;

                // Debug-Ausgabe (optional)
                System.Diagnostics.Debug.WriteLine($"Loaded {FragenChecken.Count} questions for GamePin {GamePin}");
            }
            catch (Exception ex)
            {
                // Fehlerbehandlung
                System.Diagnostics.Debug.WriteLine($"Error in OnPostView: {ex.Message}");
                ViewData["ShowViewPopup"] = false;
                // Optional: Fehlermeldung an View übergeben
                ViewData["ErrorMessage"] = "Fehler beim Laden der Fragen.";
            }

            return Page();
        }

        // Wie viele Ergbenisse gibt es
        public void countResults(int id)
        {
            GamePin = id;
            var db = new SQLconnection.DatenbankZugriff();
            using var connection = db.GetConnection();
            connection.Open();

            string query = @"SELECT COUNT(*) FROM PlayerPoints WHERE GamePin = @ID;";
            using var cmd = new MySqlCommand(query, connection);

            int totalCount = 0;

            foreach (var fragebogen in Frageboegen)
            {
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@ID", GamePin);

                var result = Convert.ToInt32(cmd.ExecuteScalar());
                countPlayer += result;
            }
        }







        // Bearbeiten des Fragebogens 
        public IActionResult OnPostEdit(int id)
        {
            return Page();
        }








        

        // Hier wird geckeckt ob du dieses Quizz erstellt hast
        public void checkCreater()
        {

        }

    }
}
