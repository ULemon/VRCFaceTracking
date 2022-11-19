using System;
using System.Threading;
using System.Runtime.InteropServices;

using System.Diagnostics;

using VRCFaceTracking;
using VRCFaceTracking.Params;
using VRCFaceTracking.Params.Lip;

namespace VRCFT_Module___QuestOpenXR
{
    public enum FBExpression
    {
        Brow_Lowerer_L = 0,
        Brow_Lowerer_R = 1,
        Cheek_Puff_L = 2,
        Cheek_Puff_R = 3,
        Cheek_Raiser_L = 4,
        Cheek_Raiser_R = 5,
        Cheek_Suck_L = 6,
        Cheek_Suck_R = 7,
        Chin_Raiser_B = 8,
        Chin_Raiser_T = 9,
        Dimpler_L = 10,
        Dimpler_R = 11,
        Eyes_Closed_L = 12,
        Eyes_Closed_R = 13,
        Eyes_Look_Down_L = 14,
        Eyes_Look_Down_R = 15,
        Eyes_Look_Left_L = 16,
        Eyes_Look_Left_R = 17,
        Eyes_Look_Right_L = 18,
        Eyes_Look_Right_R = 19,
        Eyes_Look_Up_L = 20,
        Eyes_Look_Up_R = 21,
        Inner_Brow_Raiser_L = 22,
        Inner_Brow_Raiser_R = 23,
        Jaw_Drop = 24,
        Jaw_Sideways_Left = 25,
        Jaw_Sideways_Right = 26,
        Jaw_Thrust = 27,
        Lid_Tightener_L = 28,
        Lid_Tightener_R = 29,
        Lip_Corner_Depressor_L = 30,
        Lip_Corner_Depressor_R = 31,
        Lip_Corner_Puller_L = 32,
        Lip_Corner_Puller_R = 33,
        Lip_Funneler_LB = 34,
        Lip_Funneler_LT = 35,
        Lip_Funneler_RB = 36,
        Lip_Funneler_RT = 37,
        Lip_Pressor_L = 38,
        Lip_Pressor_R = 39,
        Lip_Pucker_L = 40,
        Lip_Pucker_R = 41,
        Lip_Stretcher_L = 42,
        Lip_Stretcher_R = 43,
        Lip_Suck_LB = 44,
        Lip_Suck_LT = 45,
        Lip_Suck_RB = 46,
        Lip_Suck_RT = 47,
        Lip_Tightener_L = 48,
        Lip_Tightener_R = 49,
        Lips_Toward = 50,
        Lower_Lip_Depressor_L = 51,
        Lower_Lip_Depressor_R = 52,
        Mouth_Left = 53,
        Mouth_Right = 54,
        Nose_Wrinkler_L = 55,
        Nose_Wrinkler_R = 56,
        Outer_Brow_Raiser_L = 57,
        Outer_Brow_Raiser_R = 58,
        Upper_Lid_Raiser_L = 59,
        Upper_Lid_Raiser_R = 60,
        Upper_Lip_Raiser_L = 61,
        Upper_Lip_Raiser_R = 62,
        Max = 63
    }

    public class TrackingSensitivity
    {
        // Tracking Sensitivity Multipliers
        public static float EyeLid = 1.1f;
        public static float EyeSquint = 1.0f;
        public static float EyeWiden = 1.0f;
        public static float BrowInnerUp = 1.0f;
        public static float BrowOuterUp = 1.0f;
        public static float BrowDown = 1.0f;
        public static float CheekPuff = 1.4f;
        public static float CheekSuck = 2.72f;
        public static float CheekRaiser = 1.1f;
        public static float JawOpen = 1.1f;
        public static float MouthApeShape = 2.0f;
        public static float JawX = 1.0f;
        public static float JawForward = 1.0f;
        public static float LipPucker = 1.21f;
        public static float MouthX = 1.0f;
        public static float MouthSmile = 1.22f;
        public static float MouthFrown = 1.1f;
        public static float LipFunnelTop = 1.13f;
        public static float LipFunnelBottom = 8.0f;  //VERY NOT SENSITIVE
        public static float LipSuckTop = 1.0f;
        public static float LipSuckBottom = 1.0f;
        public static float ChinRaiserTop = 0.75f;
        public static float ChinRaiserBottom = 0.7f;
        public static float MouthLowerDown = 2.87f;
        public static float MouthUpperUp = 1.75f;
        public static float MouthDimpler = 4.3f;
        public static float MouthStretch = 3.0f;
        public static float MouthPress = 10f;        //VERY NOT SENSITIVE
        public static float MouthTightener = 2.13f;
        public static float NoseSneer = 3.16f;
    }

    public class QuestProTrackingModule : ExtTrackingModule
    {
        [StructLayout(LayoutKind.Sequential)]
        class XrQuat
        {
            public float x;
            public float y;
            public float z;
            public float w;
        }

        [DllImport("QuestFaceTrackingOpenXR.dll")]
        static extern int InitOpenXRRuntime();

        [DllImport("QuestFaceTrackingOpenXR.dll")]
        static extern int UpdateOpenXRFaceTracker();

        [DllImport("QuestFaceTrackingOpenXR.dll")]
        static extern float GetCheekPuff(int cheekIndex);

        [DllImport("QuestFaceTrackingOpenXR.dll")]
        static extern void GetEyeOrientation(int eyeIndex, XrQuat outOrientation);

        [DllImport("QuestFaceTrackingOpenXR.dll")]
        static extern void GetFaceWeights([MarshalAs(UnmanagedType.LPArray, SizeConst = 63)] float[] faceExpressionFB);


        private const int expressionsSize = 63;
        private byte[] rawExpressions = new byte[expressionsSize * 4 + (8 * 2 * 4)];
        private float[] expressions = new float[expressionsSize + (8 * 2)];
        float[] FaceExpressionFB = new float[expressionsSize];
        bool bIsInited = false;

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
            bIsInited = true;

            return (true, true);
        }

        // This will be run in the tracking thread. This is exposed so you can control when and if the tracking data is updated down to the lowest level.
        public override Action GetUpdateThreadFunc()
        {
            return () =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    if (!bIsInited) 
                    {
                        continue;
                    }
                    Update();
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
            else if (updateResult == 3)
            {
                Logger.Msg("[QuestOpenXR] Failed to GetEyeGazesFB.");
            }
            else if (updateResult == 4)
            {
                Logger.Msg("[QuestOpenXR] Failed to BeginFrame.");
            }
            else if (updateResult == 3)
            {
                Logger.Msg("[QuestOpenXR] Failed to EndFrame.");
            }

            if (Status.EyeState == ModuleState.Active)
                Console.WriteLine("Eye data is being utilized.");
            if (Status.LipState == ModuleState.Active)
                Console.WriteLine("Lip data is being utilized.");

            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.CheekPuffLeft] = Math.Max(0, Math.Min(1, GetCheekPuff(0)));
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.CheekPuffRight] = Math.Max(0, Math.Min(1, GetCheekPuff(1)));

            GetFaceWeights(FaceExpressionFB);
            for (int i = 0; i < expressionsSize; ++i)
            {
                expressions[i] = FaceExpressionFB[i];
            }

            XrQuat orientation_L = new XrQuat();
            GetEyeOrientation(0, orientation_L);
            double q_x = (float)orientation_L.x;
            double q_y = (float)orientation_L.y;
            double q_z = (float)orientation_L.z;
            double q_w = (float)orientation_L.w;

            double yaw = Math.Atan2(2.0 * (q_y * q_z + q_w * q_x), q_w * q_w - q_x * q_x - q_y * q_y + q_z * q_z);
            double pitch = Math.Asin(-2.0 * (q_x * q_z - q_w * q_y));

            double pitch_L = (180.0 / Math.PI) * pitch; // from radians
            double yaw_L = (180.0 / Math.PI) * yaw;

            XrQuat orientation_R = new XrQuat();
            GetEyeOrientation(1, orientation_R);

            q_x = (float)orientation_L.x;
            q_y = (float)orientation_L.y;
            q_z = (float)orientation_L.z;
            q_w = (float)orientation_L.w;
            yaw = Math.Atan2(2.0 * (q_y * q_z + q_w * q_x), q_w * q_w - q_x * q_x - q_y * q_y + q_z * q_z);
            pitch = Math.Asin(-2.0 * (q_x * q_z - q_w * q_y));

            double pitch_R = (180.0 / Math.PI) * pitch; // from radians
            double yaw_R = (180.0 / Math.PI) * yaw;

            // Recover true eye closed values; as you look down the eye closes.
            // from FaceTrackingSystem.CS from Movement Aura Scene in https://github.com/oculus-samples/Unity-Movement
            float eyeClosedL = Math.Min(1, expressions[(int)FBExpression.Eyes_Closed_L] + expressions[(int)FBExpression.Eyes_Look_Down_L] * 0.5f);
            float eyeClosedR = Math.Min(1, expressions[(int)FBExpression.Eyes_Closed_R] + expressions[(int)FBExpression.Eyes_Look_Down_R] * 0.5f);

            // Add Lid tightener to eye lid close to help get value closed
            eyeClosedL = Math.Min(1, eyeClosedL + expressions[(int)FBExpression.Lid_Tightener_L] * 0.5f);
            eyeClosedR = Math.Min(1, eyeClosedR + expressions[(int)FBExpression.Lid_Tightener_R] * 0.5f);

            // Convert from Eye Closed to Eye Openness and limit from going negative. Set the max higher than normal to offset the eye lid to help keep eye lid open.
            float opennessL = Math.Min(1, Math.Max(0, 1.1f - eyeClosedL * TrackingSensitivity.EyeLid));
            float opennessR = Math.Min(1, Math.Max(0, 1.1f - eyeClosedR * TrackingSensitivity.EyeLid));

            // As eye opens there is an issue flickering between eye wide and eye not fully open with the combined eye lid parameters. Need to reduce the eye widen value until openess is closer to value of 1. When not fully open will do constant value to reduce the eye widen.
            float eyeWidenL = Math.Max(0, expressions[(int)FBExpression.Upper_Lid_Raiser_L] * TrackingSensitivity.EyeWiden - 3.0f * (1 - opennessL));
            float eyeWidenR = Math.Max(0, expressions[(int)FBExpression.Upper_Lid_Raiser_R] * TrackingSensitivity.EyeWiden - 3.0f * (1 - opennessR));

            // Feedback eye widen to openess, this will help drive the openness value higher from eye widen values
            opennessL += eyeWidenL;
            opennessR += eyeWidenR;

            // Lid Tightener is not tracked the same as SRanipal eye squeeze. This causes problems with combined parameters. The lid tightener has more controls the fine state of closing the eye while the eye lid is more of control blinking.
            // Eye close is non-linear and seems to be based on the confidence of that eye blink is detected. Lid tightener will be used to control the eye state thus squeeze will be disabled for now for the Quest Pro mapping.

            // Subtract eye close
            //float squeezeL = Math.Max(0, expressions[(int)FBExpression.Lid_Tightener_L] - expressions[(int)FBExpression.Eyes_Closed_L] * 1.0f);
            //float squeezeR = Math.Max(0, expressions[(int)FBExpression.Lid_Tightener_R] - expressions[(int)FBExpression.Eyes_Closed_R] * 1.0f);
            float squeezeL = 0;
            float squeezeR = 0;

            // pitch = 47(left)-- > -47(right)
            // yaw = -55(down)-- > 43(up)
            // Eye look angle (degrees) limits calibrated to SRanipal eye tracking
            float eyeLookUpLimit = 43;
            float eyeLookDownLimit = 55;
            float eyeLookOutLimit = 47;
            float eyeLookInLimit = 47;
            if (pitch_L > 0)
            {
                expressions[(int)FBExpression.Eyes_Look_Left_L] = Math.Min(1, (float)(pitch_L / eyeLookOutLimit));
                expressions[(int)FBExpression.Eyes_Look_Right_L] = 0;
            }
            else
            {
                expressions[(int)FBExpression.Eyes_Look_Left_L] = 0;
                expressions[(int)FBExpression.Eyes_Look_Right_L] = Math.Min(1, (float)((-pitch_L) / eyeLookInLimit));
            }
            if (yaw_L > 0)
            {
                expressions[(int)FBExpression.Eyes_Look_Up_L] = Math.Min(1, (float)(yaw_L / eyeLookUpLimit));
                expressions[(int)FBExpression.Eyes_Look_Down_L] = 0;
            }
            else
            {
                expressions[(int)FBExpression.Eyes_Look_Up_L] = 0;
                expressions[(int)FBExpression.Eyes_Look_Down_L] = Math.Min(1, (float)((-yaw_L) / eyeLookDownLimit));
            }

            if (pitch_R > 0)
            {
                expressions[(int)FBExpression.Eyes_Look_Left_R] = Math.Min(1, (float)(pitch_R / eyeLookInLimit));
                expressions[(int)FBExpression.Eyes_Look_Right_R] = 0;
            }
            else
            {
                expressions[(int)FBExpression.Eyes_Look_Left_R] = 0;
                expressions[(int)FBExpression.Eyes_Look_Right_R] = Math.Min(1, (float)((-pitch_R) / eyeLookOutLimit));
            }
            if (yaw_R > 0)
            {
                expressions[(int)FBExpression.Eyes_Look_Up_R] = Math.Min(1, (float)(yaw_R / eyeLookUpLimit));
                expressions[(int)FBExpression.Eyes_Look_Down_R] = 0;
            }
            else
            {
                expressions[(int)FBExpression.Eyes_Look_Up_R] = 0;
                expressions[(int)FBExpression.Eyes_Look_Down_R] = Math.Min(1, (float)((-yaw_R) / eyeLookDownLimit));
            }

            //Porting of eye tracking parameters
            UnifiedTrackingData.LatestEyeData.Left = MakeEye
            (
                LookLeft: expressions[(int)FBExpression.Eyes_Look_Left_L],
                LookRight: expressions[(int)FBExpression.Eyes_Look_Right_L],
                LookUp: expressions[(int)FBExpression.Eyes_Look_Up_L],
                LookDown: expressions[(int)FBExpression.Eyes_Look_Down_L],
                Openness: Math.Min(1, opennessL),
                Squeeze: Math.Min(1, squeezeL),
                Widen: Math.Min(1, eyeWidenL)
            );

            UnifiedTrackingData.LatestEyeData.Right = MakeEye
            (
                LookLeft: expressions[(int)FBExpression.Eyes_Look_Left_R],
                LookRight: expressions[(int)FBExpression.Eyes_Look_Right_R],
                LookUp: expressions[(int)FBExpression.Eyes_Look_Up_R],
                LookDown: expressions[(int)FBExpression.Eyes_Look_Down_R],
                Openness: Math.Min(1, opennessR),
                Squeeze: Math.Min(1, squeezeR),
                Widen: Math.Min(1, eyeWidenR)
            );

            UnifiedTrackingData.LatestEyeData.Combined = MakeEye
            (
                LookLeft: (expressions[(int)FBExpression.Eyes_Look_Left_L] + expressions[(int)FBExpression.Eyes_Look_Left_R]) / 2.0f,
                LookRight: (expressions[(int)FBExpression.Eyes_Look_Right_L] + expressions[(int)FBExpression.Eyes_Look_Right_R]) / 2.0f,
                LookUp: (expressions[(int)FBExpression.Eyes_Look_Up_L] + expressions[(int)FBExpression.Eyes_Look_Up_R]) / 2.0f,
                LookDown: (expressions[(int)FBExpression.Eyes_Look_Down_L] + expressions[(int)FBExpression.Eyes_Look_Down_R]) / 2.0f,
                Openness: Math.Min(1, (opennessL + opennessR) / 2.0f),
                Squeeze: Math.Min(1, (squeezeL + squeezeR) / 2.0f),
                Widen: Math.Min(1, (eyeWidenL + eyeWidenR) / 2.0f)
            );

            // Eye dilation code, automated process maybe?
            UnifiedTrackingData.LatestEyeData.EyesDilation = 0.73f;
            UnifiedTrackingData.LatestEyeData.EyesPupilDiameter = 0.0035f;

            UpdateExpressions();
        }

        // A chance to de-initialize everything. This runs synchronously inside main game thread. Do not touch any Unity objects here.
        public override void Teardown()
        {
            Logger.Msg("[QuestOpenXR] Teardown...");
        }

        private Eye MakeEye(float LookLeft, float LookRight, float LookUp, float LookDown, float Openness, float Squeeze, float Widen)
        {
            return new Eye()
            {
                Look = new Vector2(LookRight - LookLeft, LookUp - LookDown),
                Openness = Openness,
                Squeeze = Squeeze,
                Widen = Widen,
            };
        }

        // Thank you @adjerry on the VRCFT discord for these conversions! https://docs.google.com/spreadsheets/d/118jo960co3Mgw8eREFVBsaJ7z0GtKNr52IB4Bz99VTA/edit#gid=0
        private void UpdateExpressions()
        {

            // Mapping to existing parameters

            // Mouth Ape Shape is combination of shapes. The shape by itself and is combination with Lips Towards and Lip Corner Depressors.              
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthApeShape] = Math.Min(1, expressions[(int)FBExpression.Lips_Toward] * TrackingSensitivity.MouthApeShape + expressions[(int)FBExpression.Lips_Toward] * (expressions[(int)FBExpression.Lip_Corner_Depressor_L] + expressions[(int)FBExpression.Lip_Corner_Depressor_R]) * 0.9f);
            // Subtract ApeShapeShape as Jaw Open will go towards zero as ape shape increase.
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.JawOpen] = Math.Min(1, Math.Min(1, expressions[(int)FBExpression.Jaw_Drop] * TrackingSensitivity.JawOpen) - UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthApeShape] * 0.9f);

            //UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.JawDrop] = Math.Min(1, expressions[(int)FBExpression.Jaw_Drop] * TrackingSensitivity.JawDrop); //TESTING MOUTH APE CONTROL - NON COMPENSATED VALUE

            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.JawLeft] = Math.Min(1, expressions[(int)FBExpression.Jaw_Sideways_Left] * TrackingSensitivity.JawX);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.JawRight] = Math.Min(1, expressions[(int)FBExpression.Jaw_Sideways_Right] * TrackingSensitivity.JawX);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.JawForward] = Math.Min(1, expressions[(int)FBExpression.Jaw_Thrust] * TrackingSensitivity.JawForward);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthPout] = Math.Min(1, (expressions[(int)FBExpression.Lip_Pucker_L] + expressions[(int)FBExpression.Lip_Pucker_R]) / 2.0f * TrackingSensitivity.LipPucker);

            // Cheek puff can be triggered by low values of lip pucker (around 0.1), subtract cheek puff with mouth pout values.
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.CheekPuffLeft] = Math.Max(0, Math.Min(1, expressions[(int)FBExpression.Cheek_Puff_L] * TrackingSensitivity.CheekPuff) - UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthPout]);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.CheekPuffRight] = Math.Max(0, Math.Min(1, expressions[(int)FBExpression.Cheek_Puff_R] * TrackingSensitivity.CheekPuff) - UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthPout]);

            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.CheekSuck] = Math.Min(1, (expressions[(int)FBExpression.Cheek_Suck_L] + expressions[(int)FBExpression.Cheek_Suck_R]) / 2.0f * TrackingSensitivity.CheekSuck);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthUpperLeft] = Math.Min(1, expressions[(int)FBExpression.Mouth_Left] * TrackingSensitivity.MouthX);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthLowerLeft] = Math.Min(1, expressions[(int)FBExpression.Mouth_Left] * TrackingSensitivity.MouthX);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthUpperRight] = Math.Min(1, expressions[(int)FBExpression.Mouth_Right] * TrackingSensitivity.MouthX);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthLowerRight] = Math.Min(1, expressions[(int)FBExpression.Mouth_Right] * TrackingSensitivity.MouthX);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthSmileLeft] = Math.Min(1, expressions[(int)FBExpression.Lip_Corner_Puller_L] * TrackingSensitivity.MouthSmile);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthSmileRight] = Math.Min(1, expressions[(int)FBExpression.Lip_Corner_Puller_R] * TrackingSensitivity.MouthSmile);

            // Lip corner depressors are part of mouth ape shape, will subtract the current value of mouthApeShape from lip corner depressor to compensate
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthSadLeft] = Math.Max(0, Math.Min(1, expressions[(int)FBExpression.Lip_Corner_Depressor_L] * TrackingSensitivity.MouthFrown) - UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthApeShape] * 1.0f);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthSadRight] = Math.Max(0, Math.Min(1, expressions[(int)FBExpression.Lip_Corner_Depressor_R] * TrackingSensitivity.MouthFrown) - UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthApeShape] * 1.0f);

            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthUpperOverturn] = Math.Min(1, (expressions[(int)FBExpression.Lip_Funneler_LT] + expressions[(int)FBExpression.Lip_Funneler_RT]) / 2.0f * TrackingSensitivity.LipFunnelTop);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthLowerOverturn] = Math.Min(1, (expressions[(int)FBExpression.Lip_Funneler_LB] + expressions[(int)FBExpression.Lip_Funneler_RB]) / 2.0f * TrackingSensitivity.LipFunnelBottom);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthUpperInside] = Math.Min(1, (expressions[(int)FBExpression.Lip_Suck_LT] + expressions[(int)FBExpression.Lip_Suck_RT]) / 2.0f * TrackingSensitivity.LipSuckTop);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthLowerInside] = Math.Min(1, (expressions[(int)FBExpression.Lip_Suck_LB] + expressions[(int)FBExpression.Lip_Suck_RB]) / 2.0f * TrackingSensitivity.LipSuckBottom);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthLowerOverlay] = Math.Min(1, expressions[(int)FBExpression.Chin_Raiser_T] * TrackingSensitivity.ChinRaiserTop);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthLowerDownLeft] = Math.Min(1, expressions[(int)FBExpression.Lower_Lip_Depressor_L] * TrackingSensitivity.MouthLowerDown);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthLowerDownRight] = Math.Min(1, expressions[(int)FBExpression.Lower_Lip_Depressor_R] * TrackingSensitivity.MouthLowerDown);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthUpperUpLeft] = Math.Min(1, expressions[(int)FBExpression.Upper_Lip_Raiser_L] * TrackingSensitivity.MouthUpperUp);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthUpperUpRight] = Math.Min(1, expressions[(int)FBExpression.Upper_Lip_Raiser_R] * TrackingSensitivity.MouthUpperUp);

            // Mapping of Quest Pro FACS to VRCFT Unique Shapes     
            // Custom Brow Tracking Expanded Set
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.BrowsInnerUp] = Math.Min(1, (expressions[(int)FBExpression.Inner_Brow_Raiser_L] + expressions[(int)FBExpression.Inner_Brow_Raiser_R]) / 2.0f * TrackingSensitivity.BrowInnerUp);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.BrowInnerUpLeft] = Math.Min(1, expressions[(int)FBExpression.Inner_Brow_Raiser_L] * TrackingSensitivity.BrowInnerUp);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.BrowInnerUpRight] = Math.Min(1, expressions[(int)FBExpression.Inner_Brow_Raiser_R] * TrackingSensitivity.BrowInnerUp);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.BrowsOuterUp] = Math.Min(1, (expressions[(int)FBExpression.Outer_Brow_Raiser_L] + expressions[(int)FBExpression.Outer_Brow_Raiser_R]) / 2.0f * TrackingSensitivity.BrowOuterUp);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.BrowOuterUpLeft] = Math.Min(1, expressions[(int)FBExpression.Outer_Brow_Raiser_L] * TrackingSensitivity.BrowOuterUp);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.BrowOuterUpRight] = Math.Min(1, expressions[(int)FBExpression.Outer_Brow_Raiser_R] * TrackingSensitivity.BrowOuterUp);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.BrowsDown] = Math.Min(1, (expressions[(int)FBExpression.Brow_Lowerer_L] + expressions[(int)FBExpression.Brow_Lowerer_R]) / 2.0f * TrackingSensitivity.BrowDown);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.BrowDownLeft] = Math.Min(1, expressions[(int)FBExpression.Brow_Lowerer_L] * TrackingSensitivity.BrowDown);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.BrowDownRight] = Math.Min(1, expressions[(int)FBExpression.Brow_Lowerer_R] * TrackingSensitivity.BrowDown);

            // Custom Eye Tracking Expanded Set
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.EyesSquint] = Math.Min(1, (expressions[(int)FBExpression.Lid_Tightener_L] + expressions[(int)FBExpression.Lid_Tightener_R]) / 2.0f * TrackingSensitivity.EyeSquint);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.EyeSquintLeft] = Math.Min(1, expressions[(int)FBExpression.Lid_Tightener_L] * TrackingSensitivity.EyeSquint);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.EyeSquintRight] = Math.Min(1, expressions[(int)FBExpression.Lid_Tightener_R] * TrackingSensitivity.EyeSquint);

            // Custom Face Tracking Expanded Set               
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.CheekSquintLeft] = Math.Min(1, expressions[(int)FBExpression.Cheek_Raiser_L] * TrackingSensitivity.CheekRaiser);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.CheekSquintRight] = Math.Min(1, expressions[(int)FBExpression.Cheek_Raiser_R] * TrackingSensitivity.CheekRaiser);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthRaiserUpper] = Math.Min(1, expressions[(int)FBExpression.Chin_Raiser_B] * TrackingSensitivity.ChinRaiserBottom);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthRaiserLower] = Math.Min(1, expressions[(int)FBExpression.Chin_Raiser_T] * TrackingSensitivity.ChinRaiserTop);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthDimpleLeft] = Math.Min(1, expressions[(int)FBExpression.Dimpler_L] * TrackingSensitivity.MouthDimpler);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthDimpleRight] = Math.Min(1, expressions[(int)FBExpression.Dimpler_R] * TrackingSensitivity.MouthDimpler);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.LipFunnelBottomLeft] = Math.Min(1, expressions[(int)FBExpression.Lip_Funneler_LB] * TrackingSensitivity.LipFunnelBottom);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.LipFunnelBottomRight] = Math.Min(1, expressions[(int)FBExpression.Lip_Funneler_RB] * TrackingSensitivity.LipFunnelBottom);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.LipFunnelTopLeft] = Math.Min(1, expressions[(int)FBExpression.Lip_Funneler_LT] * TrackingSensitivity.LipFunnelTop);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.LipFunnelTopRight] = Math.Min(1, expressions[(int)FBExpression.Lip_Funneler_RT] * TrackingSensitivity.LipFunnelTop);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthPress] = Math.Min(1, (expressions[(int)FBExpression.Lip_Pressor_L] + expressions[(int)FBExpression.Lip_Pressor_R]) / 2.0f * TrackingSensitivity.MouthPress);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthPressLeft] = Math.Min(1, expressions[(int)FBExpression.Lip_Pressor_L] * TrackingSensitivity.MouthPress);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthPressRight] = Math.Min(1, expressions[(int)FBExpression.Lip_Pressor_R] * TrackingSensitivity.MouthPress);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.LipPuckerLeft] = Math.Min(1, expressions[(int)FBExpression.Lip_Pucker_L] * TrackingSensitivity.LipPucker);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.LipPuckerRight] = Math.Min(1, expressions[(int)FBExpression.Lip_Pucker_R] * TrackingSensitivity.LipPucker);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthStretchLeft] = Math.Min(1, expressions[(int)FBExpression.Lip_Stretcher_L] * TrackingSensitivity.MouthStretch);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthStretchRight] = Math.Min(1, expressions[(int)FBExpression.Lip_Stretcher_R] * TrackingSensitivity.MouthStretch);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.LipSuckBottomLeft] = Math.Min(1, expressions[(int)FBExpression.Lip_Suck_LB] * TrackingSensitivity.LipSuckBottom);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.LipSuckBottomRight] = Math.Min(1, expressions[(int)FBExpression.Lip_Suck_RB] * TrackingSensitivity.LipSuckBottom);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.LipSuckTopRight] = Math.Min(1, expressions[(int)FBExpression.Lip_Suck_RT] * TrackingSensitivity.LipSuckTop);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.LipSuckTopLeft] = Math.Min(1, expressions[(int)FBExpression.Lip_Suck_LT] * TrackingSensitivity.LipSuckTop);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthTightener] = Math.Min(1, (expressions[(int)FBExpression.Lip_Tightener_L] + expressions[(int)FBExpression.Lip_Tightener_R]) / 2.0f * TrackingSensitivity.MouthTightener);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthTightenerLeft] = Math.Min(1, expressions[(int)FBExpression.Lip_Tightener_L] * TrackingSensitivity.MouthTightener);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthTightenerRight] = Math.Min(1, expressions[(int)FBExpression.Lip_Tightener_R] * TrackingSensitivity.MouthTightener);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.NoseSneerLeft] = Math.Min(1, expressions[(int)FBExpression.Nose_Wrinkler_L] * TrackingSensitivity.NoseSneer);
            UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.NoseSneerRight] = Math.Min(1, expressions[(int)FBExpression.Nose_Wrinkler_R] * TrackingSensitivity.NoseSneer);

            //UnifiedTrackingData.LatestLipData.LatestShapes[(int)UnifiedExpression.MouthClosed] = Math.Min(1, (expressions[(int)FBExpression.Lips_Toward]) * TrackingSensitivity.MouthTowards);
        }
    }
}
