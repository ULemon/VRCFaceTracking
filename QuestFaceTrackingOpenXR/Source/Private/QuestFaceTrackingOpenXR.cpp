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
