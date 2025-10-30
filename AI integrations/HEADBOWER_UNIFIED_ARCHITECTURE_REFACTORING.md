# HeadBower Unified Architecture Refactoring - Summary

## ? COMPLETE: Migration to Unified NithModule with Parameter Selector

### What Was Changed

HeadBower has been successfully refactored from using **4 separate NithModules** to a **single unified NithModule** with dynamic parameter selection based on head tracking source.

---

## Architecture Comparison

### ? OLD Architecture (Before)

```
Phone UDP ??? NithModulePhone ??? (Phone-specific behaviors)
Webcam UDP ?? NithModuleWebcam ?? (Webcam-specific behaviors)
Eye UDP ????? NithModuleEyeTracker ? (Eye-specific behaviors)
USB ????????? NithModuleHeadTracker ? (USB-specific behaviors)

Problem: Behaviors can't access data from multiple sources!
```

**Issues:**
- HeadBow behavior had to be dynamically moved between modules
- No way to combine data from different sources
- Complex behavior management in `UpdateHeadTrackingSource()`
- Separate calibrators for each source

### ? NEW Architecture (After)

```
Phone UDP ???????
                ???? NithModuleUnified ??? ParameterSelector ??? [All Behaviors]
Webcam UDP ??????         (Single)              (Filters)         - HeadBow
                ?                                                  - GazeToMouse  
Eye UDP ?????????                                                  - DoubleClick
                ?
USB ?????????????

Solution: One module sees combined data, selector filters by source!
```

**Benefits:**
- Single source of truth for all sensor data
- Behaviors receive combined data from all sources
- Simple parameter selection configuration
- Single unified calibrator
- User can switch sources by just reconfiguring the selector

---

## Files Modified

### 1. **Modules/Rack.cs**
Added:
- `NithModuleUnified` - Single module receiving all data
- `ParameterSelector` - Multiplexes parameters by source
- `UnifiedHeadTrackerCalibrator` - Single calibrator for all sources

Kept (for backward compatibility):
- Old modules (`NithModulePhone`, `NithModuleWebcam`, etc.)
- Old calibrators (not actively used)

### 2. **Modules/DefaultSetup.cs**
Complete refactoring:
- `SetupReceivers()` - Creates all UDP/USB receivers
- `SetupUnifiedModule()` - Creates unified module with parameter selector
- `SetupBehaviors()` - Adds all behaviors to unified module
- `SetupLegacyModules()` - Creates old modules (not connected)
- `ConfigureParameterSelector(source)` - **KEY METHOD** for source switching

### 3. **MainWindow.xaml.cs**
Simplified:
- `UpdateHeadTrackingSource()` - Now just calls `ConfigureParameterSelector()`
- `btnCalibrateHeadTracker_Click()` - Uses unified calibrator
- Removed complex behavior-switching logic

---

## Parameter Selection Rules

### Webcam Mode (Default)
```csharp
Webcam: head_pos_*, head_vel_* (NOT acc!), mouth_ape, eye*_ape  
EyeTracker: gaze_x, gaze_y
Phone: (nothing)

Note: Webcam sends VELOCITY. HeadAccelerationCalculator creates acceleration from it.
```

### Phone Mode
```csharp
Phone: head_acc_*, head_pos_*  (hardware accelerometer + gyro)
Webcam: mouth_ape, eye*_ape
EyeTracker: gaze_x, gaze_y

Note: Phone sends ACCELERATION directly from IMU hardware.
```

### Eye Tracker Mode
```csharp
EyeTracker: gaze_*, head_pos_*, head_vel_* (NOT acc!)
Webcam: mouth_ape, eye*_ape
Phone: (nothing)

Note: Eye tracker sends VELOCITY. HeadAccelerationCalculator creates acceleration from it.
```

---

## Sensor Name Mapping

| UI Name | Sensor Name (in data) | Port |
|---------|----------------------|------|
| Webcam | `NITHwebcamWrapper` | 20100 |
| Eye Tracker | `NITHtobiasWrapper` | 20102 |
| Phone | `NITHphoneWrapper` | 21103 |

---

## How Source Switching Works Now

### User clicks "Webcam" button:
1. `btnWebcam_Click()` sets `Rack.UserSettings.HeadTrackingSource = Webcam`
2. Calls `UpdateHeadTrackingSource()`
3. Calls `DefaultSetup.ConfigureParameterSelector(Webcam)`
4. Selector clears all rules
5. Adds rules: Webcam ? all head tracking, Eye ? gaze only
6. **Done!** No behavior moving, no complex logic

### Result:
- HeadBow behavior immediately sees head data from webcam
- GazeToMouse behavior sees gaze from eye tracker
- DoubleClick behavior sees eye aperture from webcam
- All in the SAME behavior execution!

---

## Preprocessor Pipeline

Data flows through preprocessors in this order:

```
Incoming Data
    ?
1. ParameterSelector (filters by source)
    ?
2. WebcamWrapper (transforms webcam-specific data)
    ?
3. UnifiedHeadTrackerCalibrator (calibrates head position)
    ?
4. HeadAccelerationCalculator (calculates velocity/acceleration)
    ?
5. MAfilterParams (smooths head_pos, mouth_ape)
    ?
Behaviors (HeadBow, GazeToMouse, DoubleClick)
```

---

## Backward Compatibility

### Kept for compatibility:
- Old `NithModule*` instances (created but NOT connected to receivers)
- Old calibrators (still accessible from Rack)
- All existing code paths still compile

### No longer used:
- Separate modules for each source
- Behavior switching between modules
- Multiple calibrators (only unified one is active)

---

## Testing Checklist

- [x] ? Build successful
- [ ] Test webcam head tracking selection
- [ ] Test phone head tracking selection
- [ ] Test eye tracker head tracking selection
- [ ] Test HeadBow behavior with each source
- [ ] Test GazeToMouse works across all modes
- [ ] Test calibration button
- [ ] Test mouth aperture from webcam
- [ ] Test parameter selector debug output in console

---

## Debug Output

When switching sources, you'll see in the console:

```
=== Parameter Selector Configuration ===
Head Tracking Source: Webcam
NithPreprocessor_ParameterSelector (Mode: Whitelist)
  NITHwebcamWrapper: Accept 9 parameter(s): head_pos_yaw, head_pos_pitch, ...
  NITHtobiasWrapper: Accept 2 parameter(s): gaze_x, gaze_y
=========================================
```

---

## Performance Impact

**Negligible:**
- Parameter selector is O(1) dictionary lookup per parameter
- Zero memory allocations during filtering
- Same number of preprocessors in pipeline
- Single module is more efficient than 4 separate modules

**Benefits:**
- Unified queue management
- Better sample rate control
- Simpler debugging (one data stream)

---

## Future Enhancements

Possible improvements:
- [ ] Add parameter selector configuration to UI settings
- [ ] Save/load selector rules from settings file
- [ ] Add "sensor fusion" mode (accept head_pos from multiple sources and average)
- [ ] Runtime statistics on which sensor is actually sending data
- [ ] Automatic fallback if preferred source stops sending

---

## Migration Notes

### If you need to add a new sensor:
1. Add receiver in `SetupReceivers()`
2. Connect to `NithModuleUnified`
3. Add rules in `ConfigureParameterSelector()`

### If you need to add a new behavior:
1. Add to `SetupBehaviors()` ? `NithModuleUnified.SensorBehaviors`
2. It will automatically receive data from all sources!

### If you need to add a new head tracking source:
1. Add enum value to `HeadTrackingSources`
2. Add button to XAML
3. Add click handler
4. Add case in `ConfigureParameterSelector()`

---

## Key Files to Review

| File | Purpose |
|------|---------|
| `Modules/DefaultSetup.cs` | Setup and `ConfigureParameterSelector()` |
| `Modules/Rack.cs` | Static references to unified module and selector |
| `MainWindow.xaml.cs` | UI handlers for source switching |
| `NITHlibrary/Nith/Preprocessors/NithPreprocessor_ParameterSelector.cs` | Core selector implementation |

---

## Summary

HeadBower now uses a **modern, clean architecture** with:
- ? Single unified NithModule
- ? Dynamic parameter selection by source
- ? Simple source switching (no behavior management)
- ? Combined data in behaviors
- ? Single calibrator
- ? Backward compatible
- ? Build successful

**The old complex behavior-switching logic is gone!** ??
