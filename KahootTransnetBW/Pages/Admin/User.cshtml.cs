using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using KahootTransnetBW.Model;

namespace KahootTransnetBW.Pages.Admin
{
    public class UserModel : PageModel
    {

        public class AdminUser
        {
            public int ID { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
        }


        public List<AdminUser> UserList { get; set; } = new();

        public void OnGet()
        {
            try
            {
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                string query = "select * from adminuser;";
                using var cmd = new MySqlCommand(query, connection);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    UserList.Add(new AdminUser
                    {
                        ID = reader.GetInt32("User_ID"),
                        Username = reader.GetString("Username"),
                        Password = reader.GetString("password")
                    });
                }
            }
            catch (Exception ex)
            {
                // Optional: Fehlerbehandlung
            }
        }
    }
}
