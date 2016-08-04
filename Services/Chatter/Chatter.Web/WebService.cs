// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Chatter.Web
{
    using System.Collections.Generic;
    using System.Fabric;
    using System.IO;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting.Client;
    using Microsoft.ServiceFabric.Services.Runtime;

    public class WebService : StatelessService
    {
        public WebService(StatelessServiceContext context)
            : base(context)
        {
        }

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new[]
            {
                new ServiceInstanceListener(
                    context =>
                        new WebHostCommunicationListener(
                            context,
                            "chatter",
                            "ServiceEndpoint",
                            uri =>
                            // NOTE: Kestrel is currently NOT SUPPORTED as an edge server.
                            //       A reverse proxy or API management service with traffic throttling
                            //       must be place between this service and the public Internet.
                                new WebHostBuilder().UseKestrel()
                                    .ConfigureServices(
                                        services => services
                                            .AddSingleton<IServiceProxyFactory>(new ServiceProxyFactory()))
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseStartup<Startup>()
                                    .UseUrls(uri)
                                    .Build()))
            };
        }
    }
}