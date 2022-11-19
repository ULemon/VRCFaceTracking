#include "QuestOpenXR.h"
#include <vector>
#include <algorithm>
#include <iterator>
#include <iostream>
#pragma comment(lib,"d3d11.lib")

using namespace std;

QuestOpenXR::QuestOpenXR()
	: FaceWeights{}
	, FaceConfidence{}
	, EyeGazes{ XR_TYPE_EYE_GAZES_FB }
{
	Instance = XR_NULL_HANDLE;
	SystemId = XR_NULL_SYSTEM_ID;
	Session = XR_NULL_HANDLE;
	FaceTracker = XR_NULL_HANDLE;
	EyeTracker = XR_NULL_HANDLE;
	StereoViewConfigurationType = XR_VIEW_CONFIGURATION_TYPE_PRIMARY_STEREO;
}

QuestOpenXR::~QuestOpenXR()
{
}

int QuestOpenXR::InitRuntime()
{
	XrResult Result;
	InitializeOpenXRRuntimeWrapperGlobal(&RuntimeWrapper);

	if (!EnumerateExtensions())
	{
		return 2;
	}

	vector<const char*> extensions;
	extensions.clear();
	vector<string> requiredExtensions
	{
		XR_KHR_D3D11_ENABLE_EXTENSION_NAME,
		XR_FB_FACE_TRACKING_EXTENSION_NAME,
		XR_FB_EYE_TRACKING_SOCIAL_EXTENSION_NAME,
	};

	transform(requiredExtensions.begin(), requiredExtensions.end(), back_inserter(extensions),
		[](const string& ext) { return ext.c_str(); });

	char appName[XR_MAX_APPLICATION_NAME_SIZE] = "QuestFaceTracking";

	XrInstanceCreateInfo Info{ XR_TYPE_INSTANCE_CREATE_INFO };
	Info.type = XR_TYPE_INSTANCE_CREATE_INFO;
	Info.next = nullptr;
	Info.createFlags = 0;
	_memccpy(Info.applicationInfo.applicationName, appName, '\0', XR_MAX_APPLICATION_NAME_SIZE);
	Info.applicationInfo.applicationVersion = 0xA5A5;
	Info.applicationInfo.engineVersion = 0xA5A5;
	Info.applicationInfo.apiVersion = XR_CURRENT_API_VERSION;
	Info.enabledApiLayerCount = 0;
	Info.enabledApiLayerNames = NULL;

	Info.enabledExtensionCount = extensions.size();
	Info.enabledExtensionNames = extensions.data();

	Result = RuntimeWrapper.CreateInstance(&Info, &Instance);
	if (XR_FAILED(Result))
	{
		return 2;
	}

	InitializeOpenXRRuntimeWrapperInstance(&RuntimeWrapper, Instance);

	XrInstanceProperties instanceProps = { XR_TYPE_INSTANCE_PROPERTIES, nullptr };
	XR_SUCCESS(RuntimeWrapper.GetInstanceProperties(Instance, &instanceProps));
	instanceProps.runtimeName[XR_MAX_RUNTIME_NAME_SIZE - 1] = 0; // Ensure the name is null terminated.

	XrSystemGetInfo systemInfo;
	systemInfo.type = XR_TYPE_SYSTEM_GET_INFO;
	systemInfo.next = nullptr;
	systemInfo.formFactor = XR_FORM_FACTOR_HEAD_MOUNTED_DISPLAY;

	Result = RuntimeWrapper.GetSystem(Instance, &systemInfo, &SystemId);
	if (XR_FAILED(Result))
	{
		return 3;
	}

	uint32_t ConfigurationCount;
	vector<XrViewConfigurationType> viewConfigTypes;
	RuntimeWrapper.EnumerateViewConfigurations(Instance, SystemId, 0, &ConfigurationCount, nullptr);
	viewConfigTypes.resize(ConfigurationCount);
	// Fill the initial array with valid enum types (this will fail in the validation layer otherwise).
	for (auto& TypeIter : viewConfigTypes)
		TypeIter = XR_VIEW_CONFIGURATION_TYPE_PRIMARY_MONO;
	RuntimeWrapper.EnumerateViewConfigurations(Instance, SystemId, ConfigurationCount, &ConfigurationCount, viewConfigTypes.data());
	bool bSupportStereoView = false;

	for (XrViewConfigurationType viewConfigType : viewConfigTypes)
	{
		if (viewConfigType == StereoViewConfigurationType)
		{
			bSupportStereoView = true;
			break;
		}
	}

	if (!bSupportStereoView)
	{
		return 4;
	}

	PFN_xrGetD3D11GraphicsRequirementsKHR GetD3D11GraphicsRequirementsKHR;
	RuntimeWrapper.GetInstanceProcAddr(Instance, "xrGetD3D11GraphicsRequirementsKHR", (PFN_xrVoidFunction*)&GetD3D11GraphicsRequirementsKHR);

	XrGraphicsRequirementsD3D11KHR Requirements;
	Requirements.type = XR_TYPE_GRAPHICS_REQUIREMENTS_D3D11_KHR;
	Requirements.next = nullptr;
	Result = GetD3D11GraphicsRequirementsKHR(Instance, SystemId, &Requirements);
	if (XR_FAILED(Result))
	{
		return 5;
	}

	ID3D11Device* Device;
	D3D11CreateDevice(NULL, D3D_DRIVER_TYPE_HARDWARE, NULL, 0x00, NULL, 0, D3D11_SDK_VERSION, &Device, NULL, NULL);

	XrGraphicsBindingD3D11KHR graphicBinding;
	graphicBinding.type = XR_TYPE_GRAPHICS_BINDING_D3D11_KHR;
	graphicBinding.next = nullptr;
	graphicBinding.device = Device;

	XrSessionCreateInfo sessionInfo;
	sessionInfo.type = XR_TYPE_SESSION_CREATE_INFO;
	sessionInfo.next = &graphicBinding;
	sessionInfo.createFlags = 0;
	sessionInfo.systemId = SystemId;

	XrSystemEyeTrackingPropertiesFB eyeTrackingSystemProperties{ XR_TYPE_SYSTEM_EYE_TRACKING_PROPERTIES_FB };
	XrSystemProperties systemPropertiesEye{ XR_TYPE_SYSTEM_PROPERTIES, &eyeTrackingSystemProperties };
	RuntimeWrapper.GetSystemProperties(Instance, SystemId, &systemPropertiesEye);
	if (!eyeTrackingSystemProperties.supportsEyeTracking)
	{
		return -1;
	}

	XrSystemFaceTrackingPropertiesFB faceTrackingSystemProperties{ XR_TYPE_SYSTEM_FACE_TRACKING_PROPERTIES_FB };
	XrSystemProperties systemPropertiesFace{ XR_TYPE_SYSTEM_PROPERTIES, &faceTrackingSystemProperties };
	RuntimeWrapper.GetSystemProperties(Instance, SystemId, &systemPropertiesFace);
	if (!faceTrackingSystemProperties.supportsFaceTracking)
	{
		return -2;
	}

	Result = RuntimeWrapper.CreateSession(Instance, &sessionInfo, &Session);
	if (XR_FAILED(Result))
	{
		return 6;
	}

	XrReferenceSpaceCreateInfo spaceInfo;
	spaceInfo.type = XR_TYPE_REFERENCE_SPACE_CREATE_INFO;
	spaceInfo.next = nullptr;
	spaceInfo.referenceSpaceType = XR_REFERENCE_SPACE_TYPE_VIEW;
	XrQuaternionf orientation{ 0.0f, 0.0f, 0.0f, 1.0f };
	XrVector3f position{ 0.0f, 0.0f,0.0f };
	spaceInfo.poseInReferenceSpace.orientation = orientation;
	spaceInfo.poseInReferenceSpace.position = position;
	Result = RuntimeWrapper.CreateReferenceSpace(Session, &spaceInfo, &HmdSpace);
	if (XR_FAILED(Result))
	{
		return 7;
	}

	spaceInfo.referenceSpaceType = XR_REFERENCE_SPACE_TYPE_LOCAL;
	Result = RuntimeWrapper.CreateReferenceSpace(Session, &spaceInfo, &LocalSpace);
	if (XR_FAILED(Result))
	{
		return 8;
	}

	bool bSessionHasBegun = false;
	while (!bSessionHasBegun)
	{
		XrEventDataBuffer Event;
		while (ReadNextEvent(&Event))
		{
			switch (Event.type)
			{
			case XR_TYPE_EVENT_DATA_SESSION_STATE_CHANGED:
			{
				const XrEventDataSessionStateChanged& SessionState =
					reinterpret_cast<XrEventDataSessionStateChanged&>(Event);

				CurrentSessionState = SessionState.state;

				if (SessionState.state == XR_SESSION_STATE_READY)
				{
					if (!BeginSession())
					{
						return 8;
					}
					else
					{
						bSessionHasBegun = true;
					}
				}
				else if (SessionState.state == XR_SESSION_STATE_SYNCHRONIZED)
				{
				}
				else if (SessionState.state == XR_SESSION_STATE_IDLE)
				{
				}
				else if (SessionState.state == XR_SESSION_STATE_STOPPING)
				{
					EndSession();
				}
				else if (SessionState.state == XR_SESSION_STATE_EXITING || SessionState.state == XR_SESSION_STATE_LOSS_PENDING)
				{
				}

				break;
			}
			case XR_TYPE_EVENT_DATA_INSTANCE_LOSS_PENDING:
			{
			}
			case XR_TYPE_EVENT_DATA_REFERENCE_SPACE_CHANGE_PENDING:
			{
			}
			case XR_TYPE_EVENT_DATA_VISIBILITY_MASK_CHANGED_KHR:
			{
			}
			}
		}
	}

	XrFaceTrackerCreateInfoFB faceTrackerCreateInfo{ XR_TYPE_FACE_TRACKER_CREATE_INFO_FB };
	Result = RuntimeWrapper.CreateFaceTrackerFB(Session, &faceTrackerCreateInfo, &FaceTracker);
	if (XR_FAILED(Result))
	{
		return 9;
	}

	XrEyeTrackerCreateInfoFB eyeTrackerCreateInfo{ XR_TYPE_EYE_TRACKER_CREATE_INFO_FB };
	Result = RuntimeWrapper.CreateEyeTrackerFB(Session, &eyeTrackerCreateInfo, &EyeTracker);
	if (XR_FAILED(Result))
	{
		return 10;
	}

	return 0;
}

int QuestOpenXR::Update()
{
	XrResult Result;

	/*XrFrameWaitInfo waitInfo;
	waitInfo.type = XR_TYPE_FRAME_WAIT_INFO;
	waitInfo.next = nullptr;

	XrFrameState frameState{ XR_TYPE_FRAME_STATE };

	Result = RuntimeWrapper.WaitFrame(Session, &waitInfo, &frameState);
	if (XR_FAILED(Result))
	{
		return 1;
	}

	XrFrameBeginInfo beginInfo{ XR_TYPE_FRAME_BEGIN_INFO };
	Result = RuntimeWrapper.BeginFrame(Session, &beginInfo);
	if (XR_FAILED(Result))
	{
		return 4;
	}*/

	XrFaceExpressionWeightsFB expressionWeights{ XR_TYPE_FACE_EXPRESSION_WEIGHTS_FB };
	expressionWeights.next = nullptr;
	expressionWeights.weights = FaceWeights;
	expressionWeights.confidences = FaceConfidence;
	expressionWeights.weightCount = XR_FACE_EXPRESSION_COUNT_FB;
	expressionWeights.confidenceCount = XR_FACE_CONFIDENCE_COUNT_FB;

	XrFaceExpressionInfoFB expressionInfo{ XR_TYPE_FACE_EXPRESSION_INFO_FB };

	Result = RuntimeWrapper.GetFaceExpressionWeightsFB(FaceTracker, &expressionInfo, &expressionWeights);
	if (XR_FAILED(Result))
	{
		return 2;
	}

	EyeGazes.next = nullptr;

	XrEyeGazesInfoFB gazesInfo{ XR_TYPE_EYE_GAZES_INFO_FB };
	gazesInfo.baseSpace = HmdSpace;
	//gazesInfo.time = frameState.predictedDisplayTime;

	Result = RuntimeWrapper.GetEyeGazesFB(EyeTracker, &gazesInfo, &EyeGazes);
	if (XR_FAILED(Result))
	{
		return 3;
	}

	//XrFrameEndInfo endInfo{ XR_TYPE_FRAME_END_INFO };
	//endInfo.next = nullptr;
	//endInfo.displayTime = frameState.predictedDisplayTime;
	//endInfo.environmentBlendMode = XR_ENVIRONMENT_BLEND_MODE_OPAQUE;
	//endInfo.layerCount = 0;
	//endInfo.layers = nullptr;

	//Result = RuntimeWrapper.EndFrame(Session, &endInfo);
	//if (XR_FAILED(Result))
	//{
	//	return 5;
	//}

	return 0;
}

float QuestOpenXR::GetCheekPuff(int cheekIndex)
{
	if (cheekIndex == 0)
	{
		return FaceWeights[2];
	}
	return FaceWeights[3];
}

XrQuaternionf QuestOpenXR::GetEyeOrientation(int eyeIndex)
{
	return EyeGazes.gaze[eyeIndex].gazePose.orientation;
}

void QuestOpenXR::GetFaceWeights(float outFaceExpressionFB[10])
{
	for (int i = 0; i < XR_FACE_EXPRESSION_COUNT_FB; ++i)
	{
		(outFaceExpressionFB)[i] = FaceWeights[i];
	}
}

bool QuestOpenXR::EnumerateExtensions()
{
	uint32_t extensionsCount = 0;
	if (XR_FAILED(RuntimeWrapper.EnumerateInstanceExtensionProperties(nullptr, 0, &extensionsCount, nullptr)))
	{
		// If it fails this early that means there's no runtime installed
		return false;
	}

	vector<XrExtensionProperties> properties;
	properties.resize(extensionsCount);
	for (auto& prop : properties)
	{
		prop = XrExtensionProperties{ XR_TYPE_EXTENSION_PROPERTIES };
	}

	if (XR_SUCCEEDED(RuntimeWrapper.EnumerateInstanceExtensionProperties(nullptr, extensionsCount, &extensionsCount, properties.data())))
	{
		for (const XrExtensionProperties& prop : properties)
		{
			AvailableExtensions.emplace(prop.extensionName);
		}
		return true;
	}
	return false;
}

bool QuestOpenXR::ReadNextEvent(XrEventDataBuffer* Buffer)
{
	XrEventDataBaseHeader* baseHeader = reinterpret_cast<XrEventDataBaseHeader*>(Buffer);
	*baseHeader = { XR_TYPE_EVENT_DATA_BUFFER };
	const XrResult xr = RuntimeWrapper.PollEvent(Instance, Buffer);
	if (xr == XR_SUCCESS)
	{
		return true;
	}
	return false;
}

bool QuestOpenXR::BeginSession()
{
	XrSessionBeginInfo beginInfo = { XR_TYPE_SESSION_BEGIN_INFO, nullptr, StereoViewConfigurationType };

	return XR_SUCCEEDED(RuntimeWrapper.BeginSession(Session, &beginInfo));
}

bool QuestOpenXR::EndSession()
{
	return XR_SUCCEEDED(RuntimeWrapper.EndSession(Session));
}
