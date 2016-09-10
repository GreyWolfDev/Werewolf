using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using Newtonsoft.Json;
using TcpFramework;
using Werewolf_Website.Models;

namespace Werewolf_Website.Helpers
{
    public class TcpAdminConnection
    {
        //for now, just connect to localhost, for debugging
        public SimpleTcpClient Client;
        private string _ip;
        private int _port;

        public TcpAdminConnection(string ip, int port)
        {
            _ip = ip;
            _port = port;
            Client = new SimpleTcpClient();
            Client.DataReceived += ClientOnDataReceived;
            Client.DelimiterDataReceived += ClientOnDelimiterDataReceived;
            //connection lost, let's try to reconnect
            
        }

        private void Connect()
        {
            while (Client.TcpClient == null || !Client.TcpClient.Connected)
            {
                try
                {
                    Client.Connect(_ip, _port);
                    return;
                }
                catch (Exception ex)
                {
                    while (ex.InnerException != null)
                        ex = ex.InnerException;
                    Console.WriteLine($"Error in reconnect: {ex.Message}\n{ex.StackTrace}\n");
                }
                Thread.Sleep(100);
            }
        }

        public StatusResponseInfo GetStatus()
        {
            return JsonConvert.DeserializeObject<StatusResponseInfo>(GetResponse(new GetStatusInfo()));
        }

        public NodeResponseInfo GetNodeInfo(Guid id)
        {
            return JsonConvert.DeserializeObject<NodeResponseInfo>(GetResponse(new GetNodeInfo {ClientId = id}));
        }

        private string GetResponse(TcpRequest request)
        {
            Connect();
            return Client.WriteLineAndGetReply(JsonConvert.SerializeObject(request), TimeSpan.FromSeconds(30)).MessageString;
        }



        private void ClientOnDelimiterDataReceived(object sender, Message e)
        {
            
        }

        private void ClientOnDataReceived(object sender, Message e)
        {
            
        }
    }
}