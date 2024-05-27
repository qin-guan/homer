using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Homer.NetDaemon.Apps.Core;
using Homer.NetDaemon.Entities;
using NetDaemon.AppModel;
using NetDaemon.HassModel;
using NetDaemon.HassModel.Entities;

namespace Homer.NetDaemon.Apps.Bathroom;

[NetDaemonApp]
public class BathroomPresence : Occupancy
{
    private readonly List<NumericSensorEntity> _lightSensors;
    private readonly SwitchEntities _switchEntities;
    private bool TooBright => _lightSensors.Min(e => e.State) > 25;

    private List<SwitchEntity> Switches
    {
        get
        {
            return TimeOnly.FromDateTime(DateTime.Now) switch
            {
                var value when value >= new TimeOnly(0, 0) && value < new TimeOnly(6, 0) =>
                [
                    _switchEntities.BathroomLightsRight
                ],
                
                var value when value >= new TimeOnly(6, 0) && value < new TimeOnly(18, 0) =>
                [
                    _switchEntities.BathroomLightsCenter
                ],
                
                var value when value >= new TimeOnly(18, 0) && value < new TimeOnly(0, 0) =>
                [
                    _switchEntities.BathroomLightsCenter, _switchEntities.BathroomLightsLeft
                ],
                
                _ => [_switchEntities.BathroomLightsRight]
            };
        }
    }

    public BathroomPresence(
        IScheduler scheduler,
        SensorEntities sensorEntities,
        InputDatetimeEntities inputDatetimeEntities,
        InputBooleanEntities inputBooleanEntities,
        BinarySensorEntities contactSensors,
        BinarySensorEntities motionSensors,
        SwitchEntities switchEntities
    ) : base(
        inputDatetimeEntities.BathroomLastPresence,
        inputBooleanEntities.BathroomPresence,
        [contactSensors.BathroomContactSensorContact],
        [motionSensors.BathroomDoorMotionOccupancy, motionSensors.BathroomSinkMotionOccupancy],
        TimeSpan.FromSeconds(22)
    )
    {
        _switchEntities = switchEntities;

        _lightSensors =
        [
            sensorEntities.BathroomSinkMotionIlluminanceLux,
            sensorEntities.BathroomDoorMotionIlluminanceLux
        ];

        inputBooleanEntities.BathroomPresence.StateChanges()
            .Where(e => e.Entity.IsOn())
            .Subscribe(_ =>
            {
                if (TooBright) return;

                foreach (var light in Switches)
                {
                    light.TurnOn();
                }
            });

        inputBooleanEntities.BathroomPresence.StateChanges()
            .WhenStateIsFor(e => e.IsOff(), TimeSpan.FromMinutes(1), scheduler)
            .Subscribe(_ =>
            {
                switchEntities.BathroomLightsCenter.TurnOff();
                switchEntities.BathroomLightsLeft.TurnOff();
                switchEntities.BathroomLightsRight.TurnOff();
            });
    }
}