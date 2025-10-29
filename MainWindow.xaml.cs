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

        private void btnModSourcePitch_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.ModulationControlSource = ModulationControlSources.HeadPitch;
                UpdateGUIVisuals();
            }
        }

        private void btnModSourceMouth_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.ModulationControlSource = ModulationControlSources.MouthAperture;
                UpdateGUIVisuals();
            }
        }

        private void btnModSourceRoll_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.ModulationControlSource = ModulationControlSources.HeadRoll;
                UpdateGUIVisuals();
            }
        }

        private void btnBowPressureSourcePitch_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.BowPressureControlSource = BowPressureControlSources.HeadPitch;
                UpdateGUIVisuals();
            }
        }

        private void btnBowPressureSourceMouth_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.BowPressureControlSource = BowPressureControlSources.MouthAperture;
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

            // Display local IP address
            DisplayLocalIPAddress();

            brdSettings.Visibility = Visibility.Hidden;

            // LEAVE AT THE END!
            InstrumentStarted = true;
            UpdateHeadTrackingSource();
            UpdateSensorConnection();
            UpdateGUIVisuals();
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

            /// INDICATORS
            indHeadBow.Background = Rack.UserSettings.InteractionMapping == InteractionMappings.HeadBow ? ActiveBrush : BlankBrush;
            indEyeTracker.Background = Rack.UserSettings.HeadTrackingSource == HeadTrackingSources.EyeTracker ? ActiveBrush : BlankBrush;
            indWebcam.Background = Rack.UserSettings.HeadTrackingSource == HeadTrackingSources.Webcam ? ActiveBrush : BlankBrush;
            indPhone.Background = Rack.UserSettings.HeadTrackingSource == HeadTrackingSources.Phone ? ActiveBrush : BlankBrush;
            indMod.Background = Rack.UserSettings.ModulationControlMode == _ModulationControlModes.On ? ActiveBrush : BlankBrush;
            indSlidePlay.Background = Rack.UserSettings.SlidePlayMode == _SlidePlayModes.On ? ActiveBrush : BlankBrush;
            indToggleCursor.Background = Rack.MappingModule.CursorHidden ? ActiveBrush : BlankBrush;
            indToggleAutoScroll.Background = Rack.AutoScroller.Enabled ? ActiveBrush : BlankBrush;
            indToggleEyeTracker.Background = Rack.GazeToMouse.Enabled ? ActiveBrush : BlankBrush;
            indSettings.Background = IsSettingsShown ? ActiveBrush : BlankBrush;
            
            // Modulation source indicators
            indModSourcePitch.Background = Rack.UserSettings.ModulationControlSource == ModulationControlSources.HeadPitch ? ActiveBrush : BlankBrush;
            indModSourceMouth.Background = Rack.UserSettings.ModulationControlSource == ModulationControlSources.MouthAperture ? ActiveBrush : BlankBrush;
            indModSourceRoll.Background = Rack.UserSettings.ModulationControlSource == ModulationControlSources.HeadRoll ? ActiveBrush : BlankBrush;
            
            // Bow pressure source indicators
            indBowPressureSourcePitch.Background = Rack.UserSettings.BowPressureControlSource == BowPressureControlSources.HeadPitch ? ActiveBrush : BlankBrush;
            indBowPressureSourceMouth.Background = Rack.UserSettings.BowPressureControlSource == BowPressureControlSources.MouthAperture ? ActiveBrush : BlankBrush;
            
            //indNoteNames.Background = Rack.NetytarDmiBox.NetytarSurface.NoteNamesVisualized ? ActiveBrush : BlankBrush;

            /* MIDI */
            txtMIDIch.Text = "MP" + Rack.MidiModule.OutDevice.ToString();
            CheckMidiPort();

            Update_SensorIntensityVisuals();
        }

        private void Update_SensorIntensityVisuals()
        {
            switch (Rack.UserSettings.InteractionMapping)
            {
                case InteractionMappings.HeadBow:
                    txtSensingIntensity.Text = Rack.UserSettings.SensorIntensityHead.ToString("F0");
                    break;

                default:
                    break;
            }
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
        private void UpdateHeadTrackingSource()
        {
            // Remove HeadBow behavior and WriteToConsoleBehavior from all modules
            if (Rack.NithModuleHeadTracker.SensorBehaviors.Contains(Rack.Behavior_HeadBow))
            {
                Rack.NithModuleHeadTracker.SensorBehaviors.Remove(Rack.Behavior_HeadBow);
            }
            if (Rack.NithModuleWebcam.SensorBehaviors.Contains(Rack.Behavior_HeadBow))
            {
                Rack.NithModuleWebcam.SensorBehaviors.Remove(Rack.Behavior_HeadBow);
            }
            if (Rack.NithModulePhone.SensorBehaviors.Contains(Rack.Behavior_HeadBow))
            {
                Rack.NithModulePhone.SensorBehaviors.Remove(Rack.Behavior_HeadBow);
            }
            if (Rack.NithModuleEyeTracker.SensorBehaviors.Contains(Rack.Behavior_HeadBow))
            {
                Rack.NithModuleEyeTracker.SensorBehaviors.Remove(Rack.Behavior_HeadBow);
            }

            // Remove WriteToConsoleBehavior from all modules
            var writeToConsoleBehaviors = Rack.NithModuleWebcam.SensorBehaviors.OfType<WriteToConsoleBehavior>().ToList();
            foreach (var behavior in writeToConsoleBehaviors)
            {
                Rack.NithModuleWebcam.SensorBehaviors.Remove(behavior);
            }

            writeToConsoleBehaviors = Rack.NithModulePhone.SensorBehaviors.OfType<WriteToConsoleBehavior>().ToList();
            foreach (var behavior in writeToConsoleBehaviors)
            {
                Rack.NithModulePhone.SensorBehaviors.Remove(behavior);
            }

            writeToConsoleBehaviors = Rack.NithModuleEyeTracker.SensorBehaviors.OfType<WriteToConsoleBehavior>().ToList();
            foreach (var behavior in writeToConsoleBehaviors)
            {
                Rack.NithModuleEyeTracker.SensorBehaviors.Remove(behavior);
            }

            switch (Rack.UserSettings.HeadTrackingSource)
            {
                case HeadTrackingSources.Webcam:
                    Rack.Behavior_HeadBow.Sensitivity = Rack.UserSettings.SensorIntensityHead;
                    Rack.NithModuleWebcam.SensorBehaviors.Add(new WriteToConsoleBehavior());
                    Rack.NithModuleWebcam.SensorBehaviors.Add(Rack.Behavior_HeadBow);
                    break;
                case HeadTrackingSources.EyeTracker:
                    Rack.Behavior_HeadBow.Sensitivity = Rack.UserSettings.SensorIntensityHead;
                    Rack.NithModuleEyeTracker.SensorBehaviors.Add(new WriteToConsoleBehavior());
                    Rack.NithModuleEyeTracker.SensorBehaviors.Add(Rack.Behavior_HeadBow);
                    break;
                case HeadTrackingSources.Phone:
                    Rack.Behavior_HeadBow.Sensitivity = Rack.UserSettings.SensorIntensityHead;
                    Rack.NithModulePhone.SensorBehaviors.Add(new WriteToConsoleBehavior());
                    Rack.NithModulePhone.SensorBehaviors.Add(Rack.Behavior_HeadBow);
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
                UpdateGUIVisuals();
            }
        }

        private void btnEyeTracker_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.HeadTrackingSource = HeadTrackingSources.EyeTracker;
                UpdateHeadTrackingSource();
                UpdateGUIVisuals();
            }
        }

        private void btnWebcam_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.HeadTrackingSource = HeadTrackingSources.Webcam;
                UpdateHeadTrackingSource();
                UpdateGUIVisuals();
            }
        }

        private void btnPhone_Click(object sender, RoutedEventArgs e)
        {
            if (InstrumentStarted)
            {
                Rack.UserSettings.HeadTrackingSource = HeadTrackingSources.Phone;
                UpdateHeadTrackingSource();
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

        private void btnCalibrateHeadTracker_Click(object sender, RoutedEventArgs e)
        {
            Rack.EyeTrackerHeadTrackerCalibrator.SetCenterToCurrentPosition();
            Rack.WebcamHeadTrackerCalibrator.SetCenterToCurrentPosition();

            // Also calibrate phone head tracker if available
            if (Rack.PhoneHeadTrackerCalibrator != null)
            {
                Rack.PhoneHeadTrackerCalibrator.SetCenterToCurrentPosition();
            }
        }

        private void btnReconnect_Click(object sender, RoutedEventArgs e)
        {
            Rack.UDPreceiverEyeTracker.Disconnect();
            Rack.UDPreceiverWebcam.Disconnect();
            Rack.UDPreceiverPhone.Disconnect();
            ////Rack.USBreceiverHeadTracker.Disconnect();

            //// Aggiornare l'IP anche durante la riconnessione
            //if (Rack.UDPsenderPhone != null)
            //{
            //    Rack.UDPsenderPhone.SetIpAddress(txtIPAddress.Text);
            //}

            Rack.UDPreceiverWebcam.Connect();
            Rack.UDPreceiverEyeTracker.Connect();
            Rack.UDPreceiverPhone.Connect();
            //Rack.USBreceiverHeadTracker.Connect(SensorPort);
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
                    Rack.UserSettings.SensorIntensityHead -= 1f;
                    break;
            }

            UpdateHeadTrackingSource();
            UpdateGUIVisuals();
        }

        private void BtnSensingIntensityPlus_OnClick(object sender, RoutedEventArgs e)
        {
            switch (Rack.UserSettings.InteractionMapping)
            {
                case InteractionMappings.HeadBow:
                    Rack.UserSettings.SensorIntensityHead += 1f;
                    break;
            }

            UpdateHeadTrackingSource();
            UpdateGUIVisuals();
        }

        /// <summary>
        /// Update the phone IP textbox from other threads via dispatcher.
        /// Called by RenderingModule.NotifyPhoneIpChanged.
        /// </summary>
        /// <param name="ip">New IP address to display.</param>
        public void UpdatePhoneIpText(string ip)
        {
            try
            {
                // Ensure this runs on UI thread
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.BeginInvoke(() => UpdatePhoneIpText(ip));
                    return;
                }

                if (txtIPAddress != null)
                {
                    txtIPAddress.Text = ip;
                }
            }
            catch
            {
                // Silent failure
            }
        }
    }
}