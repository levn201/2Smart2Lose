using MySql.Data.MySqlClient;
using Smart2Lose.Model;

namespace Smart2Lose.Helper
{
    public class Spiel
    {
        public int HowManyQuestions(int gameId)
        {
            var db = new SQLconnection.DatenbankZugriff();
            using var connection = db.GetConnection();
            connection.Open();

            string query = @"SELECT COUNT(*) FROM Fragen WHERE FragebogenID = @ID;";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@ID", gameId);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }
}
