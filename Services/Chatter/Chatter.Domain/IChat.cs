// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Chatter.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Services.Remoting;

    public interface IChat : IService
    {
        Task AddMessageAsync(Message message);

        Task<IEnumerable<KeyValuePair<DateTimeOffset, Message>>> GetMessagesAsync();

        Task ClearMessagesAsync();
    }
}