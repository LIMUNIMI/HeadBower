# Modulation and Bow Pressure Control Sources Implementation

## Overview
This document describes the implementation of multi-switch controls for modulation (CC1) and bow pressure (CC9) in the HeadBower application.

## Features Implemented

### 1. Modulation Control Source (3-way switch)
Modulation can now be controlled through three different sources:
- **Head Pitch**: Uses head pitch rotation (forward/backward head tilt)
- **Mouth Aperture**: Uses mouth opening amount (requires webcam with face tracking)
- **Head Roll**: Uses head roll rotation (left/right head tilt)

### 2. Bow Pressure Control Source (2-way switch)
Bow pressure (CC9) can be controlled through two sources:
- **Head Pitch**: Uses head pitch rotation
- **Mouth Aperture**: Uses mouth opening amount

### 3. Independent Operation
The modulation and bow pressure controls are completely independent:
- You can use head pitch for modulation while using mouth aperture for bow pressure
- Or any other combination of sources

## Implementation Details

### New Enums Added
**File: `Modules/SwitchableSelectors.cs`**
```csharp
public enum ModulationControlSources
{
    HeadPitch,
    MouthAperture,
    HeadRoll
}

public enum BowPressureControlSources
{
    HeadPitch,
    MouthAperture
}
```

### Settings Properties
**File: `Settings/NetytarSettings.cs`**
- Added `ModulationControlSource` property (default: HeadPitch)
- Added `BowPressureControlSource` property (default: HeadPitch)
- Both properties auto-save when changed

### Behavior Logic
**File: `Behaviors/HeadBow/NITHbehavior_HeadViolinBow.cs`**

The behavior now:
1. Reads head pitch, head roll, and mouth aperture from sensor data
2. Filters all three sources using exponential moving average filters
3. Applies appropriate thresholds and mappings for each source
4. Sends modulation CC1 based on `ModulationControlSource` setting (when modulation is enabled)
5. Sends bow pressure CC9 based on `BowPressureControlSource` setting

#### Thresholds and Mappings
- **Head Pitch**: 15.0° threshold, maps up to 50.0° to 0-127
- **Mouth Aperture**: 15.0% threshold, maps up to 100% to 0-127
- **Head Roll**: 15.0° threshold, maps up to 50.0° to 0-127

### UI Components
**File: `MainWindow.xaml`**

Added two new button groups in the middle column of the settings panel:

#### Modulation Source Selector (3 buttons in one row)
- **Pitch** button: Sets modulation source to head pitch
- **Mouth** button: Sets modulation source to mouth aperture
- **Roll** button: Sets modulation source to head roll

#### Bow Pressure Source Selector (2 buttons in one row)
- **Pitch** button: Sets bow pressure source to head pitch
- **Mouth** button: Sets bow pressure source to mouth aperture

All buttons:
- Show active indicator (green) when selected
- Are gaze-clickable (follow the existing interaction pattern)
- Use the existing `DWoodLeft` and `DWoodRight` styles

### Event Handlers
**File: `MainWindow.xaml.cs`**

Added event handlers:
- `btnModSourcePitch_Click`
- `btnModSourceMouth_Click`
- `btnModSourceRoll_Click`
- `btnBowPressureSourcePitch_Click`
- `btnBowPressureSourceMouth_Click`

Updated `UpdateGUIVisuals()` to show correct indicators for all source selections.

## Usage

### Enabling Modulation
1. Open Settings panel
2. Toggle the "Modul." button to ON (green indicator)
3. Select your desired modulation source (Pitch, Mouth, or Roll)
4. The selected source will control CC1 when playing notes

### Controlling Bow Pressure
1. The bow pressure (CC9) is always active when notes are playing
2. Select your desired bow pressure source (Pitch or Mouth)
3. The selected source will control CC9 continuously while playing

### Requirements for Mouth Aperture
To use mouth aperture as a control source:
- **Webcam** must be selected as the head tracking source
- The webcam must support face tracking (NITHwebcamWrapper)
- Mouth aperture data (`NithParameters.mouth_ape`) must be available

## Sensor Compatibility

| Source | Eye Tracker | Webcam | Phone |
|--------|-------------|---------|-------|
| Head Pitch | ? | ? | ? |
| Head Roll | ? | ? | ? |
| Mouth Aperture | ? | ? | ? |

## Technical Notes

### Filtering
All control sources use exponential moving average filters:
- Head pitch: ? = 0.9
- Head roll: ? = 0.9
- Mouth aperture: ? = 0.7 (more responsive for facial movements)

### MIDI Behavior
- **Modulation (CC1)**: Only sent when "Modul." is ON and a note is playing
- **Bow Pressure (CC9)**: Always sent when a note is playing, regardless of modulation setting
- Both controls reset to 0 when notes stop playing
- Deadzone prevents jitter at neutral positions

### Backwards Compatibility
- Existing settings files will automatically get default values (HeadPitch for both)
- The old "Modulation On/Off" toggle remains functional and works with the new source selection
- No breaking changes to existing functionality

## Fixed Issues
- Fixed pre-existing XAML error: `MouseLeave="NoteButton.MouseLeave"` ? `MouseLeave="NoteButton_MouseLeave"` on two buttons (lines 198 and 200)

## Build Status
? All files compile successfully
? No warnings or errors
? Ready for testing
