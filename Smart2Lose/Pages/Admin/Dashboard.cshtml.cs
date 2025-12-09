using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static Smart2Lose.Model.SQLconnection;
using Smart2Lose.Model;

namespace Smart2Lose.Pages.Admin
{
    public class DashboardModel : PageModel
    {

        public string StatusMessage { get; set; }
        public void OnGet()
        {
            StatusMessage = "Bereit zum Testen.";
        }

        public projektName pn = new projektName();




        // Auslesen 
        public void OnPostTestVerbindung()
        {
            //Verbindung zur Datenbank 
            var db = new DatenbankZugriff();
            using var connection = db.GetConnection();
            try
            {
                connection.Open();
                StatusMessage = "Verbindung erfolgreich!";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler bei Verbindung: " + ex.Message;
            }
        }

        // Einschreiben
        public void OnPostEintragHinzufuegen()
        {
            var db = new DatenbankZugriff(); // ?? Instanz erzeugen
            using var connection = db.GetConnection();
            try
            {
                connection.Open();

                string insertQuery = "INSERT INTO test (Name, ErstelltAm) VALUES (@name, @datum)";
                using var command = new MySqlCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@name", "Neuer Eintrag");
                command.Parameters.AddWithValue("@datum", DateTime.Now);

                int rows = command.ExecuteNonQuery();
                StatusMessage = $"{rows} Eintrag eingefügt.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Einfügen: " + ex.Message;
            }
        }

        // Logs 
        public void OnPostEintraegeAnzeigen()
        {
            var db = new DatenbankZugriff(); // ?? Instanz erzeugen
            using var connection = db.GetConnection();
            try
            {
                connection.Open();

                string selectQuery = "SELECT Id, Name, ErstelltAm FROM test";
                using var command = new MySqlCommand(selectQuery, connection);
                using var reader = command.ExecuteReader();

                StatusMessage = "Einträge:\n";
                while (reader.Read())
                {
                    int id = reader.GetInt32("Id");
                    string name = reader.GetString("Name");
                    DateTime erstelltAm = reader.GetDateTime("ErstelltAm");

                    StatusMessage += $"ID: {id}, Name: {name}, Datum: {erstelltAm}\n";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Fehler beim Lesen: " + ex.Message;
            }
        }
    }
}
