using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using KahootTransnetBW.Model;
using Mysqlx.Crud;

namespace KahootTransnetBW.Pages._1Viewer
{
    public class PlaygroundModel : PageModel
    {
        // Listen Aufruf 
        public List<FragenChecknerModel> FragenChecken { get; set; } = new();


        [BindProperty(SupportsGet = true)] 
        public int GamePin { get; set; }



        public void OnGet()
        {
            LadeAlleFragen();
        }


        // Berechtet wie viele Fragen der Fragebogen hat!
        public int HowManyQuestions()
        {
            var db = new SQLconnection.DatenbankZugriff();
            using var connection = db.GetConnection();
            connection.Open();

            string query = @"SELECT COUNT(*) FROM Fragen WHERE FragebogenID = @ID;";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@ID", GamePin);

            // ExecuteScalar liefert das erste Feld der ersten Zeile des Ergebnisses
            int countNumber = Convert.ToInt32(cmd.ExecuteScalar());

            return countNumber;
        }



        // Lade die Fragen 
        private void LadeAlleFragen()
        {
            try
            {
                var db = new SQLconnection.DatenbankZugriff();
                using var connection = db.GetConnection();
                connection.Open();

                string query = @" 
                Select 
                    Fragestellung,
                    Antwort1, IstAntwort1Richtig,
                    Antwort2, IstAntwort2Richtig,
                    Antwort3, IstAntwort3Richtig,
                    Antwort4, IstAntwort4Richtig
                FROM Fragen
                WHERE FragebogenID = @ID
                ORDER BY FragebogenID
                LIMIT 1 OFFSET 1;

                ";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@ID", GamePin); 

                using var reader = cmd.ExecuteReader();     

                while (reader.Read())
                {
                    FragenChecken.Add(new FragenChecknerModel
                    {
                        DB_Fragestellung = reader.GetString("Fragestellung"),

                        DB_Antwort1 = reader.GetString("Antwort1"),
                        DB_IstAntwort1Richtig = reader.GetBoolean("IstAntwort1Richtig"),

                        DB_Antwort2 = reader.GetString("Antwort2"),
                        DB_IstAntwort2Richtig = reader.GetBoolean("IstAntwort2Richtig"),

                        DB_Antwort3 = reader.GetString("Antwort3"),
                        DB_IstAntwort3Richtig = reader.GetBoolean("IstAntwort3Richtig"),

                        DB_Antwort4 = reader.GetString("Antwort4"),
                        DB_IstAntwort4Richtig = reader.GetBoolean("IstAntwort4Richtig"),
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Laden: {ex.Message}");
            }
        }






        // Alle Propertys werden Aufgezeichnet 
        public class FragenChecknerModel
        {
            public string DB_Fragestellung { get; set; }
            public string DB_Antwort1 { get; set; }
            public bool DB_IstAntwort1Richtig { get; set; }
            public string DB_Antwort2 { get; set; }
            public bool DB_IstAntwort2Richtig { get; set; }
            public string DB_Antwort3 { get; set; }
            public bool DB_IstAntwort3Richtig { get; set; }
            public string DB_Antwort4 { get; set; }
            public bool DB_IstAntwort4Richtig { get; set; }
            public string DB_FragenError { get; set; }
        }
    }
}
