namespace FootballClient
{
    class Program
    {
        public static string inviteToken = "";

        public static void Main(string[] args)
        {
            var client = new MMClient("Ars")
            {
                OnInviteAccepted = System.Console.WriteLine,
                OnInviteCanceled = System.Console.WriteLine,
                OnInviteDeclined = System.Console.WriteLine,
                OnInviteRecieved = Recieve,
                OnInviteError = System.Console.WriteLine
            };
            client.Connect(System.Net.IPAddress.Parse("127.0.0.1"), 26000);
            while (true)
            {
                var s = System.Console.ReadLine();
                client.Tick();
                switch (s)
                {
                    case "crt":
                        client.CreateInvite("Ars");
                        break;
                    case "acc":
                        client.AcceptInvite(inviteToken);
                        break;
                    case "dec":
                        client.DeclineInvite(inviteToken);
                        break;
                    case "can":
                        client.CancelInvite();
                        break;
                    default:
                        client.Tick();
                        break;
                }
            }
        }

        private static void Recieve(FootballClient.Models.Invite s)
        {
            inviteToken = s.Token;
            System.Console.WriteLine(s.Token);
        }
    }
}