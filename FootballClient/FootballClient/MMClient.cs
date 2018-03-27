using FootballClient.Enums;
using FootballClient.Models;
using FootballClient.Models.Requests;
using FootballClient.Models.Results;
using System;
using System.Net;

namespace FootballClient
{
    public class MMClient : Client
    {
        private readonly Player _player;

        public delegate void Action();
        public Action<Invite> OnInviteCreated;
        public Action<Invite> OnInviteRecieved;
        public Action<string> OnInviteAccepted;
        public Action OnInviteDeclined;
        public Action OnInviteCanceled;
        public Action<string> OnInviteError;

        public MMClient(string token) : base()
        {
            _player = new Player(token.ToString());

            AddHandler((int)MessageType.CREATE_INVITE,
                (msg) => 
                {
                    var result = msg.ToObject<ValueResult<Invite>>();
                    OnInviteCreated(result.Value);
                });
            AddHandler((int)MessageType.RECEIVE_INVITE,
                (msg) =>
                {
                    var result = msg.ToObject<ValueResult<Invite>>();
                    OnInviteRecieved(result.Value);
                });
            AddHandler((int)MessageType.ACCEPT_INVITE,
                (msg) => 
                {
                    var result = msg.ToObject<ValueResult<string>>();
                    OnInviteAccepted(result.Value);
                });
            AddHandler((int)MessageType.DECLINE_INVITE,
                (msg) => 
                {
                    var result = msg.ToObject<Result>();
                    OnInviteDeclined();
                });
            AddHandler((int)MessageType.CANCEL_INVITE,
                (msg) =>
                {
                    var result = msg.ToObject<Result>();
                    OnInviteCanceled();
                });
            AddHandler((int)MessageType.ERROR,
                (msg) =>
                {
                    var result = msg.ToObject<ValueResult<string>>();
                    OnInviteError(result.Value);
                });
        }

        public override void Connect(IPAddress ip, int port)
        {
            base.Connect(ip, port);
            Send(new Request((int)MessageType.CONNECT, _player));
        }

        public void CreateInvite(string receiverToken)
        {
            Send(new ValueRequest<string>
                ((int)MessageType.CREATE_INVITE, _player, receiverToken));
        }

        public void AcceptInvite(string inviteToken)
        {
            Send(new ValueRequest<string>
                ((int)MessageType.ACCEPT_INVITE, _player, inviteToken));
        }

        public void DeclineInvite(string inviteToken)
        {
            Send(new ValueRequest<string>
                ((int)MessageType.DECLINE_INVITE, _player, inviteToken));
        }

        public void CancelInvite(string inviteToken)
        {
            Send(new ValueRequest<string>
                ((int)MessageType.CANCEL_INVITE, _player, inviteToken));
        }

        public void Disconnect()
        {
            Send(new Request((int)MessageType.DISCONNECT, _player));
        }
    }
}