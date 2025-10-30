# Visual Feedback Behavior Unification

## Summary

Successfully refactored visual feedback from being scattered across multiple musical control behaviors into a **single, dedicated `VisualFeedbackBehavior`**.

## Problem

**Before:** Visual feedback was mixed with musical logic:
- `BowMotionIndicatorBehavior` ? White ellipse (yaw velocity)
- `BowPressureControlBehavior` ? Red rectangle + yellow lines (pitch position) **BUT only when playing!**
- `ModulationControlBehavior` ? Also tried to update red rectangle (overwrite conflict!)

**Issues:**
1. ? Visual feedback disappeared when not playing (`if (!Blow) return`)
2. ? Two behaviors overwriting each other's visual updates
3. ? Pitch threshold hardcoded in multiple places (15.0, 50.0)
4. ? Sensitivity scattered across behaviors
5. ? Mixing concerns: MIDI control + visual feedback

## Solution

**After:** Clean separation of concerns:
```
MUSICAL LOGIC BEHAVIORS:
??> BowMotionBehavior          ? Note triggering (Blow on/off)
??> ModulationControlBehavior  ? MIDI CC1 (when enabled & playing)
??> BowPressureControlBehavior ? MIDI CC9 (when playing)

VISUAL FEEDBACK BEHAVIOR:
??> VisualFeedbackBehavior     ? ALL visual feedback (ALWAYS runs)
    ??> White ellipse position (yaw velocity)
    ??> Red rectangle position (pitch position)
    ??> Yellow threshold lines (pitch threshold)
```

---

## Architecture Changes

### 1. **New Unified Behavior**

**File:** `Behaviors/HeadBow/VisualFeedbackBehavior.cs`

```csharp
public class VisualFeedbackBehavior : INithSensorBehavior
{
    public void HandleData(NithSensorData nithData)
    {
        UpdateBowMotionIndicator(nithData);   // White ellipse
        UpdatePitchIndicator(nithData);       // Red rectangle
        UpdateThresholds();                   // Yellow lines
    }
}
```

**Features:**
- ? **Always runs** - regardless of `Blow` state
- ? **Normalized output** - all values -1 to +1 for easy rendering
- ? **Source-aware sensitivity** - reads from UserSettings based on active source
- ? **Shared threshold** - reads from UserSettings.PitchThreshold

### 2. **Centralized Settings**

**File:** `Settings/UserSettings.cs`

**Added properties:**
```csharp
public double PitchThreshold { get; set; } = 15.0;  // Threshold for all behaviors
public double PitchRange { get; set; } = 50.0;      // Max range for normalization
```

**Benefits:**
- ? **Single source of truth** - change threshold in one place
- ? **User-configurable** - can be exposed in UI later
- ? **Persisted** - auto-saves with other settings

### 3. **Updated Visual State**

**File:** `Visuals/ViolinOverlayState.cs`

**Changed from:**
```csharp
public double PitchPosition { get; set; }       // Raw value (-10 to +10)
public double PitchThreshold { get; set; }      // Raw value (15.0)
```

**To:**
```csharp
public double PitchPosition { get; set; }       // Normalized (-1 to +1)
public double PitchThreshold { get; set; }      // Normalized (0 to 1)
```

**Benefits:**
- ? **Rendering-ready** - no conversion needed in ViolinOverlayManager
- ? **Source-independent** - works same for all sources
- ? **Cleaner code** - no hardcoded maxDeviation

### 4. **Simplified Rendering**

**File:** `Visuals/ViolinOverlayManager.cs`

**Before:**
```csharp
double maxDeviation = 50.0;  // Hardcoded!
double effectiveDeviation = Math.Clamp(pitchPosition / maxDeviation, -1.0, 1.0);
```

**After:**
```csharp
// pitchPosition is already normalized -1 to +1
double newPitchY = middle - (pitchPosition * available);
```

---

## Data Flow

### White Ellipse (Bow Motion Indicator)
```
Phone/Webcam sends head_vel_yaw
    ?
ParameterSelector (whitelists based on source)
    ?
SourceNormalizer (applies per-source sensitivity)
    ?
VisualFeedbackBehavior:
  ??> Filter (? = 0.85)
  ??> Get current source sensitivity
  ??> Normalize: vel / (10.0 / sensitivity) ? -1 to +1
  ??> ViolinOverlayState.BowMotionIndicator = normalized
    ?
RenderingModule (60 FPS)
    ?
ViolinOverlayManager positions white ellipse
```

### Red Rectangle (Pitch Indicator)
```
Phone/Webcam sends head_pos_pitch
    ?
ParameterSelector (whitelists based on source)
    ?
HeadTrackerCalibrator (applies calibration offset)
    ?
SourceNormalizer (applies per-source sensitivity)
    ?
MAfilterParams (smooths pitch, ? = 0.5)
    ?
VisualFeedbackBehavior:
  ??> Filter (? = 0.9)
  ??> Normalize: pitch / PitchRange ? -1 to +1
  ??> ViolinOverlayState.PitchPosition = normalized
    ?
RenderingModule (60 FPS)
    ?
ViolinOverlayManager positions red rectangle
```

### Yellow Lines (Thresholds)
```
VisualFeedbackBehavior:
  ??> Read UserSettings.PitchThreshold (e.g., 15.0)
  ??> Read UserSettings.PitchRange (e.g., 50.0)
  ??> Normalize: threshold / range (15.0 / 50.0 = 0.3)
  ??> ViolinOverlayState.PitchThreshold = 0.3
    ?
RenderingModule (60 FPS)
    ?
ViolinOverlayManager positions yellow lines at ±30% height
```

---

## Musical Behaviors (Cleaned Up)

### BowPressureControlBehavior
**Before:**
```csharp
if (!Rack.MappingModule.Blow) return;  // Exits early!

// Updates visual feedback
Rack.ViolinOverlayState.PitchPosition = filteredPitch;
Rack.ViolinOverlayState.PitchThreshold = PITCH_THRESHOLD;

// Hardcoded threshold
const double PITCH_THRESHOLD = 15.0;
```

**After:**
```csharp
if (!Rack.MappingModule.Blow)
{
    Rack.MidiModule.SendControlChange(9, 0);
    return;  // Only returns AFTER sending CC9=0
}

// NO visual feedback updates!

// Shared threshold from settings
double pitchThreshold = Rack.UserSettings.PitchThreshold;
double maxPitchDeviation = Rack.UserSettings.PitchRange;
```

### ModulationControlBehavior
**Before:**
```csharp
if (!Blow || !ModulationOn) return;

// Updates visual feedback (overwrites BowPressure!)
Rack.ViolinOverlayState.PitchPosition = filteredPitch;

// Hardcoded threshold
const double PITCH_THRESHOLD = 15.0;
```

**After:**
```csharp
if (!Blow || !ModulationOn)
{
    Rack.MappingModule.Modulation = 0;
    return;
}

// NO visual feedback updates!

// Shared threshold from settings
double pitchThreshold = Rack.UserSettings.PitchThreshold;
```

---

## Behavior Order (Important!)

**In `DefaultSetup.cs`:**
```csharp
// Musical logic first (order doesn't matter much)
Rack.NithModuleUnified.SensorBehaviors.Add(Rack.Behavior_BowMotion);
Rack.NithModuleUnified.SensorBehaviors.Add(Rack.Behavior_ModulationControl);
Rack.NithModuleUnified.SensorBehaviors.Add(Rack.Behavior_BowPressureControl);
Rack.NithModuleUnified.SensorBehaviors.Add(Rack.Behavior_HapticFeedback);
Rack.NithModuleUnified.SensorBehaviors.Add(Rack.GazeToMouse);
Rack.NithModuleUnified.SensorBehaviors.Add(new EBBdoubleCloseClick());

// Visual feedback LAST (ensures it always updates)
Rack.NithModuleUnified.SensorBehaviors.Add(Rack.Behavior_VisualFeedback);
```

**Why last?**
- ? Musical behaviors can't interfere with visual state
- ? Always gets the final say on what's displayed
- ? Runs every frame, even if musical behaviors exit early

---

## Sensitivity Handling

### Old (Scattered)
```csharp
// BowMotionBehavior
Rack.Behavior_BowMotion.Sensitivity = Rack.UserSettings.SensorIntensityHead;

// BowMotionIndicatorBehavior
Rack.Behavior_BowMotionIndicator.Sensitivity = Rack.UserSettings.SensorIntensityHead;

// HapticFeedbackBehavior
Rack.Behavior_HapticFeedback.Sensitivity = Rack.UserSettings.SensorIntensityHead;
```

### New (Centralized)
```csharp
// Musical behaviors still use SensorIntensityHead
Rack.Behavior_BowMotion.Sensitivity = Rack.UserSettings.SensorIntensityHead;
Rack.Behavior_HapticFeedback.Sensitivity = Rack.UserSettings.SensorIntensityHead;

// Visual feedback reads per-source sensitivity automatically
float sensitivity = Rack.UserSettings.HeadTrackingSource switch
{
    HeadTrackingSources.Webcam => Rack.UserSettings.WebcamSensitivity,
    HeadTrackingSources.Phone => Rack.UserSettings.PhoneSensitivity,
    HeadTrackingSources.EyeTracker => Rack.UserSettings.EyeTrackerSensitivity,
    _ => 1.0f
};
```

**Benefits:**
- ? Visual feedback matches data sensitivity
- ? No need to manually sync multiple behaviors
- ? Consistent with SourceNormalizer approach

---

## Removed / Deprecated

### Removed Behavior
- ? `BowMotionIndicatorBehavior` - functionality merged into `VisualFeedbackBehavior`

**In `Rack.cs`:**
```csharp
[Obsolete("Use Behavior_VisualFeedback instead - visual feedback is now unified")]
public static BowMotionIndicatorBehavior Behavior_BowMotionIndicator { get; set; }
```

### Removed Constants
From `BowPressureControlBehavior.cs`:
```csharp
// Removed (now in UserSettings):
// private const double MAX_PITCH_DEVIATION = 50.0;
// private const double PITCH_THRESHOLD = 15.0;
```

From `ModulationControlBehavior.cs`:
```csharp
// Removed (now in UserSettings):
// private const double MAX_PITCH_DEVIATION = 50.0;
// private const double PITCH_THRESHOLD = 15.0;
```

---

## What Gets Fixed

### Phone Head Pitch Issue ?
**Problem:** Red rectangle didn't move with phone headtracking
**Root cause:** `BowPressureControlBehavior` returned early when `!Blow`
**Solution:** `VisualFeedbackBehavior` always runs, updates visual state every frame

### Visual Feedback Always Active ?
**Before:** Red rectangle disappeared when not playing
**After:** Red rectangle and white ellipse ALWAYS visible and responsive

### No More Overwrites ?
**Before:** `ModulationControlBehavior` and `BowPressureControlBehavior` fought for control
**After:** Only `VisualFeedbackBehavior` updates visual state

### Shared Threshold ?
**Before:** Three places had hardcoded `15.0` and `50.0`
**After:** One place in `UserSettings`, easily changeable

---

## Testing Checklist

### Phone Headtracking
- [ ] ? White ellipse moves with yaw velocity (horizontal)
- [ ] ? Red rectangle moves with pitch position (vertical) **EVEN WHEN NOT PLAYING**
- [ ] ? Yellow threshold lines at correct position
- [ ] ? Visual feedback works before playing first note
- [ ] ? Bow pressure MIDI CC9 works when playing

### Webcam Headtracking
- [ ] ? White ellipse moves with yaw velocity
- [ ] ? Red rectangle moves with pitch position **EVEN WHEN NOT PLAYING**
- [ ] ? Yellow threshold lines at correct position

### Sensitivity
- [ ] ? Phone sensitivity affects visual feedback correctly
- [ ] ? Webcam sensitivity affects visual feedback correctly
- [ ] ? SensorIntensityHead affects musical behaviors only

---

## Files Modified

| File | Changes |
|------|---------|
| `Behaviors/HeadBow/VisualFeedbackBehavior.cs` | **NEW** - Unified visual feedback |
| `Settings/UserSettings.cs` | Added `PitchThreshold` and `PitchRange` |
| `Visuals/ViolinOverlayState.cs` | Changed to normalized values |
| `Visuals/ViolinOverlayManager.cs` | Simplified to use normalized values |
| `Behaviors/HeadBow/BowPressureControlBehavior.cs` | Removed visual updates, use shared threshold |
| `Behaviors/HeadBow/ModulationControlBehavior.cs` | Removed visual updates, use shared threshold |
| `Modules/DefaultSetup.cs` | Use `VisualFeedbackBehavior`, remove `BowMotionIndicatorBehavior` |
| `Modules/Rack.cs` | Added `Behavior_VisualFeedback`, marked old as obsolete |

---

## Status

? **COMPLETE** - Build successful, ready for testing
?? **Clean Architecture** - Single responsibility, no mixed concerns
?? **Better Separation** - Visual feedback completely independent from musical logic
