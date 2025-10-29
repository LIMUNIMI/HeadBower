# Quick Reference: Anti-Lag Configuration

## TL;DR - Copy & Paste This

```csharp
// Eye Tracker (fast, 100-200 Hz)
Rack.UDPreceiverEyeTracker.MaxSamplesPerSecond = 100;
Rack.NithModuleEyeTracker.MaxQueueSize = 30;
Rack.NithModuleEyeTracker.OverflowBehavior = QueueOverflowBehavior.DropOldest;

// Head Tracker USB (medium, ~60 Hz)
Rack.USBreceiverHeadTracker.MaxSamplesPerSecond = 60;
Rack.NithModuleHeadTracker.MaxQueueSize = 20;

// Webcam (slow, ~30 Hz)
Rack.UDPreceiverWebcam.MaxSamplesPerSecond = 30;
Rack.NithModuleWebcam.MaxQueueSize = 10;

// Phone (variable, ~60 Hz)
Rack.UDPreceiverPhone.MaxSamplesPerSecond = 60;
Rack.NithModulePhone.MaxQueueSize = 20;
```

## Monitoring (Optional)

```csharp
// Log stats every 5 seconds
var timer = new System.Timers.Timer(5000);
timer.Elapsed += (s, e) =>
{
    Console.WriteLine($"Eye Tracker - Queue: {Rack.NithModuleEyeTracker.QueueDepth}, " +
                      $"Dropped: {Rack.NithModuleEyeTracker.DroppedSamplesCount}");
};
timer.Start();
```

## If Still Lagging

```csharp
// Reduce these values
module.MaxSamplesPerSecond = 50; // Lower rate limit
module.MaxQueueSize = 10;        // Smaller queue
```

## If Losing Important Data

```csharp
// Increase these values
module.MaxSamplesPerSecond = 150; // Higher rate limit
module.MaxQueueSize = 100;        // Bigger queue
module.OverflowBehavior = QueueOverflowBehavior.Block; // Never drop
```

## Disable New Features (Emergency Fallback)

```csharp
receiver.MaxSamplesPerSecond = 0;     // No rate limiting
module.UseAsyncProcessing = false;     // Old synchronous mode
```

## What Changed?

- **Before**: Direct synchronous processing ? lag at high rates
- **After**: Rate limiting + async queue ? no lag, controlled drops
- **Default**: 100 Hz rate limit, 50-sample queue, drop oldest
- **Impact**: Adds 1-10ms latency, prevents crashes

## Files to Check

- `NITHlibrary/PERFORMANCE_OPTIMIZATION_GUIDE.md` - Full documentation
- `NITHlibrary/ANTI_LAG_IMPLEMENTATION_SUMMARY.md` - Technical details

## Questions?

Check the `QueueDepth` and `DroppedSamplesCount` properties to diagnose issues.
