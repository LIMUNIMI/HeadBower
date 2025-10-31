# Mouth Gate Centralized Fix

## Problem

The mouth closed note prevention feature wasn't working properly - notes would flicker when trying to play with mouth closed. The issue was that `MouthClosedNotePreventionBehavior` was trying to override `Blow = false` AFTER `BowMotionBehavior` had already set `Blow = true`, creating a race condition where both behaviors were fighting each other.

**Root Cause:**
- Behaviors run sequentially, not atomically
- `BowMotionBehavior` continuously sets `Blow = true` when yaw velocity exceeds threshold
- `MouthClosedNotePreventionBehavior` runs after and tries to override with `Blow = false`
- On the next frame, `BowMotionBehavior` sets it back to `true` again
- Result: flickering notes that turn on/off rapidly

## Solution

Implemented a **centralized gating mechanism** at the property level in `MappingModule` with **hysteresis-based mouth aperture control**:

### 1. Added Gate Flag in MappingModule

```csharp
/// <summary>
/// Indicates whether the mouth gate is currently blocking note activation.
/// When true, the Blow property will refuse to activate (set to true).
/// Set by MouthClosedNotePreventionBehavior based on mouth aperture (mouth_ape) with hysteresis:
/// - Gate closes when mouth_ape falls below 10
/// - Gate opens when mouth_ape rises above 15
/// </summary>
public bool IsMouthGateBlocking { get; set; } = false;
```

### 2. Modified Blow Property Setter

```csharp
public bool Blow
{
    get { return blow; }
    set
    {
        // MOUTH GATE: Block activation if mouth is closed (when feature is enabled)
        if (value == true && IsMouthGateBlocking)
        {
            // Mouth is closed - refuse activation
            // Also ensure we stop if somehow we're still playing
            if (blow == true)
            {
                blow = false;
                StopSelectedNote();
            }
            return;
        }

        // ...rest of the setter logic...
    }
}
```

**Key Points:**
- ? Gate check happens BEFORE any note logic executes
- ? Early return prevents state changes when mouth is closed
- ? ALL behaviors are automatically gated without modification
- ? No race conditions - the property itself enforces the gate
- ? **Critical:** If somehow playing when gate activates, force immediate stop

### 3. Updated MouthClosedNotePreventionBehavior - Hysteresis Control

Changed from boolean `mouth_isOpen` to **double-threshold hysteresis** using `mouth_ape`:

```csharp
private const double MOUTH_APERTURE_LOWER_THRESHOLD = 10.0;  // Gate closes below this
private const double MOUTH_APERTURE_UPPER_THRESHOLD = 15.0;  // Gate opens above this

public void HandleData(NithSensorData nithData)
{
    if (Rack.UserSettings.MouthClosedNotePreventionMode != _MouthClosedNotePreventionModes.On)
    {
        // Feature disabled - open the gate
        Rack.MappingModule.IsMouthGateBlocking = false;
        return;
    }

    if (nithData.ContainsParameters(requiredParams)) // mouth_ape
    {
        double mouthAperture = nithData.GetParameterValue(NithParameters.mouth_ape).Value.ValueAsDouble;

        // Apply hysteresis logic:
        if (Rack.MappingModule.IsMouthGateBlocking)
        {
            // Gate is currently blocking - only open if aperture exceeds upper threshold
            if (mouthAperture > MOUTH_APERTURE_UPPER_THRESHOLD)
            {
                Rack.MappingModule.IsMouthGateBlocking = false;
            }
        }
        else
        {
            // Gate is currently open - only close if aperture falls below lower threshold
            if (mouthAperture < MOUTH_APERTURE_LOWER_THRESHOLD)
            {
                Rack.MappingModule.IsMouthGateBlocking = true;
            }
        }
    }
    else
    {
        // If parameters missing, open the gate
        Rack.MappingModule.IsMouthGateBlocking = false;
    }
}
```

**Hysteresis Benefits:**
- ?? **No flickering between thresholds** - Once gate state changes, it stays stable until opposite threshold crossed
- ?? **Manual control** - Uses continuous mouth_ape value instead of derived boolean
- ?? **Adjustable sensitivity** - Thresholds can be tuned independently (10 = close, 15 = open)
- ?? **Prevents rapid toggling** - 5-unit deadband between thresholds

### 4. Reordered Behavior Execution

**Critical:** `MouthClosedNotePreventionBehavior` MUST run BEFORE `BowMotionBehavior`:

```csharp
// Add behaviors to unified module
// CRITICAL ORDER: MouthClosedNotePrevention MUST run BEFORE BowMotion to set gate state
Rack.NithModuleUnified.SensorBehaviors.Add(Rack.Behavior_MouthClosedNotePrevention); // FIRST - sets gate state
Rack.NithModuleUnified.SensorBehaviors.Add(Rack.Behavior_BowMotion);                 // SECOND - respects gate
Rack.NithModuleUnified.SensorBehaviors.Add(Rack.Behavior_ModulationControl);
Rack.NithModuleUnified.SensorBehaviors.Add(Rack.Behavior_BowPressureControl);
Rack.NithModuleUnified.SensorBehaviors.Add(Rack.Behavior_HapticFeedback);
```

## Architecture Benefits

### ? Separation of Concerns
- **Gate behavior**: Sets the gate state based on mouth aperture sensor
- **Musical behaviors**: Focus on musical logic, automatically respect gate
- **Property**: Enforces the gate rule centrally AND handles cleanup

### ? Hysteresis Stability
- **Deadband zone (10-15)**: Prevents flickering when mouth aperture hovers near threshold
- **State persistence**: Once closed, stays closed until aperture > 15; once open, stays open until aperture < 10
- **Smooth transitions**: Natural feel when opening/closing mouth

### ? Extensibility
- Any future behavior that tries to set `Blow = true` will be automatically gated
- No need to modify each behavior individually
- Easy to add additional gating conditions (e.g., eye closure, head tilt limits)
- Thresholds are constants that can be exposed to UI for user adjustment

### ? Predictability
- Gate state is set once per frame (by gate behavior)
- All subsequent behaviors see the same gate state
- No race conditions or timing issues
- Property itself handles edge cases (already playing when gate closes)

## Data Flow

```
Frame N (mouth_ape = 8):
  1. MouthClosedNotePreventionBehavior runs
     ? Reads mouth_ape = 8
     ? Current state: IsMouthGateBlocking = false (open)
     ? 8 < 10 (lower threshold) ? Close gate
     ? Sets IsMouthGateBlocking = true
  
  2. BowMotionBehavior runs
     ? Detects yaw velocity above threshold
     ? Attempts: Blow = true
     ? Property checks: if (value == true && IsMouthGateBlocking)
        ? Gate is blocking!
        ? if (blow == true): Force stop and cleanup
        ? return; (refuse the change)
     ? Result: Note does NOT activate

Frame N+1 (mouth_ape = 12 - in deadband):
  1. MouthClosedNotePreventionBehavior runs
     ? Reads mouth_ape = 12
     ? Current state: IsMouthGateBlocking = true (blocking)
     ? 12 NOT > 15 (upper threshold) ? Stay blocked
     ? IsMouthGateBlocking remains true
  
  2. BowMotionBehavior runs
     ? Still blocked, note still doesn't activate

Frame N+2 (mouth_ape = 18):
  1. MouthClosedNotePreventionBehavior runs
     ? Reads mouth_ape = 18
     ? Current state: IsMouthGateBlocking = true (blocking)
     ? 18 > 15 (upper threshold) ? Open gate
     ? Sets IsMouthGateBlocking = false
  
  2. BowMotionBehavior runs
     ? Gate is open
     ? Blow = true succeeds ? Note plays!
```

## Hysteresis Graph

```
Mouth Aperture Value:
  0        10       15        100
  |--------|--------|----------|
      ?         ?          ?
    CLOSE   DEADBAND    OPEN

Gate State Transitions:
  - When aperture < 10: Gate CLOSES (blocks notes)
  - When 10 ? aperture ? 15: Gate MAINTAINS current state (stable)
  - When aperture > 15: Gate OPENS (allows notes)
```

## The Flickering Fix

**Why the flickering happened (original):**
1. `MouthClosedNotePreventionBehavior` set `Blow = false` (allowed, no gate check)
2. `BowMotionBehavior` tried to set `Blow = true` (blocked by gate)
3. Next frame: repeat steps 1-2
4. Result: Blow kept switching false?false (allowed)?blocked?false?false...

**Why it's fixed now:**
1. `MouthClosedNotePreventionBehavior` sets `IsMouthGateBlocking = true` (doesn't touch Blow)
2. `BowMotionBehavior` tries to set `Blow = true`
3. Property setter sees gate is blocking
4. Property setter checks if `blow == true` (playing) ? if so, cleanly stops
5. Property setter returns early without changing state
6. Next frame: same stable behavior (no flip-flopping)

**Key insight:** The behavior doesn't fight the property - it just sets the gate flag. The property handles all the logic consistently.

**Hysteresis advantage:** Even if mouth aperture fluctuates between 11-14, gate state remains stable - no rapid state changes!

## Testing Scenarios

### ? Mouth Closed (< 10) + Head Movement
- **Before:** Flickering notes (on/off/on/off)
- **After:** No notes play, stable state, clean stop if playing

### ? Mouth Slightly Open (10-15) + Head Movement
- **After:** Gate maintains previous state - stable behavior in deadband zone

### ? Mouth Open (> 15) + Head Movement
- **Before:** Works correctly
- **After:** Still works correctly

### ? Playing ? Close Mouth (< 10) ? Keep Moving Head
- **Before:** Flickering
- **After:** Immediate clean stop when crosses lower threshold, stays stopped

### ? Closed ? Open Mouth Slowly Through Deadband (8?12?18)
- **After:** Gate closes at 8, stays closed through deadband (12), opens at 18 - smooth transition

### ? Feature Disabled
- **Before:** Notes play normally
- **After:** Notes play normally (gate always open)

### ? Missing mouth_ape Parameter
- **Before:** Behavior would crash or interfere
- **After:** Gate opens automatically (failsafe)

## Threshold Configuration

Current values:
- **Lower threshold: 10** - Gate closes (blocks notes)
- **Upper threshold: 15** - Gate opens (allows notes)
- **Deadband: 5 units** - Stability zone

**To adjust thresholds**, modify constants in `MouthClosedNotePreventionBehavior.cs`:
```csharp
private const double MOUTH_APERTURE_LOWER_THRESHOLD = 10.0;
private const double MOUTH_APERTURE_UPPER_THRESHOLD = 15.0;
```

**Future enhancement:** Expose these as UserSettings properties for runtime adjustment via UI.

## Related Files

- `Modules/MappingModule.cs` - Gate flag and Blow property gating logic with cleanup
- `Behaviors/HeadBow/MouthClosedNotePreventionBehavior.cs` - Hysteresis-based gate control using mouth_ape
- `Modules/DefaultSetup.cs` - Behavior registration order
- `Settings/UserSettings.cs` - MouthClosedNotePreventionMode enum

## Future Improvements

### Multiple Gating Conditions

This pattern can be extended for other gating conditions:

```csharp
// Example: Multiple gate conditions
if (value == true && (IsMouthGateBlocking || IsHeadTiltGateBlocking || IsEyeClosedGateBlocking))
{
    // Cleanup if playing
    if (blow == true)
    {
        blow = false;
        StopSelectedNote();
    }
    return; // Any gate blocks activation
}
```

### Configurable Hysteresis

```csharp
// Example: User-adjustable thresholds
private double LowerThreshold => Rack.UserSettings.MouthGateLowerThreshold;
private double UpperThreshold => Rack.UserSettings.MouthGateUpperThreshold;
```

### Multi-Source Hysteresis

```csharp
// Example: Different thresholds for different control sources
if (Rack.UserSettings.MouthGateSource == MouthGateSources.MouthAperture)
{
    // Use mouth_ape with thresholds 10/15
}
else if (Rack.UserSettings.MouthGateSource == MouthGateSources.HeadPitch)
{
    // Use head_pos_pitch with thresholds -20/20
}
```

## Summary

The fix implements a **centralized gating mechanism with hysteresis** where:
1. The gate behavior reads mouth aperture and sets flag using double thresholds (no Blow manipulation)
2. The property setter checks the flag BEFORE changing state
3. The property setter handles cleanup if already playing when gate closes
4. All behaviors are automatically protected without modification
5. **Hysteresis prevents flickering** when mouth aperture hovers near threshold

This is a clean, scalable solution that follows the principle of "single responsibility":
- **Gate behavior**: Reads sensor, applies hysteresis logic, sets flag
- **Property**: Enforces gate, handles state transitions
- **Musical behaviors**: Focus on music, trust the gate

**Result:** No flickering, smooth gate transitions with deadband stability, manual threshold control! ??

## Hysteresis State Machine

```
        mouth_ape < 10
    ????????????????????
    ?                  ?
    ?                  ?
?????????              ?
? GATE  ? mouth_ape > 15
? CLOSED????????????????
? (true)?              ?
?????????              ?
    ?                  ?
    ?                  ?
    ?              ?????????
    ???????????????? GATE  ?
      mouth_ape < 10?  OPEN ?
                    ?(false)?
                    ?????????
```

Deadband (10-15): No state transitions, maintains current state.
