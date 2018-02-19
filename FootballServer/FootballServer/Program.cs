using FootballServer.Enums;
using FootballServer.Models.Requests;
using FootballServer.Models.Results;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace FootballServer
{
    class Program
    {
        private static ConcurrentDictionary<Token, Invite> _invites =
            new ConcurrentDictionary<Token, Invite>();

        static void Main(string[] args)
        {
            var server = new Server<Player>(26000);

            server.AddHandler((int)MessageType.CONNECT,
                (player, msg, client) => Task.Run(() =>
                {
                    var request = msg.ToObject<Message<Player>>();
                    System.Console.WriteLine(System.DateTime.Now.Hour
                        + ":" + System.DateTime.Now.Minute
                        + ":" + System.DateTime.Now.Second
                        + " [Server] Player "
                        + request.Player.Token.ToString()
                        + " connected");

                }));
            server.AddHandler((int)MessageType.CREATE_INVITE,
                (player, msg, client) => Task.Run(() =>
                {
                    try
                    {
                        var request = msg.ToObject<ValueRequest<Token>>();
                        System.Console.WriteLine("Create request from " + request.Player.Token.ToString() + " to " + request.Value.ToString());
                        if (server.Players.Any(x => x.Token == request.Value.ToString()))
                        {
                            var inv = new Invite(Token.Generate(), player, server.Players.First(x => x.Token == request.Value.ToString()));
                            _invites[inv.Token] = inv;
                            server.Send(new ValueResult<Invite>((int)MessageType.INVITE_CREATED, inv.From, inv));
                            server.Send(new ValueResult<Invite>((int)MessageType.RECIEVED_INVITE, inv.To, inv));
                        }
                        else
                        {
                            server.Send(new ErrorResult("No such player on server", player));
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine(ex);
                    }
                }));
            server.AddHandler((int)MessageType.ACCEPT_INVITE,
                (player, msg, client) => Task.Run(() =>
                {
                    try
                    {
                        var request = msg.ToObject<ValueRequest<Invite>>();
                        if (_invites.ContainsKey(request.Value.Token))
                        {
                            System.Console.WriteLine("[Server] Invite accepted");

                            _invites[request.Value.Token].Status = InviteStatus.ACCEPTED;
                            var inv = _invites[request.Value.Token];
                            var token = Token.Generate();
                            server.Send(new ValueResult<Token>((int)MessageType.ACCEPT_INVITE,
                                inv.To, token));
                            server.Send(new ValueResult<Token>((int)MessageType.ACCEPT_INVITE,
                                inv.From, token));
                            _invites.TryRemove(request.Value.Token, out inv);
                        }
                        else
                        {
                            server.Send(new ErrorResult("Invite is not valid", player));
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine(ex);
                    }
                }));
            server.AddHandler((int)MessageType.DECLINE_INVITE,
                (player, msg, client) => Task.Run(() =>
                {
                    try
                    {
                        var request = msg.ToObject<ValueRequest<Invite>>();
                        if (_invites.ContainsKey(request.Value.Token))
                        {
                            System.Console.WriteLine("[Server] Invite declined by " + request.Player.Token);
                            _invites[request.Value.Token].Status = InviteStatus.REJECTED;
                            var inv = _invites[request.Value.Token];
                            server.Send(new ValueResult<Invite>((int)MessageType.DECLINE_INVITE,
                                inv.From, inv));
                            _invites.TryRemove(request.Value.Token, out inv);
                        }
                        else
                        {
                            server.Send(new ErrorResult("Invite is not valid", player));
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine(ex);
                    }
                }));
            server.AddHandler((int)MessageType.CANCEL_INVITE,
                (player, msg, client) => Task.Run(() =>
                {
                    try
                    {
                        var request = msg.ToObject<ValueRequest<Invite>>();
                        if (_invites.Count > 0 && _invites.ContainsKey(request.Value.Token))
                        {
                            System.Console.WriteLine("[Server] Invite canceled");

                            _invites[request.Value.Token].Status = InviteStatus.REJECTED;
                            var inv = _invites[request.Value.Token];
                            server.Send(new ValueResult<Invite>((int)MessageType.CANCEL_INVITE,
                                inv.From, inv));
                            server.Send(new ValueResult<Invite>((int)MessageType.CANCEL_INVITE,
                                inv.To, inv));
                            _invites.TryRemove(request.Value.Token, out inv);
                        }
                        else
                        {
                            server.Send(new ErrorResult("Invite is not valid", player));
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine(ex);
                    }
                }));
            server.AddHandler((int)MessageType.DISCONNECT,
                (player, msg, client) => Task.Run(() =>
                {
                    var request = msg.ToObject<Message<Player>>();
                    
                    try
                    {
                        var invite = _invites.First(x => x.Value.To.Token.ToString() == request.Player.Token);
                        System.Console.WriteLine(System.DateTime.Now.Hour
                            + ":" + System.DateTime.Now.Minute
                            + ":" + System.DateTime.Now.Second
                            + " [Server] Player "
                            + request.Player.Token.ToString()
                            + " disconnected");
                        server.Send(new ValueResult<Invite>((int)MessageType.CANCEL_INVITE, invite.Value.From, invite.Value));
                        Invite value;
                        _invites.TryRemove(invite.Key, out value);
                    }
                    catch
                    {
                        try
                        {
                            var invite = _invites.First(x => x.Value.From.Token.ToString() == request.Player.Token);
                            server.Send(new ValueResult<Invite>((int)MessageType.CANCEL_INVITE, invite.Value.To, invite.Value));
                            Invite value;
                            _invites.TryRemove(invite.Key, out value);
                        }
                        catch (Exception ex)
                        {
                            System.Console.WriteLine(ex);
                        }
                    }
                }));

            Console.WriteLine("[Server] Started...");
            Task.Run(() => 
            {
                while (true)
                {
                    server.Tick();
                    System.Threading.Thread.Sleep(500);
                }             
            });
            server.StartListener().Wait();
        }
    }
}