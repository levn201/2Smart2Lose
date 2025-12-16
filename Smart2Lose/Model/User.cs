using System.Data;

namespace Smart2Lose.Model
{
    public class User
    {
        public int ID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; 
        public string Role { get; set; } = string.Empty;

        public User()
        {
        }

        public User(string username,string role) {
            Username = username;    
            Role = role;
        }   
    }   
}
