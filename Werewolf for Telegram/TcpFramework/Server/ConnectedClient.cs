using System;
using System.Net;
using System.Net.Sockets;

namespace TcpFramework.Server
{
    public class ConnectedClient
    {
        public Guid ClientId { get; set; }
        public IPAddress ServerIP { get; internal set; }
        public TcpClient Client { get; internal set; }
    }
}
