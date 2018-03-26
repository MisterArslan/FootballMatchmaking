using FootballServer.Enums;
using FootballServer.Models;
using FootballServer.Models.Requests;
using FootballServer.Models.Results;
using log4net;
using log4net.Config;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace FootballServer
{
    class Program
    {
        public static readonly ILog Log = LogManager.GetLogger("LOGGER");
        private static ConcurrentDictionary<string, Invite> _invites =
            new ConcurrentDictionary<string, Invite>();

        static void Main(string[] args)
        {
            var server = new Server<Player>(26000);
            XmlConfigurator.Configure();

            server.AddHandler((int)MessageType.CONNECT,
                (player, msg, client) => Task.Run(() =>
                {
                   //For getting info about client
                }));
            server.AddHandler((int)MessageType.CREATE_INVITE,
                (player, msg, client) => Task.Run(() =>
                {
                    var request = msg.ToObject<ValueRequest<string>>();
                    player.Token = request.Player.Token;
                    try
                    {
                        if (server.Players.Any(x =>  x != null && x.Token == request.Value))
                        {
                            var receiver = (from member in server.Players
                                            where member.Token == request.Value
                                            select member).First();
                            var invite = new Invite(player, receiver);
                            _invites.TryAdd(invite.Token, invite);
                            server.Send(new ValueResult<Invite>
                                ((int)MessageType.CREATE_INVITE, player, invite));
                            server.Send(new ValueResult<Invite>
                                ((int)MessageType.RECEIVE_INVITE, receiver, invite));
                            Log.Info("[Server] Invite from " +
                            player.Token + " to " + request.Value + " created");
                        }
                        else
                        {
                            Log.Error("Receiver doesn't exist");
                            server.Send(new ValueResult<string>
                                ((int)MessageType.ERROR, player, "Receiver doesn't exist"));
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                }));
            server.AddHandler((int)MessageType.ACCEPT_INVITE,
                (player, msg, client) => Task.Run(() =>
                {
                    var request = msg.ToObject<ValueRequest<string>>();
                    player.Token = request.Player.Token;
                    try
                    {
                        if (_invites.ContainsKey(request.Value))
                        {
                            var invite = _invites[request.Value];
                            var token = Token.Generate();
                            server.Send(new ValueResult<string>
                                ((int)MessageType.ACCEPT_INVITE, invite.Receiver, token));
                            server.Send(new ValueResult<string>
                                ((int)MessageType.ACCEPT_INVITE, invite.Sender, token));
                            _invites.TryRemove(request.Value, out invite);
                            Log.Info("[Server] Invite from " +
                                player.Token + " to " + request.Value + " accepted");
                        }
                        else
                        {
                            Log.Error("Invite doesn't exist");
                            server.Send(new ValueResult<string>
                                ((int)MessageType.ERROR, player, "Invite doesn't exist"));
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                }));
            server.AddHandler((int)MessageType.DECLINE_INVITE,
                (player, msg, client) => Task.Run(() =>
                {
                    var request = msg.ToObject<ValueRequest<string>>();
                    player.Token = request.Player.Token;
                    try
                    {
                        if (_invites.ContainsKey(request.Value))
                        {
                            var invite = _invites[request.Value];
                            server.Send(new Result((int)MessageType.DECLINE_INVITE, invite.Sender));
                            _invites.TryRemove(request.Value, out invite);
                            Log.Info("[Server] Invite from " +
                                player.Token + " to " + request.Value + " declined");
                        }
                        else
                        {
                            Log.Error("Invite doesn't exist");
                            server.Send(new ValueResult<string>
                                ((int)MessageType.ERROR, player, "Invite doesn't exist"));
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                }));
            server.AddHandler((int)MessageType.CANCEL_INVITE,
                (player, msg, client) => Task.Run(() =>
                {
                    var request = msg.ToObject<ValueRequest<string>>();
                    player.Token = request.Player.Token;
                    try
                    {
                        if (_invites.ContainsKey(request.Value))
                        {
                            var invite = _invites[request.Value];
                            CancelInvite(ref server, invite);
                            Log.Info("[Server] Invite from " +
                                player.Token + " to " + request.Value + " canceled");
                        }
                        else
                        {
                            Log.Error("Invite doesn't exist");
                            server.Send(new ValueResult<string>
                                ((int)MessageType.ERROR, player, "Invite doesn't exist"));
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                }));
            server.AddHandler((int)MessageType.DISCONNECT,
                (player, msg, client) => Task.Run(() =>
                {
                    var request = msg.ToObject<Request>();
                    player.Token = request.Player.Token;
                    try
                    {
                        if (_invites.Any(x => x.Value.Receiver.Token == player.Token 
                            || x.Value.Sender.Token == player.Token))
                        {
                            var invite = _invites.First(x => x.Value.Receiver.Token == player.Token
                                || x.Value.Sender.Token == player.Token).Value;
                            CancelInvite(ref server, invite);
                            Log.Info("[Server] Invite from " + invite.Sender.Token +
                                " to " + invite.Receiver.Token + " deleted");
                        }
                        else
                        {
                            Log.Error("Invite doesn't exist");
                            server.Send(new ValueResult<string>
                                ((int)MessageType.ERROR, player, "Invite doesn't exist"));
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                }));

            Log.Info("[Server] Started");
            //Handling connections
            Task.Run(() => 
            {
                while (true)
                {
                    server.Tick();
                    System.Threading.Thread.Sleep(100);
                }             
            });
            //Refreshing invite list
            Task.Run(() => 
            {
                while (true)
                {
                    foreach (var invite in _invites.Values)
                    {
                        if (DateTime.Now - invite.CreateTime 
                            >= TimeSpan.FromSeconds(60))
                        {
                            try
                            {
                                CancelInvite(ref server, invite);
                                Log.Info("[Server] Invite from " + invite.Sender.Token +
                                    " to " + invite.Receiver.Token + " deleted");
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex);
                            }
                        }
                    }
                    System.Threading.Thread.Sleep(1000);
                }
            });
            server.StartListener().Wait();
        }

        private static void CancelInvite(ref Server<Player> server, Invite invite)
        {
            if (IsConnected(invite.Receiver.Client))
                server.Send(new Result((int)MessageType.CANCEL_INVITE, invite.Receiver));
            if (IsConnected(invite.Sender.Client))
                server.Send(new Result((int)MessageType.CANCEL_INVITE, invite.Sender));
            _invites.TryRemove(invite.Token, out Invite inv);
        }

        private static bool IsConnected(TcpClient _tcpClient)
        {
            try
            {
                if (_tcpClient != null && _tcpClient.Client != null && _tcpClient.Client.Connected)
                {
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