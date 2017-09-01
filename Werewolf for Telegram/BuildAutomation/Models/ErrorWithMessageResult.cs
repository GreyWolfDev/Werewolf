using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace BuildAutomation.Models
{
    public class ErrorWithMessageResult : IHttpActionResult
    {
        private string message;

        public ErrorWithMessageResult(string message)
        {
            this.message = message;
        }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.NotFound);
            response.Content = new StringContent(message);
            return Task.FromResult(response);
        }
    }
}