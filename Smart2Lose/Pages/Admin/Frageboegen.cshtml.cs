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
        public projektName pn = new projektName(); // Projektname holen
        public int GamePin { get; set; }
        public int countPlayer { get; set; }


        public List<Fragebogen> Frageboegen { get; set; } = new(); // DB Fragebogen (Titel, Autor, Kategorie, ErstelltAm)
        public List<Fragen> FragenDB { get; set; } = new(); // DB Fragen Tabelle (Fragestellung, Antworten, Richtig/Falsch)
        public FragenHelper fHelper { get; set; } = new FragenHelper();

        public void OnGet()
        {
            LadeAlleFrageboegen();
            fHelper.activeUser = User.FindFirstValue(ClaimTypes.Email);
        }

        // DatabaseHelper einmal anlegen, überall verwenden
        private readonly DatabaseHelper db = new DatabaseHelper();

        // Alle Fragebögen laden (SELECT ohne Parameter)
        public void LadeAlleFrageboegen()
        {
            db.HoleDaten(
                sql: "SELECT Join_ID, Titel, Autor, Kategorie, ErstelltAm FROM Fragebogen ORDER BY Join_ID ASC;",
                parameter: null, // kein WHERE nötig
                zeilenVerarbeiten: reader => Frageboegen.Add(new Fragebogen
                {
                    JoinId = reader.GetInt32("Join_ID"),
                    Titel = reader.GetString("Titel"),
                    Autor = reader.GetString("Autor"),
                    Kategorie = reader.GetString("Kategorie"),
                    ErstelltAm = reader.GetDateTime("ErstelltAm")
                })
            );
        }

        // Fragen für einen bestimmten Fragebogen laden (SELECT mit Parameter)
        // Wird von OnPostView und OnPostEdit verwendet
        private void LadeFragen(int fragebogenId)
        {
            db.HoleDaten(
                sql: "SELECT Fragestellung, Antwort1, IstAntwort1Richtig, Antwort2, IstAntwort2Richtig, " +
                     "Antwort3, IstAntwort3Richtig, Antwort4, IstAntwort4Richtig " +
                     "FROM Fragen WHERE FragebogenID = @ID ORDER BY ID;",
                parameter: new Dictionary<string, object> { { "@ID", fragebogenId } }, // WHERE FragebogenID = fragebogenId
                zeilenVerarbeiten: reader => FragenDB.Add(new Fragen
                {
                    Fragestellung = reader.GetString("Fragestellung"),
                    Antwort1 = reader.GetString("Antwort1"),
                    IstAntwort1Richtig = reader.GetBoolean("IstAntwort1Richtig"),
                    Antwort2 = reader.GetString("Antwort2"),
                    IstAntwort2Richtig = reader.GetBoolean("IstAntwort2Richtig"),
                    Antwort3 = reader.GetString("Antwort3"),
                    IstAntwort3Richtig = reader.GetBoolean("IstAntwort3Richtig"),
                    Antwort4 = reader.GetString("Antwort4"),
                    IstAntwort4Richtig = reader.GetBoolean("IstAntwort4Richtig")
                })
            );
        }

        // Fragebogen löschen (DELETE mit Parameter)
        public IActionResult OnPostLoeschen(int id)
        {
            db.Ausfuehren(
                sql: "DELETE FROM Fragebogen WHERE Join_ID = @id;" +
                     "DELETE FROM Fragen WHERE FragebogenID = @id;" +
                     "DELETE FROM playerpoints WHERE GAMEPIN = @id",
                parameter: new Dictionary<string, object> { { "@id", id } }
            );
            return RedirectToPage();
        }

        // Anschauen Button - lädt Fragen und zeigt Popup
        public IActionResult OnPostView(int id)
        {
            GamePin = id;
            LadeFragen(GamePin); // Fragen über die ausgelagerte Methode laden
            ViewData["ShowViewPopup"] = true;
            LadeAlleFrageboegen();
            return Page();
        }

        // Bearbeiten Button - lädt Fragen und zeigt Edit-Popup
        public IActionResult OnPostEdit(int id)
        {
            GamePin = id;
            LadeFragen(GamePin); // gleiche Methode wie bei View
            ViewData["ShowEditPopup"] = true;
            LadeAlleFrageboegen();
            return Page();
        }

        // Speichern der editierten Fragen (UPDATE mit Transaktion)
        public IActionResult OnPostSaveEdit(int fragebogenId, List<Fragen> Fragen)
        {
            // Zuerst alle IDs der Fragen holen
            var frageIds = new List<int>();
            db.HoleDaten(
                sql: "SELECT ID FROM Fragen WHERE FragebogenID = @FragebogenID ORDER BY ID;",
                parameter: new Dictionary<string, object> { { "@FragebogenID", fragebogenId } },
                zeilenVerarbeiten: reader => frageIds.Add(reader.GetInt32("ID"))
            );

            // Jede Frage einzeln updaten
            for (int i = 0; i < Fragen.Count; i++)
            {
                db.Ausfuehren(
                    sql: "UPDATE Fragen SET " +
                         "Fragestellung = @Fragestellung, " +
                         "Antwort1 = @Antwort1, IstAntwort1Richtig = @IstAntwort1Richtig, " +
                         "Antwort2 = @Antwort2, IstAntwort2Richtig = @IstAntwort2Richtig, " +
                         "Antwort3 = @Antwort3, IstAntwort3Richtig = @IstAntwort3Richtig, " +
                         "Antwort4 = @Antwort4, IstAntwort4Richtig = @IstAntwort4Richtig " +
                         "WHERE ID = @ID;",
                    parameter: new Dictionary<string, object>
                    {
                    { "@ID",                 frageIds[i] },
                    { "@Fragestellung",      Fragen[i].Fragestellung },
                    { "@Antwort1",           Fragen[i].Antwort1 },
                    { "@IstAntwort1Richtig", Fragen[i].IstAntwort1Richtig },
                    { "@Antwort2",           Fragen[i].Antwort2 },
                    { "@IstAntwort2Richtig", Fragen[i].IstAntwort2Richtig },
                    { "@Antwort3",           Fragen[i].Antwort3 },
                    { "@IstAntwort3Richtig", Fragen[i].IstAntwort3Richtig },
                    { "@Antwort4",           Fragen[i].Antwort4 },
                    { "@IstAntwort4Richtig", Fragen[i].IstAntwort4Richtig }
                    }
                );
            }

            return RedirectToPage();
        }
    }
}
