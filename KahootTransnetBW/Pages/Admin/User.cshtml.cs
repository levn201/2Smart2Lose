using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using KahootTransnetBW.Model;
using System.Data;
using KahootTransnetBW.Data;

namespace KahootTransnetBW.Pages.Admin
{
    public class UserModel : PageModel
    {
        public void OnGet()
        {
            GetAllUsers();
        }


        // Admin User Klasse
        public class User
        {
            public int ID { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string Role { get; set; }
        }
        public List<User> UserList { get; set; } = new();


        // Auslesen aller User 
        public void GetAllUsers()
        {
            try
            {
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                string query = "SELECT * FROM DasboardUser;";
                using var cmd = new MySqlCommand(query, connection);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    UserList.Add(new User
                    {
                        ID = reader.GetInt32("ID_User"),
                        Username = reader.GetString("Username"),
                        Password = reader.GetString("Password"),
                        Role = reader.GetString("Role")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Abrufen der Benutzer: {ex.Message}");
            }
        }


        // User Anlegen
        [BindProperty]
        public string Username { get; set; }
        [BindProperty]
        public string Password { get; set; }
        [BindProperty]
        public string Role { get; set; }
        public string Message { get; set; }


        // Einschreiben neuer User 
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

                string query = "INSERT INTO DasboardUser (Username, Password, Role) VALUES (@username, @password, @role);";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@username", Username);
                cmd.Parameters.AddWithValue("@password", Password); // Später bitte Passwort-Hash!
                cmd.Parameters.AddWithValue("@role", Role);

                cmd.ExecuteNonQuery();

                GetAllUsers();

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
