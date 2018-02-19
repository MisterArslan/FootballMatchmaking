namespace FootballServer
{
    public class Message<TPlayer>
        where TPlayer : Player
    {
        public readonly int Id;
        public TPlayer Player { get; set; }

        public Message(int id, TPlayer player) {
            Id = id;
            Player = player;
        }
    }
}