using System.Reactive.Concurrency;
using System.Text.Json;
using Homer.ServiceDefaults.Metrics;
using NetDaemon.AppModel;
using NetDaemon.HassModel;

namespace Homer.NetDaemon.Apps;

[Focus]
[NetDaemonApp]
public class DefaultApp
{
    public DefaultApp(ILogger<DefaultApp> logger, IHaContext haContext, IScheduler scheduler)
    {
        var eventsProcessedMeter =
            EntityMetrics.MeterInstance.CreateCounter<int>("homer.netdaemon.homeassistant.events_processed");

        haContext.Events.Subscribe(e =>
        {
            if (e.DataElement.HasValue)
            {
                var val = e.DataElement.GetValueOrDefault();
                var tags = new List<KeyValuePair<string, object?>>();

                if (val.TryGetProperty("entity_id", out var entityId))
                {
                    tags.Add(new KeyValuePair<string, object?>("ha.entity_id", entityId.GetString()));
                }

                if (val.TryGetProperty("new_state", out var state))
                {
                    if (state.ValueKind == JsonValueKind.Object &&
                        state.TryGetProperty("attributes", out var attributes))
                    {
                        if (attributes.ValueKind == JsonValueKind.Object &&
                            attributes.TryGetProperty("friendly_name", out var friendlyName))
                        {
                            if (friendlyName.GetString() is not null)
                            {
                                tags.Add(
                                    new KeyValuePair<string, object?>("ha.friendly_name", friendlyName.GetString()));
                            }
                        }
                    }

                    if (state.ValueKind == JsonValueKind.Object && state.TryGetProperty("context", out var context))
                    {
                        if (context.ValueKind == JsonValueKind.Object &&
                            context.TryGetProperty("user_id", out var userIdElement))
                        {
                            if (userIdElement.GetString() is not null)
                            {
                                tags.Add(
                                    new KeyValuePair<string, object?>("ha.user_id", userIdElement.GetString()));
                            }
                        }
                    }
                }

                eventsProcessedMeter.Add(1, tags.ToArray());
            }
            else
            {
                eventsProcessedMeter.Add(1);
            }
        });

        logger.LogInformation("Hello, home!");
    }
}