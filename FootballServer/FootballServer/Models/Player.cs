using System;
using System.Net.Sockets;
using Newtonsoft.Json;

namespace FootballServer.Models
{
    public class Player
    {
        public string Token { get; set; }
        internal TcpClient Client { get; set; }
        [JsonIgnore]    
        public Action<Player> OnLeave;
    }
}