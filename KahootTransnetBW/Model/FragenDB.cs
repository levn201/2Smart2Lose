namespace KahootTransnetBW.Model
{
    public class FragenDB
    {
        public string DB_Fragestellung { get; set; } = string.Empty;
        public string DB_Antwort1 { get; set; } = string.Empty;
        public bool DB_IstAntwort1Richtig { get; set; }
        public string DB_Antwort2 { get; set; } = string.Empty;
        public bool DB_IstAntwort2Richtig { get; set; }
        public string DB_Antwort3 { get; set; } = string.Empty;
        public bool DB_IstAntwort3Richtig { get; set; }
        public string DB_Antwort4 { get; set; } = string.Empty;
        public bool DB_IstAntwort4Richtig { get; set; }
    }

    public class  Fragen
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

    public class FragenInput
    {
        public bool C_IstAntwort1Richtig { get; set; }
        public bool C_IstAntwort2Richtig { get; set; }
        public bool C_IstAntwort3Richtig { get; set; }
        public bool C_IstAntwort4Richtig { get; set; }
    }

}
