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
                    var cpuData = new SceneStoredCPUData
                    {
                        Numeric = Numeric.GetMemory(numericParameterIndex, numericParametersFound),
                        Text = Text.GetMemory(textParameterIndex, textParametersFound)
                    };

                    var gpuData = new SceneStoredGPUData
                    {
                        Textures = Textures.GetMemory(textureParameterIndex, textureParametersFound)
                    };

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
        
        void ApplyData<TData>(IList<TData> data, Action<IRemoteParameterWrapper, TData> applyData)
        {
            if (RemoteParameters.Count != data.Count)
                throw new InvalidOperationException("Data and parameter lists size mismatch");
            
            for (var i = 0; i < RemoteParameters.Count; i++)
            {
                var remoteParameter = RemoteParameters[i];
                var element = data[i];
                applyData.Invoke(remoteParameter, element);
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
    
    /// <summary>
    /// An internal mutable representation of <see cref="SceneCPUData"/>.
    /// </summary>
    struct SceneStoredCPUData
    {
        public ReadOnlyMemory<float> Numeric;
        public ReadOnlyMemory<string> Text;

        public SceneCPUData ToReadOnly()
        {
            return new SceneCPUData(Numeric.Span, Text.Span);
        }
    }
    
    /// <summary>
    /// An internal mutable representation of <see cref="SceneGPUData"/>.
    /// </summary>
    struct SceneStoredGPUData
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
        readonly T[] m_Data;
        
        public DataBlock(int length)
        {
            m_Data = new T[length];
        }

        public Memory<T> GetMemory(int start, int length)
        {
            return new Memory<T>(m_Data, start, length);
        }

        public void SetValue(int index, T value)
        {
            m_Data[index] = value;
        }
        
        /// <summary>
        /// This supports <see cref="Unity.Collections.NativeArray{T}"/> which doesn't implement IList.
        /// </summary>
        public void SetData(IEnumerator<T> data, int dataLength)
        {
            // We can't accept a NativeArray directly because it would force T to be a value type.
            
            if (m_Data.Length != dataLength)
                throw new InvalidOperationException($"The provided {nameof(dataLength)} and the internal capacity are different.");

            for (var i = 0; data.MoveNext() && i < m_Data.Length; i++)
            {
                m_Data[i] = data.Current;
            }
        }
    }
}
