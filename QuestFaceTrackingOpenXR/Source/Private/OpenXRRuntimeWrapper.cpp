// Copyright Epic Games, Inc. All Rights Reserved.

#include "OpenXRRuntimeWrapper.h"

#include <windows.h>
#include <iostream>
#include <string>

using std::wstring;

HINSTANCE hLoaderHandle;

#define GET_OPENXR_ENTRYPOINTS(Func) if (!XR_SUCCEEDED(wrapper->GetInstanceProcAddr(Instance, "xr" #Func, (PFN_xrVoidFunction*)&(wrapper->Func)))) \
		{ std::cerr << "Failed to find entry point for" << "xr" #Func << std::endl; return false; }

int InitializeOpenXRRuntimeWrapperGlobal(OpenXRRuntimeWrapper * wrapper)
{
    //wrapper->GetInstanceProcAddr = (PFN_xrGetInstanceProcAddr)FPlatformProcess::GetDllExport(LoaderHandle, TEXT("xrGetInstanceProcAddr"));
    
    wstring LoaderPath = L"openxr_loader.dll";
    hLoaderHandle = LoadLibrary(LoaderPath.c_str());

    if (hLoaderHandle == NULL)
    {
        std::cerr << "Failed to load OpenXRLoader" << std::endl;
        return 2;
    }

    wrapper->GetInstanceProcAddr = (PFN_xrGetInstanceProcAddr)GetProcAddress(hLoaderHandle, "xrGetInstanceProcAddr");

    XrInstance Instance = XR_NULL_HANDLE;

    ENUM_OPENXR_ENTRYPOINTS_GLOBAL(GET_OPENXR_ENTRYPOINTS)
    return nullptr == wrapper->GetInstanceProcAddr;
}

bool InitializeOpenXRRuntimeWrapperInstance(OpenXRRuntimeWrapper* wrapper, XrInstance Instance)
{
    ENUM_OPENXR_ENTRYPOINTS(GET_OPENXR_ENTRYPOINTS)
    return true;
}

void DestoryOpenXRRuntimeWrapper(OpenXRRuntimeWrapper* wrapper)
{
    FreeLibrary(hLoaderHandle);
}

#undef GET_OPENXR_ENTRYPOINTS
