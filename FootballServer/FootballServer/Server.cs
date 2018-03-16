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
using FootballServer.Models;

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

        private readonly Dictionary<int, Handler> _handlers
            = new Dictionary<int, Handler>();

        public Server(int port) {
            _listener = TcpListener.Create(port);
            _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        }

        public Task StartListener() {
            return Task.Run(async () => {
                _listener.Start();
                while (true) {
                    var tcpClient = await _listener.AcceptTcpClientAsync();
                    Program.Log.Info("[Server] Client has connected");
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
                Program.Log.Error(ex);
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
                        Program.Log.Error(e.Message);
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
                Program.Log.Error(ex);
            }
        }

        public Task Tick()
        {
            var toRemove = (
                    from player in _players
                    select player.Key
                    into client
                    where !IsConnected(client)
                    select client)
                .ToList();
            foreach (var client in toRemove)
            {
                if (client == null
                    || !_players.ContainsKey(client)
                    || _players[client] == null) continue;
                _players.TryRemove(client, out TPlayer player);
                if (player.OnLeave == null) continue;
                player?.OnLeave?.Invoke(player);
                player?.Client?.Close();
            }
            var tasks = _players
                .Select(player => Task.Run(async () => { await HandleConnectionAsync(player.Key); }))
                .ToList();
            return Task.WhenAll(tasks);
        }

        private bool IsConnected(TcpClient _tcpClient)
        {
            try
            {
                if (_tcpClient != null && _tcpClient.Client != null && _tcpClient.Client.Connected)
                {
                    /* pear to the documentation on Poll:
                     * When passing SelectMode.SelectRead as a parameter to the Poll method it will return 
                     * -either- true if Socket.Listen(Int32) has been called and a connection is pending;
                     * -or- true if data is available for reading; 
                     * -or- true if the connection has been closed, reset, or terminated; 
                     * otherwise, returns false
                     */

                    // Detect if client disconnected
                    if (_tcpClient.Client.Poll(0, SelectMode.SelectRead))
                    {
                        byte[] buff = new byte[1];
                        if (_tcpClient.Client.Receive(buff, SocketFlags.Peek) == 0)
                        {
                            // Client disconnected
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}