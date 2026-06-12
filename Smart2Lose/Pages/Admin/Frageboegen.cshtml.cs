using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using Smart2Lose.Helper;
using Smart2Lose.Model;
using System.Security.Claims;

namespace Smart2Lose.Pages.Admin
{
    [Authorize(Roles = "Admin,User,ReadOnly")]
    public class FragebögenModel : PageModel
    {
        private readonly IWebHostEnvironment _env;
        public FragebögenModel(IWebHostEnvironment env) => _env = env;

        public projektName pn = new projektName();
        public int GamePin { get; set; }
        public int countPlayer { get; set; }
        [BindProperty]
        public int FragebogenId { get; set; }

        [BindProperty]
        public List<Fragen> _Fragen { get; set; } = new();
        public List<Fragebogen> Frageboegen { get; set; } = new();
        public List<Fragen> _FragenDB { get; set; } = new();
        public FragenHelper fHelper { get; set; } = new FragenHelper();

        public void OnGet()
        {
            LadeAlleFrageboegen();
            fHelper.activeUser = User.FindFirstValue(ClaimTypes.Email);
        }

        // Alle Fragen Cards laden
        public void LadeAlleFrageboegen()
        {
            try
            {
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                string query = "SELECT Join_ID, Titel, Autor, Kategorie, Typ, ErstelltAm FROM Fragebogen ORDER BY Join_ID ASC;";
                using var cmd = new MySqlCommand(query, connection);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Frageboegen.Add(Fragebogen.FromReader(reader));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Laden der Fragebögen: {ex.Message}");
            }
        }

        // Card - Anschauen Button
        public IActionResult OnPostView(int id)
        {
            fHelper.activeUser = User.FindFirstValue(ClaimTypes.Email);
            fHelper.countResults(GamePin, countPlayer);
            try
            {
                GamePin = id;
                _FragenDB.Clear();

                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                const string query = @"
                    SELECT Fragestellung,
                        Antwort1, IstAntwort1Richtig,
                        Antwort2, IstAntwort2Richtig,
                        Antwort3, IstAntwort3Richtig,
                        Antwort4, IstAntwort4Richtig,
                        BildUrl, LinkUrl
                    FROM Fragen
                    WHERE FragebogenID = @ID
                    ORDER BY ID;";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@ID", GamePin);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    _FragenDB.Add(Fragen.FromReader(reader));
                }

                ViewData["ShowViewPopup"] = true;
                System.Diagnostics.Debug.WriteLine($"Loaded {_FragenDB.Count} questions for GamePin {GamePin}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnPostView: {ex.Message}");
                ViewData["ShowViewPopup"] = false;
                ViewData["ErrorMessage"] = "Fehler beim Laden der Fragen.";
            }

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

                string query = "DELETE FROM Fragebogen WHERE Join_ID = @id;" +
                               "DELETE FROM Fragen WHERE FragebogenID = @id;" +
                               "DELETE FROM playerpoints WHERE GAMEPIN = @id";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();

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
            fHelper.activeUser = User.FindFirstValue(ClaimTypes.Email);
            try
            {
                GamePin = id;
                _FragenDB.Clear();

                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                const string query = @"
                    SELECT Fragestellung,
                        Antwort1, IstAntwort1Richtig,
                        Antwort2, IstAntwort2Richtig,
                        Antwort3, IstAntwort3Richtig,
                        Antwort4, IstAntwort4Richtig,
                        BildUrl, LinkUrl
                    FROM Fragen
                    WHERE FragebogenID = @ID
                    ORDER BY ID;";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@ID", GamePin);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    _FragenDB.Add(Fragen.FromReader(reader));
                }

                ViewData["ShowEditPopup"] = true;
                System.Diagnostics.Debug.WriteLine($"Loaded {_FragenDB.Count} questions for GamePin {GamePin}");
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

        public IActionResult OnGetExportCsv(int id)
        {
            fHelper.activeUser = User.FindFirstValue(ClaimTypes.Email);

            if (!User.IsInRole("Admin") && !fHelper.CheckIfPlayerIsAutor(id))
                return Forbid();

            var db = new SQLconnection.DatenbankZugriff();
            using var connection = db.GetConnection();
            connection.Open();

            string titel = id.ToString();
            using (var cmd = new MySqlCommand("SELECT Titel FROM Fragebogen WHERE Join_ID = @id", connection))
            {
                cmd.Parameters.AddWithValue("@id", id);
                var result = cmd.ExecuteScalar();
                if (result != null) titel = result.ToString()!;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Platz,Nickname,Punkte,Richtig,Gesamt,Datum");

            using var cmd2 = new MySqlCommand(
                "SELECT SessionPints, User_Nickname, CorrectAnswered, PossibleAnswers, saveTime FROM PlayerPoints WHERE GamePin = @id ORDER BY SessionPints DESC;",
                connection);
            cmd2.Parameters.AddWithValue("@id", id);
            using var reader = cmd2.ExecuteReader();

            int platz = 1;
            while (reader.Read())
            {
                sb.AppendLine($"{platz},{EscapeCsv(reader.GetString("User_Nickname"))},{reader.GetInt32("SessionPints")},{reader.GetInt32("CorrectAnswered")},{reader.GetInt32("PossibleAnswers")},{reader.GetDateTime("saveTime"):dd.MM.yyyy HH:mm}");
                platz++;
            }

            var bom = System.Text.Encoding.UTF8.GetPreamble();
            var data = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            var dateiname = $"Smart2Lose_{id}_{DateTime.Now:yyyyMMdd}.csv";
            return File(bom.Concat(data).ToArray(), "text/csv", dateiname);
        }

        private static string EscapeCsv(string val)
        {
            if (val.Contains(',') || val.Contains('"') || val.Contains('\n'))
                return $"\"{val.Replace("\"", "\"\"")}\"";
            return val;
        }

        // Card - Bearbeiten Button => Speichern der editierten Fragen
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

                // Bild-Uploads verarbeiten
                for (int i = 0; i < Fragen.Count; i++)
                {
                    var file = Request.Form.Files[$"bild_{i}"];
                    if (file != null && file.Length > 0)
                    {
                        var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", "fragen");
                        Directory.CreateDirectory(uploadsPath);
                        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                        var fileName = $"{Guid.NewGuid()}{ext}";
                        using var stream = System.IO.File.Create(Path.Combine(uploadsPath, fileName));
                        file.CopyTo(stream);
                        Fragen[i].BildUrl = $"/uploads/fragen/{fileName}";
                    }
                    else
                    {
                        Fragen[i].BildUrl = Request.Form[$"existingBild_{i}"];
                    }
                }

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

                if (frageIds.Count != Fragen.Count)
                {
                    ViewData["ErrorMessage"] = "Anzahl der Fragen stimmt nicht überein.";
                    LadeAlleFrageboegen();
                    return Page();
                }

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
                        IstAntwort4Richtig = @IstAntwort4Richtig,
                        BildUrl = @BildUrl,
                        LinkUrl = @LinkUrl
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
                        cmd.Parameters.AddWithValue("@BildUrl", (object?)frage.BildUrl ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LinkUrl", (object?)frage.LinkUrl ?? DBNull.Value);

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
            return RedirectToPage();
        }
    }
}
