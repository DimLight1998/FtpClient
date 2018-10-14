#region

using System.Net;
using System.Net.Sockets;

#endregion

namespace FtpClientBase
{
    public class Connection
    {
        private readonly IPAddress _remoteIpAddress;
        private readonly int _remotePort;

        public Connection(string hostname, int port = 21)
        {
            _remoteIpAddress = Dns.GetHostAddresses(hostname)[0];
            _remotePort = port;
        }

        public OperationPerformer Performer { get; private set; }
        public bool Connected { get; private set; }

        public void Establish(string username, string password)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(_remoteIpAddress, _remotePort);
            Performer = new OperationPerformer(socket);
            Performer.OmitWelcomeResponse();
            Performer.LogIn(username, password);
            Connected = true;
        }

        public void Destory()
        {
            Performer.LogOut();
            Connected = false;
        }
    }
}