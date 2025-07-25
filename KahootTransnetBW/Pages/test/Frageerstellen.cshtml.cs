using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using KahootTransnetBW.Model;

namespace KahootTransnetBW.Pages.test
{
    public class FrageerstellenModel : PageModel
    {
        public class AnswerInputModel
        {
            public string Text { get; set; }
            public bool IsCorrect { get; set; }   // NEU
        }


        public class QuestionInputModel
        {
            public string Text { get; set; }
            public List<AnswerInputModel> Answers { get; set; } = new();
        }

        public class QuizInputModel
        {
            public string Title { get; set; }
            public List<QuestionInputModel> Questions { get; set; } = new();
        }


        [BindProperty]
        public QuizInputModel Quiz { get; set; } = new();

        public void OnGet()
        {
            // Optional: Erste Frage hinzufügen
            if (Quiz.Questions.Count == 0)
            {
                Quiz.Questions.Add(new QuestionInputModel
                {
                    Answers = new List<AnswerInputModel>
                {
                    new AnswerInputModel(),
                    new AnswerInputModel(),
                    new AnswerInputModel(),
                    new AnswerInputModel()
                }
                });
            }
        }

        public void SaveQuiz(QuizInputModel quiz, string gamePin)
        {
            var db = new SQLconnection.DatenbankZugriff();
            using var connection = db.GetConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            const string insertSql = @"
        INSERT INTO FragenKahoot (GamePin, FrageText, AntwortText, IstRichtig)
        VALUES (@GamePin, @FrageText, @AntwortText, @IstRichtig);
    ";

            using var cmd = new MySqlCommand(insertSql, connection, transaction);

            cmd.Parameters.Add("@GamePin", MySqlDbType.VarChar);
            cmd.Parameters.Add("@FrageText", MySqlDbType.Text);
            cmd.Parameters.Add("@AntwortText", MySqlDbType.Text);
            cmd.Parameters.Add("@IstRichtig", MySqlDbType.Bit);

            // Alle Fragen und Antworten durchlaufen
            foreach (var question in quiz.Questions)
            {
                foreach (var answer in question.Answers)
                {
                    cmd.Parameters["@GamePin"].Value = gamePin;
                    cmd.Parameters["@FrageText"].Value = question.Text;
                    cmd.Parameters["@AntwortText"].Value = answer.Text;
                    cmd.Parameters["@IstRichtig"].Value = answer.IsCorrect; // bool -> 0/1

                    cmd.ExecuteNonQuery();
                }
            }

            transaction.Commit();
        }
    }
}
