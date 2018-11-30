using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(workIT.Web.Startup))]
namespace workIT.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
