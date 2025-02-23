using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Owin;

namespace SonataCinemaV2
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var hubConfiguration = new HubConfiguration
            {
                EnableDetailedErrors = true,
                EnableJSONP = true
            };

            // Sửa lại cách gọi MapSignalR
            app.MapSignalR(hubConfiguration);

            GlobalHost.Configuration.MaxIncomingWebSocketMessageSize = null;
        }
    }
}