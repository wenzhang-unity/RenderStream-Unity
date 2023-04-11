using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Disguise.RenderStream
{
    static class DisguiseTextures
    {
        public static Texture2D CreateTexture(int width, int height, RSPixelFormat format, bool sRGB, string name)
        {
            Texture2D texture = null;
            
            switch (PluginEntry.instance.GraphicsDeviceType)
            {
                case GraphicsDeviceType.Direct3D11:
                    texture = new Texture2D(width, height, PluginEntry.ToGraphicsFormat(format, sRGB), 1, TextureCreationFlags.None);
                    break;
                
                case GraphicsDeviceType.Direct3D12:
                    RS_ERROR error = PluginEntry.instance.useDX12SharedHeapFlag(out var heapFlag);
                    if (error != RS_ERROR.RS_ERROR_SUCCESS)
                        Debug.LogError(string.Format("Error checking shared heap flag: {0}", error));

                    if (heapFlag == UseDX12SharedHeapFlag.RS_DX12_USE_SHARED_HEAP_FLAG)
                    {
                        var nativeTex = NativeRenderingPlugin.CreateNativeTexture(name, width, height, format, sRGB);
                        texture = Texture2D.CreateExternalTexture(width, height, PluginEntry.ToTextureFormat(format), false, !sRGB, nativeTex);
                        
                        break;
                    }
                    else
                    {
                        texture = new Texture2D(width, height, PluginEntry.ToGraphicsFormat(format, sRGB), 1, TextureCreationFlags.None);
                        break;
                    }
            }

            if (texture != null)
            {
                texture.hideFlags = HideFlags.HideAndDontSave;
                texture.name = name;
            }
            
            return texture;
        }
        
        public static void ConvertDisguiseTexture(Texture src, RenderTexture dst, CommandBuffer cmd)
        {
            var flipY = SystemInfo.graphicsUVStartsAtTop;
            var scale = flipY
                ? new Vector2(1f, -1f)
                : Vector2.one;
            var offset = flipY
                ? new Vector2(0f, 1f)
                : Vector2.zero;
            
            cmd.Blit(src, dst, scale, offset);
            cmd.IncrementUpdateCount(dst);
        }

        public static bool RenderTextureMatchesTexture(RenderTexture rt, Texture texture)
        {
            return
                rt.width == texture.width &&
                rt.height == texture.height &&
                rt.graphicsFormat == texture.graphicsFormat;
        }
        
        public static bool SameImageFrameDataProperties(ImageFrameData lhs, ImageFrameData rhs)
        {
            return
                lhs.width == rhs.width &&
                lhs.height == rhs.height &&
                lhs.format == rhs.format;
        }
        
        public static bool IsNewImageFrameData(ImageFrameData lhs, ImageFrameData rhs)
        {
            return
                lhs.imageId != rhs.imageId ||
                !SameImageFrameDataProperties(lhs, rhs);
        }
    }
}
