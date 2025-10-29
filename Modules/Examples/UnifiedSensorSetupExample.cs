using NITHlibrary.Nith.Internals;
using NITHlibrary.Nith.Module;
using NITHlibrary.Nith.Preprocessors;
using NITHlibrary.Tools.Ports;

namespace HeadBower.Modules.Examples
{
    /// <summary>
    /// Example setup demonstrating how to use NithPreprocessor_ParameterSelector
    /// to multiplex data from multiple NITH sensors into a unified data stream.
    /// </summary>
    /// <remarks>
    /// This example shows a typical HeadBower scenario where:
    /// - NITHphone provides head acceleration and position
    /// - NITHwebcam provides mouth and eye aperture
    /// - NITHeyeTracker provides gaze data
    /// 
    /// All three sources feed into a SINGLE NithModule with a ParameterSelector
    /// that filters which parameters to accept from each source.
    /// </remarks>
    public class UnifiedSensorSetupExample
    {
        // Receivers for different data sources
        private UDPreceiver _udpReceiverPhone;
        private UDPreceiver _udpReceiverWebcam;
        private UDPreceiver _udpReceiverEyeTracker;

        // Unified module that receives data from all sources
        private NithModule _unifiedModule;

        public void SetupUnifiedSensorProcessing()
        {
            // ============================================================================
            // STEP 1: Create receivers for each data source
            // ============================================================================

            // Phone receiver (provides head tracking via accelerometer + gyroscope)
            _udpReceiverPhone = new UDPreceiver(21100);
            _udpReceiverPhone.MaxSamplesPerSecond = 60; // Limit rate
            _udpReceiverPhone.Connect();

            // Webcam receiver (provides mouth/eye aperture via computer vision)
            _udpReceiverWebcam = new UDPreceiver(20100);
            _udpReceiverWebcam.MaxSamplesPerSecond = 30; // Webcam is slower
            _udpReceiverWebcam.Connect();

            // Eye tracker receiver (provides precise gaze data)
            _udpReceiverEyeTracker = new UDPreceiver(20102);
            _udpReceiverEyeTracker.MaxSamplesPerSecond = 100; // Eye tracker is fast
            _udpReceiverEyeTracker.Connect();

            // ============================================================================
            // STEP 2: Create the unified NithModule
            // ============================================================================

            _unifiedModule = new NithModule();
            _unifiedModule.MaxQueueSize = 20;
            _unifiedModule.OverflowBehavior = QueueOverflowBehavior.DropOldest;

            // ============================================================================
            // STEP 3: Configure the ParameterSelector (THE KEY COMPONENT!)
            // ============================================================================

            var parameterSelector = new NithPreprocessor_ParameterSelector();
            // Using default Whitelist mode - only explicitly allowed parameters will pass

            // --- Phone Rules ---
            // Accept head acceleration (fast motion detection from phone's IMU)
            parameterSelector.AddRule("NITHphone", NithParameters.head_acc_yaw);
            parameterSelector.AddRule("NITHphone", NithParameters.head_acc_pitch);
            parameterSelector.AddRule("NITHphone", NithParameters.head_acc_roll);

            // Accept head position from phone (for bow position tracking)
            parameterSelector.AddRule("NITHphone", NithParameters.head_pos_pitch);
            parameterSelector.AddRule("NITHphone", NithParameters.head_pos_roll);
            // Note: NOT accepting head_pos_yaw from phone to avoid drift

            // --- Webcam Rules ---
            // Accept mouth and eye aperture (best detected via webcam)
            parameterSelector.AddRule("NITHwebcam", NithParameters.mouth_ape);
            parameterSelector.AddRule("NITHwebcam", NithParameters.eyeLeft_ape);
            parameterSelector.AddRule("NITHwebcam", NithParameters.eyeRight_ape);

            // Optionally accept head position from webcam (drift-free but slower)
            parameterSelector.AddRule("NITHwebcam", NithParameters.head_pos_yaw);

            // --- Eye Tracker Rules ---
            // Accept gaze data (most accurate from dedicated eye tracker)
            parameterSelector.AddRule("NITHeyeTracker", NithParameters.gaze_x);
            parameterSelector.AddRule("NITHeyeTracker", NithParameters.gaze_y);

            // Print configuration for debugging
            Console.WriteLine(parameterSelector.GetRulesSummary());

            // ============================================================================
            // STEP 4: Add preprocessors to the unified module
            // ============================================================================

            // Add ParameterSelector FIRST to filter incoming data
            _unifiedModule.Preprocessors.Add(parameterSelector);

            // Add other preprocessors after filtering
            // (they will only see the parameters that passed the selector)

            // Calibrator for head tracking
            var headCalibrator = new NithPreprocessor_HeadTrackerCalibrator();
            _unifiedModule.Preprocessors.Add(headCalibrator);

            // Acceleration calculator (for velocity/acceleration if needed)
            var accelerationCalculator = new NithPreprocessor_HeadAccelerationCalculator(
                filterAlpha: 0.2f,
                accelerationSensitivity: 0.2f
            );
            _unifiedModule.Preprocessors.Add(accelerationCalculator);

            // Smoothing filter for specific parameters
            var smoothingFilter = new NithPreprocessor_MAfilterParams(
                new List<NithParameters>
                {
                    NithParameters.head_pos_pitch,
                    NithParameters.head_pos_roll,
                    NithParameters.mouth_ape
                },
                0.5f // Alpha value
            );
            _unifiedModule.Preprocessors.Add(smoothingFilter);

            // ============================================================================
            // STEP 5: Connect all receivers to the unified module
            // ============================================================================

            _udpReceiverPhone.Listeners.Add(_unifiedModule);
            _udpReceiverWebcam.Listeners.Add(_unifiedModule);
            _udpReceiverEyeTracker.Listeners.Add(_unifiedModule);

            // ============================================================================
            // STEP 6: Add behaviors to the unified module
            // ============================================================================

            // Now you can add your behaviors that will receive the combined data
            // For example, the HeadBow behavior will receive:
            // - head_acc_yaw from phone
            // - head_pos_pitch from phone
            // - mouth_ape from webcam
            // - gaze_x/y from eye tracker
            // All in a single NithSensorData stream!

            // Example:
            // _unifiedModule.SensorBehaviors.Add(new NITHbehavior_HeadViolinBow());
        }

        // ============================================================================
        // Alternative Example: Blacklist Mode
        // ============================================================================

        public void SetupWithBlacklistMode()
        {
            _unifiedModule = new NithModule();

            var selector = new NithPreprocessor_ParameterSelector();
            selector.Mode = NithPreprocessor_ParameterSelector.FilterMode.Blacklist;

            // Block head position from eye tracker (we prefer phone/webcam for this)
            selector.AddRule("NITHeyeTracker", NithParameters.head_pos_pitch);
            selector.AddRule("NITHeyeTracker", NithParameters.head_pos_roll);
            selector.AddRule("NITHeyeTracker", NithParameters.head_pos_yaw);

            // Accept everything else from all sensors
            _unifiedModule.Preprocessors.Add(selector);

            // ... rest of setup
        }

        // ============================================================================
        // Alternative Example: Accept All from One Sensor
        // ============================================================================

        public void SetupWithWildcard()
        {
            _unifiedModule = new NithModule();

            var selector = new NithPreprocessor_ParameterSelector();

            // Accept ALL parameters from phone
            selector.AddSensorWildcard("NITHphone");

            // Accept only specific parameters from others
            selector.AddRule("NITHwebcam", NithParameters.mouth_ape);
            selector.AddRule("NITHeyeTracker", NithParameters.gaze_x);
            selector.AddRule("NITHeyeTracker", NithParameters.gaze_y);

            _unifiedModule.Preprocessors.Add(selector);

            // ... rest of setup
        }

        // ============================================================================
        // Cleanup
        // ============================================================================

        public void Dispose()
        {
            _udpReceiverPhone?.Dispose();
            _udpReceiverWebcam?.Dispose();
            _udpReceiverEyeTracker?.Dispose();
            _unifiedModule?.Dispose();
        }
    }
}
