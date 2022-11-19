#include "QuestFaceTrackingOpenXR.h"
#include "OpenXRRuntimeWrapper.h"

#include "QuestOpenXR.h"

QuestOpenXR gQuestOpenXR;

int InitOpenXRRuntime()
{
	return gQuestOpenXR.InitRuntime();
}

int UpdateOpenXRFaceTracker()
{
	return gQuestOpenXR.Update();
}

float GetCheekPuff(int cheekIndex)
{
	return gQuestOpenXR.GetCheekPuff(cheekIndex);
}

void GetEyeOrientation(int eyeIndex, XrQuat* outData)
{
	XrQuaternionf orientation = gQuestOpenXR.GetEyeOrientation(eyeIndex);

	outData->x = orientation.x;
	outData->y = orientation.y;
	outData->z = orientation.z;
	outData->w = orientation.w;
}

void GetFaceWeights(float outFaceExpressionFB[10])
{
	gQuestOpenXR.GetFaceWeights(outFaceExpressionFB);
}

