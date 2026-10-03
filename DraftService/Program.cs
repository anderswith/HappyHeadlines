using System.Diagnostics;
using DraftService.BLL;
using DraftService.BLL.Interfaces;
using DraftService.DAL;
using DraftService.DAL.Repositories;
using DraftService.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Monitoring;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

MonitorService.Configure(builder, "DraftService");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<DraftContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DraftDatabase"),
        postgres => postgres.CommandTimeout(5));
});

builder.Services.AddScoped<IDraftRepository, DraftRepository>();
builder.Services.AddScoped<IDraftLogic, DraftLogic>();

var app = builder.Build();

// Logger HTTP-metode, sti, statuskode og varighed.
app.UseSerilogRequestLogging();

// Fælles håndtering af exceptions fra controller/BLL/DAL.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features
            .Get<IExceptionHandlerFeature>()?.Error;

        if (exception is ArgumentException)
        {
            MonitorService.Log.Warning(
                "Draft request rejected: {Reason}",
                exception.Message);

            context.Response.StatusCode = 400;

            await context.Response.WriteAsJsonAsync(new
            {
                Message = exception.Message
            });

            return;
        }

        MonitorService.Log.Error(
            exception,
            "Unexpected error handling {Method} {Path}",
            context.Request.Method,
            context.Request.Path);

        Activity.Current?.SetStatus(
            ActivityStatusCode.Error,
            "Request failed");

        context.Response.StatusCode = 500;

        await context.Response.WriteAsJsonAsync(new
        {
            Message = "An unexpected error occurred.",
            TraceId = Activity.Current?.TraceId.ToString()
        });
    });
});

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();