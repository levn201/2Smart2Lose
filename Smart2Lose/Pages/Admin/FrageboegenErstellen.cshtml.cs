using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;
using Smart2Lose.Helper;
using Smart2Lose.Model;
using System.Security.Claims;


namespace Smart2Lose.Pages.Admin
{
    [Authorize(Roles = "Admin,User")]
    public class FrageboegenErstellenModel : PageModel
    {
        public projektName pn = new projektName(); // Projektname holen
        public AdminHelper AdminHelper = new AdminHelper();

        [BindProperty] public Fragebogen fragebogen { get; set; } = new();
        [BindProperty] public string Titel { get; set; }
        [BindProperty] public List<Fragen> Fragen { get; set; } = new();
        [BindProperty] public string CreaterName { get; set; } = string.Empty;
        [BindProperty] public string Kategorie { get; set; } = string.Empty;
        [BindProperty] public int JoinNumber { get; set; }
        public string TitelError { get; set; }
        public string FragenError { get; set; }


        // Macht JoinNumber gleich wie FragebogenID 
        public void OnGet()
        {
            Kategorie = HttpContext.Session.GetString("kategorie") ?? "Sonstige";
            JoinNumber = AdminHelper.RandomNum();
        }


        // Hauptspeicher-Methode: Speichert Fragebogen UND Fragen in einer Transaktion
        public IActionResult OnPostAddFrage()
        {
            // Validierung: Titel prüfen
            if (string.IsNullOrWhiteSpace(Titel))
            {
                TitelError = "Bitte geben Sie einen Titel ein.";
                return Page();
            }

            // Validierung: Fragen prüfen
            if (Fragen == null || Fragen.Count == 0)
            {
                FragenError = "Bitte fügen Sie mindestens eine Frage hinzu.";
                ViewData["ShowPopup"] = false;
                return Page();
            }

            // Prüfer für die Frage eingabe 
            for (int i = 0; i < Fragen.Count; i++)
            {
                var frage = Fragen[i];

                if (string.IsNullOrWhiteSpace(frage.Fragestellung))
                {
                    FragenError = $"Frage {i + 1}: Bitte geben Sie eine Fragestellung ein.";
                    return Page();
                }

                if (string.IsNullOrWhiteSpace(frage.Antwort1) &&
                    string.IsNullOrWhiteSpace(frage.Antwort2) &&
                    string.IsNullOrWhiteSpace(frage.Antwort3) &&
                    string.IsNullOrWhiteSpace(frage.Antwort4))
                {
                    FragenError = $"Frage {i + 1}: Bitte geben Sie mindestens eine Antwort ein.";
                    return Page();
                }
                int richtigeAntworten =
                    (frage.IstAntwort1Richtig ? 1 : 0) +
                    (frage.IstAntwort2Richtig ? 1 : 0) +
                    (frage.IstAntwort3Richtig ? 1 : 0) +
                    (frage.IstAntwort4Richtig ? 1 : 0);

                if (richtigeAntworten != 1)
                {
                    FragenError = $"Frage {i + 1}: Es muss genau eine richtige Antwort ausgewählt werden.";
                    return Page();
                }


                // Prüfen ob mindestens eine richtige Antwort ausgewählt wurde
                if (!frage.IstAntwort1Richtig &&
                    !frage.IstAntwort2Richtig &&
                    !frage.IstAntwort3Richtig &&
                    !frage.IstAntwort4Richtig)
                {
                    FragenError = $"Frage {i + 1}: Bitte markieren Sie mindestens eine richtige Antwort.";
                    return Page();
                }
            }

            // Schreiben in die Datenbank 
            try
            {
                CreaterName = HttpContext.Session.GetString("createrName") ?? "Unbekannt";
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                // Transaktion starten für atomare Operation
                using var transaction = connection.BeginTransaction();

                try
                {
                    // 1. Fragebogen speichern
                    string queryFragebogen = "INSERT INTO Fragebogen (Titel, Join_ID, Autor, Kategorie) VALUES (@titel, @Join_ID, @Autor, @kategorie)";
                    using (var cmd = new MySqlCommand(queryFragebogen, connection, transaction))
                    {
                        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                        cmd.Parameters.AddWithValue("@titel", Titel);
                        cmd.Parameters.AddWithValue("@Join_ID", JoinNumber);
                        cmd.Parameters.AddWithValue("@Autor", userId);
                        cmd.Parameters.AddWithValue("@kategorie", Kategorie);
                        cmd.ExecuteNonQuery();
                    }

                    // 2. Alle Fragen speichern
                    foreach (var frage in Fragen)
                    {
                        string queryFragen = @"
                            INSERT INTO Fragen (
                                FragebogenID,
                                Fragestellung,
                                Antwort1, IstAntwort1Richtig,
                                Antwort2, IstAntwort2Richtig,
                                Antwort3, IstAntwort3Richtig,
                                Antwort4, IstAntwort4Richtig
                            ) VALUES (
                                @FragebogenID,
                                @Fragestellung,
                                @Antwort1, @IstAntwort1Richtig,
                                @Antwort2, @IstAntwort2Richtig,
                                @Antwort3, @IstAntwort3Richtig,
                                @Antwort4, @IstAntwort4Richtig
                            );";

                        using var cmd = new MySqlCommand(queryFragen, connection, transaction);
                        cmd.Parameters.AddWithValue("@FragebogenID", JoinNumber);
                        cmd.Parameters.AddWithValue("@Fragestellung", frage.Fragestellung ?? "");
                        cmd.Parameters.AddWithValue("@Antwort1", frage.Antwort1 ?? "");
                        cmd.Parameters.AddWithValue("@IstAntwort1Richtig", frage.IstAntwort1Richtig);
                        cmd.Parameters.AddWithValue("@Antwort2", frage.Antwort2 ?? "");
                        cmd.Parameters.AddWithValue("@IstAntwort2Richtig", frage.IstAntwort2Richtig);
                        cmd.Parameters.AddWithValue("@Antwort3", frage.Antwort3 ?? "");
                        cmd.Parameters.AddWithValue("@IstAntwort3Richtig", frage.IstAntwort3Richtig);
                        cmd.Parameters.AddWithValue("@Antwort4", frage.Antwort4 ?? "");
                        cmd.Parameters.AddWithValue("@IstAntwort4Richtig", frage.IstAntwort4Richtig);

                        cmd.ExecuteNonQuery();
                    }

                    // Transaktion bestätigen
                    transaction.Commit();


                    // Optional: Weiterleitung zur Übersicht
                    return RedirectToPage("/Admin/Frageboegen");
                }
                catch (Exception)
                {
                    // Bei Fehler: Rollback
                    transaction.Rollback();
                    throw;
                }
            }
            catch (MySqlException ex)
            {
                FragenError = $"Datenbankfehler: {ex.Message}";
                return Page();
            }
            catch (Exception ex)
            {
                FragenError = $"Fehler beim Speichern: {ex.Message}";
                return Page();
            }
        }
    }
}