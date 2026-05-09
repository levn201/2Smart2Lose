using Smart2Lose.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Http;
using Smart2Lose.Helper;
using Newtonsoft.Json;

namespace Smart2Lose.Pages._1Viewer
{
    public class QuestionState
    {
        public bool Correct { get; set; }
        public int SelectedAnswer { get; set; } // 1–4
    }

    public class PlaygroundModel : PageModel
    {
        public projektName pn = new projektName();

        [BindProperty]
        public Fragen UserAnswer { get; set; } = new();
        public List<Fragen> FragenDB { get; set; } = new();

        public FragenPruefung fp = new FragenPruefung();
        public SpielDurchlauf sd = new SpielDurchlauf();
        public Spiel spiel = new Spiel();

        [BindProperty]
        public int CurrentOffset { get; set; }
        public int QuestionCount { get; set; }

        // Navigation
        public List<QuestionState?> AllQuestionStates { get; set; } = new();
        public int CurrentProgressOffset { get; set; }
        public bool IsReview { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        private const string QStatesKey = "QStates";
        private const string QStatesGameKey = "QStatesGameId";

        public void OnGet(int currentOffset)
        {
            loadHTTP();
            CurrentOffset = currentOffset;
            QuestionCount = spiel.HowManyQuestions(sd.GameID);
            LadeFrage(currentOffset);

            ResetStatesIfNewGame();
            AllQuestionStates = LoadQuestionStates();
            EnsureStatesLength();

            var state = AllQuestionStates[currentOffset];
            if (state != null)
            {
                fp.AnswerChecked = true;
                fp.AnswerCorrect = state.Correct;
                UserAnswer = new Fragen
                {
                    IstAntwort1Richtig = state.SelectedAnswer == 1,
                    IstAntwort2Richtig = state.SelectedAnswer == 2,
                    IstAntwort3Richtig = state.SelectedAnswer == 3,
                    IstAntwort4Richtig = state.SelectedAnswer == 4,
                };
                IsReview = true;
            }

            ComputeProgress();
        }

        private void loadHTTP()
        {
            sd.GameID = HttpContext.Session.GetInt32("GameNumber") ?? 0;
            sd.UserName = HttpContext.Session.GetString("Name") ?? "";
            fp.PlayerPoints = HttpContext.Session.GetInt32("PlayerPoints") ?? 0;
            fp.RightAnswer = HttpContext.Session.GetInt32("RightAnswer") ?? 0;
        }

        private void LadeFrage(int offset)
        {
            FragenDB.Clear();

            if (offset >= spiel.HowManyQuestions(sd.GameID))
                return;

            var db = new SQLconnection.DatenbankZugriff();
            using var connection = db.GetConnection();
            connection.Open();

            string query = @"
                SELECT
                    Fragestellung,
                    Antwort1, IstAntwort1Richtig,
                    Antwort2, IstAntwort2Richtig,
                    Antwort3, IstAntwort3Richtig,
                    Antwort4, IstAntwort4Richtig,
                    BildUrl, LinkUrl
                FROM Fragen
                WHERE FragebogenID = @ID
                ORDER BY ID
                LIMIT 1 OFFSET @Offset;";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@ID", sd.GameID);
            cmd.Parameters.AddWithValue("@Offset", offset);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                FragenDB.Add(Fragen.FromReader(reader));

            QuestionCount = spiel.HowManyQuestions(sd.GameID);
        }

        private void ResetStatesIfNewGame()
        {
            var storedGameId = HttpContext.Session.GetInt32(QStatesGameKey) ?? 0;
            if (storedGameId != sd.GameID)
            {
                HttpContext.Session.Remove(QStatesKey);
                HttpContext.Session.SetInt32(QStatesGameKey, sd.GameID);
            }
        }

        private List<QuestionState?> LoadQuestionStates()
        {
            var json = HttpContext.Session.GetString(QStatesKey);
            if (string.IsNullOrEmpty(json))
                return new List<QuestionState?>();
            return JsonConvert.DeserializeObject<List<QuestionState?>>(json) ?? new List<QuestionState?>();
        }

        private void SaveQuestionStates(List<QuestionState?> states)
        {
            HttpContext.Session.SetString(QStatesKey, JsonConvert.SerializeObject(states));
        }

        private void EnsureStatesLength()
        {
            while (AllQuestionStates.Count < QuestionCount)
                AllQuestionStates.Add(null);
        }

        private void ComputeProgress()
        {
            CurrentProgressOffset = AllQuestionStates.FindIndex(s => s == null);
            if (CurrentProgressOffset == -1)
                CurrentProgressOffset = QuestionCount;
        }

        public IActionResult OnPostNextQuestion()
        {
            loadHTTP();
            CurrentOffset++;
            fp.AnswerChecked = false;
            return RedirectToPage(new { CurrentOffset = CurrentOffset });
        }

        public IActionResult OnPostCheckAnswer()
        {
            loadHTTP();
            LadeFrage(CurrentOffset);

            var currentQuestion = FragenDB[0];

            bool isCorrect = UserAnswer.IstAntwort1Richtig == currentQuestion.IstAntwort1Richtig &&
                             UserAnswer.IstAntwort2Richtig == currentQuestion.IstAntwort2Richtig &&
                             UserAnswer.IstAntwort3Richtig == currentQuestion.IstAntwort3Richtig &&
                             UserAnswer.IstAntwort4Richtig == currentQuestion.IstAntwort4Richtig;

            fp.AnswerChecked = true;
            fp.AnswerCorrect = isCorrect;

            if (isCorrect)
            {
                fp.RightAnswer += 1;
                fp.PlayerPoints += 100;
                HttpContext.Session.SetInt32("PlayerPoints", fp.PlayerPoints);
                HttpContext.Session.SetInt32("RightAnswer", fp.RightAnswer);
            }
            else
            {
                fp.PlayerPoints -= 5;
                HttpContext.Session.SetInt32("PlayerPoints", fp.PlayerPoints);
            }

            AllQuestionStates = LoadQuestionStates();
            EnsureStatesLength();

            int selectedAnswer = UserAnswer.IstAntwort1Richtig ? 1 :
                                 UserAnswer.IstAntwort2Richtig ? 2 :
                                 UserAnswer.IstAntwort3Richtig ? 3 :
                                 UserAnswer.IstAntwort4Richtig ? 4 : 0;

            AllQuestionStates[CurrentOffset] = new QuestionState { Correct = isCorrect, SelectedAnswer = selectedAnswer };
            SaveQuestionStates(AllQuestionStates);

            ComputeProgress();
            AktualisiereWorkshopTracking();

            return Page();
        }

        private void AktualisiereWorkshopTracking()
        {
            try
            {
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();
                using var cmd = new MySqlCommand(@"
                    INSERT INTO WorkshopTeilnehmer (GamePin, Nickname, AktuelleOffset, QuestionCount, Punkte, LetztesUpdate)
                    VALUES (@pin, @nick, @offset, @count, @pts, NOW())
                    ON DUPLICATE KEY UPDATE
                        AktuelleOffset = @offset,
                        QuestionCount  = @count,
                        Punkte         = @pts,
                        LetztesUpdate  = NOW()", connection);
                cmd.Parameters.AddWithValue("@pin",    sd.GameID);
                cmd.Parameters.AddWithValue("@nick",   sd.UserName);
                cmd.Parameters.AddWithValue("@offset", CurrentOffset + 1);
                cmd.Parameters.AddWithValue("@count",  QuestionCount);
                cmd.Parameters.AddWithValue("@pts",    fp.PlayerPoints);
                cmd.ExecuteNonQuery();
            }
            catch
            {
                // Tracking-Fehler sollen das Spielerlebnis nicht unterbrechen
            }
        }

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
