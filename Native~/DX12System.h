#pragma once
#include <memory>

#include "Unity/IUnityInterface.h"
#include "Unity/IUnityGraphics.h"

struct IDXGISwapChain;
#include "d3d12.h"
#include "Unity/IUnityGraphicsD3D12.h"

#include "Logger.h"

namespace NativeRenderingPlugin
{
    class DX12System
    {
    public:

        DX12System(IUnityInterfaces* unityInterfaces) :
            m_UnityGraphics(nullptr),
            m_Device(nullptr),
            m_CommandQueue(nullptr),
            m_IsInitialized(false)
        {
            m_UnityGraphics = unityInterfaces->Get<IUnityGraphicsD3D12v5>();

            if (m_UnityGraphics == nullptr)
            {
                s_Logger->LogError("DX12System: Failed to fetch DX12 interface.");
                return;
            }

            m_Device = m_UnityGraphics->GetDevice();
            m_CommandQueue = m_UnityGraphics->GetCommandQueue();
            m_IsInitialized = m_Device != nullptr && m_CommandQueue != nullptr;

            if (m_Device == nullptr)
            {
                s_Logger->LogError("DX12System: Failed to fetch DX12 device.");
            }

            if (m_CommandQueue == nullptr)
            {
                s_Logger->LogError("DX12System: Failed to fetch DX12 command queue.");
            }
        }

        bool IsInitialized() const
        {
            return m_IsInitialized;
        }

        ID3D12Device* GetDevice() const
        {
            return m_Device;
        }

        ID3D12CommandQueue* GetCommandQueue() const
        {
            return m_CommandQueue;
        }

    private:

        IUnityGraphicsD3D12v5* m_UnityGraphics;
        ID3D12Device* m_Device;
        ID3D12CommandQueue* m_CommandQueue;
        bool m_IsInitialized;
    };

    inline std::unique_ptr<DX12System> s_DX12System;
}
