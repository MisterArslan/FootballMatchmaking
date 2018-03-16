using Newtonsoft.Json;
using System;

namespace FootballServer.Models
{
    public static class Token
    {
        public static string Generate()
        {
            return Guid.NewGuid().ToString();
        }
    }
}