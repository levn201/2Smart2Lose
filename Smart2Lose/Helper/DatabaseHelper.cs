using MySql.Data.MySqlClient;
using Smart2Lose.Model;

namespace Smart2Lose.Helper
{
    public class DatabaseHelper
    {
        // Verbindung zur Datenbank herstellen
        private MySqlConnection OpenConnection()
        {
            var db = new SQLconnection.DatenbankZugriff();
            var connection = db.GetConnection();
            connection.Open();
            return connection;
        }

        // Für SELECT - du gibst SQL, Parameter und die While-Logik mit
        public void HoleDaten(string sql, Dictionary<string, object> parameter, Action<MySqlDataReader> zeilenVerarbeiten)
        {
            using var connection = OpenConnection();
            using var cmd = new MySqlCommand(sql, connection);

            // Parameter hinzufügen falls vorhanden z.B. WHERE ID = @ID
            if (parameter != null)
                foreach (var p in parameter)
                    cmd.Parameters.AddWithValue(p.Key, p.Value);

            using var reader = cmd.ExecuteReader();

            // Für jede Zeile die zurückkommt, wird deine Logik ausgeführt
            while (reader.Read())
                zeilenVerarbeiten(reader);
        }

        // Für INSERT / UPDATE / DELETE
        public void Ausfuehren(string sql, Dictionary<string, object> parameter = null)
        {
            using var connection = OpenConnection();
            using var cmd = new MySqlCommand(sql, connection);

            if (parameter != null)
                foreach (var p in parameter)
                    cmd.Parameters.AddWithValue(p.Key, p.Value);

            cmd.ExecuteNonQuery();
        }
    }
}
