using Newtonsoft.Json;

namespace FootballServer.Models.Results
{
    class ValueResult<TValue> : Result
    {
        [JsonProperty]
        public TValue Value { get; set; }

        // id - message type
        // player - reciever
        public ValueResult(int id, Player player, TValue value) : base(id, player)
        {
            Value = value;
        }
    }
}