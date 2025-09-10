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
        // LISTEN VON DEN PROPERTY GROUPS 
        [BindProperty]
        public FragenInputModel UserAnswer { get; set; } = new();
        public List<FragenChecknerModel> FragenChecken { get; set; } = new();


        // ZUGANG AUF DIE GAMES 
        public int GamePin { get; set; }
        public string UserName { get; set; }


        // FRAGEN SWITCHEN
        [BindProperty]
        public int CurrentOffset { get; set; }
        public int QuestionCount { get; set; }


        // PRÜFEN DER ANTWORT 
        public int PlayerPoints { get; set; }
        public int RightAnswer {  get; set; }
        public bool AnswerChecked { get; set; }
        public bool AnswerCorrect { get; set; }

        // MASSAGES
        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }





        // Erst geladen
        public void OnGet(int currentOffset)
        {
            loadHTTP();
            CurrentOffset = currentOffset;
            QuestionCount = HowManyQuestions();
            LadeFrage(currentOffset);
        }

        // Lädt HTTP SESSIONS
        private void loadHTTP()
        {
            GamePin = HttpContext.Session.GetInt32("GameNumber") ?? 0;
            UserName = HttpContext.Session.GetString("Name") ?? "";
            PlayerPoints = HttpContext.Session.GetInt32("PlayerPoints") ?? 0;
            RightAnswer = HttpContext.Session.GetInt32("RightAnswer") ?? 0;
        }

        // Wie viele Fragen
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

        // Lädt Fragen
        private void LadeFrage(int offset)
        {
            FragenChecken.Clear();

            if (offset >= HowManyQuestions())
            {
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

        // Button: NÄCHSTE
        public IActionResult OnPostNextQuestion()
        {
            loadHTTP();
            CurrentOffset++;

            AnswerChecked = false;

            return RedirectToPage(new { CurrentOffset = CurrentOffset });
        }

        // Button: ANTWORTEN PRÜFEN
        public IActionResult OnPostCheckAnswer()
        {
            loadHTTP();
            LadeFrage(CurrentOffset);

            var currentQuestion = FragenChecken[0];

            bool isCorrect = false;

            if (            UserAnswer.C_IstAntwort1Richtig == currentQuestion.DB_IstAntwort1Richtig &&
                            UserAnswer.C_IstAntwort2Richtig == currentQuestion.DB_IstAntwort2Richtig &&
                            UserAnswer.C_IstAntwort3Richtig == currentQuestion.DB_IstAntwort3Richtig &&
                            UserAnswer.C_IstAntwort4Richtig == currentQuestion.DB_IstAntwort4Richtig)
            {
                isCorrect = true;
            }

            AnswerChecked = true;
            AnswerCorrect = isCorrect;

            if (isCorrect)
            {
                RightAnswer += 1;
                PlayerPoints += 100 ;
                HttpContext.Session.SetInt32("PlayerPoints", PlayerPoints);
                HttpContext.Session.SetInt32("RightAnswer", RightAnswer);
            }
            else
            {
                PlayerPoints -= 5;
                HttpContext.Session.SetInt32("PlayerPoints", PlayerPoints);
            }


            return Page();
        }

        // Button: QUIZZ BEENDEN
        public IActionResult OnPostFinishQuiz()
        {
            loadHTTP();

            PlayerPoints = HttpContext.Session.GetInt32("PlayerPoints") ?? 0;

            var db = new SQLconnection.DatenbankZugriff();
            using var connection = db.GetConnection();
            connection.Open();

            string query = @"INSERT INTO playerpoints (User_Nickname, SessionPints, GamePin, CorrectAnswered, PossibleAnswers) 
                VALUES (@name, @points, @pin, @correct, @possible);";


            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@pin", GamePin);
            cmd.Parameters.AddWithValue("@points", PlayerPoints);
            cmd.Parameters.AddWithValue("@name", UserName);
            cmd.Parameters.AddWithValue("@Correct", RightAnswer);
            cmd.Parameters.AddWithValue("@Possible", HowManyQuestions());

            cmd.ExecuteNonQuery();

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