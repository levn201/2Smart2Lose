namespace Smart2Lose.Model
{
    public class Fragen
    {
        public string Fragestellung { get; set; } = string.Empty;
        public string Antwort1 { get; set; } = string.Empty;
        public bool IstAntwort1Richtig { get; set; }
        public string Antwort2 { get; set; } = string.Empty;
        public bool IstAntwort2Richtig { get; set; }
        public string Antwort3 { get; set; } = string.Empty;
        public bool IstAntwort3Richtig { get; set; }
        public string Antwort4 { get; set; } = string.Empty;
        public bool IstAntwort4Richtig { get; set; }


        public static Fragen FromReader(MySql.Data.MySqlClient.MySqlDataReader reader)
        {
            return new Fragen
            {
                Fragestellung = reader.GetString("Fragestellung"),
                Antwort1 = reader.GetString("Antwort1"),
                IstAntwort1Richtig = reader.GetBoolean("IstAntwort1Richtig"),
                Antwort2 = reader.GetString("Antwort2"),
                IstAntwort2Richtig = reader.GetBoolean("IstAntwort2Richtig"),
                Antwort3 = reader.GetString("Antwort3"),
                IstAntwort3Richtig = reader.GetBoolean("IstAntwort3Richtig"),
                Antwort4 = reader.GetString("Antwort4"),
                IstAntwort4Richtig = reader.GetBoolean("IstAntwort4Richtig")
            };
        }

    }


}
