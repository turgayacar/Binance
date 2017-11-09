﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Binance.Api.WebSocket;
using Moq;
using Xunit;

namespace Binance.Tests.Integration
{
    public class DepthWebSocketClientTest
    {
        [Fact]
        public Task SubscribeThrows()
        {
            using (var cts = new CancellationTokenSource())
            using (var client = new DepthWebSocketClient(new Mock<IWebSocketClient>().Object))
                return Assert.ThrowsAsync<ArgumentNullException>("symbol", () => client.SubscribeAsync(null, cts.Token));
        }

        [Fact]
        public async Task Properties()
        {
            var symbol = Symbol.BTC_USDT;

            using (var cts = new CancellationTokenSource())
            using (var client = new DepthWebSocketClient(new WebSocketClient()))
            {
                var task = client.SubscribeAsync(symbol, cts.Token);

                Assert.Equal(symbol, client.Symbol);

                cts.Cancel();
                await task;
            }
        }

        [Fact]
        public Task SubscribeTwiceThrows()
        {
            var symbol = Symbol.BTC_USDT;

            using (var cts = new CancellationTokenSource())
            using (var client = new DepthWebSocketClient(new WebSocketClient()))
            {
                var task = client.SubscribeAsync(symbol, cts.Token);

                return Assert.ThrowsAsync<InvalidOperationException>(() => client.SubscribeAsync(symbol, cts.Token));
            }
        }

        [Fact]
        public async Task SubscribeCallback()
        {
            var symbol = Symbol.BTC_USDT;

            using (var cts = new CancellationTokenSource())
            using (var client = new DepthWebSocketClient(new WebSocketClient()))
            {
                var task = client.SubscribeAsync(symbol, args =>
                {
                    // NOTE: The first event will cancel the client ...could be a while.
                    cts.Cancel();
                },
                cts.Token);

                await task;
            }
        }
    }
}