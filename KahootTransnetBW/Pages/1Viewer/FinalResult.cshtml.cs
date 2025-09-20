using KahootTransnetBW.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using static KahootTransnetBW.Pages.Admin.FragebögenModel;

namespace KahootTransnetBW.Pages._1Viewer
{
    public class FinalResultModel : PageModel
    {
        public int GamePin { get; set; }
        public List<PlayerList> Player { get; set; } = new();
        public class PlayerList
        {
            public int Points { get; set; }
            public string Nickname { get; set; }
            public int GamePin { get; set; }
            public int korrekteFagen { get; set; }
            public int alleFragen { get; set; }
            public DateTime Time { get; set; }
        }



        [BindProperty]
        public string Filter { get; set; }

        public string DBquery { get; set; }

        string DefaultQuery = @"
            SELECT SessionPints, User_Nickname, GamePin, CorrectAnswered, PossibleAnswers, saveTime
            FROM PlayerPoints
            WHERE GamePin = @GamePin
            ORDER BY SessionPints DESC;";

        string Last24hQuery = @"
            SELECT SessionPints, User_Nickname, GamePin, CorrectAnswered, PossibleAnswers, saveTime
            FROM PlayerPoints
            WHERE GamePin = @GamePin
              AND saveTime >= NOW() - INTERVAL 24 HOUR
            ORDER BY SessionPints DESC;";


        // Start loader
        public void OnGet()
        {
            GamePin = HttpContext.Session.GetInt32("GameNumber") ?? 0;

            DBquery = DefaultQuery;
            loadPLayerList();
        }

        // Filter nach allen und letzten 24 Stunden
        public IActionResult OnPost()
        {
            GamePin = HttpContext.Session.GetInt32("GameNumber") ?? 0;

            if (string.IsNullOrEmpty(Filter) || Filter == "all")
            {
                DBquery = DefaultQuery;
            }
                
            else if (Filter == "last24h")
            {
                DBquery = Last24hQuery;
            }

            else
            {
                DBquery = DefaultQuery;
            }
                

            loadPLayerList();
            return Page();
        }

        // Lädt Werte der Spieler 
        private void loadPLayerList()
        {
            Player.Clear(); 

            try
            {
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                using var cmd = new MySqlCommand(DBquery, connection);
                cmd.Parameters.AddWithValue("@GamePin", GamePin);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Player.Add(new PlayerList
                    {
                        Points = reader.GetInt32("SessionPints"), // Tippfehler bewusst so? (SessionPoints?)
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
                Console.WriteLine($"Fehler beim Laden der Daten: {ex.Message}");
            }
        }
    }
}
