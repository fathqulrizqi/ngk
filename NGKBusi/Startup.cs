using Microsoft.Owin;
using Owin;
using Microsoft.AspNet.SignalR;
using NGKBusi.SignalR;

[assembly: OwinStartupAttribute(typeof(NGKBusi.Startup))]
namespace NGKBusi
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            app.MapSignalR();
        }
    }
}
