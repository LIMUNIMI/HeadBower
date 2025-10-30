# HeadBower Unified Architecture - Quick Reference

## How to Switch Head Tracking Source (Runtime)

### In Code:
```csharp
// Just call this static method - that's it!
DefaultSetup.ConfigureParameterSelector(HeadTrackingSources.Phone);

// Update sensitivity
Rack.Behavior_HeadBow.Sensitivity = Rack.UserSettings.SensorIntensityHead;
```

### In UI:
User clicks "Phone" / "Webcam" / "Eye Tracker" button ? automatic reconfiguration

---

## Current Parameter Mapping

### When Webcam Selected:
- **Head tracking** ? Webcam (`NITHwebcamWrapper`)
- **Gaze** ? Eye Tracker (`NITHtobiasWrapper`)
- **Mouth/Eyes** ? Webcam (`NITHwebcamWrapper`)

### When Phone Selected:
- **Head tracking** ? Phone (`NITHphoneWrapper`)
- **Gaze** ? Eye Tracker (`NITHtobiasWrapper`)
- **Mouth/Eyes** ? Webcam (`NITHwebcamWrapper`)

### When Eye Tracker Selected:
- **Head tracking** ? Eye Tracker (`NITHtobiasWrapper`)
- **Gaze** ? Eye Tracker (`NITHtobiasWrapper`)
- **Mouth/Eyes** ? Webcam (`NITHwebcamWrapper`)

---

## Key Components

| Component | Location | Purpose |
|-----------|----------|---------|
| `Rack.NithModuleUnified` | Single module | Receives ALL sensor data |
| `Rack.ParameterSelector` | Preprocessor | Filters parameters by source |
| `Rack.UnifiedHeadTrackerCalibrator` | Preprocessor | Calibrates head position |
| `Rack.Behavior_HeadBow` | Behavior | Uses head data from selected source |

---

## Debug Output

Check console for:
```
=== Parameter Selector Configuration ===
Head Tracking Source: Phone
NithPreprocessor_ParameterSelector (Mode: Whitelist)
  NITHphoneWrapper: Accept 6 parameter(s): head_acc_yaw, head_acc_pitch, ...
  NITHwebcamWrapper: Accept 3 parameter(s): mouth_ape, eyeLeft_ape, eyeRight_ape
  NITHtobiasWrapper: Accept 2 parameter(s): gaze_x, gaze_y
=========================================
```

---

## Calibration

**One calibrator for all sources:**
```csharp
Rack.UnifiedHeadTrackerCalibrator.SetCenterToCurrentPosition();
```

Button: "HT cal" in UI ? calibrates currently selected source

---

## Adding New Head Tracking Source

1. Add enum: `HeadTrackingSources.NewSource`
2. Add button in XAML
3. Add click handler
4. Add case in `DefaultSetup.ConfigureParameterSelector()`:
```csharp
case HeadTrackingSources.NewSource:
    Rack.ParameterSelector.AddRules("NITHnewSensor",
        NithParameters.head_pos_yaw,
        NithParameters.head_pos_pitch,
        NithParameters.head_pos_roll);
    // ... other rules ...
    break;
```

---

## Common Tasks

### Change which parameters come from which source:
Edit `DefaultSetup.ConfigureParameterSelector()` method

### Add new behavior that uses combined data:
```csharp
// In DefaultSetup.SetupBehaviors():
Rack.NithModuleUnified.SensorBehaviors.Add(new MyNewBehavior());
```

### Add preprocessor to pipeline:
```csharp
// In DefaultSetup.SetupUnifiedModule():
Rack.NithModuleUnified.Preprocessors.Add(new MyPreprocessor());
```

---

## Troubleshooting

### Behavior not receiving data?
- Check selector rules in `ConfigureParameterSelector()`
- Check console output for active rules
- Verify sensor name matches exactly (no version!)

### Wrong data source?
- Check which source is selected in `Rack.UserSettings.HeadTrackingSource`
- Call `UpdateHeadTrackingSource()` to refresh

### Calibration not working?
- Only ONE calibrator is active: `Rack.UnifiedHeadTrackerCalibrator`
- Old calibrators exist but aren't used

---

## Files Changed

- `Modules/DefaultSetup.cs` - Main setup logic
- `Modules/Rack.cs` - Added unified module & selector
- `MainWindow.xaml.cs` - Simplified source switching

---

## What to NOT Change

? Don't modify old modules (`NithModulePhone`, etc.) - they're legacy  
? Don't use old calibrators - use `UnifiedHeadTrackerCalibrator`  
? Don't add behaviors to old modules - use `NithModuleUnified`  

? DO modify `ConfigureParameterSelector()` for source rules  
? DO use `NithModuleUnified` for everything  
? DO use `UnifiedHeadTrackerCalibrator` for calibration  
