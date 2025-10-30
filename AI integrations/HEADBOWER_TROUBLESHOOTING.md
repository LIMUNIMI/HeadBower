# HeadBower Troubleshooting Guide

## Issue: Nothing Gets Played with Webcam

### ? FIXED

**Problem:** Parameter selector was blocking webcam velocity data  
**Solution:** Accept `head_vel_*` instead of `head_acc_*` from webcam

**Why:** Webcam sends velocity, acceleration is calculated later in pipeline

---

## Common Issues and Solutions

### 1. No Sound When Using Specific Source

**Symptoms:**
- Selected source (Webcam/Phone/Eye Tracker) doesn't produce sound
- Works with other sources

**Check:**
1. Is the receiver connected?
   ```
   Console output should show: "UDP receiver connected on port XXXXX"
   ```

2. Is data being received?
   - Check console for parameter selector output
   - Should see parameter list from that sensor

3. Are correct parameters being accepted?
   - **Webcam/Eye Tracker:** Should accept `head_vel_*` (not `head_acc_*`)
   - **Phone:** Should accept `head_acc_*` (has hardware IMU)

**Fix:**
- Check `DefaultSetup.ConfigureParameterSelector()` method
- Verify correct parameters for selected source

### 2. Selector Debug Output

When switching sources, console should show:
```
=== Parameter Selector Configuration ===
Head Tracking Source: Webcam
NithPreprocessor_ParameterSelector (Mode: Whitelist)
  NITHwebcamWrapper: Accept 9 parameter(s): head_pos_yaw, head_vel_yaw, ...
  NITHtobiasWrapper: Accept 2 parameter(s): gaze_x, gaze_y
=========================================
```

**If you don't see this:**
- Selector not being configured
- Check `UpdateHeadTrackingSource()` is being called

### 3. Wrong Parameters Listed

**If selector shows `head_acc_*` for webcam:**
? **WRONG!** Webcam sends `head_vel_*`

**If selector shows `head_vel_*` for phone:**
? **WRONG!** Phone sends `head_acc_*`

### 4. Calibration Not Working

**Symptoms:**
- Pressing "HT cal" button doesn't recenter
- Head position doesn't calibrate

**Check:**
- Only `Rack.UnifiedHeadTrackerCalibrator` is active
- Old calibrators (WebcamHeadTrackerCalibrator, etc.) are NOT used

**Fix:**
```csharp
// Correct:
Rack.UnifiedHeadTrackerCalibrator.SetCenterToCurrentPosition();

// Wrong (old code):
Rack.WebcamHeadTrackerCalibrator.SetCenterToCurrentPosition(); // Not used!
```

### 5. Behaviors Not Receiving Data

**Symptoms:**
- Behavior exists but doesn't respond to head movements
- No errors in console

**Check:**
1. Is behavior added to unified module?
   ```csharp
   Rack.NithModuleUnified.SensorBehaviors.Add(Rack.Behavior_HeadBow);
   ```

2. Are behaviors in old modules?
   ```csharp
   // Wrong - old modules not connected!
   Rack.NithModuleWebcam.SensorBehaviors.Add(behavior);
   ```

**Fix:**
- Move all behaviors to `Rack.NithModuleUnified`

### 6. Multiple Sources Sending Same Parameter

**Symptoms:**
- Unpredictable behavior
- Data seems mixed from multiple sources

**Check:**
- Only ONE source should send each parameter type
- Parameter selector should have mutually exclusive rules

**Example of correct configuration:**
```csharp
// Webcam mode:
Webcam ? head_pos_*, head_vel_*    ?
Phone ? (nothing)                   ?
Eye ? gaze only                     ?
```

### 7. Preprocessor Order Issues

**Symptoms:**
- Parameters missing
- Transformations not applied

**Correct order:**
```
1. ParameterSelector (filters by source)
2. WebcamWrapper (transforms aperture data)
3. UnifiedHeadTrackerCalibrator (calibrates position)
4. HeadAccelerationCalculator (creates acceleration from velocity)
5. MAfilterParams (smooths values)
```

**Check:**
- Selector is FIRST
- Acceleration calculator is AFTER selector

### 8. Sensor Name Mismatch

**Symptoms:**
- Selector rules configured but not filtering
- All data passing through or all blocked

**Check sensor names match exactly:**
| UI Name | Sensor Name in Data | Port |
|---------|---------------------|------|
| Webcam | `NITHwebcamWrapper` | 20100 |
| Eye Tracker | `NITHtobiasWrapper` | 20102 |
| Phone | `NITHphoneWrapper` | 21103 |

**Common mistake:**
```csharp
// Wrong - includes version!
selector.AddRule("NITHwebcamWrapper-v1.0", ...);

// Correct - sensor name only!
selector.AddRule("NITHwebcamWrapper", ...);
```

---

## Quick Diagnostic Checklist

When source doesn't work:

- [ ] Check console for selector configuration output
- [ ] Verify correct parameters (vel vs acc) for source
- [ ] Check receiver is connected (console message)
- [ ] Verify behavior is in `NithModuleUnified`, not old modules
- [ ] Check preprocessor order (selector first!)
- [ ] Verify sensor name matches (no version!)
- [ ] Check only one source provides each parameter type

---

## Parameter Reference

### What Each Source Actually Sends

| Source | Position | Motion | Other |
|--------|----------|--------|-------|
| **Webcam** | `head_pos_*` | `head_vel_*` ?? | `mouth_ape`, `eye*_ape` |
| **Phone** | `head_pos_*` | `head_acc_*` ?? | - |
| **Eye Tracker** | `head_pos_*` | `head_vel_*` ?? | `gaze_*` |

?? **Critical:** Webcam and Eye Tracker send VELOCITY, Phone sends ACCELERATION

### What Behaviors Need

**HeadBow behavior prefers:**
- `head_acc_yaw` (for motion detection)
- `head_pos_pitch` (for bow pressure)

**These are created by:**
- Selector lets `head_vel_*` or `head_acc_*` through
- `HeadAccelerationCalculator` creates `head_acc_*` from `head_vel_*` if needed

---

## Emergency Reset

If everything is broken:

1. Check `DefaultSetup.ConfigureParameterSelector()` matches this guide
2. Verify webcam accepts `head_vel_*` (NOT `head_acc_*`)
3. Verify phone accepts `head_acc_*` (NOT `head_vel_*`)
4. Rebuild solution
5. Check console output when switching sources

---

## Getting Help

When reporting issues, provide:

1. Selected head tracking source
2. Console output when switching sources
3. Parameter selector configuration from console
4. Whether data is being received (console logs)
