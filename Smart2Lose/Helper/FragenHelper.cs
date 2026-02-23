using System.Diagnostics.Metrics;
using System.Media;
using MySql.Data.MySqlClient;

namespace Smart2Lose.Helper
{
    public class FragenHelper
    {

        public int countResults(int id, int counter)
        {
            counter = 0; // WICHTIG: Zurücksetzen vor dem Zählen

            var db = new SQLconnection.DatenbankZugriff();
            using var connection = db.GetConnection();
            connection.Open();

            string query = @"SELECT COUNT(*) FROM PlayerPoints WHERE GamePin = @ID;";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@ID", id);

            counter = Convert.ToInt32(cmd.ExecuteScalar());

            return counter;
        }



        public string activeUser { get; set; }
        public bool CheckIfPlayerIsAutor(int id)
        {
            var db = new SQLconnection.DatenbankZugriff();
            using var connection = db.GetConnection();
            connection.Open();

            string query = @"SELECT Autor FROM fragebogen WHERE Join_ID = @ID;";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@ID", id);

            var result = cmd.ExecuteScalar();

            if (result == null)
                return false;

            string autorFromDb = result.ToString();

            return autorFromDb == activeUser;
        }
    }
}
