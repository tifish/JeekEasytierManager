using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using MagicOnion.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace JeekEasyTierManager;

public class RemoteCall
{
    public static void StartServer(string url)
    {
        var builder = WebApplication.CreateBuilder([]);

        builder.WebHost.ConfigureKestrel(options =>
        {
            // WORKAROUND: Accept HTTP/2 only to allow insecure HTTP/2 connections during development.
            options.ConfigureEndpointDefaults(endpointOptions =>
            {
                endpointOptions.Protocols = HttpProtocols.Http2;
            });
        });

        builder.Services.AddMagicOnion();

        var app = builder.Build();

        app.MapMagicOnionService();

        app.RunAsync(url);
    }

    private static Dictionary<string, ISyncService> _cachedClients = [];

    public static async Task<ISyncService?> GetClient(string url)
    {
        // Ping to check if the server is running
        try
        {
            if (!_cachedClients.TryGetValue(url, out var client))
            {
                var channel = GrpcChannel.ForAddress(url);
                var invoker = channel.Intercept(new AuthInterceptor());
                client = MagicOnionClient.Create<ISyncService>(invoker);
            }

            var result = await client.WithDeadline(DateTime.UtcNow.AddSeconds(1)).Ping();
            if (!result)
                return null;

            _cachedClients.TryAdd(url, client);
            return client;
        }
        catch
        {
            return null;
        }
    }

    public class AuthInterceptor : Interceptor
    {
        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var headers = context.Options.Headers ?? [];
            headers.Add("authorization", Settings.SyncPassword);

            var newContext = new ClientInterceptorContext<TRequest, TResponse>(
                context.Method,
                context.Host,
                new CallOptions(headers,
                    context.Options.Deadline,
                    context.Options.CancellationToken,
                    context.Options.WriteOptions,
                    context.Options.PropagationToken,
                    context.Options.Credentials));

            return base.AsyncUnaryCall(request, newContext, continuation);
        }
    }
}