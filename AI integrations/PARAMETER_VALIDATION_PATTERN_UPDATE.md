# Parameter Validation Pattern Update

## Overview
Updated all `INithSensorBehavior` implementations to follow a consistent parameter validation pattern, ensuring that behaviors only process data when ALL required parameters are present.

## Pattern Structure

Every behavior now follows this structure:

```csharp
public class MyBehavior : INithSensorBehavior
{
    // Required parameters list at the beginning of the class
    private readonly List<NithParameters> requiredParams = new List<NithParameters>
    {
        NithParameters.parameter1,
        NithParameters.parameter2
        // ... all required parameters
    };

    public void HandleData(NithSensorData nithData)
    {
        // ONLY process if ALL required parameters are present
        if (nithData.ContainsParameters(requiredParams))
        {
            // ALL processing logic goes inside this if statement
            // Get parameter values
            // Apply filters
            // Update state
            // Send outputs
        }
        // If parameters missing, behavior does nothing (silent skip)
    }
}
```

## Benefits

1. **Robustness**: Prevents crashes when sensors don't provide all expected parameters
2. **Clarity**: Immediately shows which parameters each behavior depends on
3. **Consistency**: All behaviors follow the same validation pattern
4. **Performance**: Avoids unnecessary processing when data is incomplete
5. **Debugging**: Makes it clear when behaviors are skipped due to missing parameters

## Updated Behaviors

### HeadBow Behaviors
- ? `NITHbehavior_HeadViolinBow.cs` - Already had pattern
- ? `BowMotionBehavior.cs` - Already had pattern
- ? `ModulationControlBehavior.cs` - Already had pattern (with dual parameter sets for different sources)
- ? `BowPressureControlBehavior.cs` - Already had pattern (with dual parameter sets for different sources)
- ? `HapticFeedbackBehavior.cs` - Already had pattern (with dual parameter sets for acceleration/velocity)
- ? `VisualFeedbackBehavior.cs` - **UPDATED**: Added required parameters list and wrapped all processing
- ? `BowMotionIndicatorBehavior.cs` - **UPDATED**: Added required parameters list and wrapped all processing

### Headtracker Behaviors
- ? `NithSensorBehavior_YawPlay.cs` - **UPDATED**: Added required parameters list and wrapped all processing

### General Behaviors
- ? `WriteToConsoleBehavior.cs` - Already had pattern (with optional parameter handling inside)

## Behaviors Not Updated (Different Interfaces)

These behaviors implement different interfaces and don't use the `HandleData(NithSensorData)` pattern:

- `KBemulateMouse.cs` - Implements `IKeyboardBehavior`
- `KBstopEmulateMouse.cs` - Implements `IKeyboardBehavior`
- `KBsimulateBlow.cs` - Implements `IKeyboardBehavior`
- `EBBdoubleCloseClick.cs` - Extends `ANithBlinkEventBehavior`
- `DiscoveryBehavior_NithPhoneWrapper.cs` - Implements `IDeviceDiscoveryBehavior`

## Example: ModulationControlBehavior

This behavior shows an advanced pattern with multiple parameter sets depending on the control source:

```csharp
// Required parameters for pitch mode
private readonly List<NithParameters> requiredParamsPitch = new List<NithParameters>
{
    NithParameters.head_pos_pitch
};

// Required parameters for mouth mode
private readonly List<NithParameters> requiredParamsMouth = new List<NithParameters>
{
    NithParameters.mouth_ape
};

public void HandleData(NithSensorData nithData)
{
    // Select appropriate parameter list based on source
    List<NithParameters> requiredParams = currentSource == ModulationControlSources.HeadPitch 
        ? requiredParamsPitch 
        : requiredParamsMouth;

    // ONLY process if ALL required parameters are present
    if (nithData.ContainsParameters(requiredParams))
    {
        // Processing logic...
    }
}
```

## Verification

All behaviors compile successfully with the updated pattern. The build is clean with no errors or warnings related to these changes.

## Reference Implementation

The pattern is based on the Netytar project's `NithSensorBehaviorMouthAperture` behavior, which demonstrates best practices for parameter validation in NITH behaviors.
