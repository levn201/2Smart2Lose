using MySql.Data.MySqlClient;

namespace Smart2Lose.Helper
{
    public class SQLconnection
    {


        public class DatenbankZugriff
        {
            public string connectionString =

            "Server=localhost;Database=KahootDatabase;Uid=root;Pwd=21481TNGhello!";  //Connection zur Framework Local Database von Levin 
            public MySqlConnection GetConnection()
            {
                return new MySqlConnection(connectionString);
            }

        }

    }
}

