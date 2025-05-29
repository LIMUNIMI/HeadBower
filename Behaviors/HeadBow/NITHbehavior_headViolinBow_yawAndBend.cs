// Behaviors\HeadBow\NITHbehavior_headViolinBow_yawAndBend.cs
using HeadBower.Modules;
using NITHlibrary.Nith.Internals;
using NITHlibrary.Tools.Filters.ValueFilters;
using NITHlibrary.Tools.Mappers;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace HeadBower.Behaviors.HeadBow
{
    /// <summary>
    /// Imitates a violin bow using head movements (yaw acceleration).
    /// Depending on the OperationMode, head pitch controls either pitch bend,
    /// modulation or another CC message (09) for bow height.
    /// </summary>
    internal class NITHbehavior_headViolinBow_yawAndBend : INithSensorBehavior
    {
        // Enumeration dei diversi modi di funzionamento
        public enum YawAndBendOperationMode
        {
            PitchBend,
            Modulation,
            BowHeight
        }
        // Proprietà per controllare il mode (default come PitchBend)
        public YawAndBendOperationMode OperationMode { get; set; } = YawAndBendOperationMode.PitchBend;

        private readonly List<NithParameters> requiredParams = new List<NithParameters>
        {
            NithParameters.head_pos_roll,
            NithParameters.head_vel_yaw,
            NithParameters.head_pos_yaw
        };

        // Costanti e soglie
        private const double YAW_UPPERTHRESH = 2;
        private const double YAW_LOWERTHRESH = 1;
        private const int DIRECTION_CHANGE_PAUSE_MS = 2;

        // Nuova costante per il mapping del pitch bend
        private const double MAX_PITCH_DEVIATION = 50.0; // Deviazione massima attesa
        private const double PITCH_BEND_THRESHOLD = 15.0; // Soglia minima per iniziare ad applicare il pitch bend

        // Filtri e mappers
        private readonly DoubleFilterMAexpDecaying _yawVelFilter = new DoubleFilterMAexpDecaying(0.85f);
        private readonly SegmentMapper _yawVelMapper = new SegmentMapper(YAW_LOWERTHRESH, 10, 1, 127); // Modificato per iniziare a mappare dal valore soglia
        private readonly DoubleFilterMAexpDecaying _pitchPosFilter = new DoubleFilterMAexpDecaying(0.9f);
        private readonly DoubleFilterMAexpDecaying _yawPosFilter = new DoubleFilterMAexpDecaying(0.9f);
        private readonly SegmentMapper _pitchBendMapper = new SegmentMapper(-1.0, 1.0, -1.0, 1.0, true);
        private readonly SegmentMapper _bowPositionMapper = new SegmentMapper(-50.0, 50.0, -1.0, 1.0, true);
        // Mapper per la modulazione e per il bow height (CC 09)
        private readonly SegmentMapper _modulationMapper = new SegmentMapper(PITCH_BEND_THRESHOLD, MAX_PITCH_DEVIATION, 0, 127, true);
        private readonly SegmentMapper _bowHeightMapper = new SegmentMapper(-MAX_PITCH_DEVIATION, MAX_PITCH_DEVIATION, 0, 127, true);

        // Stato
        private int _currentDirection = 0;
        private int _previousDirection = 0;
        private double _filteredYaw = 0;
        private double _filteredYawPos = 0;
        private DateTime _lastDirectionChangeTime = DateTime.MinValue;

        private readonly double vibrationDivider = 1.5f; // Divisore per l'intensità della vibrazione

        // Vibrazione
        private DateTime _lastVibrationTime = DateTime.MinValue;
        private readonly int VIBRATION_INTERVAL_MS = 15;
        private readonly SegmentMapper _vibrationMapper = new SegmentMapper(YAW_LOWERTHRESH, 50, 0, 250);

        public NITHbehavior_headViolinBow_yawAndBend(YawAndBendOperationMode operationMode = YawAndBendOperationMode.PitchBend)
        {
            OperationMode = operationMode;
        }

        public void HandleData(NithSensorData nithData)
        {
            if (nithData.ContainsParameters(requiredParams))
            {
                // 1. Ottieni i valori dai sensori
                double rawYawVel = nithData.GetParameterValue(NithParameters.head_vel_yaw).Value.ValueAsDouble;
                double rawPitchPos = nithData.GetParameterValue(NithParameters.head_pos_roll).Value.ValueAsDouble;
                double rawYawPos = nithData.GetParameterValue(NithParameters.head_pos_yaw).Value.ValueAsDouble;

                // Set head yaw position into MappingModule for potential use
                Rack.MappingModule.HeadYawPosition = rawYawPos;

                // 2. Determina la direzione e aggiorna lo stato
                _previousDirection = _currentDirection;
                _currentDirection = Math.Sign(rawYawVel);
                bool isDirectionChanged = _previousDirection != 0 && _currentDirection != 0 && _previousDirection != _currentDirection;

                // 3. Filtra i valori yaw velocity e position
                _yawVelFilter.Push(Math.Abs(rawYawVel));
                _filteredYaw = _yawVelFilter.Pull();
                _yawPosFilter.Push(rawYawPos);
                _filteredYawPos = _yawPosFilter.Pull();

                // 4. Filtra il pitch
                _pitchPosFilter.Push(rawPitchPos);
                double filteredPitch = _pitchPosFilter.Pull();

                // 5. Gestione della violino bow con soglie corrette
                double mappedYaw = 0;
                double bowPosition = _bowPositionMapper.Map(_filteredYawPos);

                // Implementazione corretta della deadzone e del mapping proporzionale
                if (_filteredYaw >= YAW_LOWERTHRESH)
                {
                    // Mappatura diretta dal valore di soglia in poi
                    mappedYaw = _yawVelMapper.Map(_filteredYaw);
                }

                // Imposta i valori nel MappingModule
                Rack.MappingModule.Pressure = (int)mappedYaw;
                Rack.MappingModule.InputIndicatorValue = (int)mappedYaw;
                Rack.MappingModule.BowPosition = bowPosition;
                Rack.MappingModule.HeadPitchPosition = filteredPitch;
                Rack.MappingModule.PitchBendThreshold = PITCH_BEND_THRESHOLD;

                // 6. Gestione del cambio di direzione
                if (isDirectionChanged)
                {
                    Rack.MappingModule.Blow = false;
                    _lastDirectionChangeTime = DateTime.Now;
                    Rack.MappingModule.IsPlayingViolin = false;
                    // Reset pitch bend a no bending (solo per mode PitchBend)
                    if (OperationMode == YawAndBendOperationMode.PitchBend)
                        Rack.MappingModule.SetPitchBend(0);
                }
                // 7. Gestione della pausa dopo il cambio direzione
                else if ((DateTime.Now - _lastDirectionChangeTime).TotalMilliseconds < DIRECTION_CHANGE_PAUSE_MS)
                {
                    Rack.MappingModule.Blow = false;
                    Rack.MappingModule.IsPlayingViolin = false;
                }
                // 8. Gestione normale dell'attivazione/disattivazione nota
                else
                {
                    // Utilizza direttamente il confronto con la soglia superiore
                    if (_filteredYaw >= YAW_UPPERTHRESH && !Rack.MappingModule.Blow)
                    {
                        Rack.MappingModule.Velocity = (int)Math.Min(127, Math.Max(40, mappedYaw * 1.2));
                        Rack.MappingModule.Blow = true;
                        Rack.MappingModule.IsPlayingViolin = true;
                    }
                    else if (_filteredYaw < YAW_LOWERTHRESH && Rack.MappingModule.Blow)
                    {
                        Rack.MappingModule.Blow = false;
                        Rack.MappingModule.IsPlayingViolin = false;
                        // Reset dei controlli in base alla modalità
                        switch (OperationMode)
                        {
                            case YawAndBendOperationMode.PitchBend:
                                Rack.MappingModule.SetPitchBend(0);
                                break;
                            case YawAndBendOperationMode.Modulation:
                                Rack.MappingModule.Modulation = 0;
                                break;
                            case YawAndBendOperationMode.BowHeight:
                                Rack.MidiModule.SendControlChange(9, 0);
                                break;
                        }
                    }
                }

                // 9. Gestione degli effetti basati sul pitch in base al mode
                if (Rack.MappingModule.Blow)
                {
                    switch (OperationMode)
                    {
                        case YawAndBendOperationMode.PitchBend:
                            {
                                // Implementazione corretta del pitch bend con deadzone
                                if (Math.Abs(filteredPitch) <= PITCH_BEND_THRESHOLD)
                                {
                                    Rack.MappingModule.SetPitchBend(0);
                                }
                                else
                                {
                                    // Calcolo proporzionale del valore di pitch bend
                                    double normalizedPitch = filteredPitch > 0
                                        ? _modulationMapper.Map(filteredPitch) / 127.0
                                        : -_modulationMapper.Map(Math.Abs(filteredPitch)) / 127.0;

                                    Rack.MappingModule.SetPitchBend(Math.Clamp(normalizedPitch, -1.0, 1.0));
                                }
                                break;
                            }
                        case YawAndBendOperationMode.Modulation:
                            {
                                // Implementazione corretta della modulazione con deadzone
                                if (Math.Abs(filteredPitch) <= PITCH_BEND_THRESHOLD)
                                {
                                    Rack.MappingModule.Modulation = 0;
                                }
                                else
                                {
                                    // Usa solo valori positivi per la modulazione, ma mantiene la deadzone
                                    double absPitch = Math.Abs(filteredPitch);
                                    int modulationValue = (int)_modulationMapper.Map(absPitch);
                                    Rack.MappingModule.Modulation = modulationValue;
                                }
                                break;
                            }
                        case YawAndBendOperationMode.BowHeight:
                            {
                                // Per Bow Height, mappiamo direttamente il pitch
                                int bowHeight = (int)_bowHeightMapper.Map(filteredPitch);
                                Rack.MidiModule.SendControlChange(9, bowHeight);
                                break;
                            }
                    }
                }
                else
                {
                    // In modalità non blowing, resettare eventuali CC inviati
                    switch (OperationMode)
                    {
                        case YawAndBendOperationMode.PitchBend:
                            Rack.MappingModule.SetPitchBend(0);
                            break;
                        case YawAndBendOperationMode.Modulation:
                            Rack.MappingModule.Modulation = 0;
                            break;
                        case YawAndBendOperationMode.BowHeight:
                            Rack.MidiModule.SendControlChange(9, 0);
                            break;
                    }
                }

                // 10. Invio feedback aptico (vibrazione)
                if (_filteredYaw >= YAW_LOWERTHRESH &&
                    Rack.MappingModule.Blow &&
                    (DateTime.Now - _lastVibrationTime).TotalMilliseconds >= VIBRATION_INTERVAL_MS)
                {
                    int vibIntensity = (int)(_vibrationMapper.Map(_filteredYaw) / vibrationDivider);
                    SendVibrationCommand(vibIntensity, vibIntensity);
                    _lastVibrationTime = DateTime.Now;
                }
            }
        }

        /// <summary>
        /// Invia un comando di vibrazione al telefono.
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
                    Console.WriteLine($"Vibration sent: {vibrationCommand}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending vibration command: {ex.Message}");
            }
        }
    }
}