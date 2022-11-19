using System;
using System.Threading;

using System.Diagnostics;

using VRCFaceTracking;
using VRCFaceTracking.Params;

namespace VRCFT_Module___QuestOpenXR
{
    public class QuestProTrackingModule : ExtTrackingModule
    {
        // Synchronous module initialization. Take as much time as you need to initialize any external modules. This runs in the init-thread
        public override (bool SupportsEye, bool SupportsLip) Supported => (true, true);

        public override (bool eyeSuccess, bool lipSuccess) Initialize(bool eye, bool lip)
        {
            Logger.Msg("[QuestOpenXR] Loading OpenXR Loader...");
            return (true, false);
        }

        // This will be run in the tracking thread. This is exposed so you can control when and if the tracking data is updated down to the lowest level.
        public override Action GetUpdateThreadFunc()
        {
            return () =>
            {
                while (true)
                {
                    Update();
                    Thread.Sleep(10);
                }
            };
        }

        // The update function needs to be defined separately in case the user is running with the --vrcft-nothread launch parameter
        public void Update()
        {
            float timeFactor = (float)Math.Sin(sw.Elapsed.TotalSeconds * 4.0f) * 0.5f;

            if (Status.EyeState == ModuleState.Active)
                Console.WriteLine("Eye data is being utilized.");
            if (Status.LipState == ModuleState.Active)
                Console.WriteLine("Lip data is being utilized.");

            UnifiedTrackingData.LatestEyeData.Left.Openness = 0.5f + timeFactor;
            UnifiedTrackingData.LatestEyeData.Right.Openness = 0.5f - timeFactor;
        }

        // A chance to de-initialize everything. This runs synchronously inside main game thread. Do not touch any Unity objects here.
        public override void Teardown()
        {
            Logger.Msg("[QuestOpenXR] Teardown...");
        }

        Stopwatch sw = Stopwatch.StartNew();
    }
}
