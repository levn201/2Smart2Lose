using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using KahootTransnetBW.Model;
using System.Data.SqlClient;

namespace KahootTransnetBW.Pages.Admin
{
    public class FrageboegenErstellenModel : PageModel
    {
        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            // Beispiel: Nur Frage 0 wird verarbeitet. Für mehrere Fragen -> Schleife (kann ich dir auch bauen).
            string fragetext = Request.Form["frage0"];
            string antwortA = Request.Form["a0"];
            string antwortB = Request.Form["b0"];
            string antwortC = Request.Form["c0"];
            string antwortD = Request.Form["d0"];
            string richtigeAntwort = Request.Form["richtig0"]; // "A", "B", "C", "D"

            if (string.IsNullOrWhiteSpace(fragetext) || string.IsNullOrWhiteSpace(richtigeAntwort))
            {
                ModelState.AddModelError(string.Empty, "Fragetext und richtige Antwort dürfen nicht leer sein.");
                return Page();
            }

            // Daten speichern
            try
            {
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Fragen (Fragetext, AntwortA, AntwortB, AntwortC, AntwortD, RichtigeAntwort)
                    VALUES (@Fragetext, @AntwortA, @AntwortB, @AntwortC, @AntwortD, @RichtigeAntwort)";

                command.Parameters.AddWithValue("@Fragetext", fragetext);
                command.Parameters.AddWithValue("@AntwortA", antwortA);
                command.Parameters.AddWithValue("@AntwortB", antwortB);
                command.Parameters.AddWithValue("@AntwortC", antwortC);
                command.Parameters.AddWithValue("@AntwortD", antwortD);
                command.Parameters.AddWithValue("@RichtigeAntwort", richtigeAntwort);

                command.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError(string.Empty, "Fehler beim Speichern in die Datenbank: " + ex.Message);
                return Page();
            }

            // Erfolgreich
            TempData["Success"] = "Frage wurde erfolgreich gespeichert!";
            return RedirectToPage(); // Lädt die Seite neu
        }
    }
}
