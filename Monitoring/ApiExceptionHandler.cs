using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Monitoring;

public static class ApiExceptionHandler
{
    public static void UseMonitoringExceptionHandler(
        this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exception = context.Features
                    .Get<IExceptionHandlerFeature>()?.Error;

                var isValidationError =
                    exception is ArgumentException;

                context.Response.StatusCode =
                    isValidationError ? 400 : 500;

                if (isValidationError)
                {
                    MonitorService.Log.Warning(
                        "Request rejected: {Reason}",
                        exception!.Message);
                }
                else
                {
                    Activity.Current?.SetStatus(
                        ActivityStatusCode.Error);

                    MonitorService.Log.Error(
                        exception,
                        "Request failed: {Method} {Path}",
                        context.Request.Method,
                        context.Request.Path);
                }

                await context.Response.WriteAsJsonAsync(new
                {
                    Message = isValidationError
                        ? exception!.Message
                        : "An unexpected error occurred.",
                    TraceId = Activity.Current?.TraceId.ToString()
                });
            });
        });
    }
}