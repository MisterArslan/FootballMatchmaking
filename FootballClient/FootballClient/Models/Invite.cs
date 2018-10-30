namespace FootballClient.Models

{
    public class Invite
    {
        public readonly string token {get;set;};
        public readonly Player sender {get;set;};
        public readonly Player receiver {get;set;};
        public readonly System.DateTime createTime {get;set;} = default(System.DateTime.Now);

        public Invite(string token, Player sender, Player receiver)
        {
            this.token = token;
            this.sender = sender;
            this.receiver = receiver;
        }
    }
}
