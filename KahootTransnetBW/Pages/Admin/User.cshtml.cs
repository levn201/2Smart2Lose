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
            GetAllUsers();
        }

        public List<User> UserList { get; set; } = new();

        public string Message { get; set; } = string.Empty;

        public projektName pn = new projektName(); // Projektname Klasse

        public User U = new User();


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


        //Löschen von einem User 
        public IActionResult OnPostLoeschen(int id)
        {
            try
            {
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                string query = "DELETE FROM dasboarduser WHERE ID_User = @id;";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();

                GetAllUsers();  // Liste neu laden

                return RedirectToPage();  // Seite neu laden
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Löschen: {ex.Message}");
                ModelState.AddModelError("", "Löschen fehlgeschlagen.");
                return Page();  // Seite mit Fehler neu rendern
            }
        }


        // Einschreiben neuer User 
        public IActionResult OnPost()
        {
            if (string.IsNullOrWhiteSpace(U.Username) || string.IsNullOrWhiteSpace(U.Password) || string.IsNullOrWhiteSpace(U.Role))
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
                cmd.Parameters.AddWithValue("@username", U.Username);
                cmd.Parameters.AddWithValue("@password", U.Password);
                cmd.Parameters.AddWithValue("@role", U.Role);

                cmd.ExecuteNonQuery();

                GetAllUsers();

                Message = $"Benutzer erfolgreich als {U.Role} gespeichert.";
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
