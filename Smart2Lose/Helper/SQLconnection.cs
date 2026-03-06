using MySql.Data.MySqlClient;
using Smart2Lose.Pages._1Viewer;

namespace Smart2Lose.Helper
{
    public class SQLconnection
    {


        public class DatenbankZugriff
        {
            public string connectionString =

            "Server=localhost;Database=KahootDatabase;Uid=root;Pwd=21481TNGhello!"; //Connection zur Framework Local Database von Levin 
            //"Server=192.168.200.30;Port=3306;Database=kahootdatabase;Uid=Smart2Lose;Pwd=TNBWazubi1!;SslMode=None;";

            public MySqlConnection GetConnection()
            {
                return new MySqlConnection(connectionString);
            }


            public MySqlCommand CreateCommand(string sql)
            {
                var con = GetConnection();
                try
                {
                    con.Open();
                    return new MySqlCommand(sql, con);
                }
                catch (Exception)
                {
                    con.Close();
                    throw;
                }
                finally
                {
                    if(con.State == System.Data.ConnectionState.Open) 
                    {
                        con.Close(); 
                    }
                    con.Dispose();
                }

            }

        }

    }
}

