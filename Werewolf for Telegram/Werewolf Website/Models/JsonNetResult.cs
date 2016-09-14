using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace Werewolf_Website.Models
{
    /// <summary>
    /// Custom JSON result class using JSON.NET.
    /// </summary>
    public class JsonNetResult : JsonResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonNetResult" /> class.
        /// </summary>
        public JsonNetResult()
        {
            Settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Error
            };
        }

        public JsonNetResult(object data, JsonRequestBehavior b)
        {
            Settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Error
            };
            JsonRequestBehavior = b;
            Data = data;
        }

        /// <summary>
        /// Gets the settings for the serializer.
        /// </summary>
        public JsonSerializerSettings Settings { get; set; }

        /// <summary>
        /// Try to retrieve JSON from the context.
        /// </summary>
        /// <param name="context">Basic context of the controller doing the request</param>
        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            if (JsonRequestBehavior == JsonRequestBehavior.DenyGet &&
                string.Equals(context.HttpContext.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("JSON GET is not allowed");
            }

            HttpResponseBase response = context.HttpContext.Response;
            response.ContentType = string.IsNullOrEmpty(ContentType) ? "application/json" : ContentType;

            if (ContentEncoding != null)
            {
                response.ContentEncoding = ContentEncoding;
            }

            if (Data == null)
            {
                return;
            }

            var scriptSerializer = JsonSerializer.Create(Settings);

            using (var sw = new StringWriter())
            {
                scriptSerializer.Serialize(sw, Data);
                response.Write(sw.ToString());
            }
        }
    }
}