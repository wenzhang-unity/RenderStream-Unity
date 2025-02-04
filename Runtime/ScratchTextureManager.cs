using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Disguise.RenderStream
{
    /// <summary>
    /// A simpler version of <see cref="Disguise.RenderStream.TemporaryTexture2DManager"/> that doesn't need to manage texture lifetime.
    /// </summary>
    abstract class ScratchTextureManager<TTexture>
    {
        readonly Dictionary<Texture2DDescriptor, TTexture> m_Items = new Dictionary<Texture2DDescriptor, TTexture>();

        protected string m_Name;
        
        public void Clear()
        {
            DebugLog($"Cleared textures");

            foreach (var texture in m_Items.Values)
            {
                DestroyTexture(texture);
            }
            
            m_Items.Clear();
        }
        
        public TTexture Get(Texture2DDescriptor descriptor)
        {
            if (m_Items.TryGetValue(descriptor, out var item))
            {
                return item;
            }

            DebugLog($"Created texture: {descriptor}");
            
            var texture = CreateTexture(descriptor);
            m_Items.Add(descriptor, texture);
            return texture;
        }

        protected abstract TTexture CreateTexture(Texture2DDescriptor descriptor);
        
        protected abstract void DestroyTexture(TTexture texture);
        
        [Conditional("DISGUISE_VERBOSE_LOGGING")]
        void DebugLog(string message)
        {
            Debug.Log($"{m_Name}: {message}");
        }
    }
    
    class ScratchTexture2DManager : ScratchTextureManager<Texture2D>
    {
        static ScratchTexture2DManager s_Instance;
        
        public static ScratchTexture2DManager Instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new ScratchTexture2DManager();
                return s_Instance;
            }
        }
        
        ScratchTexture2DManager()
        {
            m_Name = nameof(ScratchTexture2DManager);
        }
        
        protected override Texture2D CreateTexture(Texture2DDescriptor descriptor)
        {
            return DisguiseTextures.CreateTexture(descriptor.Width, descriptor.Height, descriptor.Format, !descriptor.Linear, null);
        }
        
        protected override void DestroyTexture(Texture2D texture)
        {
#if UNITY_EDITOR
            UnityEngine.Object.DestroyImmediate(texture);
#else
            UnityEngine.Object.Destroy(texture);
#endif
        }
    }
}
