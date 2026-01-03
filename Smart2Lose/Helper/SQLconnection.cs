using MySql.Data.MySqlClient;

namespace Smart2Lose.Helper
{
    public class SQLconnection
    {


        public class DatenbankZugriff
        {
            public string connectionString =

            /*"Server=localhost;Database=KahootDatabase;Uid=root;Pwd=21481TNGhello!";*/  //Connection zur Framework Local Database von Levin 
            "Server=192.168.200.30;Port=3306;Database=KahootDatabase;Uid=root;Pwd=TNBWazubi1!";

            public MySqlConnection GetConnection()
            {
                return new MySqlConnection(connectionString);
            }

        }

    }
}

