
namespace ServerSide
{
    public interface IGameServer
    {
        void StartServer();
        void StopServer();
    }
    public interface IGameServerFactory
    {
        IGameServer CreateServer(string gameCode);
    }

    public class GameServerFactory : IGameServerFactory
    {
        public IGameServer CreateServer(string gameCode)
        {
            return new GameServer(gameCode);
        }
    }

}