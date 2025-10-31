# Mouth Gate Multi-Source Support

## Overview

The mouth gate system works **independently of head tracking source selection**. This means you can:
- Use **phone** for head tracking (yaw/pitch/roll)
- Use **webcam** for mouth aperture (gate control)
- Both work simultaneously and independently

## How It Works

### Parameter Whitelisting Strategy

In `MappingModule.SelectHeadTrackingSource()`:

```csharp
public static void SelectHeadTrackingSource(HeadTrackingSources source)
{
    // Clear all existing rules
    Rack.ParameterSelector.ClearAllRules();

    // Get the sensor name for this source
    string selectedSensorName = GetSensorNameForSource(source);

    // Whitelist all head motion parameters for the selected source
    Rack.ParameterSelector.AddRulesList(selectedSensorName, AllHeadMotionParameters);

    // ALWAYS whitelist facial parameters from webcam (regardless of selected head tracking source)
    // These are needed for:
    // - mouth_ape: modulation/bow pressure control AND mouth gate (threshold-based note prevention)
    // - eyeLeft_ape, eyeRight_ape: required by NithPreprocessor_WebcamWrapper to normalize mouth_ape
    // NOTE: These must be whitelisted for ALL head tracking sources so mouth gate works with phone/eye tracker
    Rack.ParameterSelector.AddRule("NITHwebcamWrapper", NithParameters.mouth_ape);
    Rack.ParameterSelector.AddRule("NITHwebcamWrapper", NithParameters.eyeLeft_ape);
    Rack.ParameterSelector.AddRule("NITHwebcamWrapper", NithParameters.eyeRight_ape);
    
    // ALWAYS whitelist gaze from eye tracker (needed for gaze-to-mouse control)
    Rack.ParameterSelector.AddRule("NITHeyetrackerWrapper", NithParameters.gaze_x);
    Rack.ParameterSelector.AddRule("NITHeyetrackerWrapper", NithParameters.gaze_y);
}
```

### Key Points

1. **Unconditional Whitelisting**: Mouth parameters are added **after** the head motion source selection
2. **Multi-Source Independence**: Each source can provide different types of data simultaneously:
   - **Selected head tracking source** ? head motion (yaw, pitch, roll, velocities, accelerations)
   - **Webcam (always)** ? facial data (mouth_ape, eye apertures)
   - **Eye tracker (always)** ? gaze data (gaze_x, gaze_y)

## Usage Scenarios

### Scenario 1: Phone Head Tracking + Webcam Mouth Gate
```
User Configuration:
  - Head Tracking Source: Phone
  - Mouth Gate: Enabled

Data Flow:
  Phone ? head_vel_yaw, head_pos_pitch, etc. (for bow motion)
  Webcam ? mouth_ape (for gate control)
  
Result: ? Mouth gate works! Notes only play when mouth is open.
```

### Scenario 2: Eye Tracker Head Tracking + Webcam Mouth Gate
```
User Configuration:
  - Head Tracking Source: Eye Tracker
  - Mouth Gate: Enabled

Data Flow:
  Eye Tracker ? head_pos_yaw, head_pos_pitch (for bow motion)
  Webcam ? mouth_ape (for gate control)
  
Result: ? Mouth gate works! Notes only play when mouth is open.
```

### Scenario 3: Webcam Head Tracking + Mouth Gate
```
User Configuration:
  - Head Tracking Source: Webcam
  - Mouth Gate: Enabled

Data Flow:
  Webcam ? head_pos_yaw, head_pos_pitch (for bow motion)
  Webcam ? mouth_ape (for gate control)
  
Result: ? Mouth gate works! Single source provides both types of data.
```

## Architecture Diagram

```
???????????????????????????????????????????????????????????????
?           Parameter Selector Whitelist Rules                ?
???????????????????????????????????????????????????????????????
?                                                               ?
?  SELECTED HEAD TRACKING SOURCE                                ?
?  ?? If Webcam:   NITHwebcamWrapper ? head motion params      ?
?  ?? If Phone:    NITHphoneWrapper ? head motion params       ?
?  ?? If EyeTrack: NITHeyetrackerWrapper ? head motion params  ?
?                                                               ?
?  ALWAYS ACTIVE (INDEPENDENT OF HEAD TRACKING SOURCE)          ?
?  ?? NITHwebcamWrapper ? mouth_ape, eyeLeft_ape, eyeRight_ape?
?  ?? NITHeyetrackerWrapper ? gaze_x, gaze_y                   ?
?                                                               ?
???????????????????????????????????????????????????????????????
```

## Why This Design?

### 1. Modularity
Different sensors excel at different tasks:
- **Phone**: Best for smooth head motion (IMU sensors)
- **Webcam**: Best for facial features (computer vision)
- **Eye Tracker**: Best for gaze and head position

### 2. Flexibility
Users can mix and match:
- Use phone for precise head tracking
- Use webcam for mouth gate
- Both work simultaneously

### 3. Fail-Safe Operation
If webcam is not available:
- Head tracking continues with selected source
- Mouth gate opens automatically (failsafe in `MouthClosedNotePreventionBehavior`)
- No crashes or interruptions

## Implementation Details

### MouthClosedNotePreventionBehavior
```csharp
public void HandleData(NithSensorData nithData)
{
    if (Rack.UserSettings.MouthClosedNotePreventionMode != _MouthClosedNotePreventionModes.On)
    {
        Rack.MappingModule.IsMouthGateBlocking = false;
        return;
    }

    // Check if mouth_ape is present (from webcam)
    if (nithData.ContainsParameters(requiredParams)) // mouth_ape
    {
        // Apply hysteresis logic...
    }
    else
    {
        // Webcam not available - open gate (failsafe)
        Rack.MappingModule.IsMouthGateBlocking = false;
    }
}
```

### Failsafe Behavior
When `mouth_ape` parameter is missing:
1. Gate opens automatically
2. Notes can be triggered normally
3. No errors or crashes
4. Seamless fallback to normal operation

## Testing Matrix

| Head Source | Webcam | Mouth Gate | Expected Behavior |
|-------------|--------|------------|-------------------|
| Phone       | Active | Enabled    | ? Gate works     |
| Phone       | Inactive | Enabled  | ? Gate opens (failsafe) |
| Eye Tracker | Active | Enabled    | ? Gate works     |
| Eye Tracker | Inactive | Enabled  | ? Gate opens (failsafe) |
| Webcam      | Active | Enabled    | ? Gate works     |
| Webcam      | Inactive | Enabled  | ?? No head tracking (expected) |

## Future Enhancements

### Multi-Source Mouth Data
Could potentially support mouth aperture from multiple sources:
```csharp
// Future: Check multiple sources for mouth_ape
if (nithData.ContainsParameter(NithParameters.mouth_ape))
{
    // Use whichever source provides it first
    var mouthApe = nithData.GetParameterValue(NithParameters.mouth_ape);
    // ...
}
```

### Source Priority System
Could implement priority when multiple sources provide same parameter:
```csharp
// Future: Prefer webcam mouth_ape over phone mouth_ape
var mouthApe = nithData.GetParameterValue(NithParameters.mouth_ape, preferredSource: "NITHwebcamWrapper");
```

## Troubleshooting

### Problem: Mouth gate not working with phone head tracking
**Diagnosis**: Check if webcam is connected and sending data
```
Solution:
1. Ensure webcam is connected
2. Check that NITHwebcamWrapper is running
3. Verify mouth_ape parameter is being received
4. Check parameter selector logs
```

### Problem: Gate always open even when mouth closed
**Diagnosis**: Check threshold values
```
Solution:
1. Verify mouth_ape values in debug output
2. Adjust thresholds in MouthClosedNotePreventionBehavior:
   - MOUTH_APERTURE_LOWER_THRESHOLD (default: 10)
   - MOUTH_APERTURE_UPPER_THRESHOLD (default: 15)
```

## Summary

? **Mouth gate works with ALL head tracking sources**  
? **Webcam provides mouth data independently**  
? **Failsafe behavior when webcam unavailable**  
? **No configuration needed - works automatically**

The parameter selector automatically whitelists mouth parameters from webcam regardless of which source is selected for head tracking, enabling true multi-source operation.
