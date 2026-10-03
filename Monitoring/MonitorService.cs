using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Enrichers.Span;

namespace Monitoring;

public static class MonitorService
{
    private const string SourceName = "Monitoring";

    public static readonly ActivitySource ActivitySource =
        new(SourceName);

    public static Serilog.ILogger Log => Serilog.Log.Logger;

    public static void Configure(
        WebApplicationBuilder builder,
        string serviceName)
    {
        var seqUrl =
            builder.Configuration["Monitoring:SeqUrl"]
            ?? "http://seq:5341";

        var zipkinEndpoint =
            builder.Configuration["Monitoring:ZipkinEndpoint"]
            ?? "http://zipkin:9411/api/v2/spans";

        var instance =
            Environment.GetEnvironmentVariable("HOSTNAME")
            ?? Environment.MachineName;

        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .MinimumLevel.Information()
                .MinimumLevel.Override(
                    "Microsoft",
                    LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", serviceName)
                .Enrich.WithProperty("ServiceInstance", instance)
                .Enrich.WithSpan()
                .WriteTo.Console(new JsonFormatter())
                .WriteTo.Seq(seqUrl);
        });

        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(
                    serviceName: serviceName,
                    serviceInstanceId: instance);
            })
            .WithTracing(tracing =>
            {
                tracing
                    .SetSampler(new AlwaysOnSampler())
                    .AddSource(SourceName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddZipkinExporter(options =>
                    {
                        options.Endpoint = new Uri(zipkinEndpoint);
                    });
            });
    }
}