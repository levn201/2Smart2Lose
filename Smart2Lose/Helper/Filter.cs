namespace Smart2Lose.Helper
{
    public class Filter
    {
        public string DefaultQuery { get; } = @"
            SELECT SessionPints, User_Nickname, GamePin, CorrectAnswered, PossibleAnswers, saveTime
            FROM PlayerPoints
            WHERE GamePin = @GamePin
            ORDER BY CorrectAnswered DESC;";

        public string Last24hQuery { get; } = @"
            SELECT SessionPints, User_Nickname, GamePin, CorrectAnswered, PossibleAnswers, saveTime
            FROM PlayerPoints
            WHERE GamePin = @GamePin
              AND saveTime >= NOW() - INTERVAL 24 HOUR
            ORDER BY CorrectAnswered DESC;";
    }
}
