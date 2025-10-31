# Mouth Gate Multi-Source Fix - CRITICAL

## Problem
Mouth gate only worked when Webcam was selected as head tracking source. With Phone or Eye Tracker selected, mouth gate didn't activate.

## Root Cause
ParameterSelector was whitelisting mouth parameters ONLY from "NITHwebcamWrapper", but in merged data streams, the SensorName might be from the primary source (phone/eye tracker), causing mouth data to be blocked.

## Solution
Whitelist mouth parameters from BOTH webcam AND selected source:

```csharp
// From webcam (original source)
Rack.ParameterSelector.AddRule("NITHwebcamWrapper", NithParameters.mouth_ape);
Rack.ParameterSelector.AddRule("NITHwebcamWrapper", NithParameters.eyeLeft_ape);
Rack.ParameterSelector.AddRule("NITHwebcamWrapper", NithParameters.eyeRight_ape);

// From selected source (handles merged data)
Rack.ParameterSelector.AddRule(selectedSensorName, NithParameters.mouth_ape);
Rack.ParameterSelector.AddRule(selectedSensorName, NithParameters.eyeLeft_ape);
Rack.ParameterSelector.AddRule(selectedSensorName, NithParameters.eyeRight_ape);
```

## Result
? Mouth gate now works with ALL head tracking sources (Webcam, Phone, Eye Tracker)

## Files Modified
- `Modules/MappingModule.cs` - SelectHeadTrackingSource() method
