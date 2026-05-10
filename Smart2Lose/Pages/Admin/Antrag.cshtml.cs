using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using Smart2Lose.Model;
using System.Security.Claims;

namespace Smart2Lose.Pages.Admin
{
    [Authorize(Roles = "Admin,User,ReadOnly")]
    public class AntragModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;

        public AntragModel(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

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

        public async Task OnGetAsync()
        {
            await LadeMeineAntraege();
        }

        public async Task<IActionResult> OnPostAsync(string zielRolle, string? nachricht)
        {
            if (zielRolle != "User" && zielRolle != "Admin")
            {
                StatusNachricht = "Ungültige Zielrolle.";
                await LadeMeineAntraege();
                return Page();
            }

            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
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
                    await LadeMeineAntraege();
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

            await LadeMeineAntraege();
            return Page();
        }

        private async Task LadeMeineAntraege()
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);

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
                    MeineAntraege.Add(new AntragZeile
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

            await Task.CompletedTask;
        }
    }
}
