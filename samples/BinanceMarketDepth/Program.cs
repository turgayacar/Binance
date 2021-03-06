﻿using System;
using System.IO;
using Binance;
using Binance.Application;
using Binance.Cache;
using Binance.Market;
using Binance.Utility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ReSharper disable AccessToDisposedClosure

namespace BinanceMarketDepth
{
    /// <summary>
    /// Demonstrate how to maintain an order book cache for a symbol
    /// and respond to real-time depth-of-market update events.
    /// </summary>
    internal class Program
    {
        private static void Main()
        {
            try
            {
                // Load configuration.
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", true, false)
                    .Build();

                // Configure services.
                var services = new ServiceCollection()
                    .AddBinance()
                    .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace))
                    .BuildServiceProvider();

                // Configure logging.
                services.GetService<ILoggerFactory>()
                    .AddFile(configuration.GetSection("Logging").GetSection("File"));

                // Get configuration settings.
                var limit = 10;
                var symbol = configuration.GetSection("OrderBook")?["Symbol"] ?? Symbol.BTC_USDT;
                try { limit = Convert.ToInt32(configuration.GetSection("OrderBook")?["Limit"]); }
                catch { /* ignored */ }

                // NOTE: Currently the Partial Book Depth Stream only supports limits of: 5, 10, or 20.
                if (limit > 10) limit = 20;
                else if (limit > 5) limit = 10;
                else if (limit > 0) limit = 5;

                var cache = services.GetService<IOrderBookCache>();

                using (var controller = new RetryTaskController())
                {
                    // Monitor order book and display updates in real-time.
                    // NOTE: If no limit is provided (or limit = 0) then the order book is initialized with
                    //       limit = 1000 and the diff. depth stream is used to keep order book up-to-date.
                    controller.Begin(
                        tkn => cache.SubscribeAndStreamAsync(symbol, limit, evt => Display(evt.OrderBook), tkn),
                        err => Console.WriteLine(err.Message));

                    // Alternative usage (if sharing IBinanceWebSocket for combined streams).
                    //cache.Subscribe(symbol, limit, evt => Display(evt.OrderBook));
                    //controller.Begin(
                    //    tkn => cache.StreamAsync(tkn),
                    //    err => Console.WriteLine(err.Message));

                    Console.ReadKey(true);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine();
                Console.WriteLine("  ...press any key to close window.");
                Console.ReadKey(true);
            }
        }

        private static void Display(OrderBook orderBook)
        {
            Console.SetCursorPosition(0, 0);
            orderBook.Print(Console.Out, 10);

            Console.WriteLine();
            Console.WriteLine("...press any key to exit.");
        }
    }
}
