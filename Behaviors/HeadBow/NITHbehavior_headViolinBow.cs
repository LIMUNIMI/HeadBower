// Behaviors\HeadBow\NITHbehavior_headViolinBow_yawAndBend.cs
using HeadBower.Modules;
using NITHlibrary.Nith.Internals;
using NITHlibrary.Tools.Filters.ValueFilters;
using NITHlibrary.Tools.Mappers;

namespace HeadBower.Behaviors.HeadBow
{
    /// <summary>
    /// Imitates a violin bow using head movements (yaw acceleration).
    /// Modulation and bow pressure can be controlled through different sources (head pitch, mouth aperture, head roll).
    /// </summary>
    internal class NITHbehavior_HeadViolinBow : INithSensorBehavior
    {
        // Enumeration of different operating modes
        public enum WhatDoesPitchRotationDo
        {
            PitchBend,
            Modulation,
            BowHeight
        }
        // Property to control the mode
        public WhatDoesPitchRotationDo PitchEffect { get; set; } = WhatDoesPitchRotationDo.Modulation;

        // Sensitivity property
        public float Sensitivity { get; set; } = 1.0f;

        // Require only pitch; yaw position is optional (may use acceleration/velocity instead)
        private readonly List<NithParameters> requiredParams = new List<NithParameters>
        {
            NithParameters.head_pos_pitch
        };

        // Constants and thresholds
        private const double YAW_UPPERTHRESH_BASE = 2;
        private const double YAW_LOWERTHRESH_BASE = 1;
        private const int DIRECTION_CHANGE_PAUSE_MS = 2;

        // New constant for pitch bend mapping
        private const double MAX_PITCH_DEVIATION = 50.0; // Maximum expected deviation
        private const double PITCH_BEND_THRESHOLD = 15.0; // Minimum threshold to start applying pitch bend
        
        // Constants for mouth aperture and head roll
        private const double MAX_MOUTH_APERTURE = 100.0; // 0-100 range from webcam wrapper
        private const double MOUTH_APERTURE_THRESHOLD = 15.0; // Minimum threshold
        private const double MAX_ROLL_DEVIATION = 50.0; // Maximum expected roll deviation
        private const double ROLL_THRESHOLD = 15.0; // Minimum threshold for roll

        // Filters and mappers
        // NOTE: yaw velocity filtering removed - use raw values from sensor (or acceleration)
        private readonly SegmentMapper _yawVelMapper = new SegmentMapper(1, 10, 1, 127); // Modified to start mapping from threshold value
        private readonly DoubleFilterMAexpDecaying _pitchPosFilter = new DoubleFilterMAexpDecaying(0.9f);
        private readonly DoubleFilterMAexpDecaying _rollPosFilter = new DoubleFilterMAexpDecaying(0.9f);
        private readonly DoubleFilterMAexpDecaying _mouthApertureFilter = new DoubleFilterMAexpDecaying(0.7f);
        private readonly DoubleFilterMAexpDecaying _yawPosFilter = new DoubleFilterMAexpDecaying(0.9f);
        private readonly SegmentMapper _pitchBendMapper = new SegmentMapper(-1.0, 1.0, -1.0, 1.0, true);
        private readonly SegmentMapper _bowPositionMapper = new SegmentMapper(-50.0, 50.0, -1.0, 1.0, true);
        // Mappers for modulation and bow height (CC 09)
        private readonly SegmentMapper _modulationMapperPitch = new SegmentMapper(PITCH_BEND_THRESHOLD, MAX_PITCH_DEVIATION, 0, 127, true);
        private readonly SegmentMapper _modulationMapperMouth = new SegmentMapper(MOUTH_APERTURE_THRESHOLD, MAX_MOUTH_APERTURE, 0, 127, true);
        private readonly SegmentMapper _modulationMapperRoll = new SegmentMapper(ROLL_THRESHOLD, MAX_ROLL_DEVIATION, 0, 127, true);
        private readonly SegmentMapper _bowHeightMapper = new SegmentMapper(-MAX_PITCH_DEVIATION, MAX_PITCH_DEVIATION, 0, 127, true);
        private readonly SegmentMapper _bowPressureMapperPitch = new SegmentMapper(PITCH_BEND_THRESHOLD, MAX_PITCH_DEVIATION, 0, 127, true);
        private readonly SegmentMapper _bowPressureMapperMouth = new SegmentMapper(MOUTH_APERTURE_THRESHOLD, MAX_MOUTH_APERTURE, 0, 127, true);

        // State
        private int _currentDirection = 0;
        private int _previousDirection = 0;
        private double _yawMagnitude = 0; // raw magnitude (no smoothing)
        private double _filteredYawPos = 0;
        private DateTime _lastDirectionChangeTime = DateTime.MinValue;

        private readonly double vibrationDivider = 1.5f; // Vibration intensity divider

        // Vibration
        private DateTime _lastVibrationTime = DateTime.MinValue;
        private readonly int VIBRATION_INTERVAL_MS = 15;
        private readonly SegmentMapper _vibrationMapper = new SegmentMapper(1, 50, 0, 250);

        public NITHbehavior_HeadViolinBow(WhatDoesPitchRotationDo operationMode = WhatDoesPitchRotationDo.PitchBend)
        {
            PitchEffect = operationMode;
        }

        public void HandleData(NithSensorData nithData)
        {
            if (nithData.ContainsParameters(requiredParams))
            {
                // 1. Get values from sensors
                // Prefer acceleration if present (produced by phone wrapper), otherwise use velocity if available
                double rawYawMotion = 0; // can be acceleration or velocity depending on what is provided
                bool usingAcceleration = false;

                if (nithData.ContainsParameter(NithParameters.head_acc_yaw))
                {
                    rawYawMotion = nithData.GetParameterValue(NithParameters.head_acc_yaw).Value.ValueAsDouble;
                    usingAcceleration = true;
                }
                else if (nithData.ContainsParameter(NithParameters.head_vel_yaw))
                {
                    rawYawMotion = nithData.GetParameterValue(NithParameters.head_vel_yaw).Value.ValueAsDouble;
                }

                double rawPitchPos = nithData.GetParameterValue(NithParameters.head_pos_pitch).Value.ValueAsDouble;
                
                // Get head roll position (optional)
                double rawRollPos = 0;
                if (nithData.ContainsParameter(NithParameters.head_pos_roll))
                {
                    rawRollPos = nithData.GetParameterValue(NithParameters.head_pos_roll).Value.ValueAsDouble;
                }
                
                // Get mouth aperture (optional, from webcam)
                double rawMouthAperture = 0;
                if (nithData.ContainsParameter(NithParameters.mouth_ape))
                {
                    rawMouthAperture = nithData.GetParameterValue(NithParameters.mouth_ape).Value.ValueAsDouble;
                }
                
                // Yaw position is optional - may not be present for phone/accelerometer-based sensors
                double rawYawPos = 0;
                if (nithData.ContainsParameter(NithParameters.head_pos_yaw))
                {
                    rawYawPos = nithData.GetParameterValue(NithParameters.head_pos_yaw).Value.ValueAsDouble;
                }

                // Apply sensitivity multiplier to raw yaw motion (acceleration or velocity)
                // This makes the sensitivity control directly affect the input magnitude
                rawYawMotion *= Sensitivity;

                // Update MappingModule with current mode and motion value for GUI display
                Rack.MappingModule.UsingAccelerationMode = usingAcceleration;
                Rack.MappingModule.HeadYawMotion = rawYawMotion;

                // Only set head yaw position when NOT using acceleration (avoid drift from phone)
                if (!usingAcceleration)
                {
                    Rack.MappingModule.HeadYawPosition = rawYawPos;
                }

                // 2. Determine direction and update state
                _previousDirection = _currentDirection;
                _currentDirection = Math.Sign(rawYawMotion);
                bool isDirectionChanged = _previousDirection != 0 && _currentDirection != 0 && _previousDirection != _currentDirection;

                // 3. Use raw yaw motion magnitude (acc or vel) and filter positions
                _yawMagnitude = Math.Abs(rawYawMotion);
                
                // Only use yaw position filtering when not in acceleration mode
                // In acceleration mode, we don't rely on position due to drift
                if (!usingAcceleration)
                {
                    _yawPosFilter.Push(rawYawPos);
                    _filteredYawPos = _yawPosFilter.Pull();
                }
                else
                {
                    // In acceleration mode, we don't use position-based bow position
                    _filteredYawPos = 0;
                }

                // 4. Filter the pitch, roll, and mouth aperture
                _pitchPosFilter.Push(rawPitchPos);
                double filteredPitch = _pitchPosFilter.Pull();
                
                _rollPosFilter.Push(rawRollPos);
                double filteredRoll = _rollPosFilter.Pull();
                
                _mouthApertureFilter.Push(rawMouthAperture);
                double filteredMouthAperture = _mouthApertureFilter.Pull();

                // 5. Management of violin bow with correct thresholds
                double mappedYaw = 0;
                // Bow position is only meaningful when NOT using acceleration (position is unreliable with phone)
                double bowPosition = usingAcceleration ? 0 : _bowPositionMapper.Map(_filteredYawPos);

                // Deadzone and proportional mapping using raw magnitude (already multiplied by sensitivity)
                if (_yawMagnitude >= YAW_LOWERTHRESH_BASE)
                {
                    mappedYaw = _yawVelMapper.Map(_yawMagnitude);
                }

                // Set values in MappingModule
                Rack.MappingModule.Pressure = (int)mappedYaw;
                Rack.MappingModule.InputIndicatorValue = (int)mappedYaw;
                Rack.MappingModule.BowPosition = bowPosition;
                Rack.MappingModule.HeadPitchPosition = filteredPitch;
                Rack.MappingModule.PitchBendThreshold = PITCH_BEND_THRESHOLD;

                // 6. Management of direction change
                if (isDirectionChanged)
                {
                    Rack.MappingModule.Blow = false;
                    _lastDirectionChangeTime = DateTime.Now;
                    Rack.MappingModule.IsPlayingViolin = false;
                    // Reset pitch bend to no bending (only for PitchBend mode)
                    if (PitchEffect == WhatDoesPitchRotationDo.PitchBend)
                        Rack.MappingModule.SetPitchBend(0);
                }
                // 7. Management of pause after direction change
                else if ((DateTime.Now - _lastDirectionChangeTime).TotalMilliseconds < DIRECTION_CHANGE_PAUSE_MS)
                {
                    Rack.MappingModule.Blow = false;
                    Rack.MappingModule.IsPlayingViolin = false;
                }
                // 8. Normal management of note activation/deactivation
                else
                {
                    // Use direct comparison with upper threshold (sensitivity already applied to _yawMagnitude)
                    if (_yawMagnitude >= YAW_UPPERTHRESH_BASE && !Rack.MappingModule.Blow)
                    {
                        Rack.MappingModule.Velocity = (int)Math.Min(127, Math.Max(40, mappedYaw * 1.2));
                        Rack.MappingModule.Blow = true;
                        Rack.MappingModule.IsPlayingViolin = true;
                    }
                    else if (_yawMagnitude < YAW_LOWERTHRESH_BASE && Rack.MappingModule.Blow)
                    {
                        Rack.MappingModule.Blow = false;
                        Rack.MappingModule.IsPlayingViolin = false;
                        // Reset controls based on mode
                        switch (PitchEffect)
                        {
                            case WhatDoesPitchRotationDo.PitchBend:
                                Rack.MappingModule.SetPitchBend(0);
                                break;
                            case WhatDoesPitchRotationDo.Modulation:
                                Rack.MappingModule.Modulation = 0;
                                break;
                            case WhatDoesPitchRotationDo.BowHeight:
                                Rack.MidiModule.SendControlChange(9, 0);
                                break;
                        }
                    }
                }

                // 9. Management of modulation based on selected source (when enabled)
                if (Rack.MappingModule.Blow && Rack.UserSettings.ModulationControlMode == _ModulationControlModes.On)
                {
                    int modulationValue = 0;
                    
                    switch (Rack.UserSettings.ModulationControlSource)
                    {
                        case ModulationControlSources.HeadPitch:
                            if (Math.Abs(filteredPitch) <= PITCH_BEND_THRESHOLD)
                            {
                                modulationValue = 0;
                            }
                            else
                            {
                                double absPitch = Math.Abs(filteredPitch);
                                modulationValue = (int)_modulationMapperPitch.Map(absPitch);
                            }
                            break;
                            
                        case ModulationControlSources.MouthAperture:
                            if (filteredMouthAperture <= MOUTH_APERTURE_THRESHOLD)
                            {
                                modulationValue = 0;
                            }
                            else
                            {
                                modulationValue = (int)_modulationMapperMouth.Map(filteredMouthAperture);
                            }
                            break;
                            
                        // LEGACY: HeadRoll option removed - this behavior is deprecated anyway
                        /*
                        case ModulationControlSources.HeadRoll:
                            if (Math.Abs(filteredRoll) <= ROLL_THRESHOLD)
                            {
                                modulationValue = 0;
                            }
                            else
                            {
                                double absRoll = Math.Abs(filteredRoll);
                                modulationValue = (int)_modulationMapperRoll.Map(absRoll);
                            }
                            break;
                        */
                    }
                    
                    Rack.MappingModule.Modulation = modulationValue;
                }
                else
                {
                    Rack.MappingModule.Modulation = 0;
                }

                // 10. Management of bow pressure (CC9) based on selected source
                if (Rack.MappingModule.Blow)
                {
                    int bowPressureValue = 0;
                    
                    switch (Rack.UserSettings.BowPressureControlSource)
                    {
                        case BowPressureControlSources.HeadPitch:
                            if (Math.Abs(filteredPitch) <= PITCH_BEND_THRESHOLD)
                            {
                                bowPressureValue = 0;
                            }
                            else
                            {
                                double absPitch = Math.Abs(filteredPitch);
                                bowPressureValue = (int)_bowPressureMapperPitch.Map(absPitch);
                            }
                            break;
                            
                        case BowPressureControlSources.MouthAperture:
                            if (filteredMouthAperture <= MOUTH_APERTURE_THRESHOLD)
                            {
                                bowPressureValue = 0;
                            }
                            else
                            {
                                bowPressureValue = (int)_bowPressureMapperMouth.Map(filteredMouthAperture);
                            }
                            break;
                    }
                    
                    Rack.MidiModule.SendControlChange(9, bowPressureValue);
                }
                else
                {
                    Rack.MidiModule.SendControlChange(9, 0);
                }

                // 11. Sending haptic feedback (vibration) using raw magnitude (already multiplied by sensitivity)
                if (_yawMagnitude >= YAW_LOWERTHRESH_BASE &&
                    Rack.MappingModule.Blow &&
                    (DateTime.Now - _lastVibrationTime).TotalMilliseconds >= VIBRATION_INTERVAL_MS)
                {
                    int vibIntensity = (int)(_vibrationMapper.Map(_yawMagnitude) / vibrationDivider);
                    SendVibrationCommand(vibIntensity, vibIntensity);
                    _lastVibrationTime = DateTime.Now;
                }
            }
        }

        /// <summary>
        /// Send a vibration command to the phone.
        /// </summary>
        private void SendVibrationCommand(int duration, int amplitude)
        {
            try
            {
                // Apply phone vibration sensitivity multiplier
                duration = Math.Clamp((int)(duration * Rack.UserSettings.PhoneVibrationSensitivity), 0, 255);
                amplitude = Math.Clamp((int)(amplitude * Rack.UserSettings.PhoneVibrationSensitivity), 0, 255);
                
                // Build command using new NITH protocol format
                // Format: $HeadBower-0.1.0|COM|vibration_intensity=value&vibration_duration=value^
                string vibrationCommand = VibrationCommandBuilder.BuildCommand(amplitude, duration);
                
                if (Rack.NithSenderPhone != null)
                {
                    Rack.NithSenderPhone.SendData(vibrationCommand);
                }
            }
            catch
            {
                // Silent failure - no debug output
            }
        }
    }
}