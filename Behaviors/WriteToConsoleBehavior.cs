using NITHlibrary.Nith.Internals;

namespace HeadBower.Behaviors
{
    internal class WriteToConsoleBehavior : INithSensorBehavior
    {
        private readonly List<NithParameters> requiredParams = new List<NithParameters>
        {
            NithParameters.head_pos_pitch,
            NithParameters.head_acc_yaw
        };

        public void HandleData(NithSensorData nithData)
        {
            if (nithData.ContainsParameters(requiredParams))
            {
                // Get the values of the parameters
                double head_pos_pitch = nithData.GetParameterValue(NithParameters.head_pos_pitch).Value.ValueAsDouble;
                double head_acc_yaw = nithData.GetParameterValue(NithParameters.head_acc_yaw).Value.ValueAsDouble;
                // Print the values to the console
                Console.WriteLine($"Pitch: {head_pos_pitch:F2}, Yaw: {head_acc_yaw:F2}");
            }
        }
    }
}