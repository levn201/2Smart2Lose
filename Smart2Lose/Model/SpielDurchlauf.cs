using MySql.Data.MySqlClient;
using Smart2Lose.Helper;


namespace Smart2Lose.Model
{
    public class SpielDurchlauf
    {
        // ZUGANG AUF DIE GAMES 
        public int GameID { get; set; }
        public string UserName { get; set; } = string.Empty;
    }


}
