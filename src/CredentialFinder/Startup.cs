using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(CredentialFinder.Startup))]
namespace CredentialFinder
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
