﻿using CryptoExchange.Net.Attributes;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Logging;
using CryptoExchange.Net.Objects;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CryptoExchange.Net
{
    /// <summary>
    /// The base for all clients, websocket client and rest client
    /// </summary>
    public abstract class BaseClient : IDisposable
    {
        /// <summary>
        /// The name of the exchange the client is for
        /// </summary>
        internal string ExchangeName { get; }
        /// <summary>
        /// The log object
        /// </summary>
        protected internal Log log;
        /// <summary>
        /// The authentication provider when api credentials have been provided
        /// </summary>
        protected internal AuthenticationProvider? authProvider;
        /// <summary>
        /// The last used id, use NextId() to get the next id and up this
        /// </summary>
        protected static int lastId;
        /// <summary>
        /// Lock for id generating
        /// </summary>
        protected static object idLock = new object();

        /// <summary>
        /// A default serializer
        /// </summary>
        private static readonly JsonSerializer defaultSerializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            Culture = CultureInfo.InvariantCulture
        });

        /// <summary>
        /// Provided client options
        /// </summary>
        public ClientOptions ClientOptions { get; }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="exchangeName">The name of the exchange this client is for</param>
        /// <param name="options">The options for this client</param>
        /// <param name="authenticationProvider">The authentication provider for this client (can be null if no credentials are provided)</param>
        protected BaseClient(string exchangeName, ClientOptions options, AuthenticationProvider? authenticationProvider)
        {
            log = new Log(exchangeName);
            authProvider = authenticationProvider;
            log.UpdateWriters(options.LogWriters);
            log.Level = options.LogLevel;

            ClientOptions = options;

            ExchangeName = exchangeName;

            log.Write(LogLevel.Debug, $"Client configuration: {options}, CryptoExchange.Net: v{typeof(BaseClient).Assembly.GetName().Version}, {ExchangeName}.Net: v{GetType().Assembly.GetName().Version}");
        }

        /// <summary>
        /// Set the authentication provider, can be used when manually setting the API credentials
        /// </summary>
        /// <param name="authenticationProvider"></param>
        protected void SetAuthenticationProvider(AuthenticationProvider authenticationProvider)
        {
            log.Write(LogLevel.Debug, "Setting api credentials");
            authProvider = authenticationProvider;
        }

        /// <summary>
        /// Tries to parse the json data and return a JToken, validating the input not being empty and being valid json
        /// </summary>
        /// <param name="data">The data to parse</param>
        /// <returns></returns>
        protected CallResult<JToken> ValidateJson(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                var info = "Empty data object received";
                log.Write(LogLevel.Error, info);
                return new CallResult<JToken>(null, new DeserializeError(info, data));
            }

            try
            {
                return new CallResult<JToken>(JToken.Parse(data), null);
            }
            catch (JsonReaderException jre)
            {
                var info = $"Deserialize JsonReaderException: {jre.Message}, Path: {jre.Path}, LineNumber: {jre.LineNumber}, LinePosition: {jre.LinePosition}";
                return new CallResult<JToken>(null, new DeserializeError(info, data));
            }
            catch (JsonSerializationException jse)
            {
                var info = $"Deserialize JsonSerializationException: {jse.Message}";
                return new CallResult<JToken>(null, new DeserializeError(info, data));
            }
            catch (Exception ex)
            {
                var exceptionInfo = ex.ToLogString();
                var info = $"Deserialize Unknown Exception: {exceptionInfo}";
                return new CallResult<JToken>(null, new DeserializeError(info, data));
            }
        }

        /// <summary>
        /// Deserialize a string into an object
        /// </summary>
        /// <typeparam name="T">The type to deserialize into</typeparam>
        /// <param name="data">The data to deserialize</param>
        /// <param name="serializer">A specific serializer to use</param>
        /// <param name="requestId">Id of the request the data is returned from (used for grouping logging by request)</param>
        /// <returns></returns>
        protected CallResult<T> Deserialize<T>(string data, JsonSerializer? serializer = null, int? requestId = null)
        {
            var tokenResult = ValidateJson(data);
            if (!tokenResult)
            {
                log.Write(LogLevel.Error, tokenResult.Error!.Message);
                return new CallResult<T>(default, tokenResult.Error);
            }

            return Deserialize<T>(tokenResult.Data, serializer, requestId);
        }

        /// <summary>
        /// Deserialize a JToken into an object
        /// </summary>
        /// <typeparam name="T">The type to deserialize into</typeparam>
        /// <param name="obj">The data to deserialize</param>
        /// <param name="serializer">A specific serializer to use</param>
        /// <param name="requestId">Id of the request the data is returned from (used for grouping logging by request)</param>
        /// <returns></returns>
        protected CallResult<T> Deserialize<T>(JToken obj, JsonSerializer? serializer = null, int? requestId = null)
        {
            if (serializer == null)
                serializer = defaultSerializer;

            try
            {
                return new CallResult<T>(obj.ToObject<T>(serializer), null);
            }
            catch (JsonReaderException jre)
            {
                var info = $"{(requestId != null ? $"[{requestId}] " : "")}Deserialize JsonReaderException: {jre.Message} Path: {jre.Path}, LineNumber: {jre.LineNumber}, LinePosition: {jre.LinePosition}, data: {obj}";
                log.Write(LogLevel.Error, info);
                return new CallResult<T>(default, new DeserializeError(info, obj));
            }
            catch (JsonSerializationException jse)
            {
                var info = $"{(requestId != null ? $"[{requestId}] " : "")}Deserialize JsonSerializationException: {jse.Message} data: {obj}";
                log.Write(LogLevel.Error, info);
                return new CallResult<T>(default, new DeserializeError(info, obj));
            }
            catch (Exception ex)
            {
                var exceptionInfo = ex.ToLogString();
                var info = $"{(requestId != null ? $"[{requestId}] " : "")}Deserialize Unknown Exception: {exceptionInfo}, data: {obj}";
                log.Write(LogLevel.Error, info);
                return new CallResult<T>(default, new DeserializeError(info, obj));
            }
        }

        /// <summary>
        /// Deserialize a stream into an object
        /// </summary>
        /// <typeparam name="T">The type to deserialize into</typeparam>
        /// <param name="stream">The stream to deserialize</param>
        /// <param name="serializer">A specific serializer to use</param>
        /// <param name="requestId">Id of the request the data is returned from (used for grouping logging by request)</param>
        /// <param name="elapsedMilliseconds">Milliseconds response time for the request this stream is a response for</param>
        /// <returns></returns>
        protected async Task<CallResult<T>> DeserializeAsync<T>(Stream stream, JsonSerializer? serializer = null, int? requestId = null, long? elapsedMilliseconds = null)
        {
            if (serializer == null)
                serializer = defaultSerializer;

            try
            {
                // Let the reader keep the stream open so we're able to seek if needed. The calling method will close the stream.
                using var reader = new StreamReader(stream, Encoding.UTF8, false, 512, true);

                // If we have to output the original json data or output the data into the logging we'll have to read to full response
                // in order to log/return the json data
                if (ClientOptions.OutputOriginalData || log.Level <= LogLevel.Debug)
                {
                    var data = await reader.ReadToEndAsync().ConfigureAwait(false);
                    log.Write(LogLevel.Debug, $"{(requestId != null ? $"[{requestId}] ": "")}Response received{(elapsedMilliseconds != null ? $" in {elapsedMilliseconds}" : " ")}ms: {data}");
                    var result = Deserialize<T>(data, serializer, requestId);
                    if(ClientOptions.OutputOriginalData)
                        result.OriginalData = data;
                    return result;
                }
                
                // If we don't have to keep track of the original json data we can use the JsonTextReader to deserialize the stream directly
                // into the desired object, which has increased performance over first reading the string value into memory and deserializing from that
                using var jsonReader = new JsonTextReader(reader);
                return new CallResult<T>(serializer.Deserialize<T>(jsonReader), null);
            }
            catch (JsonReaderException jre)
            {
                string data;
                if (stream.CanSeek)
                {
                    // If we can seek the stream rewind it so we can retrieve the original data that was sent
                    stream.Seek(0, SeekOrigin.Begin);
                    data = await ReadStreamAsync(stream).ConfigureAwait(false);
                }
                else
                    data = "[Data only available in Debug LogLevel]";
                log.Write(LogLevel.Error, $"{(requestId != null ? $"[{requestId}] " : "")}Deserialize JsonReaderException: {jre.Message}, Path: {jre.Path}, LineNumber: {jre.LineNumber}, LinePosition: {jre.LinePosition}, data: {data}");
                return new CallResult<T>(default, new DeserializeError($"Deserialize JsonReaderException: {jre.Message}, Path: {jre.Path}, LineNumber: {jre.LineNumber}, LinePosition: {jre.LinePosition}", data));
            }
            catch (JsonSerializationException jse)
            {
                string data;
                if (stream.CanSeek)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    data = await ReadStreamAsync(stream).ConfigureAwait(false);
                }
                else
                    data = "[Data only available in Debug LogLevel]";

                log.Write(LogLevel.Error, $"{(requestId != null ? $"[{requestId}] " : "")}Deserialize JsonSerializationException: {jse.Message}, data: {data}");
                return new CallResult<T>(default, new DeserializeError($"Deserialize JsonSerializationException: {jse.Message}", data));
            }
            catch (Exception ex)
            {
                string data;
                if (stream.CanSeek) { 
                    stream.Seek(0, SeekOrigin.Begin);
                    data = await ReadStreamAsync(stream).ConfigureAwait(false);
                }
                else
                    data = "[Data only available in Debug LogLevel]";

                var exceptionInfo = ex.ToLogString();
                log.Write(LogLevel.Error, $"{(requestId != null ? $"[{requestId}] " : "")}Deserialize Unknown Exception: {exceptionInfo}, data: {data}");
                return new CallResult<T>(default, new DeserializeError($"Deserialize Unknown Exception: {exceptionInfo}", data));
            }
        }

        private async Task<string> ReadStreamAsync(Stream stream)
        { 
            using var reader = new StreamReader(stream, Encoding.UTF8, false, 512, true);
            return await reader.ReadToEndAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Generate a new unique id. The id is staticly stored so it is guarenteed to be unique across different client instances
        /// </summary>
        /// <returns></returns>
        protected int NextId()
        {
            lock (idLock)
            {
                lastId += 1;
                return lastId;
            }
        }

        /// <summary>
        /// Fill parameters in a path. Parameters are specified by '{}' and should be specified in occuring sequence
        /// </summary>
        /// <param name="path">The total path string</param>
        /// <param name="values">The values to fill</param>
        /// <returns></returns>
        protected static string FillPathParameter(string path, params string[] values)
        {
            foreach (var value in values)
            {
                var index = path.IndexOf("{}", StringComparison.Ordinal);
                if (index >= 0)
                {
                    path = path.Remove(index, 2);
                    path = path.Insert(index, value);
                }
            }
            return path;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public virtual void Dispose()
        {
            authProvider?.Credentials?.Dispose();
            log.Write(LogLevel.Debug, "Disposing exchange client");
        }
    }
}
