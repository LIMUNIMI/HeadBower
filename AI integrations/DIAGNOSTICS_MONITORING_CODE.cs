// Optional: Add this to MainWindow.xaml.cs or create a separate diagnostics window
// This helps monitor if Phone or other sensors are dropping samples

/*
Place this code in your MainWindow constructor or initialization:

using System.Timers;
using Timer = System.Timers.Timer;

private Timer _performanceMonitorTimer;

// In MainWindow() or Setup():
_performanceMonitorTimer = new Timer(5000); // Every 5 seconds
_performanceMonitorTimer.Elapsed += PerformanceMonitor_Elapsed;
_performanceMonitorTimer.AutoReset = true;
_performanceMonitorTimer.Start();

// Add this method to MainWindow:
private void PerformanceMonitor_Elapsed(object sender, ElapsedEventArgs e)
{
    try
    {
        // Get all the metrics
        var phoneQueueDepth = Rack.NithModulePhone.QueueDepth;
        var phoneDroppedModule = Rack.NithModulePhone.DroppedSamplesCount;
        var phoneDroppedReceiver = Rack.UDPreceiverPhone.DroppedSamplesCount;
        
        var eyeQueueDepth = Rack.NithModuleEyeTracker.QueueDepth;
        var eyeDroppedModule = Rack.NithModuleEyeTracker.DroppedSamplesCount;
        var eyeDroppedReceiver = Rack.UDPreceiverEyeTracker.DroppedSamplesCount;
        
        var headQueueDepth = Rack.NithModuleHeadTracker.QueueDepth;
        var headDroppedModule = Rack.NithModuleHeadTracker.DroppedSamplesCount;
        var headDroppedReceiver = Rack.USBreceiverHeadTracker.DroppedSamplesCount;
        
        var webcamQueueDepth = Rack.NithModuleWebcam.QueueDepth;
        var webcamDroppedModule = Rack.NithModuleWebcam.DroppedSamplesCount;
        var webcamDroppedReceiver = Rack.UDPreceiverWebcam.DroppedSamplesCount;
        
        // Log to console
        Console.WriteLine("\n??????????????????????????????????????????????????????????????");
        Console.WriteLine("?           NITH SENSOR PERFORMANCE DIAGNOSTICS              ?");
        Console.WriteLine("??????????????????????????????????????????????????????????????");
        
        Console.WriteLine($"\n?? PHONE Sensor:");
        Console.WriteLine($"   Queue: {phoneQueueDepth}/20 samples");
        Console.WriteLine($"   Dropped (receiver rate limit): {phoneDroppedReceiver}");
        Console.WriteLine($"   Dropped (module overflow): {phoneDroppedModule}");
        if (phoneQueueDepth > 15) Console.WriteLine($"   ??  WARNING: Queue nearly full!");
        if (phoneDroppedModule > 10) Console.WriteLine($"   ??  WARNING: Many samples being dropped!");
        
        Console.WriteLine($"\n???  EYE TRACKER Sensor:");
        Console.WriteLine($"   Queue: {eyeQueueDepth}/30 samples");
        Console.WriteLine($"   Dropped (receiver rate limit): {eyeDroppedReceiver}");
        Console.WriteLine($"   Dropped (module overflow): {eyeDroppedModule}");
        if (eyeQueueDepth > 25) Console.WriteLine($"   ??  WARNING: Queue nearly full!");
        
        Console.WriteLine($"\n???  HEAD TRACKER Sensor:");
        Console.WriteLine($"   Queue: {headQueueDepth}/20 samples");
        Console.WriteLine($"   Dropped (receiver rate limit): {headDroppedReceiver}");
        Console.WriteLine($"   Dropped (module overflow): {headDroppedModule}");
        if (headQueueDepth > 15) Console.WriteLine($"   ??  WARNING: Queue nearly full!");
        
        Console.WriteLine($"\n?? WEBCAM Sensor:");
        Console.WriteLine($"   Queue: {webcamQueueDepth}/15 samples");
        Console.WriteLine($"   Dropped (receiver rate limit): {webcamDroppedReceiver}");
        Console.WriteLine($"   Dropped (module overflow): {webcamDroppedModule}");
        if (webcamQueueDepth > 12) Console.WriteLine($"   ??  WARNING: Queue nearly full!");
        
        Console.WriteLine("");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in performance monitor: {ex.Message}");
    }
}

// Don't forget to stop/dispose the timer on shutdown:
// In your Dispose method:
_performanceMonitorTimer?.Stop();
_performanceMonitorTimer?.Dispose();
*/
