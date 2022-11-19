// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#include <openxr/openxr.h>
#include <openxr/openxr_reflection.h>
#include <openxr/fb_face_tracking.h>
#include <openxr/fb_eye_tracking_social.h>

#define XR_USE_GRAPHICS_API_D3D11	1
#include <d3d11.h>
#include "openxr/openxr_platform.h"

/** List all OpenXR global entry points used by the plugin. */
#define ENUM_OPENXR_ENTRYPOINTS_GLOBAL(EnumMacro) \
	EnumMacro(EnumerateApiLayerProperties) \
	EnumMacro(EnumerateInstanceExtensionProperties) \
	EnumMacro(CreateInstance)

/** List all OpenXR instance entry points used by the plugin. */
#define ENUM_OPENXR_ENTRYPOINTS(EnumMacro) \
	EnumMacro(DestroyInstance) \
	EnumMacro(GetInstanceProperties) \
	EnumMacro(PollEvent) \
	EnumMacro(ResultToString) \
	EnumMacro(StructureTypeToString) \
	EnumMacro(GetSystem) \
	EnumMacro(GetSystemProperties) \
	EnumMacro(EnumerateEnvironmentBlendModes) \
	EnumMacro(CreateSession) \
	EnumMacro(DestroySession) \
	EnumMacro(EnumerateReferenceSpaces) \
	EnumMacro(CreateReferenceSpace) \
	EnumMacro(GetReferenceSpaceBoundsRect) \
	EnumMacro(CreateActionSpace) \
	EnumMacro(LocateSpace) \
	EnumMacro(DestroySpace) \
	EnumMacro(EnumerateViewConfigurations) \
	EnumMacro(GetViewConfigurationProperties) \
	EnumMacro(EnumerateViewConfigurationViews) \
	EnumMacro(EnumerateSwapchainFormats) \
	EnumMacro(CreateSwapchain) \
	EnumMacro(DestroySwapchain) \
	EnumMacro(EnumerateSwapchainImages) \
	EnumMacro(AcquireSwapchainImage) \
	EnumMacro(WaitSwapchainImage) \
	EnumMacro(ReleaseSwapchainImage) \
	EnumMacro(BeginSession) \
	EnumMacro(EndSession) \
	EnumMacro(RequestExitSession) \
	EnumMacro(WaitFrame) \
	EnumMacro(BeginFrame) \
	EnumMacro(EndFrame) \
	EnumMacro(LocateViews) \
	EnumMacro(StringToPath) \
	EnumMacro(PathToString) \
	EnumMacro(CreateActionSet) \
	EnumMacro(DestroyActionSet) \
	EnumMacro(CreateAction) \
	EnumMacro(DestroyAction) \
	EnumMacro(SuggestInteractionProfileBindings) \
	EnumMacro(AttachSessionActionSets) \
	EnumMacro(GetCurrentInteractionProfile) \
	EnumMacro(GetActionStateBoolean) \
	EnumMacro(GetActionStateFloat) \
	EnumMacro(GetActionStateVector2f) \
	EnumMacro(GetActionStatePose) \
	EnumMacro(SyncActions) \
	EnumMacro(EnumerateBoundSourcesForAction) \
	EnumMacro(GetInputSourceLocalizedName) \
	EnumMacro(ApplyHapticFeedback) \
	EnumMacro(StopHapticFeedback) \
	EnumMacro(CreateFaceTrackerFB) \
	EnumMacro(DestroyFaceTrackerFB) \
	EnumMacro(GetFaceExpressionWeightsFB) \
	EnumMacro(CreateEyeTrackerFB) \
	EnumMacro(DestroyEyeTrackerFB) \
	EnumMacro(GetEyeGazesFB) \


#define DECLARE_OPENXR_ENTRYPOINTS(Func) PFN_xr##Func Func = nullptr;

struct OpenXRRuntimeWrapper
{
	PFN_xrGetInstanceProcAddr GetInstanceProcAddr = nullptr;

    ENUM_OPENXR_ENTRYPOINTS_GLOBAL(DECLARE_OPENXR_ENTRYPOINTS)
    ENUM_OPENXR_ENTRYPOINTS(DECLARE_OPENXR_ENTRYPOINTS)
};

#undef DECLARE_OPENXR_ENTRYPOINTS

int InitializeOpenXRRuntimeWrapperGlobal(OpenXRRuntimeWrapper* wrapper);
bool InitializeOpenXRRuntimeWrapperInstance(OpenXRRuntimeWrapper* wrapper, XrInstance Instance);
void DestoryOpenXRRuntimeWrapper(OpenXRRuntimeWrapper* wrapper);
