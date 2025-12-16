using MySqlX.XDevAPI;
using Smart2Lose.Model;
using Smart2Lose.Helper;
using Microsoft.Identity.Client;

namespace Smart2Lose.Helper
{
    public class HTTPloader
    {
        private readonly ISession _session;

        public HTTPloader(IHttpContextAccessor accessor)
        {
            _session = accessor.HttpContext!.Session;
        }

        public SpielDurchlauf spielDurchlauf = new SpielDurchlauf();
        public void loadSessions(SpielDurchlauf sd)
        {
            sd.GameID = _session.GetInt32("GameNumber") ?? 0;
            sd.UserName = _session.GetString("UserName") ?? string.Empty;
        }
    }
}
