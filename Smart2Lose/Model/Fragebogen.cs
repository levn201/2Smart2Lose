using Microsoft.AspNetCore.Mvc;

namespace Smart2Lose.Model
{
    public class Fragebogen //FragebogenViewModel
    {
        public int JoinId { get; set; }
        public string Titel { get; set; } = "";
        public string Autor { get; set; } = "Keins";
        public string Kategorie { get; set; } = "Unbekannt";
        public DateTime ErstelltAm { get; set; }

        public List<Fragen> Fragen { get; set; } = new();
    }
}
