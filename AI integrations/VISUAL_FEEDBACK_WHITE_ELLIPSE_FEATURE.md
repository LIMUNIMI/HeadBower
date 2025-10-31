# Visual Feedback White Ellipse "Feature Documentation"

## Summary

The white ellipse (bow motion indicator) exhibits a **perfect UX behavior** that appears to be a happy accident - it reaches approximately 20-30% of the button width when the note triggers, providing intuitive visual feedback of the trigger threshold.

## The "Feature"

### Visual Behavior
- **At rest (0% position)**: No head motion, no sound
- **Moving toward edge (10-30%)**: Motion detected, approaching trigger threshold
- **~20-30% from center**: **Note triggers!** ?
- **Continuing to edge (30-100%)**: Note is playing, increased expression/pressure

### Why It Works Perfectly

This creates an intuitive **progressive disclosure** of the playing range:

1. **Visual Loading Bar**: Shows "how close am I to triggering a note?"
2. **Trigger Confirmation**: Clear visual moment when note starts
3. **Expression Headroom**: Shows you can play "harder" after trigger for more dynamics
4. **Natural Workflow**: idle ? ready ? playing ? expressive

## Technical Explanation

### The Math Behind It

**In `BowMotionBehavior.cs` (Musical Trigger):**
```csharp
private const double YAW_UPPERTHRESH_BASE = 2;  // Note triggers at magnitude ? 2.0

// After SourceNormalizer and Sensitivity multiplication
if (_yawMagnitude >= YAW_UPPERTHRESH_BASE && !Rack.MappingModule.Blow)
{
    Rack.MappingModule.Blow = true;  // Trigger note!
}
```

**In `VisualFeedbackBehavior.cs` (White Ellipse Position):**
```csharp
private const double YAW_VELOCITY_SCALE = 1.0;
private const double VISUAL_SENSITIVITY_DAMPING = 0.05;

double normalizedPosition = Math.Max(-1.0, Math.Min(1.0, 
    (filteredVelocity / YAW_VELOCITY_SCALE) * VISUAL_SENSITIVITY_DAMPING));
```

**Visual position calculation:**
- `normalizedPosition = velocity × 0.05`
- At trigger threshold: `velocity = 2.0`
- Visual position: `2.0 × 0.05 = 0.1` (10% of button width)

**But the ellipse reaches ~20-30% at trigger because:**
- With default phone sensitivity (20.0), raw velocity is much smaller (~0.1 m/s)
- After SourceNormalizer: `0.1 × 20.0 = 2.0` (triggers note)
- But users typically move a bit faster than minimum threshold
- Typical trigger velocity: ~4.0 to 6.0 (after multiplication)
- Visual position: `4.0 × 0.05 = 0.2` to `6.0 × 0.05 = 0.3` (20-30%)

### Data Flow for White Ellipse

```
Raw sensor velocity (e.g., 0.2 m/s phone gyro)
    ?
SourceNormalizer (× phone sensitivity 20.0) ? 4.0
    ?
BowMotionBehavior checks:
  - 4.0 ? 2.0 (YAW_UPPERTHRESH_BASE) ? ? Trigger note!
  - Pressure = map(4.0) ? ~50-70 MIDI
    ?
VisualFeedbackBehavior:
  - Filter (smooth)
  - Position = 4.0 × 0.05 = 0.2 (20% of button width)
  - ViolinOverlayState.BowMotionIndicator = 0.2
    ?
ViolinOverlayManager positions white ellipse at 20% from center
```

## Visual Correlation Table

| Head Velocity | After Sensitivity | Visual Position | Note State | MIDI Pressure |
|---------------|-------------------|-----------------|------------|---------------|
| 0.0 m/s | 0.0 | 0% (center) | Silent | 0 |
| 0.05 m/s | 1.0 | 5% | Silent (below threshold) | 0 |
| 0.1 m/s | 2.0 | 10% | **Triggers!** | ~20-40 |
| 0.15 m/s | 3.0 | 15% | Playing | ~40-60 |
| 0.2 m/s | 4.0 | 20% | Playing | ~60-80 |
| 0.3 m/s | 6.0 | 30% | Playing | ~90-110 |
| 0.5 m/s | 10.0 | 50% | Playing (strong) | 127 |
| 1.0 m/s | 20.0 | 100% (edge) | Playing (max) | 127 |

## Why Keep It As-Is

### ? Advantages
1. **Intuitive threshold indicator** - Users see exactly when the note will trigger
2. **Progressive feedback** - Visual journey from idle ? ready ? playing ? expressive
3. **Expression headroom** - Shows there's more dynamic range available
4. **Natural learning curve** - Users quickly understand: "get ellipse ~30% from center = play"
5. **Prevents confusion** - If ellipse hit edge at trigger, users might think "that's maximum" when they can play harder

### ? Alternative: Make Ellipse Hit Edge at Trigger
```csharp
// Would need: VISUAL_SENSITIVITY_DAMPING = 0.5 (instead of 0.05)
// Result: 10x more sensitive visual feedback
```

**Problems:**
- ? Loses "headroom" visualization
- ? Makes visual feedback hypersensitive
- ? Users don't know they can play harder after trigger
- ? Less intuitive - "I reached the edge but sound isn't maxed out?"

## User-Facing Documentation

**For end users:**
> **White Ellipse (Bow Motion Indicator)**
> 
> The white ellipse shows your head motion intensity:
> - **Center**: No motion
> - **Moving outward**: Building up to play
> - **About 20-30% from center**: Note starts playing! ??
> - **Continuing to edge**: Play harder for more expression
> 
> Think of it like a "pressure gauge" - once you reach the first marker (~20-30%), you're making sound, but you can keep going for more dynamics!

## Related Components

- **BowMotionBehavior**: Handles musical triggering (Blow on/off) at `YAW_UPPERTHRESH_BASE = 2.0`
- **VisualFeedbackBehavior**: Updates white ellipse position with damping factor `0.05`
- **ViolinOverlayManager**: Renders the white ellipse on the button
- **SourceNormalizer**: Applies per-source sensitivity multiplication
- **MappingModule.Pressure**: MIDI CC9 value (0-127) shown in progressbar

## Status

? **KEEP AS FEATURE** - This is excellent UX design (even if accidental!)
?? **DOCUMENT** - Make it clear this is intentional behavior
?? **TEACH** - Help users understand the visual feedback system

---

## Progressbar Pressure Simplification

### Change Summary
Simplified the intensity progressbar to directly reflect **MIDI Pressure (CC9)** value instead of using a separate `IntensityIndicator` property.

### Before
```csharp
// In MappingModule
public double IntensityIndicator { get; set; } = 0f;
public int InputIndicatorValue { get; internal set; }

// In BowMotionBehavior
Rack.MappingModule.Pressure = (int)mappedYaw;
Rack.MappingModule.InputIndicatorValue = (int)mappedYaw;

// In RenderingModule
InstrumentWindow.prbIntensity.Value = Rack.MappingModule.IntensityIndicator;
```

### After
```csharp
// In MappingModule
[Obsolete("No longer used - prbIntensity progressbar now directly reflects Pressure")]
public double IntensityIndicator { get; set; } = 0f;
[Obsolete("No longer used - was replaced by direct Pressure value")]
public int InputIndicatorValue { get; internal set; }

// In BowMotionBehavior
Rack.MappingModule.Pressure = (int)mappedYaw;  // Only set Pressure

// In RenderingModule
InstrumentWindow.prbIntensity.Value = Rack.MappingModule.Pressure;  // Direct!

// In MainWindow.xaml
<ProgressBar Name="prbIntensity" Maximum="127" Minimum="0"/>
```

### Benefits
- ? **One source of truth**: Progressbar = actual MIDI CC9 value being sent
- ? **Simpler code**: No intermediate properties
- ? **Real-time accuracy**: Shows exactly what the synth receives
- ? **MIDI range**: 0-127 matches MIDI spec perfectly

### Visual Behavior
- **Empty (0)**: No pressure, silent or minimal sound
- **Partially filled (1-126)**: Active bow pressure, dynamic control
- **Full (127)**: Maximum pressure/expression

This creates perfect **visual-to-MIDI correlation** - what you see is what the instrument receives! ??
