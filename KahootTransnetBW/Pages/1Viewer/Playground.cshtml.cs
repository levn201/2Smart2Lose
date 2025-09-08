using KahootTransnetBW.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using System.Net.NetworkInformation;
using System.Web;
using Microsoft.AspNetCore.Http;





namespace KahootTransnetBW.Pages._1Viewer
{
    public class PlaygroundModel : PageModel
    {
        [BindProperty]
        public FragenInputModel UserAnswer { get; set; } = new();
        public List<FragenChecknerModel> FragenChecken { get; set; } = new();


        public int GamePin { get; set; }
        public string UserName { get; set; }



        [BindProperty]
        public int CurrentOffset { get; set; }
        public int QuestionCount { get; set; }


        public int playerPoints { get; set; }


        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }




        // Erst geladen                     => Lädt aktuellen Offset und GamePin
        public void OnGet(int currentOffset)
        {
            loadHTTP();

            CurrentOffset = currentOffset;

            QuestionCount = HowManyQuestions();
            LadeFrage(currentOffset);
        }

        // Lädt HTTP                        => Nimmt werte aus dem HTTP Sessions und schreibt in die Properties 
        private void loadHTTP()
        {
            GamePin = HttpContext.Session.GetInt32("GameNumber") ?? 0;      //GamePin = gamePin kann weg da jetzt egal wie oft man lädt der bin in GamenUmber drinne steht. 
            UserName = HttpContext.Session.GetString("Name") ?? "";
        }

        // Wie viele Fragen                 => Wie viele Fragen enthält der Fragebogen
        public int HowManyQuestions()
        {
            var db = new SQLconnection.DatenbankZugriff();
            using var connection = db.GetConnection();
            connection.Open();

            string query = @"SELECT COUNT(*) FROM Fragen WHERE FragebogenID = @ID;";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@ID", GamePin);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        // Lädt Fragen                      => Lädt alle fragen nach dem offset
        private void LadeFrage(int offset)
        {
            FragenChecken.Clear();

            if (offset >= HowManyQuestions())
            {
                ErrorMessage = "Keine weiteren Fragen verfügbar.";
                return;
            }

            var db = new SQLconnection.DatenbankZugriff();
            using var connection = db.GetConnection();
            connection.Open();

            string query = @" 
                SELECT 
                    Fragestellung,
                    Antwort1, IstAntwort1Richtig,
                    Antwort2, IstAntwort2Richtig,
                    Antwort3, IstAntwort3Richtig,
                    Antwort4, IstAntwort4Richtig
                FROM Fragen
                WHERE FragebogenID = @ID
                ORDER BY ID
                LIMIT 1 OFFSET @Offset;";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@ID", GamePin);
            cmd.Parameters.AddWithValue("@Offset", offset);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                FragenChecken.Add(new FragenChecknerModel
                {
                    DB_Fragestellung = reader.GetString("Fragestellung"),
                    DB_Antwort1 = reader.GetString("Antwort1"),
                    DB_IstAntwort1Richtig = reader.GetBoolean("IstAntwort1Richtig"),
                    DB_Antwort2 = reader.GetString("Antwort2"),
                    DB_IstAntwort2Richtig = reader.GetBoolean("IstAntwort2Richtig"),
                    DB_Antwort3 = reader.GetString("Antwort3"),
                    DB_IstAntwort3Richtig = reader.GetBoolean("IstAntwort3Richtig"),
                    DB_Antwort4 = reader.GetString("Antwort4"),
                    DB_IstAntwort4Richtig = reader.GetBoolean("IstAntwort4Richtig")
                });
            }

            QuestionCount = HowManyQuestions();
        }

        // Button: NÄCHSTE                  => Geht zur nächsten Frage 
        public IActionResult OnPostNextQuestion()
        {
            CurrentOffset++;
            return RedirectToPage(new { CurrentOffset = CurrentOffset });
        }



        // gib girhub



        // NOCH IN NICHT FERTIG 

        // Button: ANTWORTEN PRÜFEN         => Checkt ob die antworten richtig sind 
        public IActionResult OnPostCheckAnswer()
        {
            return Page();
        }

        // Button: QUIZZ BEENDEN            => Beendet nach der letzten Frage und speichert den stand 
        public IActionResult OnPostFinishQuiz()
        {
            var db = new SQLconnection.DatenbankZugriff();
            using var connection = db.GetConnection();
            connection.Open();

            string query = @"INSERT INTO playerpoints (User_Nickname, SessionPints, GamePin) 
                     VALUES (@name, @points, @pin);";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@pin", GamePin);
            cmd.Parameters.AddWithValue("@points", playerPoints);
            cmd.Parameters.AddWithValue("@name", UserName);

            cmd.ExecuteNonQuery(); // <--- Daten werden hier gespeichert

            return RedirectToPage("/1Viewer/FinalResult");
        }


        public class FragenChecknerModel
        {
            public string DB_Fragestellung { get; set; }
            public string DB_Antwort1 { get; set; }
            public bool DB_IstAntwort1Richtig { get; set; }
            public string DB_Antwort2 { get; set; }
            public bool DB_IstAntwort2Richtig { get; set; }
            public string DB_Antwort3 { get; set; }
            public bool DB_IstAntwort3Richtig { get; set; }
            public string DB_Antwort4 { get; set; }
            public bool DB_IstAntwort4Richtig { get; set; }
        }

        public class FragenInputModel
        {
            public bool C_IstAntwort1Richtig { get; set; }
            public bool C_IstAntwort2Richtig { get; set; }
            public bool C_IstAntwort3Richtig { get; set; }
            public bool C_IstAntwort4Richtig { get; set; }
        }
    }
}
