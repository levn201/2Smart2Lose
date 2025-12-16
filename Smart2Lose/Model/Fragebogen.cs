namespace Smart2Lose.Model
{
    public class Fragebogen //FragebogenViewModel
    {
        public int JoinId { get; set; }
        public string Titel { get; set; } = "";
        public string Autor { get; set; } = string.Empty;
        public string Kategorie { get; set; } = string.Empty;
        public DateTime ErstelltAm { get; set; }
    }
}
