using NITHlibrary.Nith.Internals;

namespace HeadBower.Behaviors
{
    internal class WriteToConsoleBehavior : INithSensorBehavior
    {
        private readonly List<NithParameters> requiredParams = new List<NithParameters>
        {
            NithParameters.head_pos_pitch,
            NithParameters.head_pos_yaw,
            NithParameters.head_pos_roll
        };

        public void HandleData(NithSensorData nithData)
        {
            if (nithData.ContainsParameters(requiredParams))
            {
                // Get the values of the parameters
                double head_pos_yaw = nithData.GetParameterValue(NithParameters.head_pos_yaw).Value.ValueAsDouble;
                double head_pos_pitch = nithData.GetParameterValue(NithParameters.head_pos_pitch).Value.ValueAsDouble;
                double head_pos_roll = nithData.GetParameterValue(NithParameters.head_pos_roll).Value.ValueAsDouble;
                
                // Check for velocity values
                double head_vel_yaw = 0;
                if (nithData.ContainsParameter(NithParameters.head_vel_yaw))
                {
                    head_vel_yaw = nithData.GetParameterValue(NithParameters.head_vel_yaw).Value.ValueAsDouble;
                }
                
                // Print the values to the console
                Console.WriteLine($"HEAD POS - Yaw: {head_pos_yaw:F2}°, Pitch: {head_pos_pitch:F2}°, Roll: {head_pos_roll:F2}° | VEL Yaw: {head_vel_yaw:F4}");
            }
        }
    }
}