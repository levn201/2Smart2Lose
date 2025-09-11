using KahootTransnetBW.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using static KahootTransnetBW.Pages.Admin.FragebögenModel;


namespace KahootTransnetBW.Pages._1Viewer
{
    public class FinalResultModel : PageModel
    {
        public void OnGet()
        {
            GamePin = HttpContext.Session.GetInt32("GameNumber") ?? 0;
            loadPLayerList(); 
        }


        public int GamePin { get; set; }

        

        public List<PlayerList> Player { get; set; } = new();
        public class PlayerList
        {
            public int Points { get; set; }
            public string Nickname { get; set; }
            public int GamePin { get; set; }
            public int korrekteFagen { get; set; }
            public int alleFragen {  get; set; }
            public DateTime Time { get; set; }
        }

        // Lädt Alle Werte der Spieler 
        public void loadPLayerList()
        {
            try
            {
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                string query = @"SELECT SessionPints, User_Nickname, GamePin, CorrectAnswered, PossibleAnswers, saveTime 
                         FROM PlayerPoints 
                         WHERE GamePin = @GamePin 
                         ORDER BY SessionPints DESC;";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@GamePin", GamePin); // <-- hier dein Property verwenden!

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Player.Add(new PlayerList
                    {
                        Points = reader.GetInt32("SessionPints"),
                        Nickname = reader.GetString("User_Nickname"),
                        GamePin = reader.GetInt32("GamePin"),
                        korrekteFagen = reader.GetInt32("CorrectAnswered"),
                        alleFragen = reader.GetInt32("PossibleAnswers"),
                        Time = reader.GetDateTime("saveTime")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Laden der Fragebögen: {ex.Message}");
            }
        
        }
    }
}
