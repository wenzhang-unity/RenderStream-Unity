#pragma once
#include <d3d11.h>
#include <d3d12.h>
#include <wrl/client.h> // For ComPtr

#include "Unity/IUnityGraphicsD3D11.h"
#include "Unity/IUnityGraphicsD3D12.h"

#include "Disguise/d3renderstream.h"

#include "Logger.h"

namespace NativeRenderingPlugin
{
    // Render thread event IDs
    // Should match EventID (NativeRenderingPlugin.cs)
    enum class EventID
    {
        USER_EVENTS_START = 128, // Should be a fixed value that exceeds kUnityRenderingExtCustomBlitCount
        GET_FRAME_IMAGE = USER_EVENTS_START,
        USER_EVENTS_END
    };

    typedef RS_ERROR(*t_rs_getFrameImage)(int64_t imageId, SenderFrameType frameType, SenderFrameTypeData data);

    // EventID::GET_FRAME_IMAGE data structure
    // Should match GetFrameImageData (NativeRenderingPlugin.cs)
    struct GetFrameImageData
    {
        t_rs_getFrameImage m_rs_getFrameImage;  // Function pointer into Disguise DLL
        int64_t m_ImageID;

        RS_ERROR Execute(IUnknown* texture) const
        {
            if (texture == nullptr)
            {
                s_Logger->LogError("GetFrameImageData null texture pointer");
                return RS_ERROR::RS_ERROR_INVALID_PARAMETERS;
            }

            SenderFrameType senderType = RS_FRAMETYPE_UNKNOWN;
            SenderFrameTypeData senderData = {};

            Microsoft::WRL::ComPtr<ID3D11Resource> dx11Resource;
            Microsoft::WRL::ComPtr<ID3D12Resource> dx12Resource;

            if (texture->QueryInterface(IID_ID3D11Resource, &dx11Resource) == S_OK)
            {
                senderType = RS_FRAMETYPE_DX11_TEXTURE;
                senderData.dx11.resource = dx11Resource.Get();
            }
            else if (texture->QueryInterface(IID_ID3D12Resource, &dx12Resource) == S_OK)
            {
                senderType = RS_FRAMETYPE_DX12_TEXTURE;
                senderData.dx12.resource = dx12Resource.Get();
            }
            else
            {
                s_Logger->LogError("GetFrameImageData unknown texture type");
                return RS_ERROR::RS_ERROR_INVALID_PARAMETERS;
            }

            return m_rs_getFrameImage(m_ImageID, senderType, senderData);
        }
    };

    class EventProcessor
    {
    public:

        EventProcessor(IUnityInterfaces* unityInterfaces) :
            m_DX11Graphics(nullptr),
            m_DX12Graphics(nullptr),
            m_GetFrameImageData()
        {
            m_DX11Graphics = unityInterfaces->Get<IUnityGraphicsD3D11>();
            m_DX12Graphics = unityInterfaces->Get<IUnityGraphicsD3D12v6>();

            if (m_DX11Graphics == nullptr && m_DX12Graphics == nullptr)
            {
                s_Logger->LogError("EventProcessor: The current graphics API is not supported.");
                return;
            }
        }

        void ProcessEventAndData(int eventID, void* data)
        {
            if (eventID == (int)NativeRenderingPlugin::EventID::GET_FRAME_IMAGE)
            {
                auto srcData = reinterpret_cast<const NativeRenderingPlugin::GetFrameImageData*>(data);
                m_GetFrameImageData = *srcData;
            }
            else
            {
                s_Logger->LogError("Unsupported event ID", eventID);
            }
        }

        void ProcessCustomBlit(unsigned int command, UnityRenderingExtCustomBlitParams* data)
        {
            if (command == (int)NativeRenderingPlugin::EventID::GET_FRAME_IMAGE)
            {
                IUnknown* texture = nullptr;

                if (m_DX11Graphics != nullptr)
                {
                    texture = m_DX11Graphics->TextureFromNativeTexture(data->source);
                }
                else if (m_DX12Graphics != nullptr)
                {
                    texture = m_DX12Graphics->TextureFromNativeTexture(data->source);
                }
                else
                {
                    s_Logger->LogError("EventProcessor: The current graphics API is not supported.");
                    return;
                }

                auto result = m_GetFrameImageData.Execute(texture);
                if (result != RS_ERROR_SUCCESS)
                {
                    s_Logger->LogError("EventID::GET_FRAME_IMAGE error", result);
                }
            }
            else
            {
                s_Logger->LogError("Unsupported event ID", command);
            }
        }

    private:

        IUnityGraphicsD3D11* m_DX11Graphics;
        IUnityGraphicsD3D12v6* m_DX12Graphics;
        GetFrameImageData m_GetFrameImageData;
    };

    inline std::unique_ptr<EventProcessor> s_EventProcessor;
}
