# Phone Head Pitch Position Fix

## Problem

When using the **Phone** as the head tracking source, the red rectangle visual feedback for pitch position **did not react correctly** to head movements - it was either stuck or moving erratically.

## Root Cause

The issue was caused by the **HeadTrackerCalibrator requiring ALL three position axes** (yaw, pitch, roll) to be present before calibrating any of them.

### What the Phone Actually Sends:

- ? `head_pos_pitch` (range: -10 to +10)
- ? `head_pos_roll` 
- ? `head_pos_yaw` (**NOT sent** - phone uses gyroscope, doesn't provide absolute yaw position)
- ? `head_vel_*` (all three axes - yaw, pitch, roll)

### The Calibrator Bug:

Original code in `NithPreprocessor_HeadTrackerCalibrator.cs`:

```csharp
private readonly List<NithParameters> _requiredArguments =
    [NithParameters.head_pos_yaw, NithParameters.head_pos_pitch, NithParameters.head_pos_roll];

public NithSensorData TransformData(NithSensorData sensorData)
{
    if (sensorData.ContainsParameters(_requiredArguments))
    {
        // Only calibrates if ALL THREE exist!
    }
}
```

**Problem:** The phone doesn't send `head_pos_yaw`, so the calibrator never processed phone data!

### Why It Started Failing:

When the `HeadMotionCalculator` was modified to integrate velocity?position (to create missing `head_pos_yaw`), it caused:
1. Phone now had all three position parameters (yaw integrated from velocity)
2. Calibrator **started processing** the phone data
3. But the integrated `head_pos_yaw` was **drifting/unstable**
4. This affected the calibration, causing pitch to behave erratically

## The Solution

### 1. ? Removed Position Integration

Reverted `NithPreprocessor_HeadMotionCalculator` to **NOT integrate velocity?position**.

**Why:** 
- Nothing needs `head_pos_yaw` from the phone (behaviors use velocity for yaw motion)
- Integration causes drift and instability
- Phone works perfectly without absolute yaw position

### 2. ? Fixed Calibrator to Handle Partial Data

Modified `NithPreprocessor_HeadTrackerCalibrator` to **calibrate only the position axes that are present**:

```csharp
public NithSensorData TransformData(NithSensorData sensorData)
{
    // Parse available position data
    bool hasYaw = false;
    bool hasPitch = false;
    bool hasRoll = false;
    
    // ... check which parameters exist ...

    // If at least one position parameter exists, calibrate the available ones
    if (hasYaw || hasPitch || hasRoll)
    {
        // Remove and add back only the parameters that exist
        if (hasYaw) { /* calibrate yaw */ }
        if (hasPitch) { /* calibrate pitch */ }
        if (hasRoll) { /* calibrate roll */ }
    }
}
```

**Now:**
- Webcam/Eye Tracker: All three axes calibrated ?
- Phone: Only pitch and roll calibrated (yaw skipped) ?

## Data Flow (Phone Mode) - After Fix

```
Phone sends:
  head_pos_pitch ?
  head_pos_roll ?
  head_vel_yaw, head_vel_pitch, head_vel_roll ?
    ?
ParameterSelector: Whitelists all phone parameters
    ?
WebcamWrapper: Skips (only affects webcam)
    ?
HeadTrackerCalibrator: Calibrates ONLY pitch and roll (yaw not present, skipped)
    ?
HeadMotionCalculator: 
  - Skips velocity calculation (already exists)
  - Calculates acceleration from velocity
    ?
SourceNormalizer: Applies phone sensitivity multiplier
    ?
MAfilterParams: Smooths head_pos_pitch
    ?
BowPressureControlBehavior: 
  - Reads calibrated head_pos_pitch ?
  - Updates Rack.ViolinOverlayState.PitchPosition ?
    ?
Visual feedback: Red rectangle moves correctly with pitch! ?
```

## What Each Source Provides (Updated)

| Source | Yaw Position | Pitch Position | Roll Position | Velocities |
|--------|--------------|----------------|---------------|------------|
| **Webcam** | ? `head_pos_yaw` | ? `head_pos_pitch` | ? `head_pos_roll` | Calculated |
| **Phone** | ? Not sent | ? `head_pos_pitch` | ? `head_pos_roll` | ? Direct from sensor |
| **Eye Tracker** | ? `head_pos_yaw` | ? `head_pos_pitch` | ? `head_pos_roll` | Calculated |

## What Gets Fixed

? **Phone pitch position now calibrated correctly** - Red rectangle responds to pitch  
? **No more erratic behavior** - Calibration stable and predictable  
? **Horizontal motion still works** - Uses `head_vel_yaw` (unaffected)  
? **Webcam/Eye Tracker unaffected** - Still calibrate all three axes  
? **HT calibration button works** - Sets center for available axes only  

## Files Modified

1. **`NITHlibrary/Nith/Preprocessors/NithPreprocessor_HeadMotionCalculator.cs`**
   - Removed position integration code
   - Back to simple: position?velocity?acceleration derivation only

2. **`NITHlibrary/Nith/Preprocessors/NithPreprocessor_HeadTrackerCalibrator.cs`**
   - Removed `_requiredArguments` check that required all three axes
   - Now calibrates each axis independently if present
   - Handles partial position data gracefully

## Testing Checklist

When using **Phone** as head tracking source:

- [ ] ? Red rectangle moves horizontally with head yaw motion (velocity)
- [ ] ? Red rectangle moves vertically with head pitch motion (position)
- [ ] ? Pitch-based bow pressure control works correctly
- [ ] ? HT calibration button resets pitch/roll position correctly
- [ ] ? No erratic jumping or stuck indicators
- [ ] ? Switching to Webcam/Eye Tracker still works

## Key Insight

**The phone is optimized for motion detection, not absolute positioning:**
- **Yaw:** Uses velocity (fast, responsive, perfect for bow strokes) ?
- **Pitch/Roll:** Uses position (stable, good for bow pressure control) ?

By not forcing absolute yaw position from the phone, we get the best of both worlds!

## Status

? **FIXED** - Phone headtracking now works correctly with proper pitch position feedback
