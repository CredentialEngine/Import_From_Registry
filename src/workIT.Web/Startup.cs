﻿using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(WorkIT.Web.Startup))]
namespace WorkIT.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
