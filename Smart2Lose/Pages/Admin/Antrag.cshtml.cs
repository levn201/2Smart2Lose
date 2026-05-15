using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using Smart2Lose.Model;
using System.Security.Claims;
using Smart2Lose.Helper;

namespace Smart2Lose.Pages.Admin
{
    [Authorize(Roles = "Admin,ReadOnly")]
    public class AntragModel : PageModel
    {
        public projektName pn = new projektName();
        public List<AntragZeile> MeineAntraege { get; set; } = new();
        public string? StatusNachricht { get; set; }

        public class AntragZeile
        {
            public int Id { get; set; }
            public string ZielRolle { get; set; } = string.Empty;
            public string? Nachricht { get; set; }
            public string Status { get; set; } = string.Empty;
            public DateTime ErstelltAm { get; set; }
            public string? AdminKommentar { get; set; }
        }

        public IActionResult OnGet()
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            if (string.IsNullOrEmpty(uid)) return RedirectToPage("/Account/Login");
            MeineAntraege = LadeMeineAntraege(uid);
            return Page();
        }

        public IActionResult OnPost(string zielRolle, string? nachricht)
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            if (string.IsNullOrEmpty(uid)) return RedirectToPage("/Account/Login");

            if (zielRolle != "User" && zielRolle != "Admin")
            {
                StatusNachricht = "Ungültige Zielrolle.";
                MeineAntraege = LadeMeineAntraege(uid);
                return Page();
            }

            var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

            try
            {
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                // Prüfen ob bereits ein offener Antrag existiert
                using var checkCmd = new MySqlCommand(
                    "SELECT COUNT(*) FROM RollenAntraege WHERE UserId = @uid AND Status = 'Ausstehend'",
                    connection);
                checkCmd.Parameters.AddWithValue("@uid", uid);
                var existing = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (existing > 0)
                {
                    StatusNachricht = "Du hast bereits einen offenen Antrag.";
                    MeineAntraege = LadeMeineAntraege(uid);
                    return Page();
                }

                using var insertCmd = new MySqlCommand(
                    "INSERT INTO RollenAntraege (UserId, Email, ZielRolle, Nachricht, Status, ErstelltAm) " +
                    "VALUES (@uid, @email, @zielRolle, @nachricht, 'Ausstehend', NOW())",
                    connection);
                insertCmd.Parameters.AddWithValue("@uid", uid);
                insertCmd.Parameters.AddWithValue("@email", email);
                insertCmd.Parameters.AddWithValue("@zielRolle", zielRolle);
                insertCmd.Parameters.AddWithValue("@nachricht", (object?)nachricht ?? DBNull.Value);
                insertCmd.ExecuteNonQuery();

                StatusNachricht = "Antrag erfolgreich gestellt.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Stellen des Antrags: {ex.Message}");
                StatusNachricht = "Fehler beim Stellen des Antrags. Bitte versuche es erneut.";
            }

            MeineAntraege = LadeMeineAntraege(uid);
            return Page();
        }

        private List<AntragZeile> LadeMeineAntraege(string uid)
        {
            var liste = new List<AntragZeile>();

            try
            {
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                using var cmd = new MySqlCommand(
                    "SELECT Id, ZielRolle, Nachricht, Status, ErstelltAm, AdminKommentar " +
                    "FROM RollenAntraege WHERE UserId = @uid ORDER BY ErstelltAm DESC",
                    connection);
                cmd.Parameters.AddWithValue("@uid", uid);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    liste.Add(new AntragZeile
                    {
                        Id = reader.GetInt32("Id"),
                        ZielRolle = reader.GetString("ZielRolle"),
                        Nachricht = reader.IsDBNull(reader.GetOrdinal("Nachricht"))
                            ? null
                            : reader.GetString("Nachricht"),
                        Status = reader.GetString("Status"),
                        ErstelltAm = reader.GetDateTime("ErstelltAm"),
                        AdminKommentar = reader.IsDBNull(reader.GetOrdinal("AdminKommentar"))
                            ? null
                            : reader.GetString("AdminKommentar")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Laden der Anträge: {ex.Message}");
            }

            return liste;
        }
    }
}
