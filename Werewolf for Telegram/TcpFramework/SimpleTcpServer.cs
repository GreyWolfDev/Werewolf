using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using TcpFramework.Server;

namespace TcpFramework
{
    public class SimpleTcpServer
    {
        public SimpleTcpServer()
        {
            Delimiter = 0x13;
            StringEncoder = System.Text.Encoding.UTF8;
        }

        private readonly List<Server.ServerListener> _listeners = new List<Server.ServerListener>();
        public byte Delimiter { get; set; }
        public System.Text.Encoding StringEncoder { get; set; }
        public bool AutoTrimStrings { get; set; }

        public IEnumerable<ConnectedClient> ConnectedClients => _listeners.SelectMany(x => x.ConnectedClients);

        public event EventHandler<ConnectedClient> ClientConnected;
        public event EventHandler<ConnectedClient> ClientDisconnected;
        public event EventHandler<Message> DelimiterDataReceived;
        public event EventHandler<Message> DataReceived;

        public IEnumerable<IPAddress> GetIPAddresses()
        {
            List<IPAddress> ipAddresses = new List<IPAddress>();

            IEnumerable<NetworkInterface> enabledNetInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up);
            foreach (NetworkInterface netInterface in enabledNetInterfaces)
            {
                IPInterfaceProperties ipProps = netInterface.GetIPProperties();
                foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
                {
                    if (!ipAddresses.Contains(addr.Address))
                    {
                        ipAddresses.Add(addr.Address);
                    }
                }
            }

            var ipSorted = ipAddresses.OrderByDescending(RankIpAddress).ToList();
            return ipSorted;
        }

        public List<IPAddress> GetListeningIPs()
        {
            List<IPAddress> listenIps = new List<IPAddress>();
            foreach (var l in _listeners)
            {
                if (!listenIps.Contains(l.IPAddress))
                {
                    listenIps.Add(l.IPAddress);
                }
            }

            return listenIps.OrderByDescending(RankIpAddress).ToList();
        }

        public void Broadcast(byte[] data)
        {
            foreach (var client in _listeners.SelectMany(x => x.ConnectedClients))
            {
                try
                {
                    client.Client.GetStream().Write(data, 0, data.Length);
                }
                catch
                {
                    //disconnected?
                }
            }
        }

        public void Broadcast(string data)
        {
            if (data == null) { return; }
            Broadcast(StringEncoder.GetBytes(data));
        }

        public void Broadcast(byte[] data, TcpClient client)
        {
            client.GetStream().Write(data, 0, data.Length);
        }

        public void Broadcast(string data, TcpClient client)
        {
            if (data == null) { return; }
            Broadcast(StringEncoder.GetBytes(data), client);
        }

        public void BroadcastLine(string data)
        {
            if (string.IsNullOrEmpty(data)) { return; }
            if (data.LastOrDefault() != Delimiter)
            {
                Broadcast(data + StringEncoder.GetString(new[] { Delimiter }));
            }
            else
            {
                Broadcast(data);
            }
        }

        public void BroadcastLine(string data, TcpClient client)
        {
            if (string.IsNullOrEmpty(data)) { return; }
            if (data.LastOrDefault() != Delimiter)
            {
                Broadcast(data + StringEncoder.GetString(new[] { Delimiter }), client);
            }
            else
            {
                Broadcast(data, client);
            }
        }

        private int RankIpAddress(IPAddress addr)
        {
            int rankScore = 1000;

            if (addr.AddressFamily == AddressFamily.InterNetwork)
            {
                rankScore += 100;
            }

            // class A
            if (addr.ToString().StartsWith("10."))
            {
                rankScore += 100;
            }

            // class B
            if (addr.ToString().StartsWith("172.30."))
            {
                rankScore += 100;
            }

            // class C
            if (addr.ToString().StartsWith("192.168.1."))
            {
                rankScore += 100;
            }

            // local sucks
            if (addr.ToString().StartsWith("169."))
            {
                rankScore = 0;
            }

            return rankScore;
        }

        public SimpleTcpServer Start(int port, bool ignoreNicsWithOccupiedPorts = true)
        {
            var ipSorted = GetIPAddresses();
            bool anyNicFailed = false;
            foreach (var ipAddr in ipSorted)
            {
                try
                {
                    Start(ipAddr, port);
                }
                catch (SocketException ex)
                {
                    DebugInfo(ex.ToString());
                    anyNicFailed = true;
                }
            }

            if (!IsStarted)
                throw new InvalidOperationException("Port was already occupied for all network interfaces");

            if (anyNicFailed && !ignoreNicsWithOccupiedPorts)
            {
                Stop();
                throw new InvalidOperationException("Port was already occupied for one or more network interfaces.");
            }

            return this;
        }

        public SimpleTcpServer Start(int port, AddressFamily addressFamilyFilter)
        {
            var ipSorted = GetIPAddresses().Where(ip => ip.AddressFamily == addressFamilyFilter);
            foreach (var ipAddr in ipSorted)
            {
                try
                {
                    Start(ipAddr, port);
                }
                catch
                {
                    // ignored
                }
            }

            return this;
        }

        public bool IsStarted { get { return _listeners.Any(l => l.Listener.Active); } }

        public SimpleTcpServer Start(IPAddress ipAddress, int port)
        {
            Server.ServerListener listener = new Server.ServerListener(this, ipAddress, port);
            _listeners.Add(listener);

            return this;
        }

        public void Stop()
        {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            _listeners.All(l => l.QueueStop = true);
            while (_listeners.Any(l => l.Listener.Active))
            {
                Thread.Sleep(100);
            }
            _listeners.Clear();
        }

        public int ConnectedClientsCount
        {
            get
            {
                return _listeners.Sum(l => l.ConnectedClientsCount);
            }
        }

        internal void NotifyDelimiterMessageRx(Server.ServerListener listener, TcpClient client, byte[] msg)
        {
            if (DelimiterDataReceived != null)
            {
                Message m = new Message(msg, client, StringEncoder, Delimiter, AutoTrimStrings);
                DelimiterDataReceived(this, m);
            }
        }

        internal void NotifyEndTransmissionRx(Server.ServerListener listener, TcpClient client, byte[] msg)
        {
            if (DataReceived != null)
            {
                Message m = new Message(msg, client, StringEncoder, Delimiter, AutoTrimStrings);
                DataReceived(this, m);
            }
        }

        internal void NotifyClientConnected(Server.ServerListener listener, ConnectedClient newClient)
        {
            ClientConnected?.Invoke(this, newClient);
        }

        internal void NotifyClientDisconnected(Server.ServerListener listener, ConnectedClient disconnectedClient)
        {
            ClientDisconnected?.Invoke(this, disconnectedClient);
        }

        public Message WriteLineAndGetReply(string data, TimeSpan timeout, TcpClient client)
        {
            Message mReply = null;
            DataReceived += (s, e) => { mReply = e; };
            BroadcastLine(data, client);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            while (mReply == null && sw.Elapsed < timeout)
            {
                Thread.Sleep(10);
            }

            return mReply;
        }

        #region Debug logging

        [System.Diagnostics.Conditional("DEBUG")]
        void DebugInfo(string format, params object[] args)
        {
            if (_debugInfoTime == null)
            {
                _debugInfoTime = new System.Diagnostics.Stopwatch();
                _debugInfoTime.Start();
            }
            System.Diagnostics.Debug.WriteLine(_debugInfoTime.ElapsedMilliseconds + ": " + format, args);
        }
        System.Diagnostics.Stopwatch _debugInfoTime;

        #endregion Debug logging
    }
}
