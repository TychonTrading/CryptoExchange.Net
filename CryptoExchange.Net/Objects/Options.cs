﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Interfaces;
using CryptoExchange.Net.Logging;
using Microsoft.Extensions.Logging;

namespace CryptoExchange.Net.Objects
{
    /// <summary>
    /// Base options
    /// </summary>
    public class BaseOptions
    {
        /// <summary>
        /// The minimum log level to output. Setting it to null will send all messages to the registered ILoggers. 
        /// </summary>
        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// The log writers
        /// </summary>
        public List<ILogger> LogWriters { get; set; } = new List<ILogger> { new DebugLogger() };

        /// <summary>
        /// If true, the CallResult and DataEvent objects will also include the originally received json data in the OriginalData property
        /// </summary>
        public bool OutputOriginalData { get; set; } = false;

        /// <summary>
        /// Copy the values of the def to the input
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="def"></param>
        public void Copy<T>(T input, T def) where T : BaseOptions
        {
            input.LogLevel = def.LogLevel;
            input.LogWriters = def.LogWriters.ToList();
            input.OutputOriginalData = def.OutputOriginalData;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"LogLevel: {LogLevel}, Writers: {LogWriters.Count}, OutputOriginalData: {OutputOriginalData}";
        }
    }

    /// <summary>
    /// Base for order book options
    /// </summary>
    public class OrderBookOptions : BaseOptions
    {
        /// <summary>
        /// Whether or not checksum validation is enabled. Default is true, disabling will ignore checksum messages.
        /// </summary>
        public bool ChecksumValidationEnabled { get; set; } = true;
    }

    /// <summary>
    /// Base client options
    /// </summary>
    public class ClientOptions : BaseOptions
    {
        private string _baseAddress = string.Empty;

        /// <summary>
        /// The base address of the client
        /// </summary>
        public string BaseAddress
        {
            get => _baseAddress;
            set
            {
                if (value == null)
                    return;

                var newValue = value;
                if (!newValue.EndsWith("/"))
                    newValue += "/";
                _baseAddress = newValue;
            }
        }

        /// <summary>
        /// The api credentials
        /// </summary>        
        public ApiCredentials? ApiCredentials { get; set; }

        /// <summary>
        /// Proxy to use
        /// </summary>
        public ApiProxy? Proxy { get; set; }

        /// <summary>
        /// Copy the values of the def to the input
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="def"></param>
        public new void Copy<T>(T input, T def) where T : ClientOptions
        {
            base.Copy(input, def);

            input.BaseAddress = def.BaseAddress;
            input.ApiCredentials = def.ApiCredentials?.Copy();
            input.Proxy = def.Proxy;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{base.ToString()}, Credentials: {(ApiCredentials == null ? "-" : "Set")}, BaseAddress: {BaseAddress}, Proxy: {(Proxy == null ? "-" : Proxy.Host)}";
        }
    }

    /// <summary>
    /// Base for rest client options
    /// </summary>
    public class RestClientOptions : ClientOptions
    {
        /// <summary>
        /// List of rate limiters to use
        /// </summary>
        public List<IRateLimiter> RateLimiters { get; set; } = new List<IRateLimiter>();

        /// <summary>
        /// What to do when a call would exceed the rate limit
        /// </summary>
        public RateLimitingBehaviour RateLimitingBehaviour { get; set; } = RateLimitingBehaviour.Wait;

        /// <summary>
        /// The time the server has to respond to a request before timing out
        /// </summary>
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Http client to use. If a HttpClient is provided in this property the RequestTimeout and Proxy options will be ignored in requests and should be set on the provided HttpClient instance
        /// </summary>
        public HttpClient? HttpClient { get; set; }

        /// <summary>
        /// Copy the values of the def to the input
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="def"></param>
        public new void Copy<T>(T input, T def) where T : RestClientOptions
        {
            base.Copy(input, def);
                        
            input.HttpClient = def.HttpClient;
            input.RateLimiters = def.RateLimiters.ToList();
            input.RateLimitingBehaviour = def.RateLimitingBehaviour;
            input.RequestTimeout = def.RequestTimeout;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{base.ToString()}, RateLimiters: {RateLimiters.Count}, RateLimitBehaviour: {RateLimitingBehaviour}, RequestTimeout: {RequestTimeout:c}";
        }
    }

    /// <summary>
    /// Base for socket client options
    /// </summary>
    public class SocketClientOptions : ClientOptions
    {
        /// <summary>
        /// Whether or not the socket should automatically reconnect when losing connection
        /// </summary>
        public bool AutoReconnect { get; set; } = true;

        /// <summary>
        /// Time to wait between reconnect attempts
        /// </summary>
        public TimeSpan ReconnectInterval { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// The maximum number of times to try to reconnect
        /// </summary>
        public int? MaxReconnectTries { get; set; }

        /// <summary>
        /// The maximum number of times to try to resubscribe after reconnecting
        /// </summary>
        public int? MaxResubscribeTries { get; set; } = 5;

        /// <summary>
        /// Max number of concurrent resubscription tasks per socket after reconnecting a socket
        /// </summary>
        public int MaxConcurrentResubscriptionsPerSocket { get; set; } = 5;

        /// <summary>
        /// The time to wait for a socket response before giving a timeout
        /// </summary>
        public TimeSpan SocketResponseTimeout { get; set; } = TimeSpan.FromSeconds(10);
        /// <summary>
        /// The time after which the connection is assumed to be dropped. This can only be used for socket connections where a steady flow of data is expected.
        /// </summary>
        public TimeSpan SocketNoDataTimeout { get; set; }

        /// <summary>
        /// The amount of subscriptions that should be made on a single socket connection. Not all exchanges support multiple subscriptions on a single socket.
        /// Setting this to a higher number increases subscription speed because not every subscription needs to connect to the server, but having more subscriptions on a 
        /// single connection will also increase the amount of traffic on that single connection, potentially leading to issues.
        /// </summary>
        public int? SocketSubscriptionsCombineTarget { get; set; }

        /// <summary>
        /// Copy the values of the def to the input
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <param name="def"></param>
        public new void Copy<T>(T input, T def) where T : SocketClientOptions
        {
            base.Copy(input, def);

            input.AutoReconnect = def.AutoReconnect;
            input.ReconnectInterval = def.ReconnectInterval;
            input.MaxReconnectTries = def.MaxReconnectTries;
            input.MaxResubscribeTries = def.MaxResubscribeTries;
            input.MaxConcurrentResubscriptionsPerSocket = def.MaxConcurrentResubscriptionsPerSocket;
            input.SocketResponseTimeout = def.SocketResponseTimeout;
            input.SocketNoDataTimeout = def.SocketNoDataTimeout;
            input.SocketSubscriptionsCombineTarget = def.SocketSubscriptionsCombineTarget;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{base.ToString()}, AutoReconnect: {AutoReconnect}, ReconnectInterval: {ReconnectInterval}, SocketResponseTimeout: {SocketResponseTimeout:c}, SocketSubscriptionsCombineTarget: {SocketSubscriptionsCombineTarget}";
        }
    }
}
