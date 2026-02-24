using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using Smart2Lose.Helper;
using Smart2Lose.Model;
using System.Security.Claims;

namespace Smart2Lose.Pages.Admin
{
    [Authorize(Roles = "Admin,User")]
    public class FrageboegenErstellenModel : PageModel
    {
        public AdminHelper AdminHelper = new AdminHelper();
        public projektName pn = new projektName();
        [BindProperty] public Fragebogen fb { get; set; } = new();

        public string FragenError { get; set; } = string.Empty;

        public void OnGet()
        {
            fb.JoinId = AdminHelper.RandomNum();
            fb.Kategorie = "Unternehmen";
        }

        public IActionResult OnPostSpeichern()
        {
            // Falls JoinId beim Post fehlt (z.B. Page neu geladen, JS Mist, etc.)
            if (fb.JoinId <= 0)
                fb.JoinId = AdminHelper.RandomNum();

            // Titel prüfen
            if (string.IsNullOrWhiteSpace(fb.Titel))
            {
                FragenError = "Bitte einen Titel eingeben.";
                return Page();
            } 

            // Fragen prüfen
            if (fb.Fragen == null || fb.Fragen.Count == 0)
            {
                FragenError = "Mindestens eine Frage ist erforderlich.";
                return Page();
            }

            // Validierung: genau eine richtige Antwort pro Frage + Fragestellung nicht leer
            for (int i = 0; i < fb.Fragen.Count; i++)
            {
                var f = fb.Fragen[i];

                if (string.IsNullOrWhiteSpace(f.Fragestellung))
                {
                    FragenError = $"Frage {i + 1}: Fragestellung darf nicht leer sein.";
                    return Page();
                }

                int richtig =
                    (f.IstAntwort1Richtig ? 1 : 0) +
                    (f.IstAntwort2Richtig ? 1 : 0) +
                    (f.IstAntwort3Richtig ? 1 : 0) +
                    (f.IstAntwort4Richtig ? 1 : 0);

                if (richtig != 1)
                {
                    FragenError = $"Frage {i + 1}: Bitte genau eine richtige Antwort markieren.";
                    return Page();
                }
            }

            try
            {
                // Autor: du kannst auch ClaimTypes.Email nutzen, wenn du das in DB willst
                fb.Autor = User.FindFirstValue(ClaimTypes.Email) ?? "";

                var db = new SQLconnection.DatenbankZugriff();
                using var con = db.GetConnection();
                con.Open();

                using var tx = con.BeginTransaction();

                long fid;

                // Fragebogen einfügen
                using (var cmd = new MySqlCommand(
                    @"INSERT INTO Fragebogen (Titel, Join_ID, Autor, Kategorie)
                      VALUES (@t, @j, @a, @k);",
                    con, tx))
                {
                    cmd.Parameters.AddWithValue("@t", fb.Titel);
                    cmd.Parameters.AddWithValue("@j", fb.JoinId);
                    cmd.Parameters.AddWithValue("@a", fb.Autor);
                    cmd.Parameters.AddWithValue("@k", fb.Kategorie);

                    cmd.ExecuteNonQuery();
                    fid = cmd.LastInsertedId;
                }

                // Fragen einfügen
                foreach (var f in fb.Fragen)
                {
                    using var cmd = new MySqlCommand(@"
                        INSERT INTO Fragen
                            (FragebogenID, Fragestellung,
                             Antwort1, IstAntwort1Richtig,
                             Antwort2, IstAntwort2Richtig,
                             Antwort3, IstAntwort3Richtig,
                             Antwort4, IstAntwort4Richtig)
                        VALUES
                            (@id, @q,
                             @a1, @r1,
                             @a2, @r2,
                             @a3, @r3,
                             @a4, @r4);",
                        con, tx);

                    cmd.Parameters.AddWithValue("@id", fb.JoinId);
                    cmd.Parameters.AddWithValue("@q", f.Fragestellung ?? "");

                    cmd.Parameters.AddWithValue("@a1", f.Antwort1 ?? "");
                    cmd.Parameters.AddWithValue("@r1", f.IstAntwort1Richtig);

                    cmd.Parameters.AddWithValue("@a2", f.Antwort2 ?? "");
                    cmd.Parameters.AddWithValue("@r2", f.IstAntwort2Richtig);

                    cmd.Parameters.AddWithValue("@a3", f.Antwort3 ?? "");
                    cmd.Parameters.AddWithValue("@r3", f.IstAntwort3Richtig);

                    cmd.Parameters.AddWithValue("@a4", f.Antwort4 ?? "");
                    cmd.Parameters.AddWithValue("@r4", f.IstAntwort4Richtig);

                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
                return RedirectToPage("/Admin/Frageboegen");
            }
            catch (Exception ex)
            {
                FragenError = $"Datenbankfehler: {ex.Message}";
                return Page();
            }
        }
    }
}