namespace Smart2Lose.Helper
{
    public class Filter
    {

        public string FilterAuswahl {get; set; }
        






        public string DefaultQuery { get; } = @"
            SELECT SessionPints, User_Nickname, GamePin, CorrectAnswered, PossibleAnswers, saveTime
            FROM PlayerPoints
            WHERE GamePin = @GamePin
            ORDER BY SessionPints DESC;";
        public string Last24hQuery { get; } = @"
            SELECT SessionPints, User_Nickname, GamePin, CorrectAnswered, PossibleAnswers, saveTime
            FROM PlayerPoints
            WHERE GamePin = @GamePin
              AND saveTime >= NOW() - INTERVAL 24 HOUR
            ORDER BY SessionPints DESC;"; 
    }
}
