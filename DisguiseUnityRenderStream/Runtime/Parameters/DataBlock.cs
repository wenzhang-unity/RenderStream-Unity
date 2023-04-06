using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Disguise.RenderStream.Parameters
{
    /// <summary>
    /// Contains remote parameters and their data for a scene.
    /// </summary>
    class SceneData
    {
        /// <summary>
        /// Remote parameter wrappers in their order of declaration in the schema.
        /// </summary>
        List<IRemoteParameterWrapper> RemoteParameters;
        
        /// <summary>
        /// CPU data buffers for each remote parameter wrapper in <see cref="RemoteParameters"/>.
        /// </summary>
        readonly List<SceneStoredCPUData> CPUData = new List<SceneStoredCPUData>();
        
        /// <summary>
        /// GPU data buffers for each remote parameter wrapper in <see cref="RemoteParameters"/>.
        /// </summary>
        readonly List<SceneStoredGPUData> GPUData = new List<SceneStoredGPUData>();

        /// <summary>
        /// Data from parameters of type <see cref="RemoteParameterType.RS_PARAMETER_NUMBER"/>,
        /// <see cref="RemoteParameterType.RS_PARAMETER_POSE"/>, and
        /// <see cref="RemoteParameterType.RS_PARAMETER_TRANSFORM"/>
        /// in their order of declaration in the schema.
        /// </summary>
        public DataBlock<float> Numeric;
        
        /// <summary>
        /// Data from parameters of type <see cref="RemoteParameterType.RS_PARAMETER_TEXT"/>
        /// in their order of declaration in the schema.
        /// </summary>
        public DataBlock<string> Text;
        
        /// <summary>
        /// Data from parameters of type <see cref="RemoteParameterType.RS_PARAMETER_IMAGE"/>
        /// in their order of declaration in the schema.
        /// </summary>
        public DataBlock<Texture> Textures;
        
        /// <summary>
        /// Configures remote parameters and their data buffers for the scene.
        /// </summary>
        public void AssignRemoteParameters(List<(IRemoteParameterWrapper parameter, int parameterID)> remoteParameters, ManagedRemoteParameters sceneSchema)
        {
            RemoteParameters = remoteParameters.Select(x => x.parameter).ToList();
            var remoteParameterIDs = remoteParameters.Select(x => x.parameterID).ToList();
            CPUData.Clear();
            GPUData.Clear();
            
            var numNumericalParameters = 0;
            var numTextParameters = 0;
            var numTextureParameters = 0;
            
            foreach (var parameter in sceneSchema.parameters)
            {
                IncrementByParameterSize(parameter.type, ref numNumericalParameters, ref numTextParameters, ref numTextureParameters);
            }

            Numeric = new DataBlock<float>(numNumericalParameters);
            Text = new DataBlock<string>(numTextParameters);
            Textures = new DataBlock<Texture>(numTextureParameters);

            var parameterWrapperIndex = 0;
            
            var numericParameterIndex = 0;
            var textParameterIndex = 0;
            var textureParameterIndex = 0;

            var numericParametersFound = 0;
            var textParametersFound = 0;
            var textureParametersFound = 0;

            // Group one or more schema parameters under their associated remote parameter wrapper by ID.
            // In other words, it "unflattens" the flat DataBlock<T> lists that we receive from Disguise.
            for (var schemaParameterIndex = 0; schemaParameterIndex <= sceneSchema.parameters.Length; schemaParameterIndex++)
            {
                bool consumeParameterSpan;

                if (schemaParameterIndex < sceneSchema.parameters.Length)
                {
                    var schemaParameter = sceneSchema.parameters[schemaParameterIndex];

                    var parameterWrapperID = remoteParameterIDs[parameterWrapperIndex];
                    var schemaParameterID = Parameter.ResolveIDFromSchema(schemaParameter);

                    consumeParameterSpan = schemaParameterID != parameterWrapperID;
                }
                else
                {
                    consumeParameterSpan = true;
                }

                if (consumeParameterSpan)
                {
                    var cpuData = new SceneStoredCPUData();
                    cpuData.SignalChange();
                    cpuData.Numeric = Numeric.GetMemory(numericParameterIndex, numericParametersFound, cpuData);
                    cpuData.Text = Text.GetMemory(textParameterIndex, textParametersFound, cpuData);

                    var gpuData = new SceneStoredGPUData();
                    gpuData.SignalChange();
                    gpuData.Textures = Textures.GetMemory(textureParameterIndex, textureParametersFound, gpuData);

                    CPUData.Add(cpuData);
                    GPUData.Add(gpuData);

                    parameterWrapperIndex++;

                    numericParameterIndex += numericParametersFound;
                    textParameterIndex += textParametersFound;
                    textureParameterIndex += textureParametersFound;

                    numericParametersFound = 0;
                    textParametersFound = 0;
                    textureParametersFound = 0;
                }

                if (schemaParameterIndex < sceneSchema.parameters.Length)
                {
                    var schemaParameter = sceneSchema.parameters[schemaParameterIndex];
                    
                    IncrementByParameterSize(
                        schemaParameter.type,
                        ref numericParametersFound,
                        ref textParametersFound,
                        ref textureParametersFound);
                }
            }
        }

        /// <summary>
        /// Applies CPU data onto the scene's remote parameters.
        /// </summary>
        public void ApplyCPUData()
        {
            ApplyData(CPUData, (parameter, data) => parameter.ApplyData(data.ToReadOnly()));
        }
        
        /// <summary>
        /// Applies GPU data onto the scene's remote parameters.
        /// </summary>
        public void ApplyGPUData()
        {
            ApplyData(GPUData, (parameter, data) => parameter.ApplyData(data.ToReadOnly()));
        }
        
        void ApplyData<TData>(IList<TData> data, Action<IRemoteParameterWrapper, TData> applyData) where TData : ChangeTracker
        {
            if (RemoteParameters.Count != data.Count)
                throw new InvalidOperationException("Data and parameter lists size mismatch");
            
            for (var i = 0; i < RemoteParameters.Count; i++)
            {
                var element = data[i];
                if (element.HasChange)
                {
                    var remoteParameter = RemoteParameters[i];
                    applyData.Invoke(remoteParameter, element);
                    element.ConsumeChange();
                }
            }
        }

        void IncrementByParameterSize(RemoteParameterType parameterType, ref int numNumericalParameters, ref int numTextParameters, ref int numTextureParameters)
        {
            switch (parameterType)
            {
                case RemoteParameterType.RS_PARAMETER_NUMBER:
                    numNumericalParameters++;
                    break;
                case RemoteParameterType.RS_PARAMETER_POSE:
                case RemoteParameterType.RS_PARAMETER_TRANSFORM:
                    numNumericalParameters += 16;
                    break;
                case RemoteParameterType.RS_PARAMETER_TEXT:
                    numTextParameters++;
                    break;
                case RemoteParameterType.RS_PARAMETER_IMAGE:
                    numTextureParameters++;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }

    abstract class ChangeTracker
    {
        bool m_ChangeFlag;

        public bool HasChange => m_ChangeFlag;
        
        public void SignalChange()
        {
            m_ChangeFlag = true;
        }

        public void ConsumeChange()
        {
            m_ChangeFlag = false;
        }
    }
    
    /// <summary>
    /// An internal heap-friendly representation of <see cref="SceneCPUData"/>.
    /// Contains a span of relevant <see cref="DataBlock{T}"/> for an <see cref="IRemoteParameterWrapper"/>.
    /// </summary>
    class SceneStoredCPUData : ChangeTracker
    {
        public ReadOnlyMemory<float> Numeric;
        public ReadOnlyMemory<string> Text;

        public SceneCPUData ToReadOnly()
        {
            return new SceneCPUData(Numeric.Span, Text.Span);
        }
    }
    
    /// <summary>
    /// An internal heap-friendly representation of <see cref="SceneGPUData"/>.
    /// Contains a span of relevant <see cref="DataBlock{T}"/> for an <see cref="IRemoteParameterWrapper"/>.
    /// </summary>
    class SceneStoredGPUData : ChangeTracker
    {
        public ReadOnlyMemory<Texture> Textures;
        
        public SceneGPUData ToReadOnly()
        {
            return new SceneGPUData(Textures.Span);
        }
    }
    
    /// <summary>
    /// CPU data received from Disguise for a remote parameter.
    /// </summary>
    ref struct SceneCPUData
    {
        public readonly ReadOnlySpan<float> Numeric;
        public readonly ReadOnlySpan<string> Text;

        public SceneCPUData(ReadOnlySpan<float> numeric, ReadOnlySpan<string> text)
        {
            Numeric = numeric;
            Text = text;
        }
    }
    
    /// <summary>
    /// GPU data received from Disguise for a remote parameter.
    /// </summary>
    ref struct SceneGPUData
    {
        public readonly ReadOnlySpan<Texture> Textures;
        
        public SceneGPUData(ReadOnlySpan<Texture> textures)
        {
            Textures = textures;
        }
    }
    
    /// <summary>
    /// Represents a flat list of data received from Disguise.
    /// </summary>
    class DataBlock<T>
    {
        /// <summary>
        /// Holds a flat list of data received from Disguise.
        /// </summary>
        readonly T[] m_Data;
        
        /// <summary>
        /// The <see cref="ChangeTracker"/>s associated to each element in <see cref="m_Data"/>.
        /// </summary>
        readonly ChangeTracker[] m_ChangeTrackers;
        
        public DataBlock(int length)
        {
            m_Data = new T[length];
            m_ChangeTrackers = new ChangeTracker[length];
        }

        public Memory<T> GetMemory(int start, int length, ChangeTracker changeTracker)
        {
            for (var i = start; i < start + length; i++)
            {
                if (m_ChangeTrackers[i] != null)
                    throw new InvalidOperationException($"Overlapping remote parameter mappings at element {i}");

                m_ChangeTrackers[i] = changeTracker;
            }
            
            return new Memory<T>(m_Data, start, length);
        }

        public void SetValue(int index, T value)
        {
            if (!m_Data[index].Equals(value))
                SignalChange(index);
            
            m_Data[index] = value;
        }
        
        /// <summary>
        /// This supports <see cref="Unity.Collections.NativeArray{T}"/> which doesn't implement IList.
        /// </summary>
        public void SetData(IEnumerator<T> data, int dataLength)
        {
            // We can't accept a NativeArray directly because it would force T to be a value type.
            
            if (m_Data.Length != dataLength)
                throw new InvalidOperationException($"The provided {nameof(dataLength)} ({dataLength}) and the internal capacity ({m_Data.Length}) do not match.");

            for (var i = 0; data.MoveNext() && i < m_Data.Length; i++)
            {
                var newData = data.Current;
                
                if (!m_Data[i].Equals(newData))
                    SignalChange(i);
                
                m_Data[i] = newData;
            }
        }

        void SignalChange(int index)
        {
            var changeTracker = m_ChangeTrackers[index];
            
            if (changeTracker == null)
                throw new InvalidOperationException($"No change tracker registered for data element {index}");
            
            changeTracker.SignalChange();
        }
    }
}
