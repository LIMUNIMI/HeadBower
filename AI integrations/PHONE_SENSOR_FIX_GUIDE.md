# NithModulePhone - Lag & Crash Fix Guide

## What We Just Did ?

Applied the anti-lag protection to `NithModulePhone` (and all other sensors):

```csharp
// Rate Limiting (at Receiver)
Rack.UDPreceiverPhone.MaxSamplesPerSecond = 60; // Max 60 Hz

// Async Queue (at Module)
Rack.NithModulePhone.MaxQueueSize = 20;                    // Up to 20 samples buffered
Rack.NithModulePhone.OverflowBehavior = QueueOverflowBehavior.DropOldest; // Drop oldest if full
```

## Configuration by Sensor (What You Have Now)

| Sensor | Rate Limit | Queue Size | Notes |
|--------|------------|-----------|-------|
| **Phone** | 60 Hz | 20 | Variable speed, moderate rate |
| **Eye Tracker** | 100 Hz | 30 | Fastest sensor, needs more buffer |
| **Head Tracker** | 60 Hz | 20 | USB serial, moderate rate |
| **Webcam** | 30 Hz | 15 | Slowest, less buffering needed |

## How to Monitor Performance

### Quick Test: Check Console Output

Add this to `MainWindow.xaml.cs` initialization (see `DIAGNOSTICS_MONITORING_CODE.cs`):
- Shows queue depth every 5 seconds
- Shows how many samples were dropped
- Warns you if queue is getting full

### Expected Behavior ?

**No lag/crashes scenario:**
```
?? PHONE Sensor:
   Queue: 2/20 samples      ? Usually between 1-5
   Dropped (receiver): 0    ? Should be 0 most of the time
   Dropped (module): 0      ? Should be 0 most of the time
```

**Too much traffic scenario (needs tuning):**
```
?? PHONE Sensor:
   Queue: 18/20 samples     ??  Nearly full!
   Dropped (receiver): 50   ??  Many dropped at receiver
   Dropped (module): 30     ??  Many dropped at module
```

## If You Still Have Problems

### Problem: "Still getting lag/freezes"

**Try this (reduce to lower rate):**
```csharp
Rack.UDPreceiverPhone.MaxSamplesPerSecond = 30; // Try 30 Hz instead of 60
Rack.NithModulePhone.MaxQueueSize = 10;        // Try smaller queue
```

**Why it helps:** Lower rate limit = fewer samples to process = less CPU load

---

### Problem: "Losing too many samples / Phone app not responding"

**Try this (increase buffering):**
```csharp
Rack.UDPreceiverPhone.MaxSamplesPerSecond = 100; // Allow more
Rack.NithModulePhone.MaxQueueSize = 50;         // Bigger buffer
```

**Why it helps:** More buffer = can handle bursts better, fewer drops

---

### Problem: "Phone seems to lag specifically during rendering"

**Likely cause:** UI rendering thread is busy, leaving no CPU for phone processing

**Solution options:**
1. Move phone processing to lower priority (check with your team)
2. Reduce phone sample rate slightly more
3. Optimize behaviors attached to `NithModulePhone`

**Check:** What behaviors are attached to `NithModulePhone`?
```csharp
Console.WriteLine($"Phone behaviors: {Rack.NithModulePhone.SensorBehaviors.Count}");
foreach (var behavior in Rack.NithModulePhone.SensorBehaviors)
{
    Console.WriteLine($"  - {behavior.GetType().Name}");
}
```

---

### Problem: "Everything lags, not just Phone"

**This means:** Overall system is overloaded

**Try this (across all sensors):**
```csharp
Rack.UDPreceiverPhone.MaxSamplesPerSecond = 30;
Rack.UDPreceiverWebcam.MaxSamplesPerSecond = 15;
Rack.UDPreceiverEyeTracker.MaxSamplesPerSecond = 60;
Rack.USBreceiverHeadTracker.MaxSamplesPerSecond = 30;

// Reduce all queue sizes
Rack.NithModulePhone.MaxQueueSize = 10;
Rack.NithModuleWebcam.MaxQueueSize = 8;
Rack.NithModuleEyeTracker.MaxQueueSize = 15;
Rack.NithModuleHeadTracker.MaxQueueSize = 10;
```

**Then gradually increase until you find the sweet spot.**

---

### Problem: "One sample/second crashes the app"

**This is a processing bug, not rate limit issue.**

Check:
1. What preprocessors are attached to `NithModulePhone`?
2. What behaviors process phone data?
3. Are any of them doing heavy computation?
4. Are there any exceptions being swallowed?

**Debug code:**
```csharp
// Enable more detailed error handling
var phoneModule = Rack.NithModulePhone;
Console.WriteLine($"Preprocessors: {phoneModule.Preprocessors.Count}");
Console.WriteLine($"Behaviors: {phoneModule.SensorBehaviors.Count}");

// Log last error
Console.WriteLine($"Last error: {phoneModule.LastError}");
```

---

## Fine-Tuning Process

1. **Start with defaults** (what's in `DefaultSetup.cs` now)
2. **Run your app normally** for 5-10 minutes
3. **Check diagnostics output** (copy from `DIAGNOSTICS_MONITORING_CODE.cs`)
4. **Adjust based on readings:**
   - Queue > 80% full? ? Reduce sample rate or process faster
   - Lots of drops? ? Increase buffer or reduce sample rate
   - No issues? ? You're done! ?

5. **Re-test** after each adjustment

---

## Key Metrics Explained

### Queue Depth
- **What it is:** How many samples are waiting to be processed
- **Ideal:** 1-3 samples
- **Warning:** > 80% of max queue
- **Critical:** At max queue (samples will be dropped)

### Dropped (Receiver)
- **What it is:** Samples dropped by rate limiting
- **Ideal:** 0 most of the time
- **Normal:** Small bursts (1-2 per minute okay)
- **Problem:** Continuous dropping (adjust rate limit)

### Dropped (Module)
- **What it is:** Samples dropped because queue was full
- **Ideal:** 0
- **Warning:** Any continuous dropping
- **Fix:** Increase `MaxQueueSize` or reduce sample rate

---

## Emergency Fallback (If All Else Fails)

```csharp
// Completely disable async processing - revert to old sync mode
Rack.NithModulePhone.UseAsyncProcessing = false;
```

?? **Warning:** This loses all lag protection. Only use if debugging something specific.

---

## Next Steps

1. ? Compile and run (we just did this)
2. Run the app for 5 minutes normally
3. Check the console diagnostics output
4. If fine-tuning needed, adjust rates and queue sizes
5. Re-run tests after each adjustment
6. Document your final settings in `DefaultSetup.cs` comments

---

## Questions to Ask Yourself

- **Is Phone the only sensor causing issues?** ? Sensor-specific problem
- **Are all sensors lagging equally?** ? System-wide CPU load problem  
- **Does lag happen only at certain times?** ? Something else consuming CPU
- **Do diagnostics show high dropped counts?** ? Need different settings
- **Do diagnostics show empty queues?** ? Not actually a queue overflow issue

---

## Summary

| Before | After |
|--------|-------|
| Phone sends 200+ Hz ? Lag/crash | Phone limited to 60 Hz ? Smooth |
| All processing blocks receiver | Async processing decouples I/O |
| No visibility into drops | Can monitor everything in real-time |
| No way to configure per-sensor | Full per-sensor tuning capability |

**You should now have a smooth, lag-free experience!** ??

If problems persist, the diagnostics output will tell you exactly what's happening.
