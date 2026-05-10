using ClosedXML.Excel;
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
        private readonly IWebHostEnvironment _env;
        public FrageboegenErstellenModel(IWebHostEnvironment env) => _env = env;

        public AdminHelper AdminHelper = new AdminHelper();
        public projektName pn = new projektName();
        [BindProperty] public Fragebogen fb { get; set; } = new();

        public string FragenError { get; set; } = string.Empty;
        public string ImportError { get; set; } = string.Empty;

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

            // Titel pr�fen
            if (string.IsNullOrWhiteSpace(fb.Titel))
            {
                FragenError = "Bitte einen Titel eingeben.";
                return Page();
            } 

            // Fragen pr�fen
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

            // Bild-Uploads verarbeiten
            for (int i = 0; i < fb.Fragen.Count; i++)
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
                    fb.Fragen[i].BildUrl = $"/uploads/fragen/{fileName}";
                }
            }

            try
            {
                fb.Autor = User.FindFirstValue(ClaimTypes.Email) ?? "";

                var db = new SQLconnection.DatenbankZugriff();
                using var con = db.GetConnection();
                con.Open();

                using var tx = con.BeginTransaction();

                long fid;

                // Fragebogen einf�gen
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

                // Fragen einf�gen
                foreach (var f in fb.Fragen)
                {
                    using var cmd = new MySqlCommand(@"
                        INSERT INTO Fragen
                            (FragebogenID, Fragestellung,
                             Antwort1, IstAntwort1Richtig,
                             Antwort2, IstAntwort2Richtig,
                             Antwort3, IstAntwort3Richtig,
                             Antwort4, IstAntwort4Richtig,
                             BildUrl, LinkUrl)
                        VALUES
                            (@id, @q,
                             @a1, @r1,
                             @a2, @r2,
                             @a3, @r3,
                             @a4, @r4,
                             @bild, @link);",
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

                    cmd.Parameters.AddWithValue("@bild", (object?)f.BildUrl ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@link", (object?)f.LinkUrl ?? DBNull.Value);

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

        public IActionResult OnGetVorlage()
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Fragen");

            ws.Cell(1, 1).Value = "Fragestellung";
            ws.Cell(1, 2).Value = "Antwort1";
            ws.Cell(1, 3).Value = "Antwort2";
            ws.Cell(1, 4).Value = "Antwort3";
            ws.Cell(1, 5).Value = "Antwort4";
            ws.Cell(1, 6).Value = "RichtigeAntwort (1-4)";

            ws.Cell(2, 1).Value = "Was ist die Hauptstadt von Deutschland?";
            ws.Cell(2, 2).Value = "München";
            ws.Cell(2, 3).Value = "Berlin";
            ws.Cell(2, 4).Value = "Hamburg";
            ws.Cell(2, 5).Value = "Frankfurt";
            ws.Cell(2, 6).Value = 2;

            var header = ws.Range(1, 1, 1, 6);
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml("#024651");
            header.Style.Font.FontColor = XLColor.White;
            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Smart2Lose_Vorlage.xlsx");
        }

        public IActionResult OnPostImportExcel()
        {
            if (fb.JoinId <= 0) fb.JoinId = AdminHelper.RandomNum();

            var datei = Request.Form.Files["importDatei"];
            var titel = Request.Form["importTitel"].ToString().Trim();
            var kategorie = Request.Form["importKategorie"].ToString().Trim();

            if (datei == null || datei.Length == 0)
            {
                ImportError = "Bitte eine Excel-Datei auswählen.";
                return Page();
            }
            if (string.IsNullOrWhiteSpace(titel))
            {
                ImportError = "Bitte einen Titel eingeben.";
                return Page();
            }

            var fragen = new List<Fragen>();
            try
            {
                using var stream = datei.OpenReadStream();
                using var wb = new XLWorkbook(stream);
                var ws = wb.Worksheet(1);
                int row = 2;
                while (!ws.Row(row).IsEmpty())
                {
                    var fragestellung = ws.Cell(row, 1).GetString().Trim();
                    if (string.IsNullOrWhiteSpace(fragestellung)) break;

                    var richtigRaw = ws.Cell(row, 6).GetString().Trim();
                    if (!int.TryParse(richtigRaw, out int richtig) || richtig < 1 || richtig > 4)
                    {
                        ImportError = $"Zeile {row}: RichtigeAntwort muss 1, 2, 3 oder 4 sein.";
                        return Page();
                    }

                    fragen.Add(new Fragen
                    {
                        Fragestellung = fragestellung,
                        Antwort1 = ws.Cell(row, 2).GetString().Trim(),
                        Antwort2 = ws.Cell(row, 3).GetString().Trim(),
                        Antwort3 = ws.Cell(row, 4).GetString().Trim(),
                        Antwort4 = ws.Cell(row, 5).GetString().Trim(),
                        IstAntwort1Richtig = richtig == 1,
                        IstAntwort2Richtig = richtig == 2,
                        IstAntwort3Richtig = richtig == 3,
                        IstAntwort4Richtig = richtig == 4,
                    });
                    row++;
                }
            }
            catch (Exception ex)
            {
                ImportError = $"Fehler beim Lesen der Datei: {ex.Message}";
                return Page();
            }

            if (fragen.Count == 0)
            {
                ImportError = "Keine Fragen in der Datei gefunden (ab Zeile 2).";
                return Page();
            }

            try
            {
                var joinId = AdminHelper.RandomNum();
                var autor = User.FindFirstValue(ClaimTypes.Email) ?? "";

                var db = new SQLconnection.DatenbankZugriff();
                using var con = db.GetConnection();
                con.Open();
                using var tx = con.BeginTransaction();

                using (var cmd = new MySqlCommand(
                    "INSERT INTO Fragebogen (Titel, Join_ID, Autor, Kategorie) VALUES (@t, @j, @a, @k);",
                    con, tx))
                {
                    cmd.Parameters.AddWithValue("@t", titel);
                    cmd.Parameters.AddWithValue("@j", joinId);
                    cmd.Parameters.AddWithValue("@a", autor);
                    cmd.Parameters.AddWithValue("@k", string.IsNullOrWhiteSpace(kategorie) ? "Unternehmen" : kategorie);
                    cmd.ExecuteNonQuery();
                }

                foreach (var f in fragen)
                {
                    using var cmd = new MySqlCommand(@"
                        INSERT INTO Fragen (FragebogenID, Fragestellung,
                            Antwort1, IstAntwort1Richtig, Antwort2, IstAntwort2Richtig,
                            Antwort3, IstAntwort3Richtig, Antwort4, IstAntwort4Richtig,
                            BildUrl, LinkUrl)
                        VALUES (@id, @q, @a1, @r1, @a2, @r2, @a3, @r3, @a4, @r4, NULL, NULL);",
                        con, tx);
                    cmd.Parameters.AddWithValue("@id", joinId);
                    cmd.Parameters.AddWithValue("@q", f.Fragestellung);
                    cmd.Parameters.AddWithValue("@a1", f.Antwort1);
                    cmd.Parameters.AddWithValue("@r1", f.IstAntwort1Richtig);
                    cmd.Parameters.AddWithValue("@a2", f.Antwort2);
                    cmd.Parameters.AddWithValue("@r2", f.IstAntwort2Richtig);
                    cmd.Parameters.AddWithValue("@a3", f.Antwort3);
                    cmd.Parameters.AddWithValue("@r3", f.IstAntwort3Richtig);
                    cmd.Parameters.AddWithValue("@a4", f.Antwort4);
                    cmd.Parameters.AddWithValue("@r4", f.IstAntwort4Richtig);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
                return RedirectToPage("/Admin/Frageboegen");
            }
            catch (Exception ex)
            {
                ImportError = $"Datenbankfehler: {ex.Message}";
                return Page();
            }
        }
    }
}