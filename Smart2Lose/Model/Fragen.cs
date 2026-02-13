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
    }
}
