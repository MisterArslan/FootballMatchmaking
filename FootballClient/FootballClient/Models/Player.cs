namespace FootballClient.Models
{
    public class Player
    {
        public string Token { get; set; }

        public Player(string token)
        {
            Token = token;
        }
    }
}