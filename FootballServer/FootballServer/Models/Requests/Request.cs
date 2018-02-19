namespace FootballServer.Models.Requests
{
    class Request : Message<Player>
    {
        public Request(int id, Player player) : base(id, player)
        {

        }
    }
}