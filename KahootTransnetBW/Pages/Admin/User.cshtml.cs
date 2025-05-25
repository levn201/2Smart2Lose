using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using KahootTransnetBW.Model;
using System.Data;

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

        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        [BindProperty]
        public string Role { get; set; } // "Admin" oder "Creater"

        public string Message { get; set; }

        public IActionResult OnPost()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(Role))
            {
                Message = "Alle Felder müssen ausgefüllt sein.";
                return Page();
            }

            try
            {
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                string tableName = Role == "Admin" ? "AdminUser" : "CreaterUser";
                string query = $"INSERT INTO {tableName} (Username, Password) VALUES (@username, @password)";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@username", Username);
                cmd.Parameters.AddWithValue("@password", Password); // Achtung: später bitte Passwort-Hash!

                cmd.ExecuteNonQuery();

                Message = $"Benutzer erfolgreich als {Role} gespeichert.";
                return Page();
            }
            catch (MySqlException ex)
            {
                Message = $"MySQL-Fehler: {ex.Message}";
                return Page();
            }
            catch (Exception ex)
            {
                Message = $"Allgemeiner Fehler: {ex.Message}";
                return Page();
            }
        }
    }


}
