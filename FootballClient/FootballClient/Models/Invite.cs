namespace FootballClient.Models
{
    public class Invite
    {
        public readonly string Token;
        public readonly Player Sender;
        public readonly Player Receiver;
        public readonly System.DateTime CreateTime;

        public Invite(string token, Player sender, Player receiver)
        {
            Token = token;
            Sender = sender;
            Receiver = receiver;
            CreateTime = System.DateTime.Now;
        }
    }
}