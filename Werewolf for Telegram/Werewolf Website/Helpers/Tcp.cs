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

        public GameInfo GetGameInfo(long groupid)
        {
            try
            {
                return JsonConvert.DeserializeObject<GameInfo>(GetResponse(new GetGameInfo { GroupId = groupid }));
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public StatusResponseInfo GetStatus()
        {
            var stat = GetResponse(new GetStatusInfo());
            if (String.IsNullOrWhiteSpace(stat))
                return new StatusResponseInfo();
            return JsonConvert.DeserializeObject<StatusResponseInfo>(stat);
        }

        public NodeResponseInfo GetNodeInfo(Guid id)
        {
            try
            {
                return JsonConvert.DeserializeObject<NodeResponseInfo>(GetResponse(new GetNodeInfo {ClientId = id}));
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private string GetResponse(TcpRequest request)
        {
            try
            {
                Client.Connect(_ip, _port);
            }
            catch(Exception e)
            {
                while (e.InnerException != null)
                    e = e.InnerException;
                return e.Message;
            }
            var response =
                Client.WriteLineAndGetReply(JsonConvert.SerializeObject(request), TimeSpan.FromSeconds(30))?
                    .MessageString;
            Client.Disconnect();
            return response;
        }



        private void ClientOnDelimiterDataReceived(object sender, Message e)
        {
            
        }

        private void ClientOnDataReceived(object sender, Message e)
        {
            
        }
    }
}