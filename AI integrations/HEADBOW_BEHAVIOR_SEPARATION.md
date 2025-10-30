# HeadBow Behavior Separation

## Overview
The monolithic `NITHbehavior_HeadViolinBow` behavior has been separated into multiple focused behaviors for better modularity, maintainability, and flexibility.

## Architecture Changes

### Old Architecture (Deprecated)
- **Single behavior**: `NITHbehavior_HeadViolinBow`
  - Handled all aspects: motion detection, modulation, bow pressure, position, haptics
  - ~350+ lines of complex code
  - Hard to modify individual features
  - Tightly coupled components

### New Architecture
**Five separate behaviors**, each with a single responsibility:

#### 1. **BowMotionBehavior**
- **Responsibility**: Core bow motion detection and note triggering
- **Features**:
  - Detects head yaw acceleration/velocity
  - Triggers note on/off based on motion magnitude
  - Handles direction changes and pause logic
  - Sensitivity control (adjustable via UI)
- **Updates**: `Rack.MappingModule.Blow`, `Pressure`, `InputIndicatorValue`

#### 2. **ModulationControlBehavior**
- **Responsibility**: MIDI modulation (CC1) control
- **Features**:
  - Three switchable sources:
    - Head pitch rotation
    - Mouth aperture
    - Head roll rotation
  - Independent filtering for each source
  - Only active when note is playing and modulation is enabled
- **Updates**: `Rack.MappingModule.Modulation`

#### 3. **BowPressureControlBehavior**
- **Responsibility**: MIDI bow pressure (CC9) control
- **Features**:
  - Two switchable sources:
    - Head pitch rotation
    - Mouth aperture
  - Independent filtering for each source
  - Only active when note is playing
- **Sends**: MIDI CC9 messages

#### 4. **BowPositionBehavior**
- **Responsibility**: Maps head yaw position to bow position
- **Features**:
  - Position-based mapping (visual feedback)
  - Automatically disables when using acceleration mode (phone IMU)
  - Filtered position tracking
- **Updates**: `Rack.MappingModule.BowPosition`

#### 5. **HapticFeedbackBehavior**
- **Responsibility**: Sends vibration feedback to phone
- **Features**:
  - Intensity based on bow motion magnitude
  - Sensitivity control (adjustable via UI)
  - Rate-limited to avoid excessive vibration
  - Only active when note is playing
- **Sends**: Vibration commands to phone

## Benefits

### 1. **Single Responsibility Principle**
Each behavior has one clear purpose, making code easier to understand and maintain.

### 2. **Independent Testing**
Each behavior can be tested in isolation without affecting others.

### 3. **Flexible Configuration**
Behaviors can be:
- Enabled/disabled independently
- Reordered in the processing pipeline
- Easily replaced with alternative implementations

### 4. **Easier Debugging**
When something goes wrong, it's clear which behavior is responsible.

### 5. **Code Reusability**
Individual behaviors can be reused in other contexts or projects.

## Migration Guide

### For Developers

#### Adding/Removing Behaviors
In `DefaultSetup.cs`:
```csharp
private void SetupBehaviors()
{
    // Create behaviors
    Rack.Behavior_BowMotion = new BowMotionBehavior();
    Rack.Behavior_ModulationControl = new ModulationControlBehavior();
    // ... etc
    
    // Add to module (order matters!)
    Rack.NithModuleUnified.SensorBehaviors.Add(Rack.Behavior_BowMotion);
    Rack.NithModuleUnified.SensorBehaviors.Add(Rack.Behavior_ModulationControl);
    // ... etc
}
```

#### Modifying Sensitivity
Both bow motion and haptic feedback share sensitivity control:
```csharp
Rack.Behavior_BowMotion.Sensitivity = value;
Rack.Behavior_HapticFeedback.Sensitivity = value;
```

#### Changing Modulation/Bow Pressure Sources
Done through settings (no code changes needed):
```csharp
Rack.UserSettings.ModulationControlSource = ModulationControlSources.HeadPitch;
Rack.UserSettings.BowPressureControlSource = BowPressureControlSources.MouthAperture;
```

### For Users
No visible changes! The interface remains the same:
- Same sensitivity slider controls bow motion and haptics
- Same modulation source buttons (Pitch/Mouth/Roll)
- Same bow pressure source buttons (Pitch/Mouth)

## Implementation Details

### Data Flow
```
[Sensor Data] 
    ?
[Parameter Selector] (filters by source)
    ?
[Unified Calibrator] (centers head position)
    ?
[Acceleration Calculator] (computes acceleration from velocity)
    ?
[Smoothing Filter] (reduces noise)
    ?
[BowMotionBehavior] ??? Note On/Off
    ?
[ModulationControlBehavior] ??? CC1
    ?
[BowPressureControlBehavior] ??? CC9
    ?
[BowPositionBehavior] ??? Visual feedback
    ?
[HapticFeedbackBehavior] ??? Phone vibration
```

### Shared State
Behaviors communicate through `Rack.MappingModule`:
- `Blow` - Is note currently playing?
- `Pressure` - Current bow pressure (visual)
- `Modulation` - Current modulation value
- `BowPosition` - Current bow position (visual)
- etc.

## Files Changed

### New Files
- `Behaviors/HeadBow/BowMotionBehavior.cs`
- `Behaviors/HeadBow/ModulationControlBehavior.cs`
- `Behaviors/HeadBow/BowPressureControlBehavior.cs`
- `Behaviors/HeadBow/BowPositionBehavior.cs`
- `Behaviors/HeadBow/HapticFeedbackBehavior.cs`

### Modified Files
- `Modules/DefaultSetup.cs` - Instantiates and registers new behaviors
- `Modules/Rack.cs` - Added properties for new behaviors
- `MainWindow.xaml.cs` - Updated sensitivity application logic

### Deprecated Files
- `Behaviors/HeadBow/NITHbehavior_headViolinBow.cs` - Marked as `[Obsolete]`
  - Still present for reference but not used
  - Will be removed in future version

## Future Enhancements

### Potential Improvements
1. **Configurable thresholds**: Make constants adjustable via UI
2. **Per-behavior enable/disable**: Toggle individual behaviors from settings
3. **Custom behavior chains**: Allow users to create custom processing pipelines
4. **Behavior presets**: Save/load different behavior configurations

### Adding New Behaviors
To add a new behavior:
1. Create class implementing `INithSensorBehavior`
2. Add property to `Rack.cs`
3. Instantiate in `DefaultSetup.SetupBehaviors()`
4. Add to `NithModuleUnified.SensorBehaviors` list

## Backwards Compatibility

The old `Rack.Behavior_HeadBow` property is marked `[Obsolete]` but still exists to prevent breaking changes. It is not used in the current code.

Any code still referencing it will receive a compiler warning suggesting to use the new separated behaviors instead.

## Performance Impact

**No significant performance impact:**
- Same total number of operations
- Slightly more method call overhead (negligible)
- Better cache locality due to smaller classes
- Easier for JIT to optimize individual behaviors

## Testing Recommendations

### Unit Tests Needed
- [ ] BowMotionBehavior note triggering logic
- [ ] ModulationControlBehavior source switching
- [ ] BowPressureControlBehavior source switching
- [ ] BowPositionBehavior position mapping
- [ ] HapticFeedbackBehavior vibration rate limiting

### Integration Tests Needed
- [ ] All behaviors work together correctly
- [ ] Sensitivity changes apply to correct behaviors
- [ ] Source switching doesn't affect other behaviors
- [ ] No race conditions in shared state access

### User Acceptance Tests
- [ ] Bow motion feels responsive
- [ ] Modulation control is smooth
- [ ] Bow pressure responds correctly
- [ ] Haptic feedback is not too strong/weak
- [ ] UI controls work as before

## Conclusion

This separation improves code quality significantly while maintaining full backwards compatibility from a user perspective. Each behavior is now focused, testable, and maintainable.
