using FootballClient.Enums;
using Newtonsoft.Json;

namespace FootballClient.Models.Results
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