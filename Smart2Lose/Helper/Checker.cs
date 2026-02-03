using Microsoft.AspNetCore.Components.Web;

namespace Smart2Lose.Helper
{
    public class Checker
    {
        public void OnlyOneAnswer(bool aw1, bool aw2, bool aw3, bool aw4)
        {
            if ((aw1 && aw2) || (aw1 && aw3) || (aw1 && aw4) || (aw2 && aw3) || (aw2 && aw4) || (aw3 && aw4))
            {
                throw new ArgumentException("Only one answer can be true.");
            }
        }
    }
}
