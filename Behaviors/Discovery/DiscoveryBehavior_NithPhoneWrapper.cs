using NITHlibrary.Nith.Wrappers;
using NITHlibrary.Tools.Ports;
using NITHlibrary.Tools.Ports.Discovery;

namespace HeadBower.Behaviors.Discovery
{
    /// <summary>
    /// Behavior that automatically configures phone connection when NITHphoneWrapper is discovered.
    /// Updates UDPsender and UDPreceiver to match the discovered device.
    /// </summary>
    public class DiscoveryBehavior_NithPhoneWrapper : IDeviceDiscoveryBehavior
    {
        private readonly List<IDisposable> _disposables;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryBehavior_NithPhoneWrapper"/> class.
        /// </summary>
        /// <param name="disposables">The disposables list to manage receiver/sender lifecycle.</param>
        public DiscoveryBehavior_NithPhoneWrapper(List<IDisposable> disposables)
        {
            _disposables = disposables;
        }

        /// <summary>
        /// Called when a NITH device is discovered on the network.
        /// </summary>
        public void OnDeviceDiscovered(DeviceInfo device)
        {
            // Only handle NITHphoneWrapper devices
            if (device.DeviceType != "NITHphoneWrapper")
            {
                return;
            }

            Console.WriteLine($"[Phone Discovery] Configuring phone connection: {device.DeviceIP}:{device.DevicePort}");

            // Update or recreate UDPsender
            UpdatePhoneSender(device);

            // Update or recreate UDPreceiver if port changed
            UpdatePhoneReceiver(device);

            Console.WriteLine($"[Phone Discovery] Phone auto-configured successfully");
        }

        /// <summary>
        /// Updates the UDPsender to send to the discovered phone.
        /// </summary>
        private void UpdatePhoneSender(DeviceInfo device)
        {
            var oldSender = Modules.Rack.UDPsenderPhone;

            // Remove old sender from NithSender listeners BEFORE creating new one
            if (oldSender != null && Modules.Rack.NithSenderPhone != null)
            {
                // Remove all occurrences to avoid duplicates
                while (Modules.Rack.NithSenderPhone.PortListeners.Remove(oldSender))
                {
                    // Keep removing until no more instances
                }
                Console.WriteLine($"[Phone Discovery] Removed old sender from NithSenderPhone listeners");
            }

            // Create new sender with discovered IP and port
            var newSender = new UDPsender(device.DevicePort, device.DeviceIP);
            Modules.Rack.UDPsenderPhone = newSender;

            // Add to NithSender listeners ONLY if NithSenderPhone exists
            if (Modules.Rack.NithSenderPhone != null)
            {
                Modules.Rack.NithSenderPhone.PortListeners.Add(newSender);
                Console.WriteLine($"[Phone Discovery] Added new sender to NithSenderPhone listeners");
            }
            else
            {
                Console.WriteLine($"[Phone Discovery] WARNING: NithSenderPhone is null, cannot add new sender to listeners");
            }

            // Remove old sender from disposables and dispose
            if (oldSender != null)
            {
                _disposables.Remove(oldSender);
                oldSender.Dispose();
                Console.WriteLine($"[Phone Discovery] Disposed old sender");
            }

            // Add new sender to disposables
            _disposables.Add(newSender);

            // Notify rendering module / UI about the IP change so the textbox updates
            try
            {
                var ipToReport = newSender.IpAddress ?? device.DeviceIP;
                if (Modules.Rack.RenderingModule != null)
                {
                    Modules.Rack.RenderingModule.NotifyPhoneIpChanged(ipToReport);
                }
            }
            catch
            {
                // Silent failure
            }

            Console.WriteLine($"[Phone Discovery] Sender updated: {device.DeviceIP}:{device.DevicePort}");
        }

        /// <summary>
        /// Updates the UDPreceiver if the expected port is different from default.
        /// </summary>
        private void UpdatePhoneReceiver(DeviceInfo device)
        {
            int defaultPort = (int)NithWrappersReceiverPorts.NITHphoneWrapper;

            // Only recreate receiver if port is different from default
            if (device.ExpectedReceiverPort == defaultPort)
            {
                Console.WriteLine($"[Phone Discovery] Receiver port unchanged ({defaultPort})");
                return;
            }

            Console.WriteLine($"[Phone Discovery] Receiver port changed: {defaultPort} -> {device.ExpectedReceiverPort}");

            var oldReceiver = Modules.Rack.UDPreceiverPhone;

            // Disconnect old receiver
            if (oldReceiver != null)
            {
                oldReceiver.Disconnect();
                _disposables.Remove(oldReceiver);
                oldReceiver.Dispose();
            }

            // Create new receiver with new port
            var newReceiver = new UDPreceiver(device.ExpectedReceiverPort);
            newReceiver.MaxSamplesPerSecond = 60; // Same settings as in DefaultSetup

            // Re-add NithModule listener
            if (Modules.Rack.NithModuleUnified != null)
            {
                newReceiver.Listeners.Add(Modules.Rack.NithModuleUnified);
            }

            // Connect and update Rack
            newReceiver.Connect();
            Modules.Rack.UDPreceiverPhone = newReceiver;

            // Add to disposables
            _disposables.Add(newReceiver);

            Console.WriteLine($"[Phone Discovery] Receiver updated and connected on port {device.ExpectedReceiverPort}");
        }
    }
}
