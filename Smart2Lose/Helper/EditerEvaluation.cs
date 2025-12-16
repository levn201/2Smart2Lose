

using Azure.Identity;
using Smart2Lose.Model;

namespace Smart2Lose.Helper
{
    public class EditerEvaluation
    {
        public static void SetEditor(HttpContext context, string username, string role)
        {
            context.Session.SetString("EditerName", username);
            context.Session.SetString("createrName", role);
        }

        public static User GetEditor(HttpContext context)          
        {
            string? userName = context.Session.GetString("EditerName");
            string? role = context.Session.GetString("createrName");

            User user = new(userName, role);

            return user;
        }
    }
}
