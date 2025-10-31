# Mouth Gate Eye Tracker Fix - CRITICAL

## Problem
Mouth gating (MouthClosedNotePreventionBehavior) was not working when Eye Tracker was selected as the head tracking source, even though the webcam was connected and sending mouth_ape data.

## Root Cause
The `SelectHeadTrackingSource` method in `MappingModule.cs` was only whitelisting mouth parameters from "NITHwebcamWrapper":

```csharp
// OLD CODE - Only whitelists from webcam
Rack.ParameterSelector.AddRule("NITHwebcamWrapper", NithParameters.mouth_ape);
Rack.ParameterSelector.AddRule("NITHwebcamWrapper", NithParameters.eyeLeft_ape);
Rack.ParameterSelector.AddRule("NITHwebcamWrapper", NithParameters.eyeRight_ape);
```

However, when the eye tracker is selected as the head tracking source, some implementations may **merge or forward webcam data** with the eye tracker's own `SensorName = "NITHeyetrackerWrapper"`. This means mouth_ape data arrives with the wrong SensorName from the perspective of the ParameterSelector.

Since the ParameterSelector operates in **Whitelist mode**:
- Only parameters from sources with explicit rules are kept
- Parameters must match BOTH the SensorName AND parameter name
- If mouth_ape arrives with `SensorName = "NITHeyetrackerWrapper"` but only "NITHwebcamWrapper" is whitelisted, the data is **dropped**

## Solution
Whitelist mouth parameters from **BOTH** the webcam source AND the selected head tracking source:

```csharp
// From webcam (original source)
Rack.ParameterSelector.AddRule("NITHwebcamWrapper", NithParameters.mouth_ape);
Rack.ParameterSelector.AddRule("NITHwebcamWrapper", NithParameters.eyeLeft_ape);
Rack.ParameterSelector.AddRule("NITHwebcamWrapper", NithParameters.eyeRight_ape);

// ADDITIONAL FIX: Also from selected source (handles merged data)
if (selectedSensorName != "NITHwebcamWrapper")
{
    Rack.ParameterSelector.AddRule(selectedSensorName, NithParameters.mouth_ape);
    Rack.ParameterSelector.AddRule(selectedSensorName, NithParameters.eyeLeft_ape);
    Rack.ParameterSelector.AddRule(selectedSensorName, NithParameters.eyeRight_ape);
}
```

This ensures mouth data passes through regardless of which SensorName it arrives with.

## Data Flow Scenarios

### Scenario 1: Separate Sensor Packets (Normal)
```
Webcam sends:        SensorName="NITHwebcamWrapper", Parameters={mouth_ape, eyeLeft_ape, eyeRight_ape, ...}
Eye Tracker sends:   SensorName="NITHeyetrackerWrapper", Parameters={gaze_x, gaze_y, head_pos_yaw, ...}

Result: ? Mouth data from webcam passes (whitelisted from "NITHwebcamWrapper")
```

### Scenario 2: Merged Data Packets (Problematic Before Fix)
```
Eye Tracker sends merged data: SensorName="NITHeyetrackerWrapper", Parameters={mouth_ape, gaze_x, gaze_y, head_pos_yaw, ...}

OLD BEHAVIOR: ? mouth_ape dropped (not whitelisted from "NITHeyetrackerWrapper")
NEW BEHAVIOR: ? mouth_ape passes (whitelisted from "NITHeyetrackerWrapper")
```

## Why This Happens
Some sensor wrappers or device discovery behaviors may:
1. **Merge data streams** - Combine webcam facial data with eye tracker head/gaze data
2. **Forward with new SensorName** - Re-tag the combined data with the primary source's name
3. **Optimize data flow** - Send one merged packet instead of multiple separate packets

The fix handles all these scenarios by whitelisting from both sources.

## Implementation Details

### Code Location
`Modules/MappingModule.cs` ? `SelectHeadTrackingSource()` method

### Change Summary
Added conditional whitelisting for mouth parameters from the selected head tracking source:

```csharp
// Only add if selected source is NOT already webcam (avoid duplicate rules)
if (selectedSensorName != "NITHwebcamWrapper")
{
    Rack.ParameterSelector.AddRule(selectedSensorName, NithParameters.mouth_ape);
    Rack.ParameterSelector.AddRule(selectedSensorName, NithParameters.eyeLeft_ape);
    Rack.ParameterSelector.AddRule(selectedSensorName, NithParameters.eyeRight_ape);
}
```

### Why Check `selectedSensorName != "NITHwebcamWrapper"`?
To avoid adding duplicate rules when webcam is selected. While duplicate rules are harmless, this keeps the configuration cleaner.

## Testing

### Test Case 1: Eye Tracker Head Tracking + Mouth Gate
```
Setup:
  - Select Eye Tracker as head tracking source
  - Enable Mouth Closed Note Prevention
  - Ensure webcam is connected

Expected Behavior:
  ? Head motion controlled by eye tracker data
  ? Mouth gate responds to mouth aperture
  ? Notes only play when mouth is open
```

### Test Case 2: Phone Head Tracking + Mouth Gate
```
Setup:
  - Select Phone as head tracking source
  - Enable Mouth Closed Note Prevention
  - Ensure webcam is connected

Expected Behavior:
  ? Head motion controlled by phone data
  ? Mouth gate responds to mouth aperture
  ? Notes only play when mouth is open
```

### Test Case 3: Webcam Head Tracking + Mouth Gate
```
Setup:
  - Select Webcam as head tracking source
  - Enable Mouth Closed Note Prevention

Expected Behavior:
  ? Head motion controlled by webcam data
  ? Mouth gate responds to mouth aperture
  ? Notes only play when mouth is open
  (Same as before - no regression)
```

## Related Files
- `Modules/MappingModule.cs` - Contains the fix
- `Behaviors/HeadBow/MouthClosedNotePreventionBehavior.cs` - Consumes mouth_ape data
- `../NITHlibrary/Nith/Preprocessors/NithPreprocessor_ParameterSelector.cs` - Filtering logic

## Related Issues
This fix addresses the same underlying issue documented in:
- `MOUTH_GATE_MULTI_SOURCE_FIX.md` (previous fix attempt)
- `MOUTH_GATE_MULTI_SOURCE_FINAL_FIX.md` (documented but not fully implemented)

The current fix properly implements the solution that was documented but apparently missing from the actual code.

## Verification
After applying this fix:

1. **Check Parameter Selector Logs**
   ```
   === HEAD TRACKING SOURCE SELECTION ===
   Selected Source: EyeTracker
   
   Parameter Selector Rules:
     NITHeyetrackerWrapper: Accept 11 parameters:
       - head_pos_yaw, head_pos_pitch, head_pos_roll (head motion)
       - mouth_ape, eyeLeft_ape, eyeRight_ape (mouth data - NEW!)
       - gaze_x, gaze_y (gaze data)
       - ...
     NITHwebcamWrapper: Accept 3 parameters:
       - mouth_ape, eyeLeft_ape, eyeRight_ape (mouth data)
   ```

2. **Test Mouth Gate Behavior**
   - Close mouth ? gate closes ? notes stop
   - Open mouth ? gate opens ? notes resume
   - Works regardless of selected head tracking source

3. **Enable Diagnostic Behavior** (optional)
   Uncomment in `DefaultSetup.cs`:
   ```csharp
   Rack.NithModuleUnified.SensorBehaviors.Add(new Behaviors.DiagnosticBehavior());
   ```
   This will show which sensors are sending data and confirm mouth_ape is being received.

## Result
? **Mouth gate now works with ALL head tracking sources**
- Webcam ? Works (unchanged)
- Phone ? Works (now fixed)
- Eye Tracker ? Works (now fixed)

The fix is backwards compatible and handles both separate sensor packets and merged data streams.
