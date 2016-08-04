// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Chatter.Chat
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Chatter.Domain;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;

    /// <summary>
    /// Stateful service that manages chat messages.
    /// </summary>
    public class ChatService : StatefulService, IChat
    {
        private const string MessageDictionaryName = "messages";

        private const int MessagesToKeep = 5;

        public ChatService(StatefulServiceContext context)
            : base(context)
        {
        }

        /// <summary>
        /// Stores a new message.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task AddMessageAsync(Message message)
        {
            DateTimeOffset time = DateTimeOffset.UtcNow;

            IReliableDictionary<DateTimeOffset, Message> messagesDictionary =
                await this.StateManager.GetOrAddAsync<IReliableDictionary<DateTimeOffset, Message>>(MessageDictionaryName);

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                await messagesDictionary.AddAsync(tx, time, message);
                await tx.CommitAsync();
            }
        }

        /// <summary>
        /// Gets a list of all messages stored in the service.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<KeyValuePair<DateTimeOffset, Message>>> GetMessagesAsync()
        {
            IReliableDictionary<DateTimeOffset, Message> messagesDictionary =
                await this.StateManager.GetOrAddAsync<IReliableDictionary<DateTimeOffset, Message>>(MessageDictionaryName);

            List<KeyValuePair<DateTimeOffset, Message>> returnList = new List<KeyValuePair<DateTimeOffset, Message>>();

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                IAsyncEnumerable<KeyValuePair<DateTimeOffset, Message>> messagesEnumerable =
                    await messagesDictionary.CreateEnumerableAsync(tx, EnumerationMode.Ordered);

                using (IAsyncEnumerator<KeyValuePair<DateTimeOffset, Message>> enumerator = messagesEnumerable.GetAsyncEnumerator())
                {
                    while (await enumerator.MoveNextAsync(CancellationToken.None))
                    {
                        returnList.Add(enumerator.Current);
                    }
                }
            }

            return returnList;
        }

        /// <summary>
        /// Deletes all messages from the service.
        /// </summary>
        /// <returns></returns>
        public async Task ClearMessagesAsync()
        {
            IReliableDictionary<DateTimeOffset, Message> messagesDictionary =
                await this.StateManager.GetOrAddAsync<IReliableDictionary<DateTimeOffset, Message>>(MessageDictionaryName);

            await messagesDictionary.ClearAsync();
        }

        /// <summary>
        /// Creates a listener to allow other services to communicate with this service.
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new List<ServiceReplicaListener>
            {
                new ServiceReplicaListener(this.CreateServiceRemotingListener)
            };
        }

        /// <summary>
        /// Background processing entry point to a service.
        /// Runs a continuous loop that cleans up old messages.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            TimeSpan timeSpan = new TimeSpan(0, 0, 30);
            ServiceEventSource.Current.ServiceMessage(
                this,
                "Partition {0} started processing messages.",
                this.Context.PartitionId);

            IReliableDictionary<DateTimeOffset, Message> messagesDictionary =
                await this.StateManager.GetOrAddAsync<IReliableDictionary<DateTimeOffset, Message>>(MessageDictionaryName);

            //Use this method to periodically clean up messages in the messagesDictionary
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    IEnumerable<KeyValuePair<DateTimeOffset, Message>> messagesEnumerable = await this.GetMessagesAsync();

                    // Remove all the messages that are older than a certain time, keeping the last 50 messages
                    IEnumerable<KeyValuePair<DateTimeOffset, Message>> oldMessages =
                        from t in messagesEnumerable
                        where t.Key < (DateTimeOffset.UtcNow - timeSpan)
                        orderby t.Key ascending
                        select t;

                    using (ITransaction tx = this.StateManager.CreateTransaction())
                    {
                        int messagesCount = (int)await messagesDictionary.GetCountAsync(tx);

                        foreach (KeyValuePair<DateTimeOffset, Message> item in oldMessages.Take(messagesCount - MessagesToKeep))
                        {
                            await messagesDictionary.TryRemoveAsync(tx, item.Key);
                        }

                        await tx.CommitAsync();
                    }
                }
                catch (OperationCanceledException)
                {
                    // allow the replica to cancel operations if requested.
                    throw;
                }
                catch (FabricNotPrimaryException)
                {
                    // replica is no longer primary. 
                    // This is a normal part of a service's lifecycle. End the loop and exit.
                    break;
                }
                catch (FabricObjectClosedException)
                {
                    // replica is closed.
                    // This is a normal part of a service's lifecycle. End the loop and exit.
                    break;
                }
                catch (FabricNotReadableException)
                {
                    // replica is not readable
                    // This is a normal part of a service's lifecycle. End the loop and exit.
                    break;
                }
                catch (TimeoutException)
                {
                    // Service Fabric uses timeouts on collection operations to prevent deadlocks.
                    // If this exception is thrown, it means that this transaction was waiting the default
                    // amount of time (4 seconds) but was unable to acquire the lock. In this case we simply
                    // retry after a random backoff interval. You can also control the timeout via a parameter
                    // on the collection operation.
                }
                catch (Exception e)
                {
                    ServiceEventSource.Current.ServiceMessage(
                        this,
                        "Partition {0} stopped processing because of error {1}",
                        this.Context.PartitionId,
                        e);

                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }
    }
}