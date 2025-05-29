using System.Globalization;
using HeadBower.Modules;
using NITHlibrary.Nith.Internals;
using NITHlibrary.Tools.Filters.ValueFilters;
using NITHlibrary.Tools.Mappers;
using static System.Double;

namespace HeadBower.OLD.Behaviors.Headtracker
{
    public class NithSensorBehaviorYawPlay(double pressureMultiplier = 1.0f, double velocityMultiplier = 1.0f)
        : INithSensorBehavior
    {
        private const double Deadspeed = 5f;
        private readonly SegmentMapper _pressureMapper = new(0, 127f, 0, 127);
        private readonly DoubleFilterMAexpDecaying _speedFilter = new(0.8f);
        private int _currentDirection = 1;
        private int _previousDirection = 1;
        private double _yawSpeed = 0;
        private double _yawSpeedFiltered;

        ///<summary>
        ///Handles the NithSensorData by checking the associated control mode and calling the HTStrum_ElaboratePosition method if the NithSensorData contains the "acc_yaw" argument.
        ///</summary>
        ///<param name="nithData">The NithSensorData object to handle.</param>
        public void HandleData(NithSensorData nithData)
        {
            if (nithData.ContainsParameter(NithParameters.head_acc_yaw))
            {
                HTStrum_ElaboratePosition(nithData);
            }
        }

        public void HTStrum_ElaboratePosition(NithSensorData nithData)
        {
            _yawSpeed = Parse(nithData.GetParameterValue(NithParameters.head_acc_yaw).Value.Value, CultureInfo.InvariantCulture);
            _previousDirection = _currentDirection;
            _currentDirection = Math.Sign(_yawSpeed);
            _yawSpeed = Math.Abs(_yawSpeed);
            _yawSpeed *= pressureMultiplier;
            _yawSpeed = Math.Clamp(_yawSpeed - Deadspeed, 0f, PositiveInfinity);

            // Perché tutto 'sto casino?
            _speedFilter.Push(_yawSpeed);
            _yawSpeedFiltered = Math.Abs(_pressureMapper.Map(_speedFilter.Pull()));
            // _yawSpeedFiltered = Math.Log(_yawSpeedFiltered, 1.5f) * pressureMultiplier;
            

            Rack.MappingModule.Pressure = (int)_yawSpeedFiltered;
            Rack.MappingModule.InputIndicatorValue = (int)_yawSpeedFiltered;
            Rack.MappingModule.Velocity = (int) (_yawSpeedFiltered * velocityMultiplier);

            if (_yawSpeedFiltered <= 0)
            {
                Rack.MappingModule.Blow = false;
            }
            else if (_yawSpeedFiltered > 0)
            {
                Rack.MappingModule.Blow = true;
            }

            //if (_currentDirection != _previousDirection && _yawSpeedFiltered > 0)
            //{
            //    Rack.NetytarDmiBox.Velocity = (int)(_yawSpeedFiltered * velocityMultiplier);
            //    Rack.NetytarDmiBox.Blow = false;
            //    Rack.NetytarDmiBox.Blow = true;
            //}
        }
    }
}