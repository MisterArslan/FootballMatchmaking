namespace FootballClient.Models
{
    public class Message
    {
        public readonly int Id;
        public Player Player { get; set; }

        public Message(int id, Player player) {
            Id = id;
            Player = player;
        }
    }
}