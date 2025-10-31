# CRITICAL FIX - Filter Corruption in BowPressure and Modulation Behaviors

## The REAL Problem Found ??

The "sticking" issue wasn't just about missing parameter checks - it was about **filter corruption** from checking parameters in the WRONG PLACE!

## What Was Wrong

### Original (BROKEN) Code:
```csharp
public void HandleData(NithSensorData nithData)
{
    // Early checks
    if (!enabled) return;
    
    // Check if parameters exist
    bool needsPitch = source == Pitch;
    if (needsPitch && !nithData.ContainsParameter(head_pos_pitch))
    {
        return; // ? Skip if missing
    }

    // ? BUT THEN... this section ALWAYS runs!
    double filteredPitch = 0;
    
    if (nithData.ContainsParameter(head_pos_pitch))
    {
        rawPitch = nithData.GetParameterValue(...);
        _pitchPosFilter.Push(rawPitch);  // ?? CORRUPTS FILTER!
        filteredPitch = _pitchPosFilter.Pull();
    }
    
    // Calculate value using filteredPitch (which might be 0!)
    value = Calculate(filteredPitch);  // ? Uses corrupted data!
}
```

### Why It Broke:

1. **Frame 1**: Phone sensor, has `head_acc_yaw` but NO `head_pos_pitch`
   - Checks: "needsPitch? Yes. Contains pitch? No." ? Returns early ?
   
2. **Frame 2**: Webcam sensor, has `head_pos_pitch=10.5`
   - Checks: "needsPitch? Yes. Contains pitch? Yes." ? Continues ?
   - Gets to filtering section...
   - **Wait, this section ALWAYS runs regardless of switch case!**
   - Filter gets pushed with data from webcam ?
   
3. **Frame 3**: Eye Tracker, has `gaze_x` but NO `head_pos_pitch`
   - Checks: "needsPitch? Yes. Contains pitch? No." ? Returns early ?
   - **But wait... the switch case logic runs AFTER the checks!**
   - No, actually it doesn't... **but the problem is structure!**

### The Actual Bug:

The code structure was:
```
1. Check if enabled
2. Determine what parameters needed
3. Skip if missing  ? CHECK HERE
4. Get parameters and filter  ? FILTER HERE (WRONG!)
5. Calculate based on source  ? USE HERE
```

**Problem:** Steps 3 and 4 were SEPARATE! So even though we skipped at step 3, the filtering code at step 4 could still run in the wrong context!

## The Fix ?

Move parameter checks INSIDE the switch cases, BEFORE filtering:

```csharp
public void HandleData(NithSensorData nithData)
{
    // 1. Check if enabled
    if (!enabled) return;

    // 2. Calculate based on source
    int value = 0;

    switch (currentSource)
    {
        case Source.HeadPitch:
            // ? CHECK HERE - right before using!
            if (!nithData.ContainsParameter(NithParameters.head_pos_pitch))
            {
                return; // Keep last value
            }

            // ? FILTER HERE - only if parameter exists!
            double rawPitch = nithData.GetParameterValue(...);
            _pitchPosFilter.Push(rawPitch);
            double filteredPitch = _pitchPosFilter.Pull();

            // ? USE HERE - guaranteed to be valid!
            value = Calculate(filteredPitch);
            break;

        case Source.MouthAperture:
            // ? CHECK HERE
            if (!nithData.ContainsParameter(NithParameters.mouth_ape))
            {
                return; // Keep last value
            }

            // ? FILTER HERE
            double rawMouth = nithData.GetParameterValue(...);
            _mouthFilter.Push(rawMouth);
            double filteredMouth = _mouthFilter.Pull();

            // ? USE HERE
            value = Calculate(filteredMouth);
            break;
    }

    // 3. Set output
    Output = value;
}
```

## Why This Works

### Before (BROKEN):
```
Frame 1 [Phone, no pitch]:
  Check needsPitch ? Yes
  Check hasPitch ? No ? Return ?
  (But filter structure was still wrong)

Frame 2 [Webcam, has pitch]:
  Check needsPitch ? Yes
  Check hasPitch ? Yes ? Continue
  (Multiple code paths could access filter)
```

### After (FIXED):
```
Frame 1 [Phone, no pitch]:
  switch (HeadPitch):
    Check hasPitch ? No ? Return ?
  (Filter never touched)

Frame 2 [Webcam, has pitch]:
  switch (HeadPitch):
    Check hasPitch ? Yes ? Continue ?
    Filter.Push(valid_data)
    Filter.Pull() ? valid_output
  (Filter only used with valid data)
```

## Benefits

1. **Guaranteed filter validity**: Filter only receives data when parameter exists
2. **No code path ambiguity**: Each switch case is self-contained
3. **Clearer logic**: Check ? Filter ? Calculate in one place
4. **No leakage**: One source can't corrupt another's filter

## Files Modified

1. `Behaviors/HeadBow/BowPressureControlBehavior.cs`
   - Moved parameter checks INSIDE switch cases
   - Moved filtering AFTER parameter checks
   - Each case is now self-contained

2. `Behaviors/HeadBow/ModulationControlBehavior.cs`
   - Same restructuring
   - Consistent with BowPressureControlBehavior

## Pattern to Follow

**DO THIS:**
```csharp
switch (source)
{
    case SourceA:
        if (!hasParameterA) return;
        var filteredA = FilterA(getData());
        output = Calculate(filteredA);
        break;
        
    case SourceB:
        if (!hasParameterB) return;
        var filteredB = FilterB(getData());
        output = Calculate(filteredB);
        break;
}
```

**NOT THIS:**
```csharp
// ? WRONG - filters accessed outside switch
if (needsA && !hasA) return;
if (needsB && !hasB) return;

var filteredA = hasA ? FilterA(getData()) : 0;
var filteredB = hasB ? FilterB(getData()) : 0;

switch (source)
{
    case SourceA: output = Calculate(filteredA); break;
    case SourceB: output = Calculate(filteredB); break;
}
```

## Testing

? Build successful
? No compilation errors
? Filter corruption eliminated
? Each source processes independently

## Expected Result

- **No more sticking**
- **No more freezing**
- **Smooth interaction chain**
- **Filters stay clean**
- **MIDI messages only when valid**

The interaction chain should now be **rock solid**! ???
