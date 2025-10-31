# Pitch Sensitivity Control System

## Summary

Added a **per-source pitch sensitivity control** that allows independent adjustment of pitch (head_pos_pitch) sensitivity for each head tracking source (Webcam, Phone, Eye Tracker). This complements the existing general sensitivity control and uses the SourceNormalizer preprocessor.

## Why This Feature?

### Problem
Different head tracking sources have different pitch axis behaviors:
- **Phone**: Very sensitive pitch from gyroscope, may need dampening
- **Webcam**: Moderate pitch sensitivity, may need boosting
- **Eye Tracker**: Variable pitch sensitivity based on hardware

Previously, users could only adjust **all head parameters** together (yaw, pitch, roll, velocities, accelerations). This meant:
- Boosting pitch would also boost yaw ? making bow strokes too sensitive
- Dampening pitch would also dampen yaw ? making bow strokes too insensitive

### Solution
**Independent pitch sensitivity** allows:
- Fine-tune **pitch control** (for modulation/bow pressure)
- While keeping **yaw control** (for bow motion) at optimal sensitivity
- Each head tracking source remembers its own pitch sensitivity setting

---

## Implementation

### 1. UserSettings Properties

**File:** `Settings/UserSettings.cs`

Added three new sensitivity properties:

```csharp
/// <summary>
/// Pitch sensitivity multiplier for webcam head tracking.
/// Applied specifically to head_pos_pitch parameter from the webcam.
/// Independent from general head tracking sensitivity.
/// </summary>
public float WebcamPitchSensitivity { get; set; } = 1.0f;

/// <summary>
/// Pitch sensitivity multiplier for phone head tracking.
/// Applied specifically to head_pos_pitch parameter from the phone.
/// Independent from general head tracking sensitivity.
/// </summary>
public float PhonePitchSensitivity { get; set; } = 1.0f;

/// <summary>
/// Pitch sensitivity multiplier for eye tracker head tracking.
/// Applied specifically to head_pos_pitch parameter from the eye tracker.
/// Independent from general head tracking sensitivity.
/// </summary>
public float EyeTrackerPitchSensitivity { get; set; } = 1.0f;
```

**Features:**
- ? **Auto-save**: Changes persist to Settings.json
- ? **Min validation**: Cannot go below 0.01f
- ? **PropertyChanged events**: Triggers SourceNormalizer updates
- ? **Default 1.0f**: No scaling by default

### 2. SourceNormalizer Configuration

**File:** `Modules/DefaultSetup.cs`

The SourceNormalizer applies sensitivity in two layers:

```csharp
// LAYER 1: General head motion sensitivity (all parameters)
Rack.SourceNormalizer.AddRulesForAllHeadParameters("NITHwebcamWrapper", Rack.UserSettings.WebcamSensitivity);
Rack.SourceNormalizer.AddRulesForAllHeadParameters("NITHphoneWrapper", Rack.UserSettings.PhoneSensitivity);
Rack.SourceNormalizer.AddRulesForAllHeadParameters("NITHeyetrackerWrapper", Rack.UserSettings.EyeTrackerSensitivity);

// LAYER 2: Pitch-specific sensitivity (OVERRIDES general for head_pos_pitch)
Rack.SourceNormalizer.AddRule("NITHwebcamWrapper", NithParameters.head_pos_pitch, Rack.UserSettings.WebcamPitchSensitivity);
Rack.SourceNormalizer.AddRule("NITHphoneWrapper", NithParameters.head_pos_pitch, Rack.UserSettings.PhonePitchSensitivity);
Rack.SourceNormalizer.AddRule("NITHeyetrackerWrapper", NithParameters.head_pos_pitch, Rack.UserSettings.EyeTrackerPitchSensitivity);
```

**How it works:**
1. `AddRulesForAllHeadParameters` sets multipliers for ALL head parameters (yaw, pitch, roll, velocities, accelerations)
2. `AddRule` for `head_pos_pitch` **overrides** the general multiplier for that specific parameter
3. Result: Pitch has independent control while other parameters use general sensitivity

### 3. PropertyChanged Handlers

**File:** `Modules/DefaultSetup.cs`

When sensitivity changes at runtime:

```csharp
// When general sensitivity changes, reapply pitch-specific override
else if (e.PropertyName == nameof(Rack.UserSettings.WebcamSensitivity))
{
    Rack.SourceNormalizer.UpdateAllHeadParametersMultiplier("NITHwebcamWrapper", Rack.UserSettings.WebcamSensitivity);
    // Reapply pitch-specific sensitivity
    Rack.SourceNormalizer.AddRule("NITHwebcamWrapper", NithParameters.head_pos_pitch, Rack.UserSettings.WebcamPitchSensitivity);
}

// When pitch sensitivity changes, update only pitch
else if (e.PropertyName == nameof(Rack.UserSettings.WebcamPitchSensitivity))
{
    Rack.SourceNormalizer.AddRule("NITHwebcamWrapper", NithParameters.head_pos_pitch, Rack.UserSettings.WebcamPitchSensitivity);
}
```

---

## UI Controls

### XAML (MainWindow.xaml)

Added controls in the settings panel (left column):

```xml
<!-- Pitch sensitivity -->
<Label Content="Pitch sensitivity" />
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width = "1*" />
        <ColumnDefinition Width = "1*" />
        <ColumnDefinition Width = "1*" />
    </Grid.ColumnDefinitions>
    <Button Grid.Column="0" Name="btnPitchSensitivityMinus" Content="?" Click="BtnPitchSensitivityMinus_OnClick" Style="{StaticResource DWood}" />
    <Border Grid.Column="1" Style="{StaticResource LCDBorder}">
        <TextBlock Name="txtPitchSensitivity" Text="1.0" Style="{StaticResource LCDText}"/>
    </Border>
    <Button Grid.Column="2" Name="btnPitchSensitivityPlus" Content="?" Click="BtnPitchSensitivityPlus_OnClick"  Style="{StaticResource DWood}" />
</Grid>
```

**Position:** Directly below "Sensing intensity" control

### Code-Behind (MainWindow.xaml.cs)

Button click handlers modify the current source's pitch sensitivity:

```csharp
private void BtnPitchSensitivityMinus_OnClick(object sender, RoutedEventArgs e)
{
    if (InstrumentStarted)
    {
        switch (Rack.UserSettings.HeadTrackingSource)
        {
            case HeadTrackingSources.Webcam:
                Rack.UserSettings.WebcamPitchSensitivity -= 0.1f;
                break;
            case HeadTrackingSources.Phone:
                Rack.UserSettings.PhonePitchSensitivity -= 0.1f;
                break;
            case HeadTrackingSources.EyeTracker:
                Rack.UserSettings.EyeTrackerPitchSensitivity -= 0.1f;
                break;
        }
    }
}
```

**Behavior:**
- ? Modifies **current source only**
- ? Auto-saves to Settings.json
- ? Triggers SourceNormalizer update
- ? Step size: 0.1f (fine control)

### Display (RenderingModule.cs)

The display shows the current source's pitch sensitivity in real-time:

```csharp
// Display pitch sensitivity for current head tracking source
float currentPitchSensitivity = Rack.UserSettings.HeadTrackingSource switch
{
    HeadTrackingSources.Webcam => Rack.UserSettings.WebcamPitchSensitivity,
    HeadTrackingSources.Phone => Rack.UserSettings.PhonePitchSensitivity,
    HeadTrackingSources.EyeTracker => Rack.UserSettings.EyeTrackerPitchSensitivity,
    _ => 1.0f
};
InstrumentWindow.txtPitchSensitivity.Text = currentPitchSensitivity.ToString("F1");
```

---

## Data Flow

### At Startup

```
UserSettings loads from Settings.json
  ?
DefaultSetup.Setup()
  ?
SourceNormalizer created
  ?
General sensitivity rules added:
  - WebcamSensitivity ? all head parameters
  - PhoneSensitivity ? all head parameters
  - EyeTrackerSensitivity ? all head parameters
  ?
Pitch-specific rules added (OVERRIDE general):
  - WebcamPitchSensitivity ? head_pos_pitch only
  - PhonePitchSensitivity ? head_pos_pitch only
  - EyeTrackerPitchSensitivity ? head_pos_pitch only
```

### During Runtime (User Changes Pitch Sensitivity)

```
User clicks "Pitch sensitivity +" button
  ?
MainWindow.BtnPitchSensitivityPlus_OnClick()
  ?
Rack.UserSettings.PhonePitchSensitivity += 0.1f
  ?
UserSettings.SetProperty() triggers PropertyChanged
  ?
DefaultSetup PropertyChanged handler catches change
  ?
SourceNormalizer.AddRule("NITHphoneWrapper", head_pos_pitch, new_value)
  ?
Next sensor data frame:
  Phone sends head_pos_pitch = 5.0
    ?
  SourceNormalizer multiplies: 5.0 × (new PhonePitchSensitivity) = scaled value
    ?
  Behaviors receive scaled pitch data
    ?
  ModulationControlBehavior / BowPressureControlBehavior use scaled pitch
    ?
  Red rectangle visual feedback shows scaled movement
```

---

## Use Cases

### Use Case 1: Phone Pitch Too Sensitive

**Problem:** Phone pitch movements cause modulation/bow pressure to fluctuate wildly

**Solution:**
1. Select Phone as head tracking source
2. Decrease "Pitch sensitivity" from 1.0 ? 0.5
3. Pitch movements now dampened by 50%
4. Yaw bow motion unaffected (still uses PhoneSensitivity setting)

**Result:** Stable modulation/bow pressure, responsive bow strokes ?

### Use Case 2: Webcam Pitch Too Weak

**Problem:** Webcam pitch doesn't trigger modulation/bow pressure unless you move your head a lot

**Solution:**
1. Select Webcam as head tracking source
2. Increase "Pitch sensitivity" from 1.0 ? 2.0
3. Pitch movements now amplified by 2x
4. Yaw bow motion unaffected

**Result:** Responsive modulation/bow pressure, natural bow strokes ?

### Use Case 3: Different Optimal Settings Per Source

**Problem:** Each source needs different pitch tuning

**Solution:**
```
Webcam Pitch Sensitivity: 1.5 (boost weak webcam pitch)
Phone Pitch Sensitivity: 0.3 (dampen strong phone pitch)
Eye Tracker Pitch Sensitivity: 1.0 (eye tracker pitch is fine)
```

Switching between sources automatically applies the right pitch sensitivity ?

---

## Comparison: General vs. Pitch Sensitivity

| Aspect | General Sensitivity | Pitch Sensitivity |
|--------|-------------------|-------------------|
| **Affects** | All head parameters (yaw, pitch, roll, vel, acc) | Only head_pos_pitch |
| **Purpose** | Scale overall responsiveness | Fine-tune pitch-based controls |
| **Controls** | "Sensing intensity" | "Pitch sensitivity" |
| **Property** | `WebcamSensitivity`, etc. | `WebcamPitchSensitivity`, etc. |
| **Use Case** | Make all movements more/less sensitive | Adjust modulation/bow pressure independently |

### Example: Phone Source with Both Settings

```csharp
PhoneSensitivity = 20.0         // General (affects yaw, roll, velocities, accelerations)
PhonePitchSensitivity = 0.5     // Pitch-specific (overrides general for head_pos_pitch)

Result:
  head_vel_yaw: raw × 20.0 = high sensitivity (good for bow strokes)
  head_pos_pitch: raw × 0.5 = low sensitivity (stable modulation)
  head_pos_roll: raw × 20.0 = high sensitivity (sensitive roll control)
```

---

## Settings Persistence

### Settings.json Structure

```json
{
  "WebcamSensitivity": 1.0,
  "PhoneSensitivity": 20.0,
  "EyeTrackerSensitivity": 1.0,
  "WebcamPitchSensitivity": 1.5,
  "PhonePitchSensitivity": 0.3,
  "EyeTrackerPitchSensitivity": 1.0,
  "SensorIntensityHead": 10.0
}
```

**Auto-save:** Every change to sensitivity triggers `SetProperty()` ? saves Settings.json

**Load on startup:** `DefaultSetup.Setup()` ? `Rack.SavingSystem.LoadSettings()`

---

## Affected Components

### Direct Changes

| Component | Change | Purpose |
|-----------|--------|---------|
| `UserSettings.cs` | Added 3 properties | Store per-source pitch sensitivity |
| `DefaultSetup.cs` | SourceNormalizer config | Apply pitch-specific multipliers |
| `DefaultSetup.cs` | PropertyChanged handlers | Update SourceNormalizer at runtime |
| `MainWindow.xaml` | UI controls | Pitch sensitivity buttons/display |
| `MainWindow.xaml.cs` | Click handlers | Modify current source's pitch sensitivity |
| `RenderingModule.cs` | Display logic | Show current pitch sensitivity |

### Behaviors That Benefit

| Behavior | Uses Pitch? | Benefit |
|----------|-------------|---------|
| **ModulationControlBehavior** | ? Yes | Adjustable modulation response to pitch |
| **BowPressureControlBehavior** | ? Yes | Adjustable bow pressure (CC9) response to pitch |
| **VisualFeedbackBehavior** | ? Yes | Red rectangle movement scales with pitch sensitivity |
| **BowMotionBehavior** | ? No | Unaffected (uses yaw velocity) |
| **HapticFeedbackBehavior** | ? No | Unaffected (uses yaw velocity) |

---

## Testing Checklist

### Phone Source
- [ ] ? Increase phone pitch sensitivity ? red rectangle moves more
- [ ] ? Decrease phone pitch sensitivity ? red rectangle moves less
- [ ] ? Modulation (CC1) scales with pitch sensitivity
- [ ] ? Bow pressure (CC9) scales with pitch sensitivity
- [ ] ? Bow motion (yaw) unaffected by pitch sensitivity changes
- [ ] ? Settings persist across app restart

### Webcam Source
- [ ] ? Switch to webcam ? displays WebcamPitchSensitivity
- [ ] ? Adjust webcam pitch sensitivity independently from phone
- [ ] ? Webcam pitch sensitivity persists when switching sources

### Eye Tracker Source
- [ ] ? Switch to eye tracker ? displays EyeTrackerPitchSensitivity
- [ ] ? Adjust eye tracker pitch sensitivity independently
- [ ] ? Settings don't affect other sources

### General Behavior
- [ ] ? Changing general sensitivity doesn't reset pitch sensitivity
- [ ] ? Changing pitch sensitivity doesn't affect general sensitivity
- [ ] ? Display updates in real-time (60 FPS rendering loop)
- [ ] ? Min value enforced (0.01f)
- [ ] ? Settings.json contains all 6 sensitivity values

---

## User Guide

### What is Pitch Sensitivity?

Pitch sensitivity controls how much your **up/down head movements** affect:
- ?? **Modulation (CC1)**: Vibrato effect when enabled
- ?? **Bow pressure (CC9)**: Expression/dynamics
- ?? **Red rectangle visual**: Vertical position indicator

### When to Adjust Pitch Sensitivity?

**Increase if:**
- You need to move your head a lot for modulation/bow pressure
- Red rectangle barely moves with pitch
- Pitch-based controls feel unresponsive

**Decrease if:**
- Small head movements cause wild modulation/bow pressure
- Red rectangle jumps around too much
- Pitch-based controls feel too twitchy

### Independent from Bow Motion

**Important:** Pitch sensitivity does NOT affect:
- ? Bow strokes (horizontal head movement)
- ? Note triggering
- ? White ellipse movement

Use **"Sensing intensity"** to adjust overall responsiveness.

---

## Status

? **COMPLETE** - Build successful, ready for testing  
?? **Feature Complete** - Per-source pitch sensitivity fully implemented  
?? **Documented** - Comprehensive documentation created  
?? **Persistent** - Settings auto-save to Settings.json  
?? **Maintainable** - Clean separation using SourceNormalizer

---

## Future Enhancements (Optional)

### Per-Axis Sensitivity (Advanced)
Could extend this pattern to allow independent sensitivity for:
- **Yaw sensitivity** (left/right) - currently part of general sensitivity
- **Roll sensitivity** (tilt) - currently part of general sensitivity
- **Velocity sensitivity** - currently part of general sensitivity

**Implementation:** Same pattern as pitch sensitivity, just more AddRule() calls

### UI Slider Instead of Buttons
Replace +/- buttons with a continuous slider for finer control.

### Preset Profiles
Save/load complete sensitivity profiles (e.g., "Tight Control", "Relaxed Playing").
