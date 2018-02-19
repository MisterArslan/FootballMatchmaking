using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FootballServer
{
    public class Server<TPlayer>
        where TPlayer : Player, new()
    {
        private readonly ConcurrentDictionary<TcpClient, TPlayer> _players =
            new ConcurrentDictionary<TcpClient, TPlayer>();

        public IEnumerable<TPlayer> Players => _players.Select(p => p.Value);

        public delegate Task Handler(TPlayer player, JObject msg, TcpClient client);

        private readonly TcpListener _listener;

        private readonly Dictionary<int, Handler> _handlers = new Dictionary<int, Handler>();

        public Server(int port) {
            _listener = TcpListener.Create(port);
            _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        }

        public Task StartListener() {
            return Task.Run(async () => {
                _listener.Start();
                while (true) {
                    var tcpClient = await _listener.AcceptTcpClientAsync();
                    Console.WriteLine("[Server] Client has connected");
                    StartHandleConnection(tcpClient);
                }
                // ReSharper disable once FunctionNeverReturns
            });
        }

        private void StartHandleConnection(TcpClient tcpClient) {
            try {
                _players[tcpClient] = null;
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }
        }

        private Task HandleConnectionAsync(TcpClient tcpClient) {
            return Task.Run(async () => {
                    var networkStream = tcpClient.GetStream();
                    try {
                        if (tcpClient.Connected) {
                            int available;
                            while ((available = tcpClient.Available) > 0) {
                                var buffer = new byte[available];
                                networkStream.Read(buffer, 0, buffer.Length);
                                var fullMsg = Encoding.UTF8.GetString(buffer);
                                while (!string.IsNullOrEmpty(fullMsg)) {
                                    var msg = ReadOneJson(ref fullMsg);
                                    var msgObj = JObject.FromObject(JsonConvert.DeserializeObject(msg));
                                    var m = msgObj.ToObject<Message<TPlayer>>();
                                    if (_players[tcpClient] == null) {
                                        _players[tcpClient] = new TPlayer {
                                            Token = m.Player.Token,
                                            Client = tcpClient
                                        };
                                    }
                                    Debug.Assert(_handlers.ContainsKey(m.Id), $"_hanlers has key {m.Id}");
                                    await _handlers[m.Id].Invoke(_players[tcpClient], msgObj, tcpClient);
                                }
                            }
                        }
                    }
                    catch (Exception e) {
                        Console.WriteLine(e.Message);
                    }
                }
            );
        }

        private static string ReadOneJson(ref string str) {
            int bracketsCount = 0;
            int i = 0;
            do {
                if (str[i] == '{') bracketsCount++;
                if (str[i] == '}') bracketsCount--;
                i++;
            } while (bracketsCount > 0);
            var res = str.Substring(0, i);
            str = i == str.Length
                ? string.Empty
                : str.Substring(i, str.Length - i);
            if (!str.Contains("{")) {
                str = string.Empty;
            }
            return res;
        }

        public void AddHandler(int id, Handler handler) {
            _handlers.Add(id, handler);
        }

        public void Send(Message<TPlayer> msg) {
            var m = JsonConvert.SerializeObject(msg);
            var buffer = Encoding.UTF8.GetBytes(m);
            try {
                msg.Player.Client.GetStream().Write(buffer, 0, buffer.Length);
            }
            catch (Exception ex) {
                Console.WriteLine(ex);
            }
        }

        public Task Tick() {
            try
            {
                var toRemove = (
                        from player in _players
                        select player.Key
                        into client
                        where client.Client.Poll(0, SelectMode.SelectRead)
                        let buff = new byte[1]
                        where client.Client.Receive(buff, SocketFlags.Peek) == 0
                        select client)
                    .ToList();
                foreach (var client in toRemove)
                {
                    if (client == null || !_players.ContainsKey(client)) continue;
                    TPlayer player;
                    _players.TryRemove(client, out player);
                    player?.OnLeave?.Invoke();
                    player?.Client?.Close();
                }
            }
            catch
            {
                //Console.WriteLine(e.ToString());
            }
            var tasks = _players
                    .Select(player => Task.Run(async () => { await HandleConnectionAsync(player.Key); }))
                    .ToList();
                return Task.WhenAll(tasks);
        }
    }
}