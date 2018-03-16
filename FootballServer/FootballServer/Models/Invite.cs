using FootballServer.Enums;
using Newtonsoft.Json;

namespace FootballServer.Models
{
    class Invite
    {
        [JsonProperty]
        public readonly string Token;
        [JsonProperty]
        public readonly Player Sender;
        [JsonProperty]
        public readonly Player Receiver;

        public Invite(Player sender, Player receiver)
        {
            Token = Models.Token.Generate();
            Sender = sender;
            Receiver = receiver;
        }
    }
}