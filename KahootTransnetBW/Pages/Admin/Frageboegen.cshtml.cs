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
            LadeAlleFrageboegen();
        }


        public projektName pn = new projektName(); // Projektname holen
        public int GamePin { get; set; }
        public int countPlayer { get; set; }
        [BindProperty]
        public int FragebogenId { get; set; }



        [BindProperty]
        public List<Fragen> Fragen { get; set; } = new(); // Fragen zum bearbeiten (Fragestellung, Antworten, Richtig/Falsch)
        public List<Fragebogen> Frageboegen { get; set; } = new(); // DB Fragebogen (Titel, Autor, Kategorie, ErstelltAm)
        public List<FragenDB> FragenChecken { get; set; } = new(); // DB Fragen Tabelle (Fragestellung, Antworten, Richtig/Falsch)




        // Alle Fragen Cards laden
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
                    Frageboegen.Add(new Fragebogen
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

        // Noch machen!!!           KAtegorien Festlegen 
        [BindProperty]
        public string Kategorien { get; set; }



        // Card - Anschauen Button 
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
                    FragenChecken.Add(new FragenDB
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

                ViewData["ShowViewPopup"] = true;
                System.Diagnostics.Debug.WriteLine($"Loaded {FragenChecken.Count} questions for GamePin {GamePin}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnPostView: {ex.Message}");
                ViewData["ShowViewPopup"] = false;
                ViewData["ErrorMessage"] = "Fehler beim Laden der Fragen.";
            }

            // WICHTIG: Fragebögen neu laden, damit sie im Hintergrund angezeigt werden!
            LadeAlleFrageboegen();

            return Page();
        }

        // Card - Löschen Button  
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

                connection.Close();
                LadeAlleFrageboegen();
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Löschen: {ex.Message}");
                return StatusCode(500);
            }
        }

        // Card - Bearbeiten Button => Laden aller Fragen in Textfelder
        public IActionResult OnPostEdit(int id)
        {
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
                    FragenChecken.Add(new FragenDB
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

                ViewData["ShowEditPopup"] = true;
                System.Diagnostics.Debug.WriteLine($"Loaded {FragenChecken.Count} questions for GamePin {GamePin}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnPostEdit: {ex.Message}");
                ViewData["ShowEditPopup"] = false;
                ViewData["ErrorMessage"] = "Fehler beim Laden der Fragen.";
            }

            LadeAlleFrageboegen();

            return Page();
        }

        // Card - Bearebiten Button => Speichern der editierten Fragen
        public IActionResult OnPostSaveEdit(int fragebogenId, List<Fragen> Fragen)
        {
            if (Fragen == null || !Fragen.Any())
            {
                ViewData["ErrorMessage"] = "Keine Fragen zum Speichern gefunden.";
                LadeAlleFrageboegen();
                return Page();
            }

            try
            {
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                // Hole zuerst alle Fragen-IDs für diesen Fragebogen
                string getIdsQuery = "SELECT ID FROM Fragen WHERE FragebogenID = @FragebogenID ORDER BY ID;";
                var frageIds = new List<int>();

                using (var cmdIds = new MySqlCommand(getIdsQuery, connection))
                {
                    cmdIds.Parameters.AddWithValue("@FragebogenID", fragebogenId);
                    using var reader = cmdIds.ExecuteReader();
                    while (reader.Read())
                    {
                        frageIds.Add(reader.GetInt32("ID"));
                    }
                }

                // Validierung: Anzahl der Fragen muss übereinstimmen
                if (frageIds.Count != Fragen.Count)
                {
                    ViewData["ErrorMessage"] = "Anzahl der Fragen stimmt nicht überein.";
                    LadeAlleFrageboegen();
                    return Page();
                }

                // Update-Query für jede Frage
                string updateQuery = @"
            UPDATE Fragen 
            SET 
                Fragestellung = @Fragestellung,
                Antwort1 = @Antwort1,
                IstAntwort1Richtig = @IstAntwort1Richtig,
                Antwort2 = @Antwort2,
                IstAntwort2Richtig = @IstAntwort2Richtig,
                Antwort3 = @Antwort3,
                IstAntwort3Richtig = @IstAntwort3Richtig,
                Antwort4 = @Antwort4,
                IstAntwort4Richtig = @IstAntwort4Richtig
            WHERE ID = @ID;";

                using var transaction = connection.BeginTransaction();

                try
                {
                    for (int i = 0; i < Fragen.Count; i++)
                    {
                        var frage = Fragen[i];

                        using var cmd = new MySqlCommand(updateQuery, connection, transaction);
                        cmd.Parameters.AddWithValue("@ID", frageIds[i]);
                        cmd.Parameters.AddWithValue("@Fragestellung", frage.Fragestellung);
                        cmd.Parameters.AddWithValue("@Antwort1", frage.Antwort1);
                        cmd.Parameters.AddWithValue("@IstAntwort1Richtig", frage.IstAntwort1Richtig);
                        cmd.Parameters.AddWithValue("@Antwort2", frage.Antwort2);
                        cmd.Parameters.AddWithValue("@IstAntwort2Richtig", frage.IstAntwort2Richtig);
                        cmd.Parameters.AddWithValue("@Antwort3", frage.Antwort3);
                        cmd.Parameters.AddWithValue("@IstAntwort3Richtig", frage.IstAntwort3Richtig);
                        cmd.Parameters.AddWithValue("@Antwort4", frage.Antwort4);
                        cmd.Parameters.AddWithValue("@IstAntwort4Richtig", frage.IstAntwort4Richtig);

                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();

                    ViewData["SuccessMessage"] = "Fragebogen erfolgreich gespeichert!";
                    System.Diagnostics.Debug.WriteLine($"Successfully updated {Fragen.Count} questions for FragebogenID {fragebogenId}");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new Exception($"Fehler beim Speichern: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnPostSaveEdit: {ex.Message}");
                ViewData["ErrorMessage"] = $"Fehler beim Speichern: {ex.Message}";
            }

            LadeAlleFrageboegen();
            return RedirectToPage(); // Redirect um POST-Redirect-GET Pattern zu folgen
        }



        // Noch machen!!!           Hier wird geckeckt ob du dieses Quizz erstellt hast
        public void checkCreater()
        {

        }

        // Noch machen!!!           Wie viele Fragebögen wurden bereits Ausgefüllt
        public int countResults(int id)
        {
            GamePin = id;
            countPlayer = 0; // WICHTIG: Zurücksetzen vor dem Zählen

            var db = new SQLconnection.DatenbankZugriff();
            using var connection = db.GetConnection();
            connection.Open();

            string query = @"SELECT COUNT(*) FROM PlayerPoints WHERE GamePin = @ID;";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@ID", GamePin);

            countPlayer = Convert.ToInt32(cmd.ExecuteScalar());

            return countPlayer;
        }
    }
}
