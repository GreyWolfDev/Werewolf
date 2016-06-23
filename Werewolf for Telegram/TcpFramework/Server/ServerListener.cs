using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TcpFramework.Server
{
    internal class ServerListener
    {
        private readonly TcpListenerEx _listener;
        private readonly List<ConnectedClient> _connectedClients = new List<ConnectedClient>();
        private readonly List<ConnectedClient> _disconnectedClients = new List<ConnectedClient>();
        private readonly SimpleTcpServer _parent;
        private readonly List<byte> _queuedMsg = new List<byte>();
        private byte _delimiter = 0x13;
        private Thread _rxThread;

        public int ConnectedClientsCount => _connectedClients.Count;

        public IEnumerable<ConnectedClient> ConnectedClients => _connectedClients;

        internal ServerListener(SimpleTcpServer parentServer, IPAddress ipAddress, int port)
        {
            QueueStop = false;
            _parent = parentServer;
            IPAddress = ipAddress;
            Port = port;
            ReadLoopIntervalMs = 50;

            _listener = new TcpListenerEx(ipAddress, port);
            _listener.Start();

            ThreadPool.QueueUserWorkItem(ListenerLoop);
            //StartThread();
        }

        private void StartThread()
        {
            if (_rxThread != null) { return; }
            _rxThread = new Thread(ListenerLoop) {IsBackground = true};
            _rxThread.Start();
        }

        internal bool QueueStop { get; set; }
        internal IPAddress IPAddress { get; private set; }
        internal int Port { get; private set; }
        internal int ReadLoopIntervalMs { get; set; }

        internal TcpListenerEx Listener => _listener;


        private void ListenerLoop(object state)
        {
            while (!QueueStop)
            {
                try
                {
                    RunLoopStep();
                }
                catch
                {
                    // ignored
                }

                Thread.Sleep(ReadLoopIntervalMs);
            }
            _listener.Stop();
        }

        private void RunLoopStep()
        {
            if (_disconnectedClients.Count > 0)
            {
                var disconnectedClients = _disconnectedClients.ToArray();
                _disconnectedClients.Clear();

                foreach (var disC in disconnectedClients)
                {
                    _connectedClients.Remove(disC);
                    _parent.NotifyClientDisconnected(this, disC);
                }
            }

            if (_listener.Pending())
            {
                var newClient = _listener.AcceptTcpClient();
                _connectedClients.Add(new ConnectedClient {Client = newClient});
                _parent.NotifyClientConnected(this, new ConnectedClient {Client = newClient});
            }

            _delimiter = _parent.Delimiter;

            foreach (var c in _connectedClients)
            {
                int bytesAvailable = c.Client.Available;
                if (bytesAvailable == 0)
                {
                    //Thread.Sleep(10);
                    continue;
                }

                var bytesReceived = new List<byte>();

                while (c.Client.Available > 0 && c.Client.Connected)
                {
                    byte[] nextByte = new byte[256];
                    c.Client.Client.Receive(nextByte, 0, nextByte.Length, SocketFlags.None);
                    bytesReceived.AddRange(nextByte);
                    var delIndex = Array.IndexOf(nextByte, _delimiter);
                    if (delIndex != -1)
                    {
                        var part = nextByte.Take(delIndex + 1);
                        var next = nextByte.Skip(delIndex + 1);
                        _queuedMsg.AddRange(part);
                        byte[] msg = _queuedMsg.ToArray();
                        _queuedMsg.Clear();
                        _queuedMsg.AddRange(next);
                        _parent.NotifyDelimiterMessageRx(this, c.Client, msg);
                    }
                    else
                    {
                        _queuedMsg.AddRange(nextByte);
                    }
                }

                if (bytesReceived.Count > 0)
                {
                    _parent.NotifyEndTransmissionRx(this, c.Client, bytesReceived.ToArray());
                }

                if (!c.Client.Connected)
                {
                    _disconnectedClients.Add(c);
                }
            }
        }
    }
}
