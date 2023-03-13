using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Disguise.RenderStream
{
    /// <summary>
    /// Describes the desired final color space of the camera capture
    /// </summary>
    enum ColorSpace
    {
        Linear, // sRGB color primaries + linear transfer function
        sRGB    // sRGB color primaries + sRGB transfer function
    }
    
    [Serializable]
    struct CameraCaptureDescription : IEquatable<CameraCaptureDescription>
    {
        public static CameraCaptureDescription Default = new CameraCaptureDescription()
        {
            m_colorSpace = ColorSpace.sRGB,
            m_autoFlipY = true,
            m_width = 0,
            m_height = 0,
            m_colorFormat = GraphicsFormat.R8G8B8A8_SRGB,
            m_msaaSamples = 1,
            m_depthBufferBits = 24,
            m_copyDepth = false,
            m_depthCopyFormat = GraphicsFormat.R32_SFloat,
            m_depthCopyMode = DepthCopy.Mode.Linear01
        };

        /// <summary>
        /// The desired color space of the final output.
        /// </summary>
        public ColorSpace m_colorSpace;
        
        /// <summary>
        /// Unity unifies UV coordinates across graphics APIs. This can result in RenderTextures looking flipped
        /// to third-party applications in DirectX for example.
        /// When true, this setting will ensure correct UV orientation for third-party applications.
        /// </summary>
        public bool m_autoFlipY;
        
        /// <summary>
        /// The width of the camera render target in pixels.
        /// </summary>
        public int m_width;
        
        /// <summary>
        /// The height of the camera render target in pixels.
        /// </summary>
        public int m_height;
        public GraphicsFormat m_colorFormat;
        
        /// <summary>
        /// The number of MSAA samples for the camera output.
        /// <remarks>The final captured texture will be resolved to a non-MSAA texture.</remarks>
        /// </summary>
        public int m_msaaSamples;
        
        /// <summary>
        /// TODO doc
        /// <remarks>TODO doc</remarks>
        /// </summary>
        public int m_depthBufferBits;
        
        /// <summary>
        /// TODO doc
        /// </summary>
        public bool m_copyDepth;
        
        /// <summary>
        /// TODO doc
        /// </summary>
        public GraphicsFormat m_depthCopyFormat;
        
        /// <summary>
        /// Sets the encoding of depth in the normalized 0-1 range.
        /// For details on how to decode the captured depth texture in a third-party application, see <see cref="DepthCopy.Mode"/>.
        /// </summary>
        public DepthCopy.Mode m_depthCopyMode;

        public bool IsValid(out string message)
        {
            if (m_width == 0)
            {
                message = "Width is 0";
                return false;
            }
            
            if (m_height == 0)
            {
                message = "Height is 0";
                return false;
            }

            if (m_colorSpace == ColorSpace.Linear && GraphicsFormatUtility.IsSRGBFormat(m_colorFormat))
            {
                message = "The combination of linear color space and SRGB color format is not supported";
                return false;
            }

            message = null;
            return true;
        }
        
        /// <summary>
        /// When true, the final capture is a processed version of the <see cref="Camera.targetTexture"/>.
        /// This depends on <see cref="m_colorSpace"/> and <see cref="m_autoFlipY"/>.
        /// </summary>
        public bool NeedsBlit => NeedsFlipY || NeedsSoftwareSRGBConversion;

        /// <summary>
        /// When true, the final capture has flipped Y UV coordinates relative to <see cref="Camera.targetTexture"/>
        /// depending on <see cref="m_autoFlipY"/> and the current graphics API.
        /// </summary>
        public bool NeedsFlipY => m_autoFlipY && SystemInfo.graphicsUVStartsAtTop;

        /// <summary>
        /// We leverage the hardware linear <-> sRGB automatic conversion when possible.
        /// No hardware sRGB texture format is available for float textures for example.
        /// </summary>
        public bool NeedsSoftwareSRGBConversion => m_colorSpace == ColorSpace.sRGB && !GraphicsFormatUtility.IsSRGBFormat(m_colorFormat);
        
        public bool CameraTextureIsMSAA => m_msaaSamples > 1;

        /// <summary>
        /// Describes the texture to use for <see cref="Camera.targetTexture"/>.
        /// </summary>
        public RenderTextureDescriptor GetCameraDescriptor()
        {
            var descriptor = new RenderTextureDescriptor(m_width, m_height, m_colorFormat, m_depthBufferBits, 1);
            descriptor.msaaSamples = m_msaaSamples;
            descriptor.sRGB = m_colorSpace == ColorSpace.sRGB && !NeedsSoftwareSRGBConversion;
            
            return descriptor;
        }
        
        /// <summary>
        /// Describes the texture to contain the flipped and/or color space converted version of the <see cref="Camera.targetTexture"/>.
        /// </summary>
        public RenderTextureDescriptor GetCameraBlitDescriptor()
        {
            var descriptor = GetCameraDescriptor();
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;
            
            return descriptor;
        }

        /// <summary>
        /// Describes the texture to use for storing the depth capture.
        /// </summary>
        public RenderTextureDescriptor GetDepthCopyDescriptor()
        {
            return new RenderTextureDescriptor(m_width, m_height, m_depthCopyFormat, 0, 1);
        }
        
        public override bool Equals(object obj)
        {
            return obj is CameraCaptureDescription other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)m_colorSpace;
                hashCode = (hashCode * 397) ^ (m_autoFlipY ? 1 : 0);
                hashCode = (hashCode * 397) ^ m_width;
                hashCode = (hashCode * 397) ^ m_height;
                hashCode = (hashCode * 397) ^ (int)m_colorFormat;
                hashCode = (hashCode * 397) ^ m_msaaSamples;
                hashCode = (hashCode * 397) ^ m_depthBufferBits;
                hashCode = (hashCode * 397) ^ (m_copyDepth ? 1 : 0);
                hashCode = (hashCode * 397) ^ (int)m_depthCopyFormat;
                hashCode = (hashCode * 397) ^ (int)m_depthCopyMode;
                return hashCode;
            }
        }

        public bool Equals(CameraCaptureDescription other)
        {
            return
                m_colorSpace == other.m_colorSpace &&
                m_autoFlipY == other.m_autoFlipY &&
                m_width == other.m_width &&
                m_height == other.m_height &&
                m_colorFormat == other.m_colorFormat &&
                m_msaaSamples == other.m_msaaSamples &&
                m_depthBufferBits == other.m_depthBufferBits &&
                m_copyDepth == other.m_copyDepth &&
                m_depthCopyFormat == other.m_depthCopyFormat &&
                m_depthCopyMode == other.m_depthCopyMode;
        }
        
        public static bool operator ==(CameraCaptureDescription lhs, CameraCaptureDescription rhs) => lhs.Equals(rhs);

        public static bool operator !=(CameraCaptureDescription lhs, CameraCaptureDescription rhs) => !(lhs == rhs);
    }
}