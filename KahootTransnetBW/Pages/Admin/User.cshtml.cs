using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using KahootTransnetBW.Model;
using System.Data;

namespace KahootTransnetBW.Pages.Admin
{
    public class UserModel : PageModel
    {
        public void OnGet()
        {
            GetAdminUser();
            GetCreaterUser();
        }

        // Admin User Klasse
        public class AdminUser
        {
            public int ID { get; set; }
            public string Username { get; set; }
            public string Password { get; set; } // Optional: weglassen, wenn nicht gebraucht
        }

        // Creator User Klasse
        public class CreateUser
        {
            public int ID { get; set; }
            public string Username { get; set; }
            public string Password { get; set; } // Optional: weglassen, wenn nicht gebraucht
        }

        // Listen
        public List<AdminUser> UserList { get; set; } = new();
        public List<CreateUser> CreaterList { get; set; } = new();

        // Admin User abrufen
        public void GetAdminUser()
        {
            try
            {
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                string query = "SELECT * FROM AdminUser;";
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
                Console.WriteLine($"Fehler beim Abrufen der Admin-User: {ex.Message}");
            }
        }

        // Creator User abrufen
        public void GetCreaterUser()
        {
            try
            {
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                string query = "SELECT * FROM CreaterUser;";
                using var cmd = new MySqlCommand(query, connection);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    CreaterList.Add(new CreateUser
                    {
                        ID = reader.GetInt32("Creater_ID"),
                        Username = reader.GetString("Username"),
                        Password = reader.GetString("password")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Abrufen der Creator-User: {ex.Message}");
            }
        }





        // User Anlegen
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
