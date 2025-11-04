# HeadBower

![HeadBower Violin Interface](Screenshots/Headbower%20Violin%20Interface.png)

**HeadBower** is an Accessible Digital Musical Instrument (ADMI) designed to enable musical performance through head movements and facial gestures, making music creation accessible to users with limited hand mobility.

## Overview

HeadBower transforms subtle head movements (yaw, pitch, roll) and facial expressions (mouth aperture) into expressive musical control, simulating the nuanced bowing techniques of string instruments. The instrument combines multiple sensor inputs—webcam-based face tracking, smartphone motion sensors, or eye-tracking devices—with an intuitive visual interface to create an accessible and expressive musical performance system.

**Platform**: Windows only (.NET 9.0)

## Quick Start

### Installation
1. Extract the HeadBower ZIP file to a folder on your Windows PC
2. Ensure you have a MIDI output device or virtual MIDI port installed (e.g., loopMIDI, Microsoft GS Wavetable Synth)
3. Launch `HeadBower.exe`

### Initial Setup
1. **MIDI Configuration**: In the Settings panel (gear icon), select your desired MIDI output port using the ⮜/⮞ buttons
2. **Head Tracking Source**: Choose your preferred tracking method:
   - **Webcam**: Face tracking via your computer's webcam (requires NITHwebcamWrapper companion app)
   - **Phone**: Use your smartphone as a motion sensor (requires NITHphoneWrapper mobile app)
   - **Eye Tracker**: Use compatible eye-tracking hardware (requires NITHeyetrackerWrapper)
3. **Calibration**: Click "HT cal" to set your neutral head position

## How It Works

### Core Interaction Concept

HeadBower uses a **head-bowing metaphor** inspired by violin playing:

- **Left-Right Head Movement (Yaw)**: Controls **bow velocity** and **note intensity** (like moving a bow across violin strings)
- **Head Tilt (Pitch)**: Controls **modulation** (vibrato/expression) or **bow pressure** (CC9)
- **Mouth Aperture**: Controls **note gating**, **modulation**, or **intensity** depending on settings
- **Gaze/Eye Tracking**: Selects notes by looking at buttons on the screen

### Two Visual Layouts

#### Circle Layout
A circular arrangement of 8 note buttons (C, D, E, F, G, A, B, C) positioned around the screen like a color wheel. Each note has a distinct color for easy identification. Ideal for quick note selection and exploration.

#### Violin Layout
A grid representation of a violin fingerboard with 4 strings (G, D, A, E) and 7 positions across 2 octaves (28 chromatic notes total, G3 to A#5). Visual frets and string lines provide a realistic violin-like experience. Ideal for melodic playing and string-instrument familiarity.

Switch between layouts using the **Circle** and **Violin** buttons in the top bar.

## Interface Guide

### Top Bar

| Element | Description |
|---------|-------------|
| **HeadBower** | Application title |
| **Circle/Violin** | Switch between note layouts |
| **Note Display** | Shows current selected note name |
| **Pitch Display** | Shows current pitch value |
| **Bowing Indicator** | Shows if sound is actively being produced |
| **Intensity Bar (White)** | Visual feedback of current note velocity/intensity |
| **Modulation Bar (Green)** | Visual feedback of modulation (vibrato) amount |
| **Auto-scroll** 🔄 | Toggle automatic note selection scrolling (when enabled) |
| **Eye Tracker** 👁️ | Toggle gaze-to-mouse cursor control |
| **Cursor** 🖱️ | Toggle cursor visibility |
| **Settings** ⚙️ | Open/close settings panel |
| **HT cal** | Calibrate head tracker to current neutral position |
| **Exit** ✖️ | Close application |

Green indicators (●) show active states for toggleable features.

### Settings Panel

The settings panel appears on the left side when clicking the ⚙️ icon.

#### Port Configuration
- **MIDI Port**: Select which MIDI output device to send notes to
- **NITHheadTracker (optional) port**: COM port for USB-based head tracker (if used)

#### Sensitivity Controls
- **Yaw Intensity** (0.1-3.0): How much head yaw (left-right) affects bow velocity/intensity
- **Inclination Sensitivity** (0.1-3.0): How much head pitch (forward-backward tilt) affects modulation or bow pressure
- **Yaw Smoothing** (0.1-1.0): Filtering amount for yaw movements (lower = smoother but slower response, higher = more responsive but jittery)

#### Interaction Settings
- **Modul.** ●: Enable/disable modulation control (vibrato via head pitch or mouth aperture)
- **Bow Press.** ●: Enable/disable bow pressure control (MIDI CC9 via head pitch or mouth aperture)
- **Legato** ●: Enable legato mode (smooth note transitions without re-triggering)
- **Log Bow** ●: Use logarithmic bowing curve (more expressive dynamic range for subtle movements)
- **Mouth Gate** ●: Require mouth open to play notes (prevents accidental note triggers)

#### Source Selection
- **Modulation Source**: Choose **Pitch** (head inclination) or **Mouth** (mouth aperture) for modulation control
- **Bow Pressure (CC9) Source**: Choose **Pitch** or **Mouth** for bow pressure control (MIDI CC9, often mapped to brightness/timbre)
- **Pressure (Intensity) Source**: Choose **Head Yaw** (default, bow velocity) or **Mouth** (mouth aperture) for note intensity

#### Head Tracking Source
- **Eye Tracker**: Use eye-tracking hardware for head pose estimation
- **Webcam**: Use webcam-based facial tracking (requires NITHwebcamWrapper app running)
- **Phone**: Use smartphone motion sensors (requires NITHphoneWrapper app and IP configuration)

#### Interaction Method
- **HeadBow**: The primary interaction mode (head-bowing metaphor)

#### Toolkit
- **Local IP Address**: Your computer's IP address (for phone connection)
- **Reconnect**: Reconnect all sensor input receivers
- **Test Vibration**: Send a test vibration command to connected phone
- **Phone IP Address**: Set the IP address of your smartphone running NITHphoneWrapper
- **Phone Vibration Sensitivity**: Adjust haptic feedback intensity (0.1-3.0)

## Companion Applications

HeadBower requires companion applications depending on your chosen tracking source:

### NITHwebcamWrapper
- **Purpose**: Provides webcam-based facial tracking data
- **Connection**: Sends head pose data via UDP to port 20100
- **Setup**: Run NITHwebcamWrapper on the same computer, ensure webcam is working
- **Requirements**: Webcam, Windows

### NITHphoneWrapper
- **Purpose**: Uses smartphone accelerometer/gyroscope for head tracking
- **Connection**: Sends motion data via UDP to port 20103
- **Setup**: 
  1. Install NITHphoneWrapper on your smartphone
  2. Connect phone and computer to the same WiFi network
  3. In HeadBower settings, enter your phone's IP address
  4. Click "Apply" and "Test Vibration" to verify connection
- **Requirements**: Smartphone (Android/iOS), WiFi network

### NITHeyetrackerWrapper
- **Purpose**: Provides head pose tracking via eye-tracking hardware
- **Connection**: Sends head pose data via UDP to port 20102
- **Setup**: Run NITHeyetrackerWrapper with compatible eye tracker connected
- **Requirements**: Compatible eye-tracking device (e.g., Tobii), Windows

## Playing HeadBower

### Basic Playing Technique

1. **Select a Note**: Look at (or hover over) a note button in Circle or Violin layout
2. **Start Sound**: Move your head left or right (yaw) to "bow" the note
   - Faster movement = louder/more intense sound
   - Slower movement = softer sound
   - Return to center (neutral position) = silence
3. **Add Expression**:
   - Tilt head forward/backward for vibrato (if Modulation is enabled with Pitch source)
   - Open/close mouth for vibrato (if Modulation is enabled with Mouth source)
   - Control brightness/timbre with pitch or mouth (if Bow Pressure is enabled)
4. **Change Notes**: Look at different note buttons while continuing head movements for smooth transitions

### Performance Tips

- **Calibrate Often**: Click "HT cal" whenever you shift in your chair or adjust your position
- **Start with Defaults**: Begin with Yaw Intensity = 1.0 and increase sensitivity as you get comfortable
- **Use Mouth Gate**: Enable "Mouth Gate" to prevent accidental sounds when not actively performing
- **Experiment with Sources**: Try different combinations of Pitch vs. Mouth for modulation and bow pressure
- **Logarithmic Bowing**: Enable "Log Bow" for more expressive control at low velocities (subtle movements)
- **Smooth Motion**: Higher "Yaw Smoothing" (0.7-1.0) reduces jitter but delays response; lower values (0.2-0.4) give instant response but may feel jittery
- **Note Layouts**: Circle layout is great for improvisation; Violin layout is better for structured melodic playing

## Visual Feedback

HeadBower provides real-time visual feedback through an overlay bow indicator:

- **White Ellipse**: Shows current bow position and movement direction
  - Left movement → bow moves left, produces sound
  - Right movement → bow moves right, produces sound
  - Position indicates current velocity/intensity
- **Red Glow**: Indicates maximum velocity reached
- **Color Changes**: Reflect intensity levels and movement direction

## Synth Configuration

HeadBower sends MIDI messages to a synthesizer. Configure your synthesizer to respond to the following message types on the configured MIDI channel:

### Note Messages
- **Note On (0x90)**: Triggered when you start bowing a note (head yaw velocity exceeds threshold)
  - **Data**: Note number (0-127), Velocity (1-127)
  - **Channel**: Configured in Settings (MIDI Port selector)
  - **Behavior**: Holding a note while moving your head keeps it playing; return to center to trigger Note Off

- **Note Off (0x80)**: Triggered when you return head to neutral position
  - **Data**: Note number (0-127), Velocity (0)
  - **Channel**: Same as Note On

### Control Change (CC) Messages
- **CC 1 - Modulation (0xB0)**:
  - **Range**: 0-127
  - **Source**: Head pitch (forward/backward tilt) or mouth aperture (depending on Modulation Source setting)
  - **Effect**: Typically controls vibrato or tremolo in synthesizers
  - **Channel**: Configured in Settings

- **CC 8 - Channel Pressure (0xB0)**:
  - **Range**: 0-127
  - **Source**: Head yaw velocity or mouth aperture (depending on Pressure Source setting)
  - **Effect**: Controls note intensity/expression continuously during note sustain
  - **Behavior**: Updates in real-time as you move your head (bow velocity changes)
  - **Channel**: Configured in Settings

- **CC 9 - Bow Pressure (0xB0)**:
  - **Range**: 0-127
  - **Source**: Head pitch or mouth aperture (depending on Bow Pressure Source setting)
  - **Effect**: Typically controls brightness, timbre, or filter cutoff in synthesizers
  - **Channel**: Configured in Settings

- **Velocity (Note On message)**:
  - **Range**: 1-127
  - **Source**: Set when note is first triggered
  - **Effect**: Initial attack intensity for the note envelope
  - **Channel**: Configured in Settings

### Recommended Synthesizer Setup

For optimal results, configure your synthesizer's MIDI input as follows:

| MIDI Message | Synthesis Parameter | Suggested Mapping |
|---|---|---|
| Note On/Off | Pitch + Envelope Gate | Play the selected note while active |
| Note Velocity | Amplitude Envelope Attack | Initial note attack intensity |
| CC 1 (Modulation) | Vibrato Depth or LFO Amount | Creates expression/wobble effect |
| CC 8 (Channel Pressure) | Volume or Amplitude | Continuous intensity control during bowing |
| CC 9 (Bow Pressure) | Filter Cutoff or Brightness | Controls tone color during sustained notes |
| CC 11 (Expression) | Velocity or Amplitude Envelope | Responsive to bowing intensity |

### Example: Standalone Synthesizer
If using a standalone synth (hardware or software):
1. Set the MIDI input channel to match HeadBower's configured channel (default: Channel 1)
2. Verify the synth responds to Note On/Off events with velocity > 0
3. Assign CC 1 (Modulation), CC 8 (Channel Pressure), and CC 9 (Bow Pressure) to desired synthesis parameters
4. Test with "Note Display" showing the correct note name when selecting buttons

### Example: DAW Configuration
If using a DAW (Digital Audio Workstation):
1. Create a MIDI track with your selected instrument/synth
2. Set the input source to the HeadBower MIDI port
3. Set the MIDI channel to the value configured in HeadBower settings
4. Enable CC automation recording for CC 1, CC 8, and CC 9 to capture expression
5. Record or perform directly into the DAW

## Technical Details

### MIDI Output
- **Note On/Off**: Triggered by head movement velocity (bow motion)
- **Note Velocity**: Initial attack intensity set when note starts
- **Channel Pressure (CC 8)**: Controlled by head yaw velocity or mouth aperture (updates continuously during bowing)
- **Modulation (CC 1)**: Controlled by head pitch or mouth aperture (when enabled)
- **Bow Pressure (CC 9)**: Controlled by head pitch or mouth aperture (when enabled)
- **Channel**: Configurable via MIDI port settings (default: Channel 1)

### Network Ports
- **20100**: UDP receiver for NITHwebcamWrapper (head pose data)
- **20102**: UDP receiver for NITHeyetrackerWrapper (optiona, head pose data and gaze)
- **20103**: UDP receiver for NITHphoneWrapper (motion sensor data)
- **21103**: UDP sender to NITHphoneWrapper (vibration commands)

### System Requirements
- **OS**: Windows 10 or later
- **.NET**: .NET 9.0 Runtime (included in standalone distribution)
- **Hardware**: 
  - Webcam (for webcam tracking mode)
  - Smartphone with WiFi (for phone tracking mode)
  - Eye-tracking device (for eye-tracker mode)
- **MIDI**: Virtual or hardware MIDI output device

## Credits & Research

HeadBower is developed as part of accessibility research in digital musical instruments. The system builds upon concepts from:

- Accessible Digital Musical Instruments (ADMIs)
- Head-based interaction techniques
- Multimodal sensor fusion for expressive control

For technical details and research background, refer to the included paper: _to be included!_

## License & Support

HeadBower is part of the NITH project family.

**Developed by**: LIM (Laboratorio di Informatica Musicale), University of Milan

For issues, questions, or contributions, please refer to the project repositories:
- HeadBower: https://github.com/LIMUNIMI/HeadBower
- NITHlibrary: https://github.com/LIMUNIMI/NITHlibrary

---

**Enjoy making music with HeadBower!** 🎻🎶
