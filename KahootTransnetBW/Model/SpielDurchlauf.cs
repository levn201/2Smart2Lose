using MySql.Data.MySqlClient;


namespace KahootTransnetBW.Model
{
    public class SpielDurchlauf
    {
        // ZUGANG AUF DIE GAMES 
        public int GameID { get; set; }
        public string UserName { get; set; } = string.Empty;


        public int HowManyQuestions()
        {
            var db = new SQLconnection.DatenbankZugriff();
            using var connection = db.GetConnection();
            connection.Open();

            string query = @"SELECT COUNT(*) FROM Fragen WHERE FragebogenID = @ID;";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@ID", GameID);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

    }

}
