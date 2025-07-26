using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using MySql.Data.MySqlClient;
using KahootTransnetBW.Model;
using Mysqlx.Crud;


namespace KahootTransnetBW.Pages.Admin
{
    public class FrageboegenErstellenModel : PageModel
    {


        [BindProperty(SupportsGet = true)]
        public string Titel { get; set; }

        [BindProperty(SupportsGet = true)]
        public int JoinNumber { get; set; }

        [BindProperty]
        public int FragebogenJoinId { get; set; }


        // Macht JoinNumber gleich wie FragebogenID 
        public void OnGet()
        {
            FragebogenJoinId = JoinNumber;
        }


        // Random Join Number wird einmal beim Titel einschrirben durchgeführt und übertragen 
        public int RandomNum()
        {
            int number;
            var random = new Random();
            var db = new SQLconnection.DatenbankZugriff();

            using var connection = db.GetConnection();
            connection.Open();

            bool exists;

            do
            {
                number = random.Next(1000, 10000);

                string query = "SELECT COUNT(*) FROM Fragebogen WHERE Join_ID = @Join_ID";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Join_ID", number);

                var count = Convert.ToInt32(cmd.ExecuteScalar());
                exists = count > 0;

            } while (exists);

            return number;
        }



        // Titel Speichert und übertragen an das PopUp
        public string loadError { get; set; }
        public string TitelError { get; set; }

        public IActionResult OnPost()
        {
            if (string.IsNullOrWhiteSpace(Titel))
            {
                TitelError = "Gebe einen Titel ein.";
                return Page();
            }

            try
            {
  
                JoinNumber = RandomNum(); // speichere die ID im Property

                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                string query = "INSERT INTO Fragebogen (Titel, Join_ID) VALUES (@titel, @Join_ID)";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@titel", Titel);
                cmd.Parameters.AddWithValue("@Join_ID", JoinNumber);

                cmd.ExecuteNonQuery();

                // Ein Flag setzen, um im Frontend zu wissen, dass gespeichert wurde
                ViewData["ShowPopup"] = true;

                return Page();
            }
            catch (MySqlException ex)
            {
                TitelError = $"MySQL-Fehler: {ex.Message}";
                return Page();
            }
            catch (Exception ex)
            {
                TitelError = $"Allgemeiner Fehler: {ex.Message}";
                return Page();
            }
        }



        // INSERT die ganzen Fekder in die Datenbank und aufrufen des PopUps nach jeder speicherung 
        [BindProperty]
        public string Fragestellung { get; set; }

        [BindProperty]
        public string Antwort1 { get; set; }

        [BindProperty]
        public bool IstAntwort1Richtig { get; set; }

        [BindProperty]
        public string Antwort2 { get; set; }

        [BindProperty]
        public bool IstAntwort2Richtig { get; set; }

        [BindProperty]
        public string Antwort3 { get; set; }

        [BindProperty]
        public bool IstAntwort3Richtig { get; set; }

        [BindProperty]
        public string Antwort4 { get; set; }

        [BindProperty]
        public bool IstAntwort4Richtig { get; set; }

        public string FragenError { get; set; }
        public IActionResult OnPostAddFrage()
        {
            try
            {
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                string query = @"
                    INSERT INTO Fragen (
                        FragebogenID,
                        Fragestellung,
                        Antwort1,
                        IstAntwort1Richtig,
                        Antwort2,
                        IstAntwort2Richtig,
                        Antwort3,
                        IstAntwort3Richtig,
                        Antwort4,
                        IstAntwort4Richtig
                    ) VALUES (
                        @FragebogenID,
                        @Fragestellung,
                        @Antwort1,
                        @IstAntwort1Richtig,
                        @Antwort2,
                        @IstAntwort2Richtig,
                        @Antwort3,
                        @IstAntwort3Richtig,
                        @Antwort4,
                        @IstAntwort4Richtig
                    );";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@FragebogenID", JoinNumber);
                cmd.Parameters.AddWithValue("@Fragestellung", Fragestellung);
                cmd.Parameters.AddWithValue("@Antwort1", Antwort1);
                cmd.Parameters.AddWithValue("@IstAntwort1Richtig", IstAntwort1Richtig);
                cmd.Parameters.AddWithValue("@Antwort2", Antwort2);
                cmd.Parameters.AddWithValue("@IstAntwort2Richtig", IstAntwort2Richtig);
                cmd.Parameters.AddWithValue("@Antwort3", Antwort3);
                cmd.Parameters.AddWithValue("@IstAntwort3Richtig", IstAntwort3Richtig);
                cmd.Parameters.AddWithValue("@Antwort4", Antwort4);
                cmd.Parameters.AddWithValue("@IstAntwort4Richtig", IstAntwort4Richtig);

                cmd.ExecuteNonQuery();

                // Popup erneut anzeigen
                ViewData["ShowPopup"] = true;

                return Page();
            }
            catch (MySqlException ex)
            {
                FragenError = $"MySQL-Fehler: {ex.Message}";
                ViewData["ShowPopup"] = true;
                return Page();
            }
            catch (Exception ex)
            {
                FragenError = $"Allgemeiner Fehler: {ex.Message}";
                ViewData["ShowPopup"] = true;
                return Page();
            }
        }

    }
}
