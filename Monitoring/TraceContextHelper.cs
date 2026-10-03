using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Monitoring;

public static class TraceContextHelper
{
    private static readonly TraceContextPropagator Propagator = new();

    public static void Inject(Dictionary<string, string> header)
    {
        var activity = Activity.Current;

        if (activity is null)
        {
            return;
        }

        var propagationContext = new PropagationContext(
            activity.Context,
            default
        );

        Propagator.Inject(
            propagationContext,
            header,
            (carrier, key, value) =>
            {
                carrier[key] = value;
            }
        );
    }

    public static ActivityContext Extract(
        Dictionary<string, string> header)
    {
        var propagationContext = Propagator.Extract(
            default,
            header,
            (carrier, key) =>
            {
                if (carrier.TryGetValue(key, out var value))
                {
                    return new[] { value };
                }

                return Array.Empty<string>();
            }
        );

        return propagationContext.ActivityContext;
    }
}