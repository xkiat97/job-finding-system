using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;

[assembly: OwinStartup(typeof(JobFindingSystem.Startup))]

namespace JobFindingSystem
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var options = new CookieAuthenticationOptions
            {
                AuthenticationType = "AUTH",
                LoginPath = new PathString("/Account/Login"),
                LogoutPath = new PathString("/Account/Logout")
            };

            app.UseCookieAuthentication(options);
            app.MapSignalR();
        }
    }
}
