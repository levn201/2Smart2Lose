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

        public static Fragebogen FromReader(MySql.Data.MySqlClient.MySqlDataReader reader)
        {
            return new Fragebogen
            {
                JoinId = reader.GetInt32("Join_ID"),
                Titel = reader.GetString("Titel"),
                Autor = reader.GetString("Autor"),
                Kategorie = reader.GetString("Kategorie"),
                ErstelltAm = reader.GetDateTime("ErstelltAm")
            };
        }
    }
}
