using MySql.Data.MySqlClient;

namespace Smart2Lose.Helper
{
    public class AdminHelper
    {

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
    }
}
