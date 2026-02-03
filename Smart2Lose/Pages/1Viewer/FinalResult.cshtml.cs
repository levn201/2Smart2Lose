using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using Smart2Lose.Helper;
using Smart2Lose.Model;



namespace Smart2Lose.Pages._1Viewer
{
    public class FinalResultModel : PageModel
    {


        public projektName pn = new projektName();
        public Filter f = new Filter();

        // Einschreiben der Filter 
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


        // Filter Funktion 
        [BindProperty]
        public string Filter { get; set; }
        public string DBquery { get; set; }

        // Verschiedenen Queries für den Filter 


        // Rangliste 
        public string PlaceOne { get; set; }
        public string PlaceTwo { get; set; } 
        public string PlaceThree { get; set; }


        // Start loader
        public void OnGet()
        {
            GamePin = HttpContext.Session.GetInt32("GameNumber") ?? 0;

            DBquery = f.DefaultQuery;
            loadPLayerList();
        }

        // Filter nach allen und letzten 24 Stunden
        public IActionResult OnPost()
        {
            GamePin = HttpContext.Session.GetInt32("GameNumber") ?? 0;

            if (string.IsNullOrEmpty(Filter) || Filter == "all")
            {
                DBquery = f.DefaultQuery;
            }
                
            else if (Filter == "last24h")
            {
                DBquery =  f.Last24hQuery;
            }

            else
            {
                DBquery = f.DefaultQuery;
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
                        Points = reader.GetInt32("SessionPints"), 
                        Nickname = reader.GetString("User_Nickname"),
                        GamePin = reader.GetInt32("GamePin"),
                        korrekteFagen = reader.GetInt32("CorrectAnswered"),
                        alleFragen = reader.GetInt32("PossibleAnswers"),
                        Time = reader.GetDateTime("saveTime")
                    });
                }

                // => LINQ Methoden https://csharp-hilfe.de/c-sharp-linq/ 
                var top3 = Player
                    .OrderByDescending(p => p.Points)
                    .Take(3)
                    .Select(p => p.Nickname)
                    .ToArray();

                foreach (var name in top3) 
                {
                    PlaceOne = top3[0];
                    PlaceTwo = top3[1];
                    PlaceThree = top3[2];
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Laden der Daten: {ex.Message}");
            }
        }

    }
}
