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

        public void HandleData(NithSensorData nithData)
        {
            try
            {
                // Track packet counts
                string sensorName = nithData.SensorName ?? "UNKNOWN";
                if (!_packetCounts.ContainsKey(sensorName))
                {
                    _packetCounts[sensorName] = 0;
                }
                _packetCounts[sensorName]++;
                _lastPacketTime[sensorName] = DateTime.Now;

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
            Console.WriteLine("\n??????????????????????????????????????????????????????????????????");
            Console.WriteLine("?              SENSOR DATA DIAGNOSTIC SUMMARY                    ?");
            Console.WriteLine("??????????????????????????????????????????????????????????????????");
            
            if (_packetCounts.Count == 0)
            {
                Console.WriteLine("?  ??  NO SENSOR DATA RECEIVED                                   ?");
            }
            else
            {
                foreach (var kvp in _packetCounts)
                {
                    string sensorName = kvp.Key;
                    int count = kvp.Value;
                    DateTime lastTime = _lastPacketTime[sensorName];
                    double secondsAgo = (DateTime.Now - lastTime).TotalSeconds;
                    
                    string status = secondsAgo < 1.0 ? "? ACTIVE" : "??  STALE";
                    Console.WriteLine($"?  {status,-12} {sensorName,-25} ({count} packets)");
                }
            }
            
            Console.WriteLine("??????????????????????????????????????????????????????????????????");
            Console.WriteLine($"?  Selected Source: {Rack.UserSettings.HeadTrackingSource,-43} ?");
            Console.WriteLine("??????????????????????????????????????????????????????????????????\n");
            
            // Reset counters
            _packetCounts.Clear();
        }
    }
}
