using Newtonsoft.Json;

namespace FootballClient.Models.Requests
{
    class ValueRequest<TValue> : Request
    {
        [JsonProperty]
        public TValue Value { get; set; }

        // id - message type
        // player - reciever
        public ValueRequest(int id, Player player, TValue value) : base(id, player)
        {
            Value = value;
        }
    }
}
