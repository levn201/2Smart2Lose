using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using KahootTransnetBW.Model;
using MySqlX.XDevAPI;
using System.Web;





namespace KahootTransnetBW.Pages._1Viewer
{
    public class PlaygroundModel : PageModel
    {

        public List<FragenChecknerModel> FragenChecken { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int GamePin { get; set; }

        [BindProperty]
        public int CurrentOffset { get; set; }


        public int QuestionCount { get; set; }
        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }

        [BindProperty]
        public FragenInputModel UserAnswer { get; set; } = new();


        // Erst geladen                     => Lädt aktuellen Offset und GamePin
        public void OnGet(int currentOffset, int gamePin)
        {
            CurrentOffset = currentOffset;
            GamePin = gamePin;
            QuestionCount = HowManyQuestions();
            LadeFrage(currentOffset);

        }

        // Check Fragen                     => Wie viele Fragen enthält der Fragebogen
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
            // Gibt immer die werte nach dem hoch setzten 
            return RedirectToPage(new { GamePin = GamePin, CurrentOffset = CurrentOffset });
        }

        // Button: ANTWORTEN PRÜFEN         => Checkt ob die antworten richtig sind 
        public IActionResult OnPostCheckAnswer()
        {
            // Hier wird geckeckt ob die antowrten richtig sind 
            return Page();
        }

        // Button: QUIZZ BEENDEN            => Beendet nach der letzten Frage und speichert den stand 
        public IActionResult OnPostFinishQuiz()
        {
            // Hier wird geckeckt ob die antowrten richtig sind 
            return Page();
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
