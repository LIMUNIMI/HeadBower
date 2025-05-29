// MainWindow.xaml.cs
using HeadBower.Behaviors;
using HeadBower.Behaviors.HeadBow;
using HeadBower.Modules;
using HeadBower.OLD.Behaviors.Headtracker;
using HeadBower.OLD.Behaviors.Mouth;
using HeadBower.OLD.Behaviors.Pressure;
using NITHdmis.Music;
using NITHlibrary.Nith.Internals;
using NITHlibrary.Tools.Logging;
using System.Security.AccessControl;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using HeadBower.Visuals; // add reference

namespace HeadBower
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly SolidColorBrush ActiveBrush = new SolidColorBrush(Colors.LightGreen);
        private readonly SolidColorBrush BlankBrush = new SolidColorBrush(Colors.Black);
        private readonly SolidColorBrush GazeButtonColor = new SolidColorBrush(Colors.DarkGoldenrod);
        private readonly SolidColorBrush WarningBrush = new SolidColorBrush(Colors.DarkRed);
        private Brush LastGazedBrush = null;
        private Scale lastScale = ScalesFactory.Cmaj;
        private DefaultSetup netytarSetup;
        private bool InstrumentStarted = false;
        private DispatcherTimer updater;
        private double velocityBarMaxHeight = 0;
        private ViolinOverlayManager overlayManager; // Gestore overlay per il violino

        public MainWindow()
        {
            InitializeComponent();

            // Debugger
            TraceAdder.AddTrace();
            DataContext = this;

            // GUI updater
            updater = new DispatcherTimer();
            updater.Interval = new TimeSpan(1000);
            updater.Tick += UpdateTimedVisuals;
            updater.Start();

            // Inizialmente, imposta l'overlay per il layout Violin
            overlayManager = new ViolinOverlayManager(ViolinOverlayViolin);
        }

        public int BreathSensorValue { get; set; } = 0;
        public bool IsSettingsShown { get; set; } = false;
        public Button LastSettingsGazedButton { get; set; } = null;
        public Scale SelectedScale { get; set; } = ScalesFactory.Cmaj;

        public int SensorPort
        {
            get { return Rack.UserSettings.SensorPort; }
            set
            {
                if (value > 0)
                {
                    Rack.UserSettings.SensorPort = value;
                }
            }
        }

        public void ReceiveBlowingChange()
        {
            if (Rack.MappingModule.Blow)
            {
                txtIsBlowing.Text = "B";
            }
            else
            {
                txtIsBlowing.Text = "_";
            }
        }

        public void ReceiveNoteChange()
        {
            UpdateGUIVisuals();
        }

        internal void ChangeScale(ScaleCodes scaleCode)
        {
            Rack.MappingModule.NetytarSurface.Scale = new Scale(Rack.MappingModule.SelectedNote.ToAbsNote(), scaleCode);
        }

        private void btnBlinkPlay_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnBlinkSelectScale_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                switch (Rack.UserSettings.BlinkSelectScaleMode)
                {
                    case _BlinkSelectScaleMode.Off:
                        Rack.UserSettings.BlinkSelectScaleMode = _BlinkSelectScaleMode.On;
                        break;

                    case _BlinkSelectScaleMode.On:
                        Rack.UserSettings.BlinkSelectScaleMode = _BlinkSelectScaleMode.Off;
                        break;
                }

                UpdateGUIVisuals();
            }
        }

        private void btnBreath_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.InteractionMethod = InteractionMappings.Breath;
                ChangeMapping();
                UpdateGUIVisuals();
            }
        }

        private void btnBreathControlSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                if (Rack.MappingModule.BreathControlMode == _BreathControlModes.Switch)
                {
                    Rack.MappingModule.BreathControlMode = _BreathControlModes.Dynamic;
                }
                else if (Rack.MappingModule.BreathControlMode == _BreathControlModes.Dynamic)
                {
                    Rack.MappingModule.BreathControlMode = _BreathControlModes.Switch;
                }
            }

            BreathSensorValue = 0;

            UpdateGUIVisuals();
        }

        private void btnBSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.BreathControlMode = Rack.UserSettings.BreathControlMode == _BreathControlModes.Dynamic ? _BreathControlModes.Switch : _BreathControlModes.Dynamic;
                UpdateGUIVisuals();
            }
        }

        private void btnCalibrateHeadPose_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //btnCalibrateHeadPose.Background = new SolidColorBrush(Colors.Black);
        }

        private void btnCtrlEyePos_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.InteractionMethod = InteractionMappings.EyePos;
                Rack.MappingModule.ResetModulationAndPressure();

                BreathSensorValue = 0;

                UpdateGUIVisuals();
            }
        }

        private void btnCtrlEyeVel_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.InteractionMethod = InteractionMappings.EyeVel;
                Rack.MappingModule.ResetModulationAndPressure();

                BreathSensorValue = 0;

                UpdateGUIVisuals();
            }
        }

        private void btnCtrlKeyboard_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.InteractionMethod = InteractionMappings.Keyboard;
                Rack.MappingModule.ResetModulationAndPressure();

                BreathSensorValue = 0;

                UpdateGUIVisuals();
            }
        }

        private void btnExit_Activate(object sender, RoutedEventArgs e)
        {
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Rack.SavingSystem.SaveSettings(Rack.UserSettings);
            Close();
        }

        private void BtnFFBTest_Click(object sender, RoutedEventArgs e)
        {
            //Rack.DMIBox.FfbModule.FlashFFB();
        }

        private void btnHeadYaw_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.InteractionMethod = InteractionMappings.HeadYaw;
                ChangeMapping();
                UpdateGUIVisuals();
            }
        }

        private void btnKeyboard_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.InteractionMethod = InteractionMappings.Keyboard;
                ChangeMapping();
                UpdateGUIVisuals();
            }
        }

        private void BtnMIDIchMinus_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.MIDIPort--;
                Rack.MidiModule.OutDevice = Rack.UserSettings.MIDIPort;
                UpdateGUIVisuals();
            }
        }

        private void BtnMIDIchPlus_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.MIDIPort++;
                Rack.MidiModule.OutDevice = Rack.UserSettings.MIDIPort;
                UpdateGUIVisuals();
            }
        }

        private void btnMod_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.ModulationControlMode = Rack.UserSettings.ModulationControlMode == _ModulationControlModes.On ? _ModulationControlModes.Off : _ModulationControlModes.On;
                UpdateGUIVisuals();
            }
        }

        private void btnModulationControlSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.ModulationControlMode = Rack.UserSettings.ModulationControlMode == _ModulationControlModes.On ? _ModulationControlModes.Off : _ModulationControlModes.On;
                UpdateGUIVisuals();
            }
        }

        private void btnNeutral_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnNoCursor_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.MappingModule.CursorHidden = !Rack.MappingModule.CursorHidden;
                Cursor = Rack.MappingModule.CursorHidden ? System.Windows.Input.Cursors.None : System.Windows.Input.Cursors.Arrow;
            }

            UpdateGUIVisuals();
        }

        private void btnNoteNames_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.NoteNamesVisualized = !Rack.UserSettings.NoteNamesVisualized;
                Rack.MappingModule.NetytarSurface.NoteNamesVisualized = Rack.UserSettings.NoteNamesVisualized;
                UpdateGUIVisuals();
            }
        }

        private void btnRemoveSharps_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                if (Rack.UserSettings.SharpNotesMode == _SharpNotesModes.Off)
                {
                    Rack.UserSettings.SharpNotesMode = _SharpNotesModes.On;
                }
                else if (Rack.UserSettings.SharpNotesMode == _SharpNotesModes.On)
                {
                    Rack.UserSettings.SharpNotesMode = _SharpNotesModes.Off;
                }

                UpdateGUIVisuals();
                Rack.MappingModule.NetytarSurface.DrawButtons();
            }
        }

        private void btnRootNoteMinus_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.RootNote = Rack.UserSettings.RootNote.Previous();
                Rack.MappingModule.NetytarSurface.Scale = new Scale(Rack.UserSettings.RootNote, Rack.UserSettings.ScaleCode);
                UpdateGUIVisuals();
            }
        }

        private void btnRootNotePlus_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.RootNote = Rack.UserSettings.RootNote.Next();
                Rack.MappingModule.NetytarSurface.Scale = new Scale(Rack.UserSettings.RootNote, Rack.UserSettings.ScaleCode);
                UpdateGUIVisuals();
            }
        }

        private void btnScaleMajor_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.ScaleCode = ScaleCodes.maj;
                Rack.MappingModule.NetytarSurface.Scale = new Scale(Rack.UserSettings.RootNote, Rack.UserSettings.ScaleCode);
                UpdateGUIVisuals();
            }
        }

        private void btnScaleMinor_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.ScaleCode = ScaleCodes.min;
                Rack.MappingModule.NetytarSurface.Scale = new Scale(Rack.UserSettings.RootNote, Rack.UserSettings.ScaleCode);
                UpdateGUIVisuals();
            }
        }

        private void BtnScroll_Click(object sender, RoutedEventArgs e)
        {
            Rack.AutoScroller.Enabled = !Rack.AutoScroller.Enabled;
        }

        private void BtnSensorPortMinus_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                SensorPort--;
                UpdateSensorConnection();
            }
        }

        private void BtnSensorPortPlus_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                SensorPort++;
                UpdateSensorConnection();
            }
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            switch (IsSettingsShown)
            {
                case false:
                    IsSettingsShown = true;
                    brdSettings.Visibility = Visibility.Visible;
                    break;

                case true:
                    IsSettingsShown = false;
                    brdSettings.Visibility = Visibility.Hidden;
                    break;
            }

            UpdateGUIVisuals();
        }

        private void btnSharpNotes_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.SharpNotesMode = Rack.UserSettings.SharpNotesMode == _SharpNotesModes.On ? _SharpNotesModes.Off : _SharpNotesModes.On;
                Rack.MappingModule.NetytarSurface.DrawButtons();
                UpdateGUIVisuals();
            }
        }

        private void btnSlidePlay_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                switch (Rack.UserSettings.SlidePlayMode)
                {
                    case _SlidePlayModes.Off:
                        Rack.UserSettings.SlidePlayMode = _SlidePlayModes.On;
                        break;

                    case _SlidePlayModes.On:
                        Rack.UserSettings.SlidePlayMode = _SlidePlayModes.Off;
                        break;
                }

                UpdateGUIVisuals();
            }
        }

        private void btnSlidePlay_Click_1(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.SlidePlayMode = Rack.UserSettings.SlidePlayMode == _SlidePlayModes.On ? _SlidePlayModes.Off : _SlidePlayModes.On;
                UpdateGUIVisuals();
            }
        }

        private void btnSpacingMinus_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                if (Rack.UserSettings.HorizontalSpacer > Rack.HORIZONTALSPACING_MIN)
                {
                    Rack.UserSettings.HorizontalSpacer -= 10;
                    Rack.UserSettings.VerticalSpacer = -(Rack.UserSettings.HorizontalSpacer / 2);
                }

                Rack.MappingModule.NetytarSurface.DrawButtons();
                UpdateGUIVisuals();
            }
        }

        private void btnSpacingPlus_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                if (Rack.UserSettings.HorizontalSpacer < Rack.HORIZONTALSPACING_MAX)
                {
                    Rack.UserSettings.HorizontalSpacer += 10;
                    Rack.UserSettings.VerticalSpacer = -(Rack.UserSettings.HorizontalSpacer / 2);
                }

                Rack.MappingModule.NetytarSurface.DrawButtons();
                UpdateGUIVisuals();
            }
        }

        private void btnTeeth_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.InteractionMethod = InteractionMappings.Teeth;
                ChangeMapping();
                UpdateGUIVisuals();
            }
        }

        private void btnTestClick(object sender, RoutedEventArgs e)
        {
            throw (new NotImplementedException("Test button is not set!"));
        }

        private void btnToggleAutoScroll_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.AutoScroller.Enabled = !Rack.AutoScroller.Enabled;
            }

            UpdateGUIVisuals();
        }

        private void btnToggleEyeTracker_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.GazeToMouse.Enabled = !Rack.GazeToMouse.Enabled;
            }

            UpdateGUIVisuals();
        }

        private void CheckMidiPort()
        {
            if (Rack.MidiModule.IsMidiOk())
            {
                txtMIDIch.Foreground = ActiveBrush;
            }
            else
            {
                txtMIDIch.Foreground = WarningBrush;
            }
        }

        private void StartNetytar()
        {
            // EventHandler for all buttons
            EventManager.RegisterClassHandler(typeof(Button), Button.MouseEnterEvent, new RoutedEventHandler(Global_SettingsButton_MouseEnter));

            Rack.UserSettings = Rack.SavingSystem.LoadSettings();
            netytarSetup = new DefaultSetup(this);
            netytarSetup.Setup();

            // Checks the selected MIDI port is available
            CheckMidiPort();

            brdSettings.Visibility = Visibility.Hidden;

            // LEAVE AT THE END!
            InstrumentStarted = true;
            ChangeMapping();
            UpdateSensorConnection();
            UpdateGUIVisuals();
        }

        private void Test(object sender, RoutedEventArgs e)
        {
            Rack.MappingModule.NetytarSurface.DrawScale();
        }

        // Metodo per posizionare l'overlay sopra il pulsante corrente
        private void PositionViolinOverlay()
        {
            if (Rack.MappingModule.CurrentButton != null)
            {
                overlayManager?.UpdateOverlay(Rack.MappingModule.CurrentButton, Rack.MappingModule);
            }
            else if (overlayManager != null)
            {
                // Se nessun pulsante è selezionato, nasconde l'overlay
                overlayManager.overlayCanvas.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateGUIVisuals()
        {
            // TEXT
            txtMIDIch.Text = "MP" + Rack.UserSettings.MIDIPort.ToString();
            txtSensorPort.Text = "COM" + Rack.UserSettings.SensorPort.ToString();
            txtRootNote.Text = Rack.UserSettings.RootNote.ToString();
            txtSpacing.Text = Rack.UserSettings.HorizontalSpacer.ToString();

            /// INDICATORS
            indHeadBow.Background = Rack.UserSettings.InteractionMethod == InteractionMappings.HeadBow ? ActiveBrush : BlankBrush;
            indBreath.Background = Rack.UserSettings.InteractionMethod == InteractionMappings.Breath ? ActiveBrush : BlankBrush;
            indTeeth.Background = Rack.UserSettings.InteractionMethod == InteractionMappings.Teeth ? ActiveBrush : BlankBrush;
            indHeadYaw.Background = Rack.UserSettings.InteractionMethod == InteractionMappings.HeadYaw ? ActiveBrush : BlankBrush;
            indKeyboard.Background = Rack.UserSettings.InteractionMethod == InteractionMappings.Keyboard ? ActiveBrush : BlankBrush;
            indMouthAperture.Background = Rack.UserSettings.InteractionMethod == InteractionMappings.Mouth ? ActiveBrush : BlankBrush;
            indRootNoteColor.Background = Rack.ColorCode.FromAbsNote(Rack.UserSettings.RootNote);
            indScaleMajor.Background = (Rack.UserSettings.ScaleCode == ScaleCodes.maj) ? ActiveBrush : BlankBrush;
            indScaleMinor.Background = (Rack.UserSettings.ScaleCode == ScaleCodes.min) ? ActiveBrush : BlankBrush;
            indMod.Background = Rack.UserSettings.ModulationControlMode == _ModulationControlModes.On ? ActiveBrush : BlankBrush;
            indBSwitch.Background = Rack.UserSettings.BreathControlMode == _BreathControlModes.Switch ? ActiveBrush : BlankBrush;
            indSharpNotes.Background = Rack.UserSettings.SharpNotesMode == _SharpNotesModes.On ? ActiveBrush : BlankBrush;
            indSlidePlay.Background = Rack.UserSettings.SlidePlayMode == _SlidePlayModes.On ? ActiveBrush : BlankBrush;
            indToggleCursor.Background = Rack.MappingModule.CursorHidden ? ActiveBrush : BlankBrush;
            indToggleAutoScroll.Background = Rack.AutoScroller.Enabled ? ActiveBrush : BlankBrush;
            indToggleEyeTracker.Background = Rack.GazeToMouse.Enabled ? ActiveBrush : BlankBrush;
            indSettings.Background = IsSettingsShown ? ActiveBrush : BlankBrush;
            //indNoteNames.Background = Rack.NetytarDmiBox.NetytarSurface.NoteNamesVisualized ? ActiveBrush : BlankBrush;

            /* MIDI */
            txtMIDIch.Text = "MP" + Rack.MidiModule.OutDevice.ToString();
            CheckMidiPort();

            Update_SensorIntensityVisuals();
        }

        private void UpdateSensorConnection()
        {
            txtSensorPort.Text = "COM" + SensorPort.ToString();

            txtSensorPort.Foreground = Rack.USBreceiverHeadTracker.Connect(SensorPort) ? ActiveBrush : WarningBrush;
        }

        private void UpdateTimedVisuals(object sender, EventArgs e)
        {
            if (InstrumentStarted && sender != null)
            {
                try
                {
                    // Aggiorna eventuale cambio di scala
                    if (!SelectedScale.GetName().Equals(lastScale.GetName()))
                    {
                        lastScale = SelectedScale;
                        Rack.MappingModule.NetytarSurface.Scale = SelectedScale;
                        UpdateGUIVisuals();
                    }

                    txtNoteName.Text = Rack.MappingModule.SelectedNote.ToStandardString();
                    txtPitch.Text = Rack.MappingModule.SelectedNote.ToPitchValue().ToString();
                    txtIsBlowing.Text = Rack.MappingModule.Blow ? "B" : "_";
                    prbBreathSensor.Value = Rack.MappingModule.InputIndicatorValue;

                    // Aggiorna l'overlay grafico sopra il pulsante corrente
                    PositionViolinOverlay();
                }
                catch
                {
                    // Ignora eventuali errori
                }
            }
        }

        #region Global SettingsButtons

        public void Global_NetytarButtonMouseEnter()
        {
            if (InstrumentStarted)
            {
                if (LastSettingsGazedButton != null)
                {
                    // Reset Previous Button
                    LastSettingsGazedButton.Background = LastGazedBrush;
                    LastSettingsGazedButton = null;
                }
            }
        }

        private void Global_SettingsButton_MouseEnter(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                // Verifica se il pulsante è all'interno del NoteCanvas
                Button button = sender as Button;
                if (button != null)
                {
                    // Se il pulsante è all'interno di NoteCanvas, ignora l'effetto hover
                    DependencyObject parent = button;
                    while (parent != null && !(parent is Canvas))
                    {
                        parent = VisualTreeHelper.GetParent(parent);
                    }

                    // Se il genitore è NoteCanvas, esci dal metodo senza applicare l'effetto
                    if (parent is Canvas canvas && canvas.Name == "NoteCanvas")
                    {
                        return;
                    }

                    // Altrimenti applica l'effetto normale per gli altri pulsanti
                    if (LastSettingsGazedButton != null)
                    {
                        // Reset Previous Button
                        LastSettingsGazedButton.Background = LastGazedBrush;
                    }

                    LastSettingsGazedButton = button;
                    LastGazedBrush = LastSettingsGazedButton.Background;
                    LastSettingsGazedButton.Background = GazeButtonColor;
                }
            }
        }

        #endregion Global SettingsButtons

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            StartNetytar();
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            netytarSetup.Dispose();
        }

        private void BtnSensingIntensityMinus_OnClick(object sender, RoutedEventArgs e)
        {
            switch (Rack.UserSettings.InteractionMethod)
            {
                case InteractionMappings.Keyboard:
                    break;

                case InteractionMappings.Breath:
                    Rack.UserSettings.SensorIntensityBreath -= 0.1f;
                    break;

                case InteractionMappings.Teeth:
                    Rack.UserSettings.SensorIntensityTeeth -= 0.1f;
                    break;

                case InteractionMappings.HeadYaw:
                    Rack.UserSettings.SensorIntensityHead -= 0.1f;
                    break;

                case InteractionMappings.Mouth:
                    Rack.UserSettings.SensorIntensityMouth -= 0.1f;
                    break;
            }

            ChangeMapping();
            UpdateGUIVisuals();
        }

        private void BtnSensingIntensityPlus_OnClick(object sender, RoutedEventArgs e)
        {
            switch (Rack.UserSettings.InteractionMethod)
            {
                case InteractionMappings.Keyboard:
                    break;

                case InteractionMappings.Breath:
                    Rack.UserSettings.SensorIntensityBreath += 0.1f;
                    break;

                case InteractionMappings.Teeth:
                    Rack.UserSettings.SensorIntensityTeeth += 0.1f;
                    break;

                case InteractionMappings.HeadYaw:
                    Rack.UserSettings.SensorIntensityHead += 0.1f;
                    break;

                case InteractionMappings.Mouth:
                    Rack.UserSettings.SensorIntensityMouth += 0.1f;
                    break;
            }

            ChangeMapping();
            UpdateGUIVisuals();
        }

        private void Update_SensorIntensityVisuals()
        {
            switch (Rack.UserSettings.InteractionMethod)
            {
                case InteractionMappings.Keyboard:
                    break;

                case InteractionMappings.Breath:
                    txtSensingIntensity.Text = Rack.UserSettings.SensorIntensityBreath.ToString("F1");
                    break;

                case InteractionMappings.Teeth:
                    txtSensingIntensity.Text = Rack.UserSettings.SensorIntensityTeeth.ToString("F1");
                    break;

                case InteractionMappings.HeadYaw:
                    txtSensingIntensity.Text = Rack.UserSettings.SensorIntensityHead.ToString("F1");
                    break;

                case InteractionMappings.Mouth:
                    txtSensingIntensity.Text = Rack.UserSettings.SensorIntensityMouth.ToString("F1");
                    break;

                default:
                    break;
            }
        }

        private void ChangeMapping()
        {
            Rack.NithModuleHeadTracker.SensorBehaviors.Clear();
            Rack.NithModuleWebcam.SensorBehaviors.Clear();
            Rack.NithModulePhone.SensorBehaviors.Clear();
            Rack.NithModuleEyeTracker.SensorBehaviors.RemoveAll(behavior => behavior is NITHbehavior_headViolinBow_yawAndBend);

            switch (Rack.UserSettings.InteractionMethod)
            {
                case InteractionMappings.HeadYaw:
                    //Rack.NithModuleHeadTracker.SensorBehaviors.Add(new NithSensorBehaviorYawPlay(Rack.UserSettings.SensorIntensityHead));
                    break;

                case InteractionMappings.Breath:
                    //Rack.NithModuleSensor.SensorBehaviors.Add(new NithSensorBehaviorPressurePlay(NithParameters.breath_press, Rack.UserSettings.SensorIntensityBreath, Rack.UserSettings.SensorIntensityBreath * 1.5f));
                    break;

                case InteractionMappings.Teeth:
                    //Rack.NithModuleSensor.SensorBehaviors.Add(new NithSensorBehaviorPressurePlay(NithParameters.teeth_press, Rack.UserSettings.SensorIntensityTeeth, Rack.UserSettings.SensorIntensityTeeth * 1.5f));
                    break;

                case InteractionMappings.Keyboard:
                    break;

                case InteractionMappings.Mouth:
                    Rack.NithModuleWebcam.SensorBehaviors.Add(new NithSensorBehaviorMouthAperture(Rack.UserSettings.SensorIntensityMouth));
                    break;

                case InteractionMappings.HeadBow:
                    Rack.NithModuleWebcam.SensorBehaviors.Add(new WriteToConsoleBehavior());
                    // Rack.NithModuleWebcam.SensorBehaviors.Add(new NITHbehavior_headViolinBow_yawAndBend());
                    Rack.NithModuleEyeTracker.SensorBehaviors.Add(new NITHbehavior_headViolinBow_yawAndBend(operationMode: NITHbehavior_headViolinBow_yawAndBend.YawAndBendOperationMode.Modulation));
                    break;
            }
        }

        /// <summary>
        /// Invia un comando di vibrazione di test per verificare la connettività
        /// </summary>
        public void SendTestVibration()
        {
            try
            {
                // Invia una vibrazione di test forte e lunga che sarà facilmente riconoscibile
                string testCommand = "VIB:500:255";

                Console.WriteLine($"Invio vibrazione di test: {testCommand}");

                if (Rack.NithSenderPhone != null && Rack.UDPsenderPhone != null)
                {
                    // Verifica che l'IP sia corretto
                    Console.WriteLine($"Invio a: {Rack.UDPsenderPhone.IpAddress}:{Rack.UDPsenderPhone.Port}");
                    Rack.NithSenderPhone.SendData(testCommand);
                    Console.WriteLine("Comando inviato!");
                }
                else
                {
                    Console.WriteLine("Errore: NithSenderPhone o UDPsenderPhone non inizializzato");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore invio test: {ex.Message}");
            }
        }

        private void BtnMouthAperture_OnClick(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.InteractionMethod = InteractionMappings.Mouth;
                ChangeMapping();
                UpdateGUIVisuals();
            }
        }

        private void CButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
        }

        private void NoteButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Button button = (Button)sender;
            int note = int.Parse(button.Tag.ToString());
            Rack.MappingModule.SelectedNote = (MidiNotes)note;
            Rack.MappingModule.LastGazedButton = button;
            Rack.MappingModule.CurrentButton = button; // Ensure current button is updated
        }

        private void NoteButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //Codice eventualmente commentato per la logica del leave
        }

        private void btnHeadBow_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.InteractionMethod = InteractionMappings.HeadBow;
                ChangeMapping();
                UpdateGUIVisuals();
            }
        }

        private void btnCircle_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                // Rende visibile la scheda Circle (Netytar) e nasconde Violin
                tabCircleLayout.Visibility = Visibility.Visible;
                tabViolinLayout.Visibility = Visibility.Collapsed;
                tabSolo.SelectedItem = tabCircleLayout;

                // Aggiorna l'indicatore visivo
                btnCircle.Foreground = ActiveBrush;
                btnViolin.Foreground = Brushes.White;

                // Reinizializza l'overlayManager con il canvas corretto per il layout Circle
                overlayManager = new ViolinOverlayManager(ViolinOverlayCircle);
            }
        }

        private void btnViolin_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                // Rende visibile la scheda Violin e nasconde Circle (Netytar)
                tabViolinLayout.Visibility = Visibility.Visible;
                tabCircleLayout.Visibility = Visibility.Collapsed;
                tabSolo.SelectedItem = tabViolinLayout;

                // Aggiorna l'indicatore visivo
                btnViolin.Foreground = ActiveBrush;
                btnCircle.Foreground = Brushes.White;

                // Reinizializza l'overlayManager con il canvas corretto per il layout Violin
                overlayManager = new ViolinOverlayManager(ViolinOverlayViolin);
            }
        }

        private void btnSetIP_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted && Rack.UDPsenderPhone != null)
            {
                if (Rack.UDPsenderPhone.SetIpAddress(txtIPAddress.Text))
                {
                    // IP valido
                    txtIPAddress.Background = Brushes.LightGreen;
 
                }
                else
                {
                    // IP non valido
                    txtIPAddress.Background = Brushes.LightPink;
                }

                // Ripristina il colore di sfondo dopo 2 secondi
                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(2);
                timer.Tick += (s, e) =>
                {
                    txtIPAddress.Background = Brushes.White;
                    timer.Stop();
                };
                timer.Start();
            }
        }

        private void btnReconnect_Click(object sender, RoutedEventArgs e)
        {
            //Rack.UDPreceiverEyeTracker.Disconnect();
            Rack.UDPreceiverWebcam.Disconnect();
            //Rack.UDPreceiverPhone.Disconnect();
            ////Rack.USBreceiverHeadTracker.Disconnect();

            //// Aggiornare l'IP anche durante la riconnessione
            //if (Rack.UDPsenderPhone != null)
            //{
            //    Rack.UDPsenderPhone.SetIpAddress(txtIPAddress.Text);
            //}

            Rack.UDPreceiverWebcam.Connect();
            //Rack.UDPreceiverEyeTracker.Connect();
            //Rack.UDPreceiverPhone.Connect();
            //Rack.USBreceiverHeadTracker.Connect(SensorPort);
        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            SendTestVibration();
        }

        private void btnCalibrateHeadTracker_Click(object sender, RoutedEventArgs e)
        {
            Rack.ETheadTrackerCalibrator.SetCenterToCurrentPosition();
        }
    }
}