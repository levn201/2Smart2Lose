using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using Smart2Lose.Model;
using System.Security.Claims;
using static Smart2Lose.Helper.SQLconnection;

namespace Smart2Lose.Pages.Admin
{
    [Authorize(Roles = "Admin,User")]
    public class WorkshopDashboardModel : PageModel
    {
        public projektName pn = new projektName();

        [BindProperty(SupportsGet = true)]
        public int JoinId { get; set; }

        public string FragebogenTitel { get; set; } = "";
        public List<TeilnehmerRow> Teilnehmer { get; set; } = new();

        public class TeilnehmerRow
        {
            public string Nickname { get; set; } = "";
            public int AktuelleOffset { get; set; }
            public int QuestionCount { get; set; }
            public int Punkte { get; set; }
            public DateTime LetztesUpdate { get; set; }
        }

        public IActionResult OnGet()
        {
            if (!IstAutorisiert())
                return RedirectToPage("/Admin/Frageboegen");

            FragebogenTitel = LadeFragebogenTitel();
            Teilnehmer = LadeTeilnehmer();
            return Page();
        }

        public IActionResult OnGetData()
        {
            if (!IstAutorisiert())
                return Unauthorized();

            return new JsonResult(LadeTeilnehmer());
        }

        private bool IstAutorisiert()
        {
            if (User.IsInRole("Admin"))
                return true;

            if (!User.IsInRole("User"))
                return false;

            var autorEmail = User.FindFirstValue(ClaimTypes.Email) ?? "";
            var db = new DatenbankZugriff();
            using var con = db.GetConnection();
            con.Open();
            using var cmd = new MySqlCommand(
                "SELECT COUNT(*) FROM Fragebogen WHERE Join_ID = @id AND Autor = @autor", con);
            cmd.Parameters.AddWithValue("@id",    JoinId);
            cmd.Parameters.AddWithValue("@autor", autorEmail);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private string LadeFragebogenTitel()
        {
            var db = new DatenbankZugriff();
            using var con = db.GetConnection();
            con.Open();
            using var cmd = new MySqlCommand(
                "SELECT Titel FROM Fragebogen WHERE Join_ID = @id", con);
            cmd.Parameters.AddWithValue("@id", JoinId);
            return cmd.ExecuteScalar()?.ToString() ?? "Unbekannt";
        }

        private List<TeilnehmerRow> LadeTeilnehmer()
        {
            var liste = new List<TeilnehmerRow>();
            var db = new DatenbankZugriff();
            using var con = db.GetConnection();
            con.Open();
            using var cmd = new MySqlCommand(@"
                SELECT Nickname, AktuelleOffset, QuestionCount, Punkte, LetztesUpdate
                FROM WorkshopTeilnehmer
                WHERE GamePin = @pin
                ORDER BY AktuelleOffset DESC, Punkte DESC", con);
            cmd.Parameters.AddWithValue("@pin", JoinId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                liste.Add(new TeilnehmerRow
                {
                    Nickname       = reader.GetString("Nickname"),
                    AktuelleOffset = reader.GetInt32("AktuelleOffset"),
                    QuestionCount  = reader.GetInt32("QuestionCount"),
                    Punkte         = reader.GetInt32("Punkte"),
                    LetztesUpdate  = reader.GetDateTime("LetztesUpdate")
                });
            }
            return liste;
        }
    }
}
