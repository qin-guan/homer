using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Homer.NetDaemon.Entities;
using Homer.NetDaemon.Helpers;
using NetDaemon.AppModel;
using NetDaemon.Extensions.Scheduler;
using NetDaemon.HassModel;
using NetDaemon.HassModel.Entities;

namespace Homer.NetDaemon.Apps.Kitchen;

[Focus]
[NetDaemonApp]
public class KitchenLights : IAsyncInitializable
{
    private readonly List<BinarySensorEntity> _triggerEntities;
    private readonly List<BinarySensorEntity> _presenceEntities;
    private readonly List<SwitchEntity> _lights;
    private readonly List<SwitchEntity> _nightLights;
    private readonly NumericSensorEntity _lightSensor;

    private bool Presence => _presenceEntities.Any(e => e.IsOn());
    private bool TriggerPresence => _triggerEntities.Any(e => e.IsOn());
    private bool IsNight => TimeHelpers.TimeNow > new TimeOnly(21, 30) || TimeHelpers.TimeNow < new TimeOnly(6, 20);

    private const string NightStartCron = "30 21 * * *";
    private const string NightEndCron = "20 6 * * *";

    public KitchenLights(
        ILogger<KitchenLights> logger,
        IScheduler scheduler,
        BinarySensorEntities binarySensorEntities,
        SwitchEntities switchEntities,
        SensorEntities sensorEntities
    )
    {
        _triggerEntities =
        [
            binarySensorEntities.PresenceSensorFp2B4c4PresenceSensor6
        ];

        _presenceEntities =
        [
            binarySensorEntities.PresenceSensorFp2B4c4PresenceSensor6,
            binarySensorEntities.KitchenTuyaPresencePresence
        ];

        _lights =
        [
            switchEntities.KitchenLightsRight,
        ];

        _nightLights =
        [
            switchEntities.KitchenLightsLeft,
        ];

        _lightSensor = sensorEntities.KitchenTuyaPresenceIlluminanceLux;

        var triggerObservables = _triggerEntities.Select(e => e.StateChanges()).Merge();
        var presenceObservables = _presenceEntities.Select(e => e.StateChanges()).Merge();

        scheduler.ScheduleCron(NightStartCron, () =>
        {
            if (!_lights.Any(e => e.IsOn())) return;

            _nightLights.TurnOn();
            _lights.TurnOff();
        });

        scheduler.ScheduleCron(NightEndCron, () =>
        {
            if (!_nightLights.Any(e => e.IsOn())) return;

            _lights.TurnOn();
            _nightLights.TurnOff();
        });

        _lightSensor.StateChanges()
            .WhenStateIsFor(
                _ => _lights.Any(entity => entity.IsOn()) && _lightSensor.State > 1100,
                TimeSpan.FromSeconds(5),
                scheduler
            )
            .Subscribe(_ =>
            {
                Console.WriteLine("turned off here");
                _lights.TurnOff();
                _nightLights.TurnOff();
            });

        triggerObservables
            .Where(e => TriggerPresence)
            .Subscribe(_ =>
            {
                if (_lightSensor.State > 1100) return;

                if (IsNight)
                {
                    _nightLights.TurnOn();
                }
                else
                {
                    _lights.TurnOn();
                }
            });

        presenceObservables
            .Where(e => !Presence)
            .Subscribe(_ =>
            {
                _lights.TurnOff();
                _nightLights.TurnOff();
            });
    }

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (Presence) return Task.CompletedTask;

        _lights.TurnOff();
        _nightLights.TurnOff();

        return Task.CompletedTask;
    }
}