using EasyNetQ;
using Monitoring;
using PublisherService.BLL;
using PublisherService.BLL.Interfaces;
using PublisherService.DAL;
using PublisherService.DAL.Interfaces;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

MonitorService.Configure(builder, "PublisherService");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IBus>(_ =>
    RabbitHutch.CreateBus(
        builder.Configuration.GetConnectionString("RabbitMQ")
        ?? throw new InvalidOperationException(
            "Missing RabbitMQ connection string.")));

builder.Services.AddScoped<IArticlePublisher, ArticlePublisher>();
builder.Services.AddScoped<IPublisherLogic, PublisherLogic>();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMonitoringExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();