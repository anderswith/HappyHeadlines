using EasyNetQ;
using Microsoft.EntityFrameworkCore;
using Monitoring;
using NewsletterService.BLL;
using NewsletterService.BLL.Interfaces;
using NewsletterService.Clients;
using NewsletterService.Clients.Interfaces;
using NewsletterService.DAL;
using NewsletterService.DAL.Repositories;
using NewsletterService.DAL.Repositories.Interfaces;
using NewsletterService.Messaging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

MonitorService.Configure(builder, "NewsletterService");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<NewsletterContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString(
            "NewsletterDatabase"),
        postgres => postgres.CommandTimeout(5)));

builder.Services.AddScoped<
    ISubscriberRepository,
    SubscriberRepository>();

builder.Services.AddScoped<INewsletterLogic, NewsletterLogic>();
builder.Services.AddScoped<IEmailSender, EmailSender>();

builder.Services.AddHttpClient<IArticleClient, ArticleClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ArticleService:BaseUrl"]
        ?? throw new InvalidOperationException(
            "Missing ArticleService base URL."));

    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddSingleton<IBus>(_ =>
    RabbitHutch.CreateBus(
        builder.Configuration.GetConnectionString("RabbitMQ")
        ?? throw new InvalidOperationException(
            "Missing RabbitMQ connection string.")));

builder.Services.AddHostedService<NewsletterConsumer>();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMonitoringExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();