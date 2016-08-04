// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Chatter.Web.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Chatter.Domain;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Client;

    // This controller accepts WebApi calls from the index.html when the AJAX calls are made
    [Route("api/chat")]
    public class ChatController : Controller
    {
        private readonly Uri serviceUri = new Uri("fabric:/Chatter/Chat");

        private readonly IServiceProxyFactory proxy;

        public ChatController(IServiceProxyFactory proxy)
        {
            this.proxy = proxy;
        }

        // GET: api/chat
        [HttpGet]
        public Task<IEnumerable<KeyValuePair<DateTimeOffset, Message>>> GetMessages()
        {
            IChat messages = this.proxy.CreateServiceProxy<IChat>(this.serviceUri, new ServicePartitionKey(0));

            return messages.GetMessagesAsync();
        }

        // POST api/chat
        [HttpPost]
        public Task Add([FromBody] Message message)
        {
            IChat messages = this.proxy.CreateServiceProxy<IChat>(this.serviceUri, new ServicePartitionKey(0));

            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            return messages.AddMessageAsync(message);
        }

        //DELETE api/chat
        [HttpDelete]
        public Task ClearMessages()
        {
            IChat messages = this.proxy.CreateServiceProxy<IChat>(this.serviceUri, new ServicePartitionKey(0));

            return messages.ClearMessagesAsync();
        }
    }
}