# HeadBower Refactoring: Cleaner Head Tracking Source Selection

## Overview

This refactoring consolidates and organizes the head tracking source selection logic into `MappingModule`, with cleaner parameter management and a simpler data flow.

## Key Changes

### 1. Head Motion Parameter Lists in MappingModule

Centralized definition of all head motion parameters:

```csharp
// Individual parameter groups
public static readonly List<NithParameters> HeadPositionParameters = new()
{
    NithParameters.head_pos_yaw,
    NithParameters.head_pos_pitch,
    NithParameters.head_pos_roll
};

public static readonly List<NithParameters> HeadVelocityParameters = new()
{
    NithParameters.head_vel_yaw,
    NithParameters.head_vel_pitch,
    NithParameters.head_vel_roll
};

public static readonly List<NithParameters> HeadAccelerationParameters = new()
{
    NithParameters.head_acc_yaw,
    NithParameters.head_acc_pitch,
    NithParameters.head_acc_roll
};

// Combined for convenience
public static readonly List<NithParameters> AllHeadMotionParameters = new()
{
    // All 9 parameters combined
};
```

**Benefits:**
- Single source of truth for head motion parameters
- Easy to reference and extend
- Prevents typos and parameter duplication
- Clear organization by data type

### 2. Selector Extension Method

Added `AddRulesList()` to `NithPreprocessor_ParameterSelector`:

```csharp
public void AddRulesList(string sensorName, IEnumerable<NithParameters> parameters)
{
    foreach (var param in parameters)
    {
        AddRule(sensorName, param);
    }
}
```

**Usage:**
```csharp
// Instead of:
selector.AddRules("sensor", param1, param2, param3, ...);

// Now also supports:
selector.AddRulesList("sensor", MappingModule.AllHeadMotionParameters);
```

### 3. Centralized Selection Logic in MappingModule

New method `SelectHeadTrackingSource()` handles all logic:

```csharp
public static void SelectHeadTrackingSource(HeadTrackingSources source)
{
    // Clear all existing rules
    Rack.ParameterSelector.ClearAllRules();

    // Get the sensor name for this source
    string selectedSensorName = GetSensorNameForSource(source);

    // Whitelist all head motion parameters for the selected source
    Rack.ParameterSelector.AddRulesList(selectedSensorName, AllHeadMotionParameters);

    // Block head motion parameters from all OTHER sources
    BlockHeadMotionFromOtherSources(source);

    // Log configuration
    LogSelectionConfiguration(source);
}
```

**What it does:**
1. Clears previous rules
2. Maps source enum to sensor name
3. Whitelists all head motion parameters for selected source
4. Ensures other sources are blocked (implicit in Whitelist mode)
5. Logs the configuration for debugging

**Helper methods:**

```csharp
// Maps HeadTrackingSources enum to actual sensor names
private static string GetSensorNameForSource(HeadTrackingSources source)
{
    return source switch
    {
        HeadTrackingSources.Webcam => "NITHwebcamWrapper",
        HeadTrackingSources.Phone => "NITHphoneWrapper",
        HeadTrackingSources.EyeTracker => "NITHtobiasWrapper",
        _ => throw new ArgumentException(...)
    };
}

// Ensures other sources don't send head motion data
private static void BlockHeadMotionFromOtherSources(HeadTrackingSources selectedSource)
{
    // In Whitelist mode, not adding rules = blocking
    // So this is implicit - no need to explicitly block
}

// Logs current configuration
private static void LogSelectionConfiguration(HeadTrackingSources source)
{
    Console.WriteLine("\n=== HEAD TRACKING SOURCE SELECTION ===");
    Console.WriteLine($"Selected Source: {source}");
    Console.WriteLine(Rack.ParameterSelector.GetRulesSummary());
    Console.WriteLine("=====================================\n");
}
```

### 4. Simplified Interface Integration

In `MainWindow.xaml.cs`, button clicks now directly call MappingModule:

```csharp
private void btnWebcam_Click(object sender, RoutedEventArgs e)
{
    if (InstrumentStarted)
    {
        Rack.UserSettings.HeadTrackingSource = HeadTrackingSources.Webcam;
        UpdateHeadTrackingSource();
        UpdateGUIVisuals();
    }
}

private void UpdateHeadTrackingSource()
{
    // Call the selector in MappingModule
    MappingModule.SelectHeadTrackingSource(Rack.UserSettings.HeadTrackingSource);
    
    // Update sensitivity for behaviors
    Rack.Behavior_BowMotion.Sensitivity = Rack.UserSettings.SensorIntensityHead;
    Rack.Behavior_HapticFeedback.Sensitivity = Rack.UserSettings.SensorIntensityHead;
}
```

### 5. Simplified DefaultSetup

`SetupUnifiedModule()` now just calls the new method:

```csharp
// Create parameter selector
Rack.ParameterSelector = new NithPreprocessor_ParameterSelector();

// Configure with default (Webcam)
MappingModule.SelectHeadTrackingSource(HeadTrackingSources.Webcam);

// Add to module
Rack.NithModuleUnified.Preprocessors.Add(Rack.ParameterSelector);
```

The old `ConfigureParameterSelector()` method is kept as a deprecated wrapper for backward compatibility.

## Data Flow

### Parameter Selection Process

```
User clicks Webcam button
    ?
btnWebcam_Click() 
    ?
UpdateHeadTrackingSource()
    ?
MappingModule.SelectHeadTrackingSource(Webcam)
    ?? ClearAllRules()
    ?? GetSensorNameForSource() ? "NITHwebcamWrapper"
    ?? AddRulesList("NITHwebcamWrapper", AllHeadMotionParameters)
    ?? BlockHeadMotionFromOtherSources(Webcam)
    ?? LogSelectionConfiguration(Webcam)
    ?
Parameter Selector configured:
    ? NITHwebcamWrapper: ACCEPTS all head_pos_*, head_vel_*, head_acc_*
    ? NITHphoneWrapper: BLOCKED (no rules)
    ? NITHtobiasWrapper: BLOCKED (no rules)
    ?
Data flows through unified module with correct filtering
```

## Current Configuration (Simplified View)

```csharp
// HEAD MOTION PARAMETERS ACCEPTED FOR EACH SOURCE:

// Webcam Mode (default)
Webcam:     ? head_pos_* / head_vel_* / head_acc_*
Phone:      ? (blocked)
Eye Tracker: ? (blocked)

// Phone Mode
Webcam:      ? (blocked)
Phone:       ? head_pos_* / head_vel_* / head_acc_*
Eye Tracker: ? (blocked)

// Eye Tracker Mode
Webcam:      ? (blocked)
Phone:       ? (blocked)
Eye Tracker: ? head_pos_* / head_vel_* / head_acc_*
```

## Adding Other Parameters Later

When you're ready to add gaze, mouth, and eye parameters back, just:

1. Create parameter lists in MappingModule:
   ```csharp
   public static readonly List<NithParameters> GazeParameters = new()
   {
       NithParameters.gaze_x,
       NithParameters.gaze_y
   };
   
   public static readonly List<NithParameters> MouthApertureParameters = new()
   {
       NithParameters.mouth_ape
   };
   
   // etc.
   ```

2. Extend `SelectHeadTrackingSource()` to handle additional parameters:
   ```csharp
   // Add gaze from eye tracker (always)
   Rack.ParameterSelector.AddRulesList("NITHtobiasWrapper", GazeParameters);
   
   // Add mouth from webcam (always)
   Rack.ParameterSelector.AddRulesList("NITHwebcamWrapper", MouthApertureParameters);
   ```

## Files Modified

| File | Change |
|------|--------|
| `NITHlibrary/Nith/Preprocessors/NithPreprocessor_ParameterSelector.cs` | Added `AddRulesList()` method |
| `HeadBower/Modules/MappingModule.cs` | Added head motion parameter lists and `SelectHeadTrackingSource()` method |
| `HeadBower/Modules/DefaultSetup.cs` | Simplified to use `MappingModule.SelectHeadTrackingSource()` |
| `HeadBower/MainWindow.xaml.cs` | Updated `UpdateHeadTrackingSource()` to call MappingModule |

## Build Status

? **Build successful** - Ready for testing

## Architecture Benefits

1. **Centralized Logic**: All parameter selection in one place (MappingModule)
2. **DRY Principle**: No parameter duplication, single lists used everywhere
3. **Clean Interface**: UI buttons directly call semantic methods
4. **Maintainability**: Easy to modify parameters and rules
5. **Extensibility**: Simple to add new parameter types later
6. **Type Safety**: Using enum `HeadTrackingSources` for source mapping
7. **Debugging**: Clear console logging of selector configuration
8. **Backward Compatibility**: Old method kept as deprecated wrapper

## Testing Checklist

- [ ] Webcam mode: Should accept head motion parameters
- [ ] Phone mode: Should accept head motion parameters
- [ ] Eye Tracker mode: Should accept head motion parameters
- [ ] Switching between sources: Should reconfigure selector
- [ ] Console output: Should show correct parameter lists for each source
- [ ] Behaviors: Should receive head motion data from selected source
