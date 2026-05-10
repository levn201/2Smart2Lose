using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using Smart2Lose.Model;

namespace Smart2Lose.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class RollenantraegeModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;

        public RollenantraegeModel(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public projektName pn = new projektName();

        public int AusstehendCount { get; set; }
        public int GesamtGenehmigt { get; set; }
        public int GesamtAbgelehnt { get; set; }
        public List<AntragZeile> OffeneAntraege { get; set; } = new();
        public List<AntragZeile> Verlauf { get; set; } = new();

        public class AntragZeile
        {
            public int Id { get; set; }
            public string UserId { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string ZielRolle { get; set; } = string.Empty;
            public string? Nachricht { get; set; }
            public string Status { get; set; } = string.Empty;
            public DateTime ErstelltAm { get; set; }
            public DateTime? BearbeitetAm { get; set; }
            public string? AdminKommentar { get; set; }
        }

        public void OnGet()
        {
            // KPIs
            try
            {
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();
                using var cmd = new MySqlCommand(@"
                    SELECT
                        SUM(CASE WHEN Status='Ausstehend' THEN 1 ELSE 0 END) AS Ausstehend,
                        SUM(CASE WHEN Status='Genehmigt'  THEN 1 ELSE 0 END) AS Genehmigt,
                        SUM(CASE WHEN Status='Abgelehnt'  THEN 1 ELSE 0 END) AS Abgelehnt
                    FROM RollenAntraege", connection);
                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    AusstehendCount   = r.IsDBNull(0) ? 0 : Convert.ToInt32(r[0]);
                    GesamtGenehmigt   = r.IsDBNull(1) ? 0 : Convert.ToInt32(r[1]);
                    GesamtAbgelehnt   = r.IsDBNull(2) ? 0 : Convert.ToInt32(r[2]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Rollenantraege] KPI-Fehler: {ex.Message}");
            }

            // Offene Anträge
            try
            {
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();
                using var cmd = new MySqlCommand(
                    "SELECT Id, UserId, Email, ZielRolle, Nachricht, Status, ErstelltAm, BearbeitetAm, AdminKommentar " +
                    "FROM RollenAntraege WHERE Status='Ausstehend' ORDER BY ErstelltAm ASC", connection);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    OffeneAntraege.Add(LeseZeile(reader));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Rollenantraege] Offene-Fehler: {ex.Message}");
            }

            // Verlauf
            try
            {
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();
                using var cmd = new MySqlCommand(
                    "SELECT Id, UserId, Email, ZielRolle, Nachricht, Status, ErstelltAm, BearbeitetAm, AdminKommentar " +
                    "FROM RollenAntraege WHERE Status != 'Ausstehend' ORDER BY BearbeitetAm DESC LIMIT 20", connection);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    Verlauf.Add(LeseZeile(reader));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Rollenantraege] Verlauf-Fehler: {ex.Message}");
            }
        }

        public async Task<IActionResult> OnPostGenehmigenAsync(int id)
        {
            // Antrag aus DB laden
            AntragZeile? antrag = null;
            try
            {
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();
                using var cmd = new MySqlCommand(
                    "SELECT Id, UserId, Email, ZielRolle, Nachricht, Status, ErstelltAm, BearbeitetAm, AdminKommentar " +
                    "FROM RollenAntraege WHERE Id = @id LIMIT 1", connection);
                cmd.Parameters.AddWithValue("@id", id);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                    antrag = LeseZeile(reader);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Rollenantraege] Genehmigen-Laden-Fehler: {ex.Message}");
            }

            if (antrag != null && !string.IsNullOrEmpty(antrag.UserId))
            {
                var user = await _userManager.FindByIdAsync(antrag.UserId);
                if (user != null)
                {
                    // Alle bestehenden Rollen entfernen
                    var vorhandeneRollen = await _userManager.GetRolesAsync(user);
                    if (vorhandeneRollen.Any())
                        await _userManager.RemoveFromRolesAsync(user, vorhandeneRollen);

                    // Zielrolle zuweisen
                    await _userManager.AddToRoleAsync(user, antrag.ZielRolle);
                }
                // Wenn User nicht gefunden: trotzdem als Genehmigt markieren
            }

            // Status in DB aktualisieren
            try
            {
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();
                using var cmd = new MySqlCommand(
                    "UPDATE RollenAntraege SET Status='Genehmigt', BearbeitetAm=NOW() WHERE Id=@id", connection);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Rollenantraege] Genehmigen-Update-Fehler: {ex.Message}");
            }

            return RedirectToPage();
        }

        public IActionResult OnPostAblehnen(int id, string? kommentar)
        {
            try
            {
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();
                using var cmd = new MySqlCommand(
                    "UPDATE RollenAntraege SET Status='Abgelehnt', BearbeitetAm=NOW(), AdminKommentar=@kommentar WHERE Id=@id",
                    connection);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@kommentar", (object?)kommentar ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Rollenantraege] Ablehnen-Fehler: {ex.Message}");
            }

            return RedirectToPage();
        }

        private static AntragZeile LeseZeile(MySqlDataReader r)
        {
            return new AntragZeile
            {
                Id             = r.GetInt32("Id"),
                UserId         = r.IsDBNull(r.GetOrdinal("UserId"))         ? string.Empty : r.GetString("UserId"),
                Email          = r.IsDBNull(r.GetOrdinal("Email"))          ? string.Empty : r.GetString("Email"),
                ZielRolle      = r.IsDBNull(r.GetOrdinal("ZielRolle"))      ? string.Empty : r.GetString("ZielRolle"),
                Nachricht      = r.IsDBNull(r.GetOrdinal("Nachricht"))      ? null         : r.GetString("Nachricht"),
                Status         = r.IsDBNull(r.GetOrdinal("Status"))         ? string.Empty : r.GetString("Status"),
                ErstelltAm     = r.GetDateTime("ErstelltAm"),
                BearbeitetAm   = r.IsDBNull(r.GetOrdinal("BearbeitetAm"))   ? null         : r.GetDateTime("BearbeitetAm"),
                AdminKommentar = r.IsDBNull(r.GetOrdinal("AdminKommentar")) ? null         : r.GetString("AdminKommentar")
            };
        }
    }
}
