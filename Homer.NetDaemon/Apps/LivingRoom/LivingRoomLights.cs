using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Homer.NetDaemon.Entities;
using NetDaemon.AppModel;
using NetDaemon.HassModel;
using NetDaemon.HassModel.Entities;

namespace Homer.NetDaemon.Apps.LivingRoom;

[NetDaemonApp]
public class LivingRoomLights : IAsyncInitializable
{
    private readonly SensorEntities _sensorEntities;
    private readonly InputBooleanEntities _inputBooleanEntities;
    private readonly List<BinarySensorEntity> _presenceEntities;

    private bool Presence => _presenceEntities.Any(e => e.IsOn());

    public LivingRoomLights(
        ILogger<LivingRoomLights> logger,
        IScheduler scheduler,
        IrRemoteLock irRemoteLock,
        BinarySensorEntities binarySensorEntities,
        SensorEntities sensorEntities,
        InputBooleanEntities inputBooleanEntities,
        RemoteEntities remoteEntities
    )
    {
        _sensorEntities = sensorEntities;
        _inputBooleanEntities = inputBooleanEntities;

        _presenceEntities =
        [
            binarySensorEntities.PresenceSensorFp2B4c4PresenceSensor2,
            binarySensorEntities.PresenceSensorFp2B4c4PresenceSensor3,
            binarySensorEntities.PresenceSensorFp2B4c4PresenceSensor4,
            binarySensorEntities.PresenceSensorFp2B4c4PresenceSensor5,
            binarySensorEntities.PresenceSensorFp2B4c4PresenceSensor7,
            binarySensorEntities.PresenceSensorFp2B4c4PresenceSensor8,
        ];

        var presenceObservables = _presenceEntities.Select(e => e.StateChanges()).Merge();

        presenceObservables
            .Where(e =>
            {
                logger.LogDebug("Living room presence state changed: {Presence}", Presence);
                return Presence;
            })
            .Subscribe(_ => { inputBooleanEntities.LivingRoomFanLights.TurnOn(); });

        presenceObservables
            .WhenStateIsFor(e =>
            {
                logger.LogDebug("Living room presence state changed: {Presence}", Presence);
                return !Presence;
            }, TimeSpan.FromMinutes(1), scheduler)
            .Subscribe(_ => { inputBooleanEntities.LivingRoomFanLights.TurnOff(); });

        // sensorEntities.PresenceSensorFp2B4c4LightSensorLightLevel.StateChanges()
        //     .WhenStateIsFor(e => e?.State > 40, TimeSpan.FromMinutes(2), scheduler)
        //     .Subscribe(e =>
        //     {
        //         logger.LogDebug("Living room light level too high: {State}", e.New?.State);
        //         _inputBooleanEntities.LivingRoomFanLights.TurnOff();
        //     });
        //
        // sensorEntities.PresenceSensorFp2B4c4LightSensorLightLevel.StateChanges()
        //     .WhenStateIsFor(e => e?.State < 40, TimeSpan.FromMinutes(2), scheduler)
        //     .Subscribe(e =>
        //     {
        //         logger.LogDebug("Living room light level too low: {State}", e.New?.State);
        //
        //         if (_presenceEntities.Any(entity => entity.IsOn()))
        //         {
        //             PresenceDetected();
        //         }
        //     });

        inputBooleanEntities.LivingRoomFanLights.StateChanges()
            .SubscribeAsync(async _ =>
            {
                await irRemoteLock.SemaphoreSlim.WaitAsync();
                await Task.Delay(1500);
                remoteEntities.LivingRoomRemote.SendCommand("Light Power", "Living Room KDK");
                await Task.Delay(1500);
                irRemoteLock.SemaphoreSlim.Release();
            });
    }

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (!Presence)
        {
            _inputBooleanEntities.LivingRoomFanLights.TurnOff();
        }

        return Task.CompletedTask;
    }
}