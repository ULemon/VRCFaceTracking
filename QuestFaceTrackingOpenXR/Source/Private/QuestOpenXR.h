#pragma once
#include "OpenXRRuntimeWrapper.h"
#include "ExportedType.h"

#include <set>
#include <string>
#include <vector>

class QuestOpenXR
{
public:
	QuestOpenXR();
	~QuestOpenXR();

	int InitRuntime();
	int Update();
	float GetCheekPuff(int cheekIndex);
	XrQuaternionf GetEyeOrientation(int eyeIndex);
	void GetFaceWeights(float outFaceExpressionFB[63]);

	OpenXRRuntimeWrapper* GetRuntimeWrapper() { return &RuntimeWrapper; }
private:
	XrInstance Instance = XR_NULL_HANDLE;
	XrSystemId SystemId = XR_NULL_SYSTEM_ID;
	XrSession Session;
	XrSessionState CurrentSessionState;
	XrSpace HmdSpace;
	XrSpace LocalSpace;
	XrViewConfigurationType StereoViewConfigurationType;

	XrFaceTrackerFB FaceTracker;
	XrEyeTrackerFB EyeTracker;
	OpenXRRuntimeWrapper RuntimeWrapper;

	std::set<std::string> AvailableExtensions;

	float FaceWeights[XR_FACE_EXPRESSION_COUNT_FB];
	float FaceConfidence[XR_FACE_CONFIDENCE_COUNT_FB];
	XrEyeGazesFB EyeGazes;

	bool EnumerateExtensions();
	bool ReadNextEvent(XrEventDataBuffer* Buffer);
	bool BeginSession();
	bool EndSession();
};
