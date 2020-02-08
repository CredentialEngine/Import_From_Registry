using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(CredentialFinderWeb.Startup))]
namespace CredentialFinderWeb
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
