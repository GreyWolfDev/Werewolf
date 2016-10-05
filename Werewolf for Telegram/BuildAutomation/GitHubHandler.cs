using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using BuildAutomation.Models;
using Microsoft.AspNet.WebHooks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BuildAutomation
{
    public class GitHubHandler : WebHookHandler
    {
        
        public override Task ExecuteAsync(string receiver, WebHookHandlerContext context)
        {
            string action = context.Actions.First();
            JObject data = context.GetDataOrDefault<JObject>();
            //var push = JsonConvert.DeserializeObject<PushEvent>(data.ToString());
            //write this to App_Data so we can verift
            string path = HttpContext.Current.Server.MapPath("~/App_Data/last.json");
            using (var sw = new StreamWriter(path))
            {
                sw.WriteLine(data);
            }
            return Task.FromResult(true);
        }
    }
}