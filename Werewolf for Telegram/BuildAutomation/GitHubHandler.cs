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
            try
            {
                string action = context.Actions.First();
                JObject data = context.GetDataOrDefault<JObject>();
                var push = JsonConvert.DeserializeObject<PushEvent>(data.ToString());
                //write this to App_Data so we can verify
                string path = HttpContext.Current.Server.MapPath("~/App_Data/last.json");
                using (var sw = new StreamWriter(path))
                {
                    sw.WriteLine(data);
                }

                if (push.commits.Any(x => x.message.Contains("#build#") && x.committer.username == "parabola949"))
                {
                    //we want to build, yay!
                    new Task(BuildHelper.Automate).Start();
                }
                return Task.FromResult(true);
            }
            catch (Exception e)
            {
                throw new HttpException(501,e.Message);
            }
            
        }
    }
}