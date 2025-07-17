using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using System.Data.SqlClient;

namespace KahootTransnetBW.Model
{
    public class SQLconnection
    {


        public class DatenbankZugriff
        {
            public string connectionString =
            //"Server=mysql-347a283a-levn-5ab8.b.aivencloud.com;" +
            //"Port=24098;" +
            //"Database=KahootDatabase;" +
            //"User Id=avnadmin;" +
            //"Password=AVNS_tNLe4WVjQEonX1fIAb0;" +
            //"SslMode=Required;";

            "Server=localhost;Database=KahootDatabase;Uid=root;Pwd=21481TNGhello!";  //Connection zur Framework Local Database von Levin 
            public MySqlConnection GetConnection()
            {
                return new MySqlConnection(connectionString);
            }

        }

    }
}

