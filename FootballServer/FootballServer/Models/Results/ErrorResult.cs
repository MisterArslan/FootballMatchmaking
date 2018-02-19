using FootballServer.Enums;
using Newtonsoft.Json;

namespace FootballServer.Models.Results
{
    class ErrorResult : Result
    {
        [JsonProperty]
        public readonly string ErrorMessage;

        public ErrorResult(string errorMessage, Player player) : base((int)MessageType.ERROR, player)
        {
            ErrorMessage = errorMessage;
        }
    }
}