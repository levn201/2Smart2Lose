using Smart2Lose.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using System.Net.NetworkInformation;
using System.Web;
using Microsoft.AspNetCore.Http;
using Smart2Lose.Helper;

namespace Smart2Lose.Pages._1Viewer
{
    public class PlaygroundModel : PageModel
    {

        public projektName pn = new projektName();

        // LISTEN VON DEN PROPERTY GROUPS 
        [BindProperty]
        public SelectionCheck UserAnswer { get; set; } = new();
        public List<Fragen> FragenDB { get; set; } = new();

        public FragenPruefung fp = new FragenPruefung();

        public SpielDurchlauf sd = new SpielDurchlauf();
       
        public Spiel spiel = new Spiel();


        


        // OFFSET UND FRAGEN ANZAHL
        [BindProperty]
        public int CurrentOffset { get; set; }
        public int QuestionCount { get; set; }

        // MASSAGES
        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;



        // Erst geladen
        public void OnGet(int currentOffset)
        {
            loadHTTP();
            CurrentOffset = currentOffset;
            QuestionCount = spiel.HowManyQuestions(sd.GameID);
            LadeFrage(currentOffset);
        }

        // Lädt HTTP SESSIONS
        private void loadHTTP()
        {
            sd.GameID = HttpContext.Session.GetInt32("GameNumber") ?? 0;          // Spiel ID
            sd.UserName = HttpContext.Session.GetString("Name") ?? "";
            fp.PlayerPoints = HttpContext.Session.GetInt32("PlayerPoints") ?? 0;
            fp.RightAnswer = HttpContext.Session.GetInt32("RightAnswer") ?? 0;
        }

        // Lädt Fragen
        private void LadeFrage(int offset)
        {
            FragenDB.Clear();

            if (offset >= spiel.HowManyQuestions(sd.GameID))
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
            cmd.Parameters.AddWithValue("@ID", sd.GameID);
            cmd.Parameters.AddWithValue("@Offset", offset);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                FragenDB.Add(new Fragen
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
                });
            }

            QuestionCount = spiel.HowManyQuestions(sd.GameID);
        }

        // Button: NÄCHSTE
        public IActionResult OnPostNextQuestion()
        {
            loadHTTP();
            CurrentOffset++;

            fp.AnswerChecked = false;

            return RedirectToPage(new { CurrentOffset = CurrentOffset });
        }

        // Button: ANTWORTEN PRÜFEN
        public IActionResult OnPostCheckAnswer()
        {
            loadHTTP();
            LadeFrage(CurrentOffset);

            var currentQuestion = FragenDB[0];

            bool isCorrect = false;

            if (            UserAnswer.C_IstAntwort1Richtig == currentQuestion.IstAntwort1Richtig &&
                            UserAnswer.C_IstAntwort2Richtig == currentQuestion.IstAntwort2Richtig &&
                            UserAnswer.C_IstAntwort3Richtig == currentQuestion.IstAntwort3Richtig &&
                            UserAnswer.C_IstAntwort4Richtig == currentQuestion.IstAntwort4Richtig)
            {
                isCorrect = true;
            }

            fp.AnswerChecked = true;
            fp.AnswerCorrect = isCorrect;

            if (isCorrect)
            {
                fp.RightAnswer += 1;
                fp.PlayerPoints += 100 ;
                HttpContext.Session.SetInt32("PlayerPoints", fp.PlayerPoints);
                HttpContext.Session.SetInt32("RightAnswer", fp.RightAnswer);
            }
            else
            {
                fp.PlayerPoints -= 5;
                HttpContext.Session.SetInt32("PlayerPoints", fp.PlayerPoints);
            }


            return Page();
        }

        // Button: QUIZZ BEENDEN
        public IActionResult OnPostFinishQuiz()
        {
            loadHTTP();

            fp.PlayerPoints = HttpContext.Session.GetInt32("PlayerPoints") ?? 0;

            var db = new SQLconnection.DatenbankZugriff();
            using var connection = db.GetConnection();
            connection.Open();

            string query = @"INSERT INTO playerpoints (User_Nickname, SessionPints, GamePin, CorrectAnswered, PossibleAnswers) 
                VALUES (@name, @points, @pin, @correct, @possible);";


            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@pin", sd.GameID);
            cmd.Parameters.AddWithValue("@points", fp.PlayerPoints);
            cmd.Parameters.AddWithValue("@name", sd.UserName);
            cmd.Parameters.AddWithValue("@Correct", fp.RightAnswer);
            cmd.Parameters.AddWithValue("@Possible", spiel.HowManyQuestions(sd.GameID));

            cmd.ExecuteNonQuery();

            return RedirectToPage("/1Viewer/FinalResult");
        }
    }
}