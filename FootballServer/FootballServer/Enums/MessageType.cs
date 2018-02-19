using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballServer.Enums
{
    public enum MessageType
    {
        CREATE_INVITE,
        ACCEPT_INVITE,
        DECLINE_INVITE,
        REJECT_INVITE,
        SUCCESS_INVITE,
        FAIL_INVITE,
        RECIEVED_INVITE,
        ERROR,
        CONNECT,
        DISCONNECT,
        CANCEL_INVITE,
        INVITE_CREATED
    }
}
