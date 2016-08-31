using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Werewolf_Website.Startup))]
namespace Werewolf_Website
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
