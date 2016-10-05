using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TcpFramework
{
    public class SimpleTcpClient : IDisposable
    {
        public SimpleTcpClient()
        {
            StringEncoder = Encoding.UTF8;
            ReadLoopIntervalMs = 10;
            Delimiter = 0x13;
        }

        private Thread _rxThread;
        private readonly List<byte> _queuedMsg = new List<byte>();
        public byte Delimiter { get; set; }
        public Encoding StringEncoder { get; set; }
        private TcpClient _client;

        public event EventHandler<Message> DelimiterDataReceived;
        public event EventHandler<Message> DataReceived;

        internal bool QueueStop { get; set; }
        internal int ReadLoopIntervalMs { get; set; }
        public bool AutoTrimStrings { get; set; }

        public SimpleTcpClient Connect(string hostNameOrIpAddress, int port)
        {
            if (string.IsNullOrEmpty(hostNameOrIpAddress))
            {
                throw new ArgumentNullException(nameof(hostNameOrIpAddress));
            }

            _client = new TcpClient();
            _client.Connect(hostNameOrIpAddress, port);

            StartRxThread();

            return this;
        }

        private void StartRxThread()
        {
            if (_rxThread != null) { return; }

            _rxThread = new Thread(ListenerLoop) {IsBackground = true};
            _rxThread.Start();
        }

        public SimpleTcpClient Disconnect()
        {
            if (_client == null) { return this; }
            _client.Close();
            _client = null;
            return this;
        }

        public TcpClient TcpClient => _client;

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

            _rxThread = null;
        }

        private void RunLoopStep()
        {
            if (_client == null) { return; }
            if (_client.Connected == false) { return; }

            var delimiter = Delimiter;
            var c = _client;

            int bytesAvailable = c.Available;
            if (bytesAvailable == 0)
            {
                Thread.Sleep(10);
                return;
            }
            //_queuedMsg.Clear();
            List<byte> bytesReceived = new List<byte>();

            while (c.Available > 0 && c.Connected)
            {
                byte[] nextByte = new byte[c.Available];
                c.Client.Receive(nextByte, 0, nextByte.Length, SocketFlags.None);
                bytesReceived.AddRange(nextByte);
                var delIndex = Array.IndexOf(nextByte, delimiter);
                if (delIndex != -1)
                {
                    var part = nextByte.Take(delIndex + 1);
                    var next = nextByte.Skip(delIndex + 1);
                    _queuedMsg.AddRange(part);
                    byte[] msg = _queuedMsg.ToArray();
                    _queuedMsg.Clear();
                    _queuedMsg.AddRange(next);
                    //bytesReceived.Clear();
                    //bytesReceived.AddRange(next);
                    NotifyDelimiterMessageRx(c, msg);
                }
                else
                {
                    _queuedMsg.AddRange(nextByte);
                }
                //Thread.Sleep(75);
                //byte[] nextByte = new byte[1];
                //c.Client.Receive(nextByte, 0, 1, SocketFlags.None);
                //bytesReceived.AddRange(nextByte);
                //if (nextByte[0] == delimiter)
                //{
                //    byte[] msg = _queuedMsg.ToArray();
                //    _queuedMsg.Clear();
                //    NotifyDelimiterMessageRx(c, msg);
                //}
                //else
                //{
                //    _queuedMsg.AddRange(nextByte);
                //}
            }

            if (bytesReceived.Count > 0)
            {
                NotifyEndTransmissionRx(c, bytesReceived.ToArray());
            }
        }

        private void NotifyDelimiterMessageRx(TcpClient client, byte[] msg)
        {
            if (DelimiterDataReceived != null)
            {
                Message m = new Message(msg, client, StringEncoder, Delimiter, AutoTrimStrings);
                DelimiterDataReceived(this, m);
            }
        }

        private void NotifyEndTransmissionRx(TcpClient client, byte[] msg)
        {
            DataReceived?.Invoke(this, new Message(msg, client, StringEncoder, Delimiter, AutoTrimStrings));
        }

        public void Write(byte[] data)
        {
            if (_client == null) { throw new Exception("Cannot send data to a null TcpClient (check to see if Connect was called)"); }
            _client.ReceiveBufferSize = Int32.MaxValue;
            _client.GetStream().Write(data, 0, data.Length);
        }

        public void Write(string data)
        {
            if (data == null) { return; }
            Write(StringEncoder.GetBytes(data));
        }

        public void WriteLine(string data)
        {
            if (string.IsNullOrEmpty(data)) { return; }
            if (data.LastOrDefault() != Delimiter)
            {
                Write(data + StringEncoder.GetString(new[] { Delimiter }));
            }
            else
            {
                Write(data);
            }
        }

        public Message WriteLineAndGetReply(string data, TimeSpan timeout)
        {
            Message mReply = null;
            EventHandler<Message> handler = (s, e) => { mReply = e; };
            try
            {
                DataReceived += handler;
                WriteLine(data);

                Stopwatch sw = new Stopwatch();
                sw.Start();

                while (mReply == null && sw.Elapsed < timeout)
                {
                    Thread.Sleep(10);
                }
                sw.Stop();
                sw = null;
            }
            finally
            {
                DataReceived -= handler;
            }
            return mReply;
        }

        public void ReplyReceived(object s, string e)
        {
            
        }

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).

                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                QueueStop = true;
                if (_client != null)
                {
                    try
                    {
                        _client.Close();
                    }
                    catch
                    {
                        // ignored
                    }
                    _client = null;
                }

                _disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~SimpleTcpClient() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
