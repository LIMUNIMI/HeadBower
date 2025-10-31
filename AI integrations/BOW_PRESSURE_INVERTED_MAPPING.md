# Bow Pressure Control - Inverted Pitch Mapping Implementation

## Summary

Implemented a complete bow pressure control feature with:
1. **Inverted pitch mapping** - Bow pressure increases as head moves TOWARD neutral position
2. **On/Off toggle** - New UI button to enable/disable bow pressure control
3. **BowPressure property** in MappingModule - Central property that respects the on/off state
4. **Visual indicator** - Green light shows when bow pressure control is active

## Mapping Behavior

### Pitch-Based Control (Inverted)
```
Head Position              Bow Pressure (CC9)
?????????????????????????????????????????????
Outside upper threshold    ?  0    (no pressure)
    (Yellow line)
         ?
Between thresholds         ?  0-127 (increasing)
         ?                    (inverted gradient)
At lower threshold         ?  127  (maximum)
    (Yellow line)
         ?
Beyond lower threshold     ?  127  (clamped)
```

**Rationale:**
- When head is neutral (outside threshold) = bow not pressed
- As head moves down/up (toward threshold) = bow pressure increases
- Maximum pressure at the threshold lines = natural playing position

### Mouth Aperture Control (Normal)
```
Mouth Position             Bow Pressure (CC9)
?????????????????????????????????????????????
Closed (< 15%)             ?  0    (no pressure)
         ?
Opening                    ?  0-127 (increasing)
         ?
Fully open (100%)          ?  127  (maximum)
```

## Implementation Details

### 1. New Enum Added
**File:** `Modules/SwitchableSelectors.cs`
```csharp
public enum _BowPressureControlModes
{
    On,
    Off
}
```

### 2. Settings Properties
**File:** `Settings/UserSettings.cs`
- Added `BowPressureControlMode` property (default: On)
- Private field `_bowPressureControlMode`

**File:** `Settings/DefaultSettings.cs`
```csharp
BowPressureControlMode = _BowPressureControlModes.On;
```

### 3. MappingModule Property
**File:** `Modules/MappingModule.cs`

Added `BowPressure` property that:
- Respects the On/Off toggle setting
- Clamps values to MIDI range (0-127)
- Calls `SetBowPressure()` method to send CC9 message

```csharp
public int BowPressure
{
    get { return bowPressure; }
    set
    {
        if (Rack.UserSettings.BowPressureControlMode == _BowPressureControlModes.On)
        {
            // Apply value
            bowPressure = Math.Clamp(value, 0, 127);
            SetBowPressure();
        }
        else
        {
            // Force to zero when disabled
            bowPressure = 0;
            SetBowPressure();
        }
    }
}

private void SetBowPressure()
{
    Rack.MidiModule.SendControlChange(9, BowPressure);
}
```

### 4. Behavior Logic Update
**File:** `Behaviors/HeadBow/BowPressureControlBehavior.cs`

#### Inverted Pitch Mapping
```csharp
case BowPressureControlSources.HeadPitch:
    double absPitch = Math.Abs(filteredPitch);
    
    if (absPitch <= pitchThreshold)
    {
        // Outside upper threshold = zero
        bowPressureValue = 0;
    }
    else if (absPitch >= maxPitchDeviation)
    {
        // Beyond lower threshold = maximum
        bowPressureValue = 127;
    }
    else
    {
        // Between thresholds: INVERTED mapping
        var mapper = new SegmentMapper(pitchThreshold, maxPitchDeviation, 127, 0, true);
        bowPressureValue = (int)mapper.Map(absPitch);
    }
    break;
```

**Key Point:** Output range is `(127, 0)` instead of `(0, 127)` - this inverts the mapping!

### 5. UI Components
**File:** `MainWindow.xaml`

Added bow pressure toggle button:
```xaml
<!-- Bow Pressure -->
<StackPanel Orientation="Horizontal" HorizontalAlignment="Left">
    <Button Name="btnBowPress" Style="{StaticResource DWood}" 
            Width="{StaticResource HG}" Click="btnBowPress_Click">
        <Border Name="indBowPress" Style="{StaticResource Indicator}" />
    </Button>
    <Label VerticalAlignment="Center" Content="Bow Press." />
</StackPanel>
```

### 6. Event Handlers
**File:** `MainWindow.xaml.cs`

```csharp
private void btnBowPress_Click(object sender, RoutedEventArgs e)
{
    if (InstrumentStarted)
    {
        Rack.UserSettings.BowPressureControlMode = 
            Rack.UserSettings.BowPressureControlMode == _BowPressureControlModes.On 
                ? _BowPressureControlModes.Off 
                : _BowPressureControlModes.On;
        // Rendering loop handles UI update
    }
}
```

### 7. Rendering Update
**File:** `Modules/RenderingModule.cs`

Added indicator update in the rendering loop:
```csharp
InstrumentWindow.indBowPress.Background = 
    Rack.UserSettings.BowPressureControlMode == _BowPressureControlModes.On 
        ? ActiveBrush 
        : BlankBrush;
```

## User Interface

### Settings Panel Layout
```
??????????????????????????????????????
? Interaction settings               ?
??????????????????????????????????????
? [?] Modul.       ? [?] Bow Press.  ?  ? Toggle buttons
??????????????????????????????????????
? [?] Legato       ?                 ?
??????????????????????????????????????

??????????????????????????????????????
? Modulation source                  ?
??????????????????????????????????????
? [?] Pitch        ? [?] Mouth       ?
??????????????????????????????????????

??????????????????????????????????????
? Bow pressure (CC9) source          ?
??????????????????????????????????????
? [?] Pitch        ? [?] Mouth       ?
??????????????????????????????????????
```

- **Green indicator** (?) = Feature enabled
- **Black indicator** (?) = Feature disabled

## Behavior Summary

| Bow Press Toggle | Pitch Position  | CC9 Value | Musical Effect        |
|------------------|-----------------|-----------|----------------------|
| **OFF**          | Any             | 0         | No bow pressure      |
| **ON**           | Far from neutral | 0         | Bow not touching     |
| **ON**           | Moving to neutral| 0?127     | Bow pressing down    |
| **ON**           | At neutral (threshold) | 127  | Maximum bow pressure |
| **ON**           | Past threshold  | 127       | Maximum (clamped)    |

## Data Flow

```
1. Raw sensor data (head pitch or mouth aperture)
        ?
2. BowPressureControlBehavior.HandleData()
   - Filters raw values
   - Applies mapping (inverted for pitch, normal for mouth)
   - Calculates bow pressure value (0-127)
        ?
3. Rack.MappingModule.BowPressure = value
   - Checks if BowPressureControlMode == On
   - If On: applies value
   - If Off: forces to 0
        ?
4. MappingModule.SetBowPressure()
   - Sends MIDI CC9 with final value
        ?
5. MIDI synthesizer receives bow pressure control
```

## Visual Feedback

The two yellow lines in the overlay indicate:
- **Upper threshold** (pitch = 15°): Bow pressure starts at 0
- **Lower threshold** (pitch = 50°): Bow pressure reaches 127

Users can see their head position relative to these lines and understand:
- Outside lines = no bow pressure
- Moving toward center = increasing bow pressure
- At center line = maximum bow pressure

## Testing Checklist

? Build successful with no errors
? BowPressure property added to MappingModule
? Inverted mapping implemented for pitch
? Normal mapping maintained for mouth aperture
? On/Off toggle button added to UI
? Indicator shows green when ON, black when OFF
? Default setting is ON
? Bow pressure forced to 0 when toggle is OFF
? Settings auto-save when changed

## Files Modified

1. `Modules/SwitchableSelectors.cs` - Added `_BowPressureControlModes` enum
2. `Settings/UserSettings.cs` - Added `BowPressureControlMode` property
3. `Settings/DefaultSettings.cs` - Set default to ON
4. `Modules/MappingModule.cs` - Added `BowPressure` property and `SetBowPressure()` method
5. `Behaviors/HeadBow/BowPressureControlBehavior.cs` - Implemented inverted mapping
6. `MainWindow.xaml` - Added toggle button UI
7. `MainWindow.xaml.cs` - Added `btnBowPress_Click` event handler
8. `Modules/RenderingModule.cs` - Added indicator update

## Status

? **COMPLETE** - Ready for testing
?? **Feature**: Bow pressure control with inverted pitch mapping
??? **UI**: Toggle button with visual indicator
?? **MIDI**: CC9 sent based on head pitch or mouth aperture
