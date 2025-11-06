using HeadBower.Modules;
using NITHlibrary.Nith.Internals;

namespace HeadBower.Behaviors
{
    /// <summary>
    /// Diagnostic behavior that logs all incoming sensor data to help debug source selection issues.
    /// Enable this temporarily when troubleshooting sensor connectivity.
    /// </summary>
    public class DiagnosticBehavior : INithSensorBehavior
    {
        private DateTime _lastLogTime = DateTime.MinValue;
        private const int LOG_INTERVAL_MS = 2000; // Log every 2 seconds
        
        private Dictionary<string, int> _packetCounts = new();
        private Dictionary<string, DateTime> _lastPacketTime = new();
        private Dictionary<string, List<string>> _lastParameters = new();

        public void HandleData(NithSensorData nithData)
        {
            try
            {
                // Track packet counts
                string sensorName = nithData.SensorName ?? "UNKNOWN";
                if (!_packetCounts.ContainsKey(sensorName))
                {
                    _packetCounts[sensorName] = 0;
                    _lastParameters[sensorName] = new List<string>();
                }
                _packetCounts[sensorName]++;
                _lastPacketTime[sensorName] = DateTime.Now;

                // Track which parameters this sensor is sending
                var paramNames = nithData.Values.Select(v => v.Parameter.ToString()).ToList();
                _lastParameters[sensorName] = paramNames;

                // Log summary periodically
                if ((DateTime.Now - _lastLogTime).TotalMilliseconds >= LOG_INTERVAL_MS)
                {
                    LogDiagnostics();
                    _lastLogTime = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DiagnosticBehavior Exception: {ex.Message}");
            }
        }

        private void LogDiagnostics()
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("  SENSOR DATA DIAGNOSTIC SUMMARY");
            Console.WriteLine("========================================");
            
            if (_packetCounts.Count == 0)
            {
                Console.WriteLine("  [WARNING] NO SENSOR DATA RECEIVED");
                Console.WriteLine("  - No wrappers are running");
                Console.WriteLine("  - OR ParameterSelector is filtering ALL");
            }
            else
            {
                foreach (var kvp in _packetCounts)
                {
                    string sensorName = kvp.Key;
                    int count = kvp.Value;
                    DateTime lastTime = _lastPacketTime[sensorName];
                    double secondsAgo = (DateTime.Now - lastTime).TotalSeconds;
                    
                    string status = secondsAgo < 1.0 ? "[ACTIVE]" : "[STALE]";
                    Console.WriteLine($"  {status} {sensorName,-25} ({count} packets)");
                    
                    // Show parameters in the last packet
                    if (_lastParameters.ContainsKey(sensorName) && _lastParameters[sensorName].Count > 0)
                    {
                        var params_limited = _lastParameters[sensorName].Take(5).ToList();
                        string paramsList = string.Join(", ", params_limited);
                        if (_lastParameters[sensorName].Count > 5)
                        {
                            paramsList += $", ... +{_lastParameters[sensorName].Count - 5} more";
                        }
                        Console.WriteLine($"      Params: {paramsList}");
                    }
                    else
                    {
                        Console.WriteLine($"      [EMPTY] All parameters filtered out!");
                    }
                }
            }
            
            Console.WriteLine("========================================");
            Console.WriteLine($"  Selected Source: {Rack.UserSettings.HeadTrackingSource}");
            Console.WriteLine("========================================\n");
            
            // Reset counters
            _packetCounts.Clear();
            _lastParameters.Clear();
        }
    }
}
