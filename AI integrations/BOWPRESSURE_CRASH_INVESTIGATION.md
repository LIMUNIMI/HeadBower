# BowPressureControlBehavior Crash Investigation

## Current State

The behavior is currently disabled with `if (false && ...)` on line 52 to prevent crashes.

## Potential Crash Causes

### 1. **Null Reference on Rack.UserSettings** ?
```csharp
// Line 46
BowPressureControlSources currentSource = Rack.UserSettings.BowPressureControlSource;
```

**Test:** Check if `Rack.UserSettings` is null when behavior HandleData is called.

### 2. **GetParameterValue Returning Null** ?
```csharp
// Line 59
double rawPitch = nithData.GetParameterValue(NithParameters.head_pos_pitch).Value.ValueAsDouble;
```

**Issue:** If `ContainsParameters` check is bypassed/fails, `GetParameterValue` could return `null`, causing:
```
NullReferenceException: Object reference not set to an instance of an object
at: .Value.ValueAsDouble
```

### 3. **Filter State Corruption** ?
```csharp
// Lines 27-28
private readonly DoubleFilterMAexpDecaying _pitchPosFilter = new DoubleFilterMAexpDecaying(0.9f);
private readonly DoubleFilterMAexpDecaying _mouthApertureFilter = new DoubleFilterMAexpDecaying(0.7f);
```

**Issue:** If filters receive unexpected values (NaN, Infinity), they could corrupt state.

### 4. **MappingModule.BowPressure Setter Crash** ?
```csharp
// Line 109
Rack.MappingModule.BowPressure = bowPressureValue;
```

**Check in MappingModule.cs:**
```csharp
public int BowPressure
{
    get { return bowPressure; }
    set
    {
        int newValue;
        
        if (Rack.UserSettings.BowPressureControlMode == _BowPressureControlModes.On) // ? Could crash here
        {
            newValue = Math.Clamp(value, 0, 127);
        }
        else
        {
            newValue = 0;
        }
        
        if (newValue != bowPressure)
        {
            bowPressure = newValue;
            SetBowPressure(); // ? Or here
        }
    }
}

private void SetBowPressure()
{
    Rack.MidiModule.SendControlChange(9, BowPressure); // ? Or here
}
```

**Potential issues:**
- `Rack.UserSettings` is null
- `Rack.MidiModule` is null
- MIDI device disconnected/not initialized

## Current Code Issues (Even Without false)

### Issue 1: Early Return Missing Mode Check ?

**Current Code:**
```csharp
public void HandleData(NithSensorData nithData)
{
    // Determine which source we're using and check if ALL required parameters are present
    BowPressureControlSources currentSource = Rack.UserSettings.BowPressureControlSource;
    
    // MISSING: Check if Bow Pressure Control is actually enabled!
    // ModulationControlBehavior has this check:
    // if (Rack.UserSettings.ModulationControlMode != _ModulationControlModes.On)
    // {
    //     Rack.MappingModule.Modulation = 0;
    //     return;
    // }
```

**Should Be:**
```csharp
public void HandleData(NithSensorData nithData)
{
    // Early exit if control is disabled or no note playing
    if (!Rack.MappingModule.Blow || 
        Rack.UserSettings.BowPressureControlMode != _BowPressureControlModes.On)
    {
        Rack.MappingModule.BowPressure = 0;
        return;
    }

    BowPressureControlSources currentSource = Rack.UserSettings.BowPressureControlSource;
    // ... rest of code
}
```

### Issue 2: Unused Fields Warning ??

Build shows warnings:
```
BowPressureControlBehavior.cs(37,31,37,43): warning CS0169: The field '_pitchMapper' is never used
BowPressureControlBehavior.cs(38,31,38,43): warning CS0169: The field '_mouthMapper' is never used
BowPressureControlBehavior.cs(39,24,39,43): warning CS0414: The field '_lastPitchThreshold' is assigned but its value is never used
BowPressureControlBehavior.cs(40,24,40,39): warning CS0414: The field '_lastPitchRange' is assigned but its value is never used
```

**Reason:** These ARE used inside the `if (false && ...)` block that's currently disabled!

## Recommended Investigation Steps

### Step 1: Add Null Checks
```csharp
public void HandleData(NithSensorData nithData)
{
    try
    {
        // Check for null references BEFORE doing anything
        if (Rack.UserSettings == null)
        {
            Console.WriteLine("[BowPressure] ERROR: Rack.UserSettings is null!");
            return;
        }

        if (Rack.MappingModule == null)
        {
            Console.WriteLine("[BowPressure] ERROR: Rack.MappingModule is null!");
            return;
        }

        // Early exit if control is disabled or no note playing
        if (!Rack.MappingModule.Blow || 
            Rack.UserSettings.BowPressureControlMode != _BowPressureControlModes.On)
        {
            Rack.MappingModule.BowPressure = 0;
            return;
        }

        // Rest of original code...
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[BowPressure] EXCEPTION: {ex.Message}");
        Console.WriteLine($"[BowPressure] Stack: {ex.StackTrace}");
    }
}
```

### Step 2: Safe Parameter Access
```csharp
// Instead of:
double rawPitch = nithData.GetParameterValue(NithParameters.head_pos_pitch).Value.ValueAsDouble;

// Use:
var pitchParam = nithData.GetParameterValue(NithParameters.head_pos_pitch);
if (pitchParam == null)
{
    Console.WriteLine("[BowPressure] ERROR: head_pos_pitch parameter is null!");
    return;
}
double rawPitch = pitchParam.Value.ValueAsDouble;
```

### Step 3: Test Incrementally

**Test 1:** Remove `false`, add try-catch, add null checks
```csharp
if (nithData.ContainsParameters(requiredParams))
{
    try
    {
        Console.WriteLine("[BowPressure] Processing frame...");
        // ... rest of code
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[BowPressure] CRASH: {ex}");
        throw; // Re-throw to see full stack trace
    }
}
```

**Test 2:** If Test 1 works, remove try-catch
**Test 3:** If Test 2 works, remove console logs

## Comparison with ModulationControlBehavior

ModulationControlBehavior (WORKING) has:
1. ? Early mode check with reset
2. ? Dual parameter lists
3. ? ContainsParameters validation
4. ? Safe parameter access

BowPressureControlBehavior (BROKEN) has:
1. ? No early mode check (missing!)
2. ? Dual parameter lists
3. ? ContainsParameters validation (but disabled with `false`)
4. ?? Parameter access (safe IF ContainsParameters check passes)

## Hypothesis

**Most Likely Cause:**
The behavior is trying to access `Rack.UserSettings.BowPressureControlMode` or `Rack.MidiModule` when one of them is null during initialization.

**Evidence:**
- Build is successful (no compile errors)
- Warnings about unused fields (because code is disabled)
- No indication of which exception is thrown

**Solution:**
Add the missing early mode check like ModulationControlBehavior has.

## Next Steps

1. Remove `false &&` temporarily
2. Add try-catch wrapper
3. Add console logging to identify exact crash point
4. Add missing early mode check
5. Verify parameter access is safe
6. Test with real data

