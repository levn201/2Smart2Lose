using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using Smart2Lose.Model;
using System.Security.Claims;
using static Smart2Lose.Helper.SQLconnection;

namespace Smart2Lose.Pages.Admin
{
    [Authorize(Roles = "Admin,User,ReadOnly")]
    public class DashboardModel : PageModel
    {
        public string StatusMessage { get; set; } = "Bereit zum Testen.";
        public projektName pn = new projektName();

        // KPI Stats
        public int GesamtQuizze { get; set; }
        public int GesamtSpiele { get; set; }
        public int SpieleHeute { get; set; }
        public int GesamtTeilnehmer { get; set; }
        public int RegistrierteUser { get; set; }

        // DB Status
        public bool DbOnline { get; set; }
        public long DbLatenzMs { get; set; }
        public string DbVersion { get; set; } = "-";
        public int DbThreadsConnected { get; set; }
        public long DbGesamtAbfragen { get; set; }
        public string DbUptime { get; set; } = "-";

        public List<SpielSession> LetzteSpielSessions { get; set; } = new();
        public List<QuizAktivitat> QuizAktivitaeten { get; set; } = new();

        public class SpielSession
        {
            public string GamePin { get; set; }
            public string QuizTitel { get; set; }
            public int SpielerAnzahl { get; set; }
            public DateTime LetzteAktivitaet { get; set; }
        }

        public class QuizAktivitat
        {
            public string Titel { get; set; }
            public string Kategorie { get; set; }
            public int AnzahlSpiele { get; set; }
            public int GesamtTeilnehmer { get; set; }
        }

        public void OnGet()
        {
            LadeStats();
        }

        private void LadeStats()
        {
            try
            {
                var db = new DatenbankZugriff();
                using var connection = db.GetConnection();

                var sw = System.Diagnostics.Stopwatch.StartNew();
                connection.Open();
                sw.Stop();
                DbOnline = true;
                DbLatenzMs = sw.ElapsedMilliseconds;

                if (User.IsInRole("Admin"))
                {
                    using (var cmd = new MySqlCommand("SELECT VERSION()", connection))
                        DbVersion = cmd.ExecuteScalar()?.ToString() ?? "-";

                    using (var cmd = new MySqlCommand(
                        "SHOW GLOBAL STATUS WHERE Variable_name IN ('Threads_connected','Queries','Uptime')", connection))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            switch (r.GetString("Variable_name"))
                            {
                                case "Threads_connected":
                                    DbThreadsConnected = int.Parse(r.GetString("Value")); break;
                                case "Queries":
                                    DbGesamtAbfragen = long.Parse(r.GetString("Value")); break;
                                case "Uptime":
                                    var sek = long.Parse(r.GetString("Value"));
                                    DbUptime = $"{sek / 86400}d {(sek % 86400) / 3600}h {(sek % 3600) / 60}m";
                                    break;
                            }
                        }
                    }
                }

                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM Fragebogen", connection))
                    GesamtQuizze = Convert.ToInt32(cmd.ExecuteScalar());

                using (var cmd = new MySqlCommand("SELECT COUNT(DISTINCT GamePin) FROM PlayerPoints", connection))
                    GesamtSpiele = Convert.ToInt32(cmd.ExecuteScalar());

                using (var cmd = new MySqlCommand("SELECT COUNT(DISTINCT GamePin) FROM PlayerPoints WHERE DATE(saveTime) = CURDATE()", connection))
                    SpieleHeute = Convert.ToInt32(cmd.ExecuteScalar());

                using (var cmd = new MySqlCommand("SELECT COUNT(DISTINCT User_Nickname) FROM PlayerPoints", connection))
                    GesamtTeilnehmer = Convert.ToInt32(cmd.ExecuteScalar());

                if (User.IsInRole("Admin"))
                {
                    using var cmd = new MySqlCommand("SELECT COUNT(*) FROM aspnetusers", connection);
                    RegistrierteUser = Convert.ToInt32(cmd.ExecuteScalar());
                }

                string sessionsSql = @"
                    SELECT pp.GamePin,
                           MAX(COALESCE(f.Titel, '(unbekannt)')) AS Titel,
                           COUNT(pp.User_Nickname) AS SpielerAnzahl,
                           MAX(pp.saveTime) AS LetzteAktivitaet
                    FROM PlayerPoints pp
                    LEFT JOIN Fragebogen f ON pp.GamePin = f.Join_ID
                    GROUP BY pp.GamePin
                    ORDER BY LetzteAktivitaet DESC
                    LIMIT 10";

                using (var cmd = new MySqlCommand(sessionsSql, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        LetzteSpielSessions.Add(new SpielSession
                        {
                            GamePin = reader.GetString("GamePin"),
                            QuizTitel = reader.GetString("Titel"),
                            SpielerAnzahl = reader.GetInt32("SpielerAnzahl"),
                            LetzteAktivitaet = reader.GetDateTime("LetzteAktivitaet")
                        });
                    }
                }

                if (User.IsInRole("Admin"))
                {
                    string sql = @"
                        SELECT f.Titel, f.Kategorie,
                               COUNT(DISTINCT pp.GamePin) AS AnzahlSpiele,
                               COUNT(pp.User_Nickname)   AS GesamtTeilnehmer
                        FROM Fragebogen f
                        LEFT JOIN PlayerPoints pp ON f.Join_ID = pp.GamePin
                        GROUP BY f.Join_ID, f.Titel, f.Kategorie
                        ORDER BY AnzahlSpiele DESC";

                    using var cmd = new MySqlCommand(sql, connection);
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        QuizAktivitaeten.Add(new QuizAktivitat
                        {
                            Titel = reader.GetString("Titel"),
                            Kategorie = reader.GetString("Kategorie"),
                            AnzahlSpiele = reader.GetInt32("AnzahlSpiele"),
                            GesamtTeilnehmer = reader.GetInt32("GesamtTeilnehmer")
                        });
                    }
                }
                else if (User.IsInRole("User"))
                {
                    string autorEmail = User.FindFirstValue(ClaimTypes.Email) ?? "";
                    string sql = @"
                        SELECT f.Titel, f.Kategorie,
                               COUNT(DISTINCT pp.GamePin) AS AnzahlSpiele,
                               COUNT(pp.User_Nickname)   AS GesamtTeilnehmer
                        FROM Fragebogen f
                        LEFT JOIN PlayerPoints pp ON f.Join_ID = pp.GamePin
                        WHERE f.Autor = @autor
                        GROUP BY f.Join_ID, f.Titel, f.Kategorie
                        ORDER BY AnzahlSpiele DESC";

                    using var cmd = new MySqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@autor", autorEmail);
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        QuizAktivitaeten.Add(new QuizAktivitat
                        {
                            Titel = reader.GetString("Titel"),
                            Kategorie = reader.GetString("Kategorie"),
                            AnzahlSpiele = reader.GetInt32("AnzahlSpiele"),
                            GesamtTeilnehmer = reader.GetInt32("GesamtTeilnehmer")
                        });
                    }
                }
            }
            catch
            {
                DbOnline = false;
            }
        }

        // ── Bestehende Dev-Handlers ──────────────────────────────────────

        public void OnPostTestVerbindung()
        {
            var db = new DatenbankZugriff();
            using var connection = db.GetConnection();
            try
            {
                connection.Open();
                StatusMessage = "Verbindung erfolgreich!";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler bei Verbindung: " + ex.Message;
            }
            LadeStats();
        }

        public void OnPostEintragHinzufuegen()
        {
            var db = new DatenbankZugriff();
            using var connection = db.GetConnection();
            try
            {
                connection.Open();
                string insertQuery = "INSERT INTO test (Name, ErstelltAm) VALUES (@name, @datum)";
                using var command = new MySqlCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@name", "Neuer Eintrag");
                command.Parameters.AddWithValue("@datum", DateTime.Now);
                int rows = command.ExecuteNonQuery();
                StatusMessage = $"{rows} Eintrag eingefügt.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Einfügen: " + ex.Message;
            }
            LadeStats();
        }

        public void OnPostEintraegeAnzeigen()
        {
            var db = new DatenbankZugriff();
            using var connection = db.GetConnection();
            try
            {
                connection.Open();
                string selectQuery = "SELECT Id, Name, ErstelltAm FROM test";
                using var command = new MySqlCommand(selectQuery, connection);
                using var reader = command.ExecuteReader();
                StatusMessage = "Einträge:\n";
                while (reader.Read())
                {
                    StatusMessage += $"ID: {reader.GetInt32("Id")}, Name: {reader.GetString("Name")}, Datum: {reader.GetDateTime("ErstelltAm")}\n";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Lesen: " + ex.Message;
            }
            LadeStats();
        }
    }
}
