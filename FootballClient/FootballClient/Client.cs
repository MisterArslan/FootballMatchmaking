using FootballClient.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FootballClient
{
    public class Client
    {
        protected readonly TcpClient _client;

        public delegate void Handler(JObject msg);

        protected readonly Dictionary<int, Handler> _handlers =
            new Dictionary<int, Handler>();

        public Client()
        {
            _client = new TcpClient();
        }

        public bool Tick()
        {
            try
            {
                if(_client.Client.Poll(0, SelectMode.SelectRead))
                {
                    byte[] buff = new byte[1];
                    if(_client.Client.Receive(buff, SocketFlags.Peek) == 0)
                    {
                        return false;
                    }
                }
                var networkStream = _client.GetStream();
                int available;
                while ((available = _client.Available) > 0)
                {
                    var buffer = new byte[available];
                    networkStream.Read(buffer, 0, buffer.Length);
                    var fullMsg = Encoding.UTF8.GetString(buffer);
                    while (!string.IsNullOrEmpty(fullMsg))
                    {
                        var msg = ReadOneJson(ref fullMsg);
                        var obj = JsonConvert.DeserializeObject(msg);
                        var msgObj = JObject.FromObject(obj);
                        var m = msgObj.ToObject<Message>();
                        _handlers[m.Id].Invoke(msgObj);
                    }
                    
                }
                return true;
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }

        public virtual void Connect(IPAddress ip, int port)
        {
            _client.Client.Connect(new IPEndPoint(ip, port));
        }

        public void AddHandler(int id, Handler handler)
        {
            _handlers.Add(id, handler);
        }

        public void Send(Message msg)
        {
            var m = JsonConvert.SerializeObject(msg);
            var buffer = Encoding.UTF8.GetBytes(m);
            _client.GetStream().Write(buffer, 0, buffer.Length);
        }

        private static string ReadOneJson(ref string str)
        {
            str.Trim('\n');
            int bracketsCount = 0;
            int i = 0;
            do
            {
                if (str[i] == '{') bracketsCount++;
                if (str[i] == '}') bracketsCount--;
                i++;
            } while (bracketsCount > 0);
            var res = str.Substring(0, i);
            str = i == str.Length
                ? string.Empty
                : str.Substring(i, str.Length - i);
            if (!str.Contains("{"))
            {
                str = string.Empty;
            }
            return res;
        }

        ~Client()
        {
            _client.Client.Disconnect(true);
            _client.Close();
            _handlers.Clear();
        }
        public static Action<string> DebugLog;
    }
}