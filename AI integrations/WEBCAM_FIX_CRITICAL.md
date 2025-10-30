# CRITICAL FIX: Webcam Not Playing Issue

## Problem

After migrating to the unified architecture, **nothing gets played with webcam** head tracking selected.

## Root Cause

**The parameter selector was blocking webcam velocity data!**

### Why it happened:

1. **Webcam sends `head_vel_*` (velocity), NOT `head_acc_*` (acceleration)**
2. **Phone sends `head_acc_*` (acceleration) directly from hardware IMU**
3. **The parameter selector configuration accepted `head_acc_*` from webcam** ?
4. **But webcam doesn't SEND acceleration - it sends VELOCITY** ?
5. **The `NithPreprocessor_HeadAccelerationCalculator` creates `head_acc_*` FROM `head_vel_*`**
6. **But the selector is BEFORE the acceleration calculator in the pipeline!**

### Pipeline Order (the issue):
```
Webcam Data (has head_vel_*, not head_acc_*)
    ?
1. ParameterSelector ? BLOCKS because looking for head_acc_*
    ?
(Data filtered out - nothing passes through!)
    ?
2. HeadAccelerationCalculator (never reached!)
```

## Solution

**Accept `head_vel_*` from webcam, NOT `head_acc_*`!**

The acceleration calculator will transform velocity?acceleration AFTER the selector lets it through.

### Corrected Pipeline:
```
Webcam Data (has head_vel_*)
    ?
1. ParameterSelector ? ALLOWS head_vel_* to pass
    ?
2. WebcamWrapper (transforms aperture data)
    ?
3. HeadTrackerCalibrator
    ?
4. HeadAccelerationCalculator ? Creates head_acc_* from head_vel_*
    ?
5. Smoothing
    ?
Behaviors (now have head_acc_* to use!)
```

## Changes Made

### Before (WRONG):
```csharp
case HeadTrackingSources.Webcam:
    Rack.ParameterSelector.AddRules("NITHwebcamWrapper",
        NithParameters.head_pos_yaw,
        NithParameters.head_pos_pitch,
        NithParameters.head_pos_roll,
        NithParameters.head_acc_yaw,     // ? WRONG! Webcam doesn't send this
        NithParameters.head_acc_pitch,   // ? WRONG!
        NithParameters.head_acc_roll,    // ? WRONG!
        ...);
```

### After (CORRECT):
```csharp
case HeadTrackingSources.Webcam:
    Rack.ParameterSelector.AddRules("NITHwebcamWrapper",
        NithParameters.head_pos_yaw,
        NithParameters.head_pos_pitch,
        NithParameters.head_pos_roll,
        NithParameters.head_vel_yaw,     // ? CORRECT! Webcam sends velocity
        NithParameters.head_vel_pitch,   // ? CORRECT!
        NithParameters.head_vel_roll,    // ? CORRECT!
        ...);
```

## Data Flow by Source

### Webcam (NITHwebcamWrapper)
**Sends:**
- `head_pos_*` (position)
- `head_vel_*` (velocity) ? calculated from position changes
- `mouth_ape`, `eye*_ape` (apertures)

**Acceleration Calculator Creates:**
- `head_acc_*` (acceleration) ? derived from velocity

### Phone (NITHphoneWrapper)
**Sends:**
- `head_pos_*` (position from gyroscope)
- `head_acc_*` (acceleration from IMU hardware) ? **DIRECT from sensor!**

**No transformation needed** - phone has hardware accelerometer

### Eye Tracker (NITHtobiasWrapper)
**Sends:**
- `head_pos_*` (position)
- `head_vel_*` (velocity) ? calculated from position
- `gaze_x`, `gaze_y` (gaze)

**Acceleration Calculator Creates:**
- `head_acc_*` (acceleration) ? derived from velocity

## Summary

| Source | Sends | Selector Should Accept | Result |
|--------|-------|----------------------|--------|
| **Webcam** | `head_vel_*` | `head_vel_*` ? | Calc creates `head_acc_*` |
| **Phone** | `head_acc_*` | `head_acc_*` ? | Already has acceleration |
| **Eye Tracker** | `head_vel_*` | `head_vel_*` ? | Calc creates `head_acc_*` |

## Lesson Learned

**When using a parameter selector BEFORE a preprocessor that CREATES parameters:**
- Accept the **INPUT parameters** that the sensor sends
- NOT the **OUTPUT parameters** that will be created later in the pipeline

The selector filters BEFORE transformation, so accept what's actually IN the data stream at that point!

## File Modified

- `Modules/DefaultSetup.cs` ? `ConfigureParameterSelector()` method
  - Webcam mode: `head_acc_*` ? `head_vel_*`
  - Eye Tracker mode: `head_acc_*` ? `head_vel_*`

## Status

? **FIXED** - Webcam head tracking now works correctly
