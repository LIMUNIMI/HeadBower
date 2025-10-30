# HeadBower Critical Fix: Unified Head Motion Calculator

## Problem Identified

The previous architecture had a fundamental flaw in how head motion data (position ? velocity ? acceleration) was being handled:

1. **Webcam sends ONLY position**, but the selector was looking for velocity
2. **Phone sends velocity** (not acceleration as wrongly assumed), but this wasn't being converted to acceleration
3. **Eye Tracker sends position**, but this wasn't being converted through the derivative chain

The old `HeadAccelerationCalculator` only handled velocity ? acceleration, missing the initial position ? velocity step needed by webcam and eye tracker.

## Solution: Unified `NithPreprocessor_HeadMotionCalculator`

A new preprocessor that handles the **complete derivative chain**:

```
Position ? Velocity ? Acceleration
```

### How It Works

The calculator processes each axis (yaw, pitch, roll) sequentially:

1. **Step 1: Position ? Velocity**
   - If position exists but velocity doesn't, calculates velocity as the derivative
   - Applies exponential moving average filter for smoothing
   - Adds calculated velocity to output

2. **Step 2: Velocity ? Acceleration**
   - If velocity exists (from any source OR calculated) but acceleration doesn't, calculates acceleration
   - Applies exponential moving average filter for smoothing
   - Adds calculated acceleration to output

3. **Step 3: State Management**
   - Stores current values for next iteration
   - Maintains timestamps for accurate time-delta calculations

### Data Flow by Source

#### Webcam (NITHwebcamWrapper)
```
Input:  head_pos_*
  ?
Calculator Step 1: pos ? vel  (CALCULATED)
  ?
Calculator Step 2: vel ? acc  (CALCULATED)
  ?
Output: head_pos_*, head_vel_*, head_acc_*
```

#### Phone (NITHphoneWrapper)
```
Input:  head_vel_*
  ?
Calculator Step 1: pos ? vel  (SKIPPED - no position)
  ?
Calculator Step 2: vel ? acc  (CALCULATED)
  ?
Output: head_vel_*, head_acc_*
```

#### Eye Tracker (NITHtobiasWrapper)
```
Input:  head_pos_*
  ?
Calculator Step 1: pos ? vel  (CALCULATED)
  ?
Calculator Step 2: vel ? acc  (CALCULATED)
  ?
Output: head_pos_*, head_vel_*, head_acc_*, gaze_*
```

## Updated Parameter Selector Configuration

The `ConfigureParameterSelector()` method now reflects **what each source ACTUALLY sends**, not what will be calculated:

### Webcam Mode
```csharp
Accepted from Webcam:
  ? head_pos_yaw, head_pos_pitch, head_pos_roll (SENT)
  ? mouth_ape, eyeLeft_ape, eyeRight_ape (SENT)
  ? head_vel_* (NOT SENT - will be calculated)
  ? head_acc_* (NOT SENT - will be calculated)

Accepted from Eye Tracker:
  ? gaze_x, gaze_y (SENT)
  
Blocked from Phone: (nothing accepted)
```

### Phone Mode
```csharp
Accepted from Phone:
  ? head_vel_yaw, head_vel_pitch, head_vel_roll (SENT)
  ? head_pos_yaw, head_pos_pitch, head_pos_roll (SENT if available)
  ? head_acc_* (NOT SENT - will be calculated)

Accepted from Webcam:
  ? mouth_ape, eyeLeft_ape, eyeRight_ape (SENT)
  
Accepted from Eye Tracker:
  ? gaze_x, gaze_y (SENT)
```

### Eye Tracker Mode
```csharp
Accepted from Eye Tracker:
  ? head_pos_yaw, head_pos_pitch, head_pos_roll (SENT)
  ? gaze_x, gaze_y (SENT)
  ? head_vel_* (NOT SENT - will be calculated)
  ? head_acc_* (NOT SENT - will be calculated)

Accepted from Webcam:
  ? mouth_ape, eyeLeft_ape, eyeRight_ape (SENT)
  
Blocked from Phone: (nothing accepted)
```

## Key Insight

**When using a parameter selector BEFORE preprocessors that CREATE data:**
- Accept only the parameters that are **ACTUALLY PRESENT** in the data stream at that point
- NOT the parameters that will be created later in the pipeline

The selector filters at the beginning, so you must accept what's actually there, not what will be derived.

## Preprocessor Pipeline (Updated)

```
1. ParameterSelector 
   ? (filters by source)
   
2. WebcamWrapper 
   ? (transforms aperture data)
   
3. UnifiedHeadTrackerCalibrator 
   ? (calibrates head position)
   
4. ? NEW: HeadMotionCalculator 
   ? (pos?vel?acc derivation)
   
5. MAfilterParams 
   ? (smoothing)
   
Behaviors (now have complete motion data)
```

## Configuration in DefaultSetup.cs

```csharp
// 4. Unified motion calculator - calculates velocity from position, and acceleration from velocity
// This handles all sources: webcam (pos?vel?acc), phone (vel?acc), eye tracker (pos?vel?acc)
Rack.NithModuleUnified.Preprocessors.Add(new NithPreprocessor_HeadMotionCalculator(
    filterAlpha: 0.2f,                  // Exponential smoothing factor
    velocitySensitivity: 1.0f,          // Multiplier for calculated velocities
    accelerationSensitivity: 0.2f));    // Multiplier for calculated accelerations
```

## What Gets Fixed

? **Webcam now works**: position ? velocity ? acceleration complete chain  
? **Phone velocity is converted to acceleration**: proper motion signal  
? **Eye tracker works**: position ? velocity ? acceleration complete chain  
? **All behaviors receive consistent acceleration data**: regardless of source  
? **No data loss**: only missing derivatives are calculated, existing data is preserved  

## Testing

When you switch between sources, the console should show:

```
=== Parameter Selector Configuration ===
Head Tracking Source: Webcam
NithPreprocessor_ParameterSelector (Mode: Whitelist)
  NITHwebcamWrapper: Accept 6 parameter(s): head_pos_yaw, head_pos_pitch, ...
  NITHtobiasWrapper: Accept 2 parameter(s): gaze_x, gaze_y
=========================================
```

The HeadMotionCalculator will then add:
- `head_vel_*` from `head_pos_*`
- `head_acc_*` from calculated `head_vel_*`

## Files Modified

- `NITHlibrary/Nith/Preprocessors/NithPreprocessor_HeadMotionCalculator.cs` - NEW
- `HeadBower/Modules/DefaultSetup.cs` - UPDATED (replaced old calculator, updated selector config)

## Status

? **IMPLEMENTED** - Build successful, ready for testing
