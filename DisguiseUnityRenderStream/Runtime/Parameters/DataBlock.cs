using System;
using System.Collections.Generic;
using UnityEngine;

namespace Disguise.RenderStream.Parameters
{
    class SceneCPUData
    {
        public readonly DataBlock<float> Numeric = new DataBlock<float>();
        public readonly DataBlock<string> Text = new DataBlock<string>();
    }
    
    class SceneGPUData
    {
        public DataBlock<Texture> Textures;
    }
    
    /// <summary>
    /// Represents a flat list of data received from Disguise.
    /// </summary>
    class DataBlock<T>
    {
        T[] m_Data = Array.Empty<T>();
        int m_Index;

        public void Reserve(int length)
        {
            m_Index = 0;
            
            if (m_Data.Length != length)
                Array.Resize(ref m_Data, length);
        }

        public void SetValue(int index, T value)
        {
            m_Index = 0;
            
            m_Data[index] = value;
        }
        
        /// <summary>
        /// This supports <see cref="Unity.Collections.NativeArray{T}"/> which doesn't implement IList.
        /// </summary>
        public void SetData(IEnumerator<T> data, int dataLength)
        {
            // We can't accept a NativeArray directly because it would force T to be a value type.
            
            m_Index = 0;
            
            if (m_Data.Length != dataLength)
                Array.Resize(ref m_Data, dataLength);

            for (var i = 0; data.MoveNext() && i < m_Data.Length; i++)
            {
                m_Data[i] = data.Current;
            }
        }

        public T GetNext()
        {
            if (m_Index >= m_Data.Length)
            {
                throw new IndexOutOfRangeException();
            }
            
            return m_Data[m_Index++];
        }
    }
}
