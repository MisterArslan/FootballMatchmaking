namespace FootballServer.Models
{
    class Invite
    {
        public readonly string Token;
        public readonly Player Sender;
        public readonly Player Receiver;
        public readonly System.DateTime CreateTime;

        public Invite(Player sender, Player receiver)
        {
            Token = Models.Token.Generate();
            Sender = sender;
            Receiver = receiver;
            CreateTime = System.DateTime.Now;
        }
    }
}