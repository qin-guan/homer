using System.Reactive.Concurrency;
using Homer.NetDaemon.Apps.Remotes;
using Homer.NetDaemon.Entities;
using Homer.ServiceDefaults.Metrics;
using NetDaemon.AppModel;
using NetDaemon.HassModel;
using NetDaemon.HassModel.Entities;

namespace Homer.NetDaemon.Apps.DiningTable;

[NetDaemonApp]
public class DysonFan
{
    public DysonFan(IScheduler scheduler, IrRemoteLock irRemoteLock, SwitchEntities switchEntities,
        BinarySensorEntities binarySensorEntities, RemoteEntities remoteEntities)
    {
        var eventsProcessedMeter =
            EntityMetrics.MeterInstance.CreateCounter<int>("dyson_fan.events_processed");

        binarySensorEntities.PresenceSensorFp2B4c4PresenceSensor5.StateChanges()
            .WhenStateIsFor(e =>
            {
                eventsProcessedMeter.Add(1);
                return e.IsOn();
            }, TimeSpan.FromSeconds(15), scheduler)
            .SubscribeAsync(async e =>
            {
                switchEntities.LivingRoomIkeaPlug.TurnOn();
                await Task.Delay(500);
                await irRemoteLock.SemaphoreSlim.WaitAsync();
                remoteEntities.LivingRoomRemote.SendCommand("Power", "Dyson");
                await Task.Delay(500);
                irRemoteLock.SemaphoreSlim.Release();
            });

        binarySensorEntities.PresenceSensorFp2B4c4PresenceSensor5.StateChanges()
            .WhenStateIsFor(e =>
            {
                eventsProcessedMeter.Add(1);
                return e.IsOff();
            }, TimeSpan.FromMinutes(2), scheduler)
            .Subscribe(e => { switchEntities.LivingRoomIkeaPlug.TurnOff(); });
    }
}