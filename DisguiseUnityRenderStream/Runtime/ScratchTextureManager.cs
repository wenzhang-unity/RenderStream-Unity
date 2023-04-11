using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Disguise.RenderStream
{
    struct Texture2DDescriptor : IEquatable<Texture2DDescriptor>
    {
        public int Width;
        public int Height;
        public RSPixelFormat Format;
        public bool Linear;

        /// <summary>
        /// Disguise uses a black 1x1 placeholder texture.
        /// It's bound initially and during input parameter swapping.
        /// We treat it as a persistent texture.
        /// </summary>
        public bool IsPlaceholderTexture => Width == 1 && Height == 1;
        
        public override bool Equals(object obj)
        {
            return obj is Texture2DDescriptor other && Equals(other);
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine
            (
                Width,
                Height,
                (int)Format,
                Linear ? 1 : 0
            );
        }

        public bool Equals(Texture2DDescriptor other)
        {
            return
                Width == other.Width &&
                Height == other.Height &&
                Format == other.Format &&
                Linear == other.Linear;
        }
            
        public static bool operator ==(Texture2DDescriptor lhs, Texture2DDescriptor rhs) => lhs.Equals(rhs);

        public static bool operator !=(Texture2DDescriptor lhs, Texture2DDescriptor rhs) => !(lhs == rhs);

        public override string ToString()
        {
            return $"{Width}x{Height} Format {Format} {(Linear ? "Linear" : "SRGB")}";
        }
    }
    
    /// <summary>
    /// Provides texture re-use across a frame.
    /// Similar to <see cref="RenderTexture.GetTemporary(RenderTextureDescriptor)"/> but the texture lifetime is indefinite.
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
