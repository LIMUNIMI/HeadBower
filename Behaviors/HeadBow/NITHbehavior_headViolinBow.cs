// Behaviors\HeadBow\NITHbehavior_headViolinBow_yawAndBend.cs
using HeadBower.Modules;
using NITHlibrary.Nith.Internals;
using NITHlibrary.Tools.Filters.ValueFilters;
using NITHlibrary.Tools.Mappers;

namespace HeadBower.Behaviors.HeadBow
{
    /// <summary>
    /// Imitates a violin bow using head movements (yaw acceleration).
    /// Depending on the OperationMode, head pitch controls either pitch bend,
    /// modulation or another CC message (09) for bow height.
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

        // Require only the position/pitch values; velocity or acceleration are optional
        private readonly List<NithParameters> requiredParams = new List<NithParameters>
        {
            NithParameters.head_pos_pitch,
            NithParameters.head_pos_yaw
        };

        // Constants and thresholds
        private const double YAW_UPPERTHRESH_BASE = 2;
        private const double YAW_LOWERTHRESH_BASE = 1;
        private const int DIRECTION_CHANGE_PAUSE_MS = 2;

        // New constant for pitch bend mapping
        private const double MAX_PITCH_DEVIATION = 50.0; // Maximum expected deviation
        private const double PITCH_BEND_THRESHOLD = 15.0; // Minimum threshold to start applying pitch bend

        // Filters and mappers
        // NOTE: yaw velocity filtering removed - use raw values from sensor (or acceleration)
        private readonly SegmentMapper _yawVelMapper = new SegmentMapper(1, 10, 1, 127); // Modified to start mapping from threshold value
        private readonly DoubleFilterMAexpDecaying _pitchPosFilter = new DoubleFilterMAexpDecaying(0.9f);
        private readonly DoubleFilterMAexpDecaying _yawPosFilter = new DoubleFilterMAexpDecaying(0.9f);
        private readonly SegmentMapper _pitchBendMapper = new SegmentMapper(-1.0, 1.0, -1.0, 1.0, true);
        private readonly SegmentMapper _bowPositionMapper = new SegmentMapper(-50.0, 50.0, -1.0, 1.0, true);
        // Mappers for modulation and bow height (CC 09)
        private readonly SegmentMapper _modulationMapper = new SegmentMapper(PITCH_BEND_THRESHOLD, MAX_PITCH_DEVIATION, 0, 127, true);
        private readonly SegmentMapper _bowHeightMapper = new SegmentMapper(-MAX_PITCH_DEVIATION, MAX_PITCH_DEVIATION, 0, 127, true);

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
                double rawYawPos = nithData.GetParameterValue(NithParameters.head_pos_yaw).Value.ValueAsDouble;

                // Set head yaw position into MappingModule for potential use
                Rack.MappingModule.HeadYawPosition = rawYawPos;

                // 2. Determine direction and update state
                _previousDirection = _currentDirection;
                _currentDirection = Math.Sign(rawYawMotion);
                bool isDirectionChanged = _previousDirection != 0 && _currentDirection != 0 && _previousDirection != _currentDirection;

                // 3. Use raw yaw motion magnitude (acc or vel) and still filter positions
                _yawMagnitude = Math.Abs(rawYawMotion);
                _yawPosFilter.Push(rawYawPos);
                _filteredYawPos = _yawPosFilter.Pull();

                // 4. Filter the pitch
                _pitchPosFilter.Push(rawPitchPos);
                double filteredPitch = _pitchPosFilter.Pull();

                // 5. Management of violin bow with correct thresholds
                double mappedYaw = 0;
                double bowPosition = _bowPositionMapper.Map(_filteredYawPos);

                // Deadzone and proportional mapping using raw magnitude
                if (_yawMagnitude >= YAW_LOWERTHRESH_BASE * Sensitivity)
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
                    // Use direct comparison with upper threshold
                    if (_yawMagnitude >= YAW_UPPERTHRESH_BASE * Sensitivity && !Rack.MappingModule.Blow)
                    {
                        Rack.MappingModule.Velocity = (int)Math.Min(127, Math.Max(40, mappedYaw * 1.2));
                        Rack.MappingModule.Blow = true;
                        Rack.MappingModule.IsPlayingViolin = true;
                    }
                    else if (_yawMagnitude < YAW_LOWERTHRESH_BASE * Sensitivity && Rack.MappingModule.Blow)
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

                // 9. Management of pitch-based effects based on mode
                if (Rack.MappingModule.Blow)
                {
                    switch (PitchEffect)
                    {
                        case WhatDoesPitchRotationDo.PitchBend:
                            {
                                // Correct implementation of pitch bend with deadzone
                                if (Math.Abs(filteredPitch) <= PITCH_BEND_THRESHOLD)
                                {
                                    Rack.MappingModule.SetPitchBend(0);
                                }
                                else
                                {
                                    // Proportional calculation of pitch bend value
                                    double normalizedPitch = filteredPitch > 0
                                        ? _modulationMapper.Map(filteredPitch) / 127.0
                                        : -_modulationMapper.Map(Math.Abs(filteredPitch)) / 127.0;

                                    Rack.MappingModule.SetPitchBend(Math.Clamp(normalizedPitch, -1.0, 1.0));
                                }
                                break;
                            }
                        case WhatDoesPitchRotationDo.Modulation:
                            {
                                // Correct implementation of modulation with deadzone
                                if (Math.Abs(filteredPitch) <= PITCH_BEND_THRESHOLD)
                                {
                                    Rack.MappingModule.Modulation = 0;
                                }
                                else
                                {
                                    // Use only positive values for modulation, but maintain deadzone
                                    double absPitch = Math.Abs(filteredPitch);
                                    int modulationValue = (int)_modulationMapper.Map(absPitch);
                                    Rack.MappingModule.Modulation = modulationValue;
                                }
                                break;
                            }
                        case WhatDoesPitchRotationDo.BowHeight:
                            {
                                // For Bow Height, map the pitch directly
                                int bowHeight = (int)_bowHeightMapper.Map(filteredPitch);
                                Rack.MidiModule.SendControlChange(9, bowHeight);
                                break;
                            }
                    }
                }
                else
                {
                    // In non-blowing mode, reset any sent CC
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

                // 10. Sending haptic feedback (vibration) using raw magnitude
                if (_yawMagnitude >= YAW_LOWERTHRESH_BASE * Sensitivity &&
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
                duration = Math.Clamp(duration, 0, 255);
                amplitude = Math.Clamp(amplitude, 0, 255);
                string vibrationCommand = $"VIB:{duration}:{amplitude}";
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