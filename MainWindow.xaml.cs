// MainWindow.xaml.cs
using HeadBower.Behaviors;
using HeadBower.Behaviors.HeadBow;
using HeadBower.Modules;
using HeadBower.Visuals; // add reference
using NITHdmis.Music;
using NITHlibrary.Tools.Logging;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

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
        private double velocityBarMaxHeight = 0;

        public MainWindow()
        {
            InitializeComponent();

            // Debugger
            TraceAdder.AddTrace();
            DataContext = this;
        }

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
                // Rendering loop handles UI update
            }
        }

        private void btnCalibrateHeadPose_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //btnCalibrateHeadPose.Background = new SolidColorBrush(Colors.Black);
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

        private void BtnMIDIchMinus_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.MIDIPort--;
                Rack.MidiModule.OutDevice = Rack.UserSettings.MIDIPort;
                // Rendering loop handles UI update
            }
        }

        private void BtnMIDIchPlus_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.MIDIPort++;
                Rack.MidiModule.OutDevice = Rack.UserSettings.MIDIPort;
                // Rendering loop handles UI update
            }
        }

        private void btnMod_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.ModulationControlMode = Rack.UserSettings.ModulationControlMode == _ModulationControlModes.On ? _ModulationControlModes.Off : _ModulationControlModes.On;
                // Rendering loop handles UI update
            }
        }

        private void btnBowPress_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.BowPressureControlMode = Rack.UserSettings.BowPressureControlMode == _BowPressureControlModes.On ? _BowPressureControlModes.Off : _BowPressureControlModes.On;
                // Rendering loop handles UI update
            }
        }

        private void btnModSourcePitch_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.ModulationControlSource = ModulationControlSources.HeadPitch;
                // Rendering loop handles UI update
            }
        }

        private void btnModSourceMouth_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.ModulationControlSource = ModulationControlSources.MouthAperture;
                // Rendering loop handles UI update
            }
        }

        // Removed: btnModSourceRoll_Click - Roll modulation source has been removed

        private void btnBowPressureSourcePitch_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.BowPressureControlSource = BowPressureControlSources.HeadPitch;
                // Rendering loop handles UI update
            }
        }

        private void btnBowPressureSourceMouth_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.BowPressureControlSource = BowPressureControlSources.MouthAperture;
                // Rendering loop handles UI update
            }
        }

        private void btnModulationControlSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.ModulationControlMode = Rack.UserSettings.ModulationControlMode == _ModulationControlModes.On ? _ModulationControlModes.Off : _ModulationControlModes.On;
                // Rendering loop handles UI update
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
                // Rendering loop handles UI update
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
            // Rendering loop handles UI update
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
                // Rendering loop handles UI update
            }
        }

        private void btnSlidePlay_Click_1(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.SlidePlayMode = Rack.UserSettings.SlidePlayMode == _SlidePlayModes.On ? _SlidePlayModes.Off : _SlidePlayModes.On;
                // Rendering loop handles UI update
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
                // Rendering loop handles UI update
            }
        }

        private void btnToggleEyeTracker_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.GazeToMouse.Enabled = !Rack.GazeToMouse.Enabled;
                // Rendering loop handles UI update
            }
        }

        private void StartNetytar()
        {
            // EventHandler for all buttons
            EventManager.RegisterClassHandler(typeof(Button), Button.MouseEnterEvent, new RoutedEventHandler(Global_SettingsButton_MouseEnter));

            Rack.UserSettings = Rack.SavingSystem.LoadSettings();
            netytarSetup = new DefaultSetup(this);
            netytarSetup.Setup();

            // Display local IP address
            DisplayLocalIPAddress();

            brdSettings.Visibility = Visibility.Hidden;

            // LEAVE AT THE END!
            InstrumentStarted = true;
            UpdateHeadTrackingSource();
            UpdateSensorConnection();
            // No need for initial UpdateGUIVisuals - rendering loop handles it
        }

        private void DisplayLocalIPAddress()
        {
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    string localIP = endPoint?.Address.ToString() ?? "0.0.0.0";
                    txtLocalIP.Text = localIP;
                }
            }
            catch
            {
                txtLocalIP.Text = "Unable to retrieve IP";
            }
        }

        private void Test(object sender, RoutedEventArgs e)
        {

        }

        private void UpdateSensorConnection()
        {
            txtSensorPort.Text = "COM" + SensorPort.ToString();

            txtSensorPort.Foreground = Rack.USBreceiverHeadTracker.Connect(SensorPort) ? ActiveBrush : WarningBrush;
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
        private void UpdateHeadTrackingSource()
        {
            // Call the selector in MappingModule to configure parameter selection
            MappingModule.SelectHeadTrackingSource(Rack.UserSettings.HeadTrackingSource);
            
            // Update sensitivity for musical behaviors that use it
            Rack.Behavior_BowMotion.Sensitivity = Rack.UserSettings.SensorIntensityHead;
            Rack.Behavior_HapticFeedback.Sensitivity = Rack.UserSettings.SensorIntensityHead;
            
            // Note: VisualFeedbackBehavior reads sensitivity automatically from UserSettings based on active source
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

                if (Rack.NithSenderPhone != null && Rack.UDPsenderPhone != null)
                {
                    Rack.NithSenderPhone.SendData(testCommand);
                }
            }
            catch
            {
                // Silent failure
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
                Rack.UserSettings.InteractionMapping = InteractionMappings.HeadBow;
                UpdateHeadTrackingSource();
                // Rendering loop handles UI update
            }
        }

        private void btnEyeTracker_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.HeadTrackingSource = HeadTrackingSources.EyeTracker;
                UpdateHeadTrackingSource();
                // Rendering loop handles UI update
            }
        }

        private void btnWebcam_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.HeadTrackingSource = HeadTrackingSources.Webcam;
                UpdateHeadTrackingSource();
                // Rendering loop handles UI update
            }
        }

        private void btnPhone_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.HeadTrackingSource = HeadTrackingSources.Phone;
                UpdateHeadTrackingSource();
                // Rendering loop handles UI update
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

                // Tell rendering module to use Circle overlay canvas
                Rack.RenderingModule.SetViolinOverlayCanvas(ViolinOverlayCircle);
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

                // Tell rendering module to use Violin overlay canvas
                Rack.RenderingModule.SetViolinOverlayCanvas(ViolinOverlayViolin);
            }
        }

        private void btnCalibrateHeadTracker_Click(object sender, RoutedEventArgs e)
        {
            Rack.UnifiedHeadTrackerCalibrator.SetCenterToCurrentPosition();
        }

        private void btnReconnect_Click(object sender, RoutedEventArgs e)
        {
            Rack.UDPreceiverEyeTracker.Disconnect();
            Rack.UDPreceiverWebcam.Disconnect();
            Rack.UDPreceiverPhone.Disconnect();

            Rack.UDPreceiverWebcam.Connect();
            Rack.UDPreceiverEyeTracker.Connect();
            Rack.UDPreceiverPhone.Connect();
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

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            SendTestVibration();
        }

        private void BtnSensingIntensityMinus_OnClick(object sender, RoutedEventArgs e)
        {
            switch (Rack.UserSettings.InteractionMapping)
            {
                case InteractionMappings.HeadBow:
                    Rack.UserSettings.SensorIntensityHead -= 0.1f;
                    break;
            }

            UpdateHeadTrackingSource();
            // Rendering loop handles UI update
        }

        private void BtnSensingIntensityPlus_OnClick(object sender, RoutedEventArgs e)
        {
            switch (Rack.UserSettings.InteractionMapping)
            {
                case InteractionMappings.HeadBow:
                    Rack.UserSettings.SensorIntensityHead += 0.1f;
                    break;
            }

            UpdateHeadTrackingSource();
            // Rendering loop handles UI update
        }

        private void BtnPitchSensitivityMinus_OnClick(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                // Modify pitch sensitivity for the currently selected head tracking source
                switch (Rack.UserSettings.HeadTrackingSource)
                {
                    case HeadTrackingSources.Webcam:
                        Rack.UserSettings.WebcamPitchSensitivity -= 0.1f;
                        break;
                    case HeadTrackingSources.Phone:
                        Rack.UserSettings.PhonePitchSensitivity -= 0.1f;
                        break;
                    case HeadTrackingSources.EyeTracker:
                        Rack.UserSettings.EyeTrackerPitchSensitivity -= 0.1f;
                        break;
                }
                // Settings auto-save and PropertyChanged triggers SourceNormalizer update
                // Rendering loop handles UI update
            }
        }

        private void BtnPitchSensitivityPlus_OnClick(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                // Modify pitch sensitivity for the currently selected head tracking source
                switch (Rack.UserSettings.HeadTrackingSource)
                {
                    case HeadTrackingSources.Webcam:
                        Rack.UserSettings.WebcamPitchSensitivity += 0.1f;
                        break;
                    case HeadTrackingSources.Phone:
                        Rack.UserSettings.PhonePitchSensitivity += 0.1f;
                        break;
                    case HeadTrackingSources.EyeTracker:
                        Rack.UserSettings.EyeTrackerPitchSensitivity += 0.1f;
                        break;
                }
                // Settings auto-save and PropertyChanged triggers SourceNormalizer update
                // Rendering loop handles UI update
            }
        }
    }
}