using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using TcpFramework.Server;

namespace TcpFramework
{
    public class Message
    {
        public TcpClient TcpClient;
        private readonly System.Text.Encoding _encoder;
        private readonly byte _writeLineDelimiter;
        private readonly bool _autoTrim;
        internal Message(byte[] data, TcpClient tcpClient, System.Text.Encoding stringEncoder, byte lineDelimiter)
        {
            Data = data;
            TcpClient = tcpClient;
            _encoder = stringEncoder;
            _writeLineDelimiter = lineDelimiter;
        }

        internal Message(byte[] data, TcpClient tcpClient, System.Text.Encoding stringEncoder, byte lineDelimiter, bool autoTrim)
        {
            Data = data;
            TcpClient = tcpClient;
            _encoder = stringEncoder;
            _writeLineDelimiter = lineDelimiter;
            _autoTrim = autoTrim;
        }

        public byte[] Data { get; }
        public string MessageString
        {
            get
            {
                if (_autoTrim)
                {
                    return _encoder.GetString(Data).Trim();
                }

                return _encoder.GetString(Data);
            }
        }

        public void Reply(byte[] data)
        {
            TcpClient.SendBufferSize = Int32.MaxValue;
            TcpClient.GetStream().Write(data, 0, data.Length);
        }

        public void Reply(string data)
        {
            if (string.IsNullOrEmpty(data)) { return; }
            var bytes = _encoder.GetBytes(data);
            using (var sw = new StreamWriter("reply.log"))
                sw.WriteLine($"Original data length: {data.Length}\nBytes length: {bytes.Length}\nData: {data}\n\nBytes: {bytes}");
            Reply(bytes);
        }

        public void ReplyLine(string data)
        {
            if (string.IsNullOrEmpty(data)) { return; }
            if (data.LastOrDefault() != _writeLineDelimiter)
            {
                Reply(data + _encoder.GetString(new[] { _writeLineDelimiter }));
            }
            else
            {
                Reply(data);
            }
        }
    }
}
