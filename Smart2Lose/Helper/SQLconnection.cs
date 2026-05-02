using MySql.Data.MySqlClient;

namespace Smart2Lose.Helper
{
    public class SQLconnection
    {
        public class DatenbankZugriff
        {
            public static string ConnectionString { get; set; } = string.Empty;

            public MySqlConnection GetConnection()
            {
                return new MySqlConnection(ConnectionString);
            }
        }
    }
}

