using Newtonsoft.Json;
using System;

namespace FootballServer
{
    class Token
    {
        [JsonProperty]
        private string _token;

        public static Token Generate()
        {
            return new Token { _token = Guid.NewGuid().ToString() };
        }

        public override string ToString()
        {
            return _token.ToString();
        }

        public override bool Equals(object obj)
        {
            return _token == ((Token)obj)._token;
        }

        public override int GetHashCode()
        {
            return _token.GetHashCode();
        }
    }
}
