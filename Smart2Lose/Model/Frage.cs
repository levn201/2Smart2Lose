namespace Smart2Lose.Model
{
    public class Frage
    {
        public string Fragestellung { get; set; } = "";

        public string Antwort1 { get; set; } = "";
        public string Antwort2 { get; set; } = "";
        public string Antwort3 { get; set; } = "";
        public string Antwort4 { get; set; } = "";

        // 1–4 = richtige Antwort
        public int RichtigeAntwort { get; set; }
    }

}

