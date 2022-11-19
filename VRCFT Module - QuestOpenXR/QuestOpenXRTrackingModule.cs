using System;
using System.Threading;
using System.Runtime.InteropServices;

using System.Diagnostics;

using VRCFaceTracking;
using VRCFaceTracking.Params;
using VRCFaceTracking.Params.Lip;

namespace VRCFT_Module___QuestOpenXR
{
    public class QuestProTrackingModule : ExtTrackingModule
    {
        [DllImport("QuestFaceTrackingOpenXR.dll")]
        static extern int InitOpenXRRuntime();

        [DllImport("QuestFaceTrackingOpenXR.dll")]
        static extern int UpdateOpenXRFaceTracker();

        [DllImport("QuestFaceTrackingOpenXR.dll")]
        static extern float GetCheekPuff(int cheekIndex);

        // Synchronous module initialization. Take as much time as you need to initialize any external modules. This runs in the init-thread
        public override (bool SupportsEye, bool SupportsLip) Supported => (true, true);

        public override (bool eyeSuccess, bool lipSuccess) Initialize(bool eye, bool lip)
        {
            bool eyeSupported = true;
            bool faceSupported = true;
            Logger.Msg("[QuestOpenXR] Loading OpenXR Loader...");
            int RuntimeInitResult = InitOpenXRRuntime();
            if (RuntimeInitResult == 0)
            {
                Logger.Msg("[QuestOpenXR] OpenXR Runtime init success.");
            }
            else if (RuntimeInitResult == 2)
            {
                Logger.Msg("[QuestOpenXR] Failed to create XrInstance.");
            }
            else if (RuntimeInitResult == 3)
            {
                Logger.Msg("[QuestOpenXR] Failed to get XrSystemID.");
            }
            else if (RuntimeInitResult == 4)
            {
                Logger.Msg("[QuestOpenXR] Failed to get XrViewConfigurationType.");
            }
            else if (RuntimeInitResult == 5)
            {
                Logger.Msg("[QuestOpenXR] Failed to GetD3D11GraphicsRequirements.");
            }
            else if (RuntimeInitResult == 6)
            {
                Logger.Msg("[QuestOpenXR] Failed to create session.");
            }
            else if (RuntimeInitResult == 7)
            {
                Logger.Msg("[QuestOpenXR] Failed to create XrSpace.");
            }
            else if (RuntimeInitResult == 8)
            {
                Logger.Msg("[QuestOpenXR] Failed to begin session.");
            }
            else if (RuntimeInitResult == 9)
            {
                Logger.Msg("[QuestOpenXR] Failed to create Face Treacker.");
            }
            else if (RuntimeInitResult == 10)
            {
                Logger.Msg("[QuestOpenXR] Failed to create Eye Tracker.");
            }
            else if (RuntimeInitResult == -1)
            {
                eyeSupported = false;
                Logger.Msg("[QuestOpenXR] Eye Tracking not supported.");
            }
            else if (RuntimeInitResult == -2)
            {
                faceSupported = false;
                Logger.Msg("[QuestOpenXR] Facial Tracking not supported.");
            }
            else
            {
                Logger.Msg("[QuestOpenXR] Failed to load Core OpenXR functions.");
            }

            return (true, true);
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
            int updateResult = UpdateOpenXRFaceTracker();
            if (updateResult == 1)
            {
                Logger.Msg("[QuestOpenXR] Failed to xrWaitFrame.");
            }
            else if (updateResult == 2)
            {
                Logger.Msg("[QuestOpenXR] Failed to GetFaceExpressionWeightsFB.");
            }
            else if (updateResult == 2)
            {
                Logger.Msg("[QuestOpenXR] Failed to GetEyeGazesFB.");
            }

            float timeFactor = (float)Math.Sin(sw.Elapsed.TotalSeconds * 4.0f) * 0.5f;

            if (Status.EyeState == ModuleState.Active)
                Console.WriteLine("Eye data is being utilized.");
            if (Status.LipState == ModuleState.Active)
                Console.WriteLine("Lip data is being utilized.");

            UnifiedTrackingData.LatestEyeData.Left.Openness = 0.5f + timeFactor;
            UnifiedTrackingData.LatestEyeData.Right.Openness = 0.5f - timeFactor;

            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.CheekPuffLeft] = Math.Max(0, Math.Min(1, GetCheekPuff(0)));
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.CheekPuffRight] = Math.Max(0, Math.Min(1, GetCheekPuff(1)));
        }

        // A chance to de-initialize everything. This runs synchronously inside main game thread. Do not touch any Unity objects here.
        public override void Teardown()
        {
            Logger.Msg("[QuestOpenXR] Teardown...");
        }

        Stopwatch sw = Stopwatch.StartNew();
    }
}
