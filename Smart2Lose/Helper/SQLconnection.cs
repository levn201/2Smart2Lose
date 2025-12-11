using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using System.Data.SqlClient;

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

