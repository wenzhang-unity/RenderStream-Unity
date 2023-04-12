using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Disguise.RenderStream.Parameters
{
    /// <summary>
    /// Common suffixes for multi-part parameters.
    /// </summary>
    static class Suffix
    {
        public const string X = "x";
        public const string Y = "y";
        public const string Z = "z";
        public const string W = "w";
        
        public const string R = "r";
        public const string G = "g";
        public const string B = "b";
        public const string A = "a";
    }
    
    /// <summary>
    /// Common range values.
    /// </summary>
    static class MinMax
    {
        public const float IntMin = -1000f;
        public const float IntMax = +1000f;
        
        public const float FloatMin = -1f;
        public const float FloatMax = +1f;
        
        public const float QuaternionMin = -360f;
        public const float QuaternionMax = +360f;
    }
    
    /// <summary>
    /// Utility class for single-component numeric types.
    /// </summary>
    /// <typeparam name="T">A single-component numeric type</typeparam>
    abstract class NumericRemoteParameterWrapper<T> : RemoteParameterWrapper<T>
    {
        public float Min;
        public float Max;

        public override void ApplyData(SceneGPUData data)
        {
            
        }
        
#if UNITY_EDITOR
        public override IList<DisguiseRemoteParameter> GetParametersForSchema()
        {
            return new[]
            {
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, GetValue(), Min, Max)
            };
        }
#endif
    }
    
    [RemoteParameterWrapper(typeof(sbyte))]
    class SByteRemoteParameterWrapper : NumericRemoteParameterWrapper<sbyte>
    {
        public SByteRemoteParameterWrapper()
        {
            Min = Mathf.Max(sbyte.MinValue, MinMax.IntMin);
            Max = Mathf.Min(sbyte.MaxValue, MinMax.IntMax);
        }
        
        public override void ApplyData(SceneCPUData data)
        {
            SetValue(Convert.ToSByte(data.Numeric[0]));
        }
    }
    
    [RemoteParameterWrapper(typeof(byte))]
    class ByteRemoteParameterWrapper : NumericRemoteParameterWrapper<byte>
    {
        public ByteRemoteParameterWrapper()
        {
            Min = Mathf.Max(byte.MinValue, MinMax.IntMin);
            Max = Mathf.Min(byte.MaxValue, MinMax.IntMax);
        }
        
        public override void ApplyData(SceneCPUData data)
        {
            SetValue(Convert.ToByte(data.Numeric[0]));
        }
    }
    
    [RemoteParameterWrapper(typeof(short))]
    class ShortRemoteParameterWrapper : NumericRemoteParameterWrapper<short>
    {
        public ShortRemoteParameterWrapper()
        {
            Min = Mathf.Max(short.MinValue, MinMax.IntMin);
            Max = Mathf.Min(short.MaxValue, MinMax.IntMax);
        }
        
        public override void ApplyData(SceneCPUData data)
        {
            SetValue(Convert.ToInt16(data.Numeric[0]));
        }
    }
    
    [RemoteParameterWrapper(typeof(ushort))]
    class UShortRemoteParameterWrapper : NumericRemoteParameterWrapper<ushort>
    {
        public UShortRemoteParameterWrapper()
        {
            Min = Mathf.Max(ushort.MinValue, MinMax.IntMin);
            Max = Mathf.Min(ushort.MaxValue, MinMax.IntMax);
        }
        
        public override void ApplyData(SceneCPUData data)
        {
            SetValue(Convert.ToUInt16(data.Numeric[0]));
        }
    }
    
    [RemoteParameterWrapper(typeof(int))]
    class IntRemoteParameterWrapper : NumericRemoteParameterWrapper<int>
    {
        public IntRemoteParameterWrapper()
        {
            Min = Mathf.Max(int.MinValue, MinMax.IntMin);
            Max = Mathf.Min(int.MaxValue, MinMax.IntMax);
        }
        
        public override void ApplyData(SceneCPUData data)
        {
            SetValue(Convert.ToInt32(data.Numeric[0]));
        }
    }
    
    [RemoteParameterWrapper(typeof(uint))]
    class UIntRemoteParameterWrapper : NumericRemoteParameterWrapper<uint>
    {
        public UIntRemoteParameterWrapper()
        {
            Min = Mathf.Max(uint.MinValue, MinMax.IntMin);
            Max = Mathf.Min(uint.MaxValue, MinMax.IntMax);
        }
        
        public override void ApplyData(SceneCPUData data)
        {
            SetValue(Convert.ToUInt32(data.Numeric[0]));
        }
    }
    
    [RemoteParameterWrapper(typeof(float))]
    class FloatRemoteParameterWrapper : NumericRemoteParameterWrapper<float>
    {
        public FloatRemoteParameterWrapper()
        {
            Min = MinMax.FloatMin;
            Max = MinMax.FloatMax;
        }
        
        public override void ApplyData(SceneCPUData data)
        {
            SetValue(data.Numeric[0]);
        }
    }
    
    [RemoteParameterWrapper(typeof(Vector2Int))]
    class Vector2IntRemoteParameterWrapper : RemoteParameterWrapper<Vector2Int>
    {
        public float Min = MinMax.IntMin;
        public float Max = MinMax.IntMax;
        
        public override void ApplyData(SceneCPUData data)
        {
            var value = new Vector2Int(
                Convert.ToInt32(data.Numeric[0]),
                Convert.ToInt32(data.Numeric[1])
            );
            SetValue(value);
        }

        public override void ApplyData(SceneGPUData data)
        {
            
        }
        
#if UNITY_EDITOR
        public override IList<DisguiseRemoteParameter> GetParametersForSchema()
        {
            var value = GetValue();
            
            return new[]
            {
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, value.x, Min, Max, null, Suffix.X),
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, value.y, Min, Max, null, Suffix.Y)
            };
        }
#endif
    }
    
    [RemoteParameterWrapper(typeof(Vector3Int))]
    class Vector3IntRemoteParameterWrapper : RemoteParameterWrapper<Vector3Int>
    {
        public float Min = MinMax.IntMin;
        public float Max = MinMax.IntMax;
        
        public override void ApplyData(SceneCPUData data)
        {
            var value = new Vector3Int(
                Convert.ToInt32(data.Numeric[0]),
                Convert.ToInt32(data.Numeric[1]),
                Convert.ToInt32(data.Numeric[2])
            );
            SetValue(value);
        }

        public override void ApplyData(SceneGPUData data)
        {
            
        }
        
#if UNITY_EDITOR
        public override IList<DisguiseRemoteParameter> GetParametersForSchema()
        {
            var value = GetValue();
            
            return new[]
            {
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, value.x, Min, Max, null, Suffix.X),
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, value.y, Min, Max, null, Suffix.Y),
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, value.z, Min, Max, null, Suffix.Z)
            };
        }
#endif
    }
    
    [RemoteParameterWrapper(typeof(Vector2))]
    class Vector2RemoteParameterWrapper : RemoteParameterWrapper<Vector2>
    {
        public float Min = MinMax.FloatMin;
        public float Max = MinMax.FloatMax;
        
        public override void ApplyData(SceneCPUData data)
        {
            var value = new Vector2(
                data.Numeric[0],
                data.Numeric[1]
            );
            SetValue(value);
        }

        public override void ApplyData(SceneGPUData data)
        {
            
        }
        
#if UNITY_EDITOR
        public override IList<DisguiseRemoteParameter> GetParametersForSchema()
        {
            var value = GetValue();
            
            return new[]
            {
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, value.x, Min, Max, null, Suffix.X),
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, value.y, Min, Max, null, Suffix.Y)
            };
        }
#endif
    }
    
    [RemoteParameterWrapper(typeof(Vector3))]
    class Vector3RemoteParameterWrapper : RemoteParameterWrapper<Vector3>
    {
        public float Min = MinMax.FloatMin;
        public float Max = MinMax.FloatMax;
        
        public override void ApplyData(SceneCPUData data)
        {
            var value = new Vector3(
                data.Numeric[0],
                data.Numeric[1],
                data.Numeric[2]
            );
            SetValue(value);
        }

        public override void ApplyData(SceneGPUData data)
        {
            
        }
        
#if UNITY_EDITOR
        public override IList<DisguiseRemoteParameter> GetParametersForSchema()
        {
            var value = GetValue();
            
            return new[]
            {
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, value.x, Min, Max, null, Suffix.X),
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, value.y, Min, Max, null, Suffix.Y),
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, value.z, Min, Max, null, Suffix.Z)
            };
        }
#endif
    }
    
    [RemoteParameterWrapper(typeof(Vector4))]
    class Vector4RemoteParameterWrapper : RemoteParameterWrapper<Vector4>
    {
        public float Min = MinMax.FloatMin;
        public float Max = MinMax.FloatMax;
        
        public override void ApplyData(SceneCPUData data)
        {
            var value = new Vector4(
                data.Numeric[0],
                data.Numeric[1],
                data.Numeric[2],
                data.Numeric[3]
            );
            SetValue(value);
        }

        public override void ApplyData(SceneGPUData data)
        {
            
        }
        
#if UNITY_EDITOR
        public override IList<DisguiseRemoteParameter> GetParametersForSchema()
        {
            var value = GetValue();
            
            return new[]
            {
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, value.x, Min, Max, null, Suffix.X),
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, value.y, Min, Max, null, Suffix.Y),
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, value.z, Min, Max, null, Suffix.Z),
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, value.w, Min, Max, null, Suffix.W)
            };
        }
#endif
    }
    
    [RemoteParameterWrapper(typeof(Color))]
    class ColorRemoteParameterWrapper : RemoteParameterWrapper<Color>
    {
        public override void ApplyData(SceneCPUData data)
        {
            var value = new Color(
                data.Numeric[0],
                data.Numeric[1],
                data.Numeric[2],
                data.Numeric[3]
            );
            SetValue(value);
        }

        public override void ApplyData(SceneGPUData data)
        {
            
        }
        
#if UNITY_EDITOR
        public override IList<DisguiseRemoteParameter> GetParametersForSchema()
        {
            var value = GetValue();
            
            return new[]
            {
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, value.r, 0f, 1f, null, Suffix.R),
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, value.g, 0f, 1f, null, Suffix.G),
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, value.b, 0f, 1f, null, Suffix.B),
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, value.a, 0f, 1f, null, Suffix.A)
            };
        }
#endif
    }
    
    [RemoteParameterWrapper(typeof(Color32))]
    class Color32RemoteParameterWrapper : RemoteParameterWrapper<Color32>
    {
        public override void ApplyData(SceneCPUData data)
        {
            var value = new Color32(
                Convert.ToByte(data.Numeric[0] * 255f),
                Convert.ToByte(data.Numeric[1] * 255f),
                Convert.ToByte(data.Numeric[2] * 255f),
                Convert.ToByte(data.Numeric[3] * 255f)
            );
            SetValue(value);
        }

        public override void ApplyData(SceneGPUData data)
        {
            
        }
        
#if UNITY_EDITOR
        public override IList<DisguiseRemoteParameter> GetParametersForSchema()
        {
            var value = GetValue();
            
            return new[]
            {
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, value.r / 255f, 0f, 1f, null, Suffix.R),
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, value.g / 255f, 0f, 1f, null, Suffix.G),
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, value.b / 255f, 0f, 1f, null, Suffix.B),
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, value.a / 255f, 0f, 1f, null, Suffix.A)
            };
        }
#endif
    }
    
    [RemoteParameterWrapper(typeof(Quaternion))]
    class QuaternionRemoteParameterWrapper : RemoteParameterWrapper<Quaternion>
    {
        public override void ApplyData(SceneCPUData data)
        {
            var euler = new Vector3(
                data.Numeric[0],
                data.Numeric[1],
                data.Numeric[2]
            );
            var quaternion = Quaternion.Euler(euler);
            SetValue(quaternion);
        }

        public override void ApplyData(SceneGPUData data)
        {
            
        }
        
#if UNITY_EDITOR
        public override IList<DisguiseRemoteParameter> GetParametersForSchema()
        {
            var value = GetValue().eulerAngles;
            
            return new[]
            {
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, value.x, MinMax.QuaternionMin, MinMax.QuaternionMax, null, Suffix.X),
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, value.y, MinMax.QuaternionMin, MinMax.QuaternionMax, null, Suffix.Y),
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, value.z, MinMax.QuaternionMin, MinMax.QuaternionMax, null, Suffix.Z)
            };
        }
#endif
    }
    
    [RemoteParameterWrapper(typeof(Matrix4x4))]
    class Matrix4x4RemoteParameterWrapper : RemoteParameterWrapper<Matrix4x4>
    {
        public static Matrix4x4 ParseData(ReadOnlySpan<float> data)
        {
            var matrix = new Matrix4x4();
            matrix.SetColumn(0, new Vector4(data[ 0], data[ 1], data[ 2], data[ 3]));
            matrix.SetColumn(1, new Vector4(data[ 4], data[ 5], data[ 6], data[ 7]));
            matrix.SetColumn(2, new Vector4(data[ 8], data[ 9], data[10], data[11]));
            matrix.SetColumn(3, new Vector4(data[12], data[13], data[14], data[15]));
            return matrix;
        }
        
        public override void ApplyData(SceneCPUData data)
        {
            var matrix = ParseData(data.Numeric);
            SetValue(matrix);
        }

        public override void ApplyData(SceneGPUData data)
        {
            
        }
        
#if UNITY_EDITOR
        public override IList<DisguiseRemoteParameter> GetParametersForSchema()
        {
            return new[]
            {
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_TRANSFORM, 0f, 0f, 255f),
            };
        }
#endif
    }
    
    [RemoteParameterWrapper(typeof(string))]
    class StringRemoteParameterWrapper : RemoteParameterWrapper<string>
    {
        public override void ApplyData(SceneCPUData data)
        {
            var value = data.Text[0];
            SetValue(value);
        }

        public override void ApplyData(SceneGPUData data)
        {
            
        }
        
#if UNITY_EDITOR
        public override IList<DisguiseRemoteParameter> GetParametersForSchema()
        {
            var value = GetValue();
            
            return new[]
            {
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_TEXT, value, 0, 0)
            };
        }
#endif
    }

    abstract class StringCollectionRemoteParameterWrapper<TCollection> : RemoteParameterWrapper<TCollection>
    {
        protected const char k_Separator = ' ';
        
        protected abstract TCollection StringToCollection(string value);
        protected abstract string CollectionToString(TCollection collection);
        
        public override void ApplyData(SceneCPUData data)
        {
            var value = data.Text[0];
            var collectionValue = StringToCollection(value);
            SetValue(collectionValue);
        }

        public override void ApplyData(SceneGPUData data)
        {
            
        }
        
#if UNITY_EDITOR
        public override IList<DisguiseRemoteParameter> GetParametersForSchema()
        {
            var collectionValue = GetValue();
            var stringValue = CollectionToString(collectionValue);
            
            return new[]
            {
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_TEXT, stringValue, 0, 0)
            };
        }
#endif
    }

    [RemoteParameterWrapper(typeof(string[]))]
    class StringArrayRemoteParameterWrapper : StringCollectionRemoteParameterWrapper<string[]>
    {
        protected override string[] StringToCollection(string value)
        {
            return value.Split(k_Separator);
        }

        protected override string CollectionToString(string[] collection)
        {
            return string.Join(k_Separator, collection);
        }
    }
    
    [RemoteParameterWrapper(typeof(List<string>))]
    class StringListRemoteParameterWrapper : StringCollectionRemoteParameterWrapper<List<string>>
    {
        protected override List<string> StringToCollection(string value)
        {
            return value.Split(k_Separator).ToList();
        }

        protected override string CollectionToString(List<string> collection)
        {
            return string.Join(k_Separator, collection);
        }
    }

    [RemoteParameterWrapper(typeof(bool))]
    class BoolRemoteParameterWrapper : RemoteParameterWrapper<bool>
    {
        public override void ApplyData(SceneCPUData data)
        {
            SetValue(Convert.ToBoolean(data.Numeric[0]));
        }

        public override void ApplyData(SceneGPUData data)
        {
            
        }
        
#if UNITY_EDITOR
        public override IList<DisguiseRemoteParameter> GetParametersForSchema()
        {
            return new[]
            {
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, GetValue() ? 1 : 0, 0f, 1f, new [] { "Off", "On" })
            };
        }
#endif
    }
    
    [RemoteParameterWrapper(typeof(Enum))]
    class EnumRemoteParameterWrapper : RemoteParameterWrapper<object>
    {
        class EnumValuesCache
        {
            readonly Dictionary<Type, Array> m_Cache = new Dictionary<Type, Array>();

            public Array GetValues(Type enumType)
            {
                if (m_Cache.TryGetValue(enumType, out var cachedValues))
                {
                    return cachedValues;
                }
                
                var values = Enum.GetValues(enumType);
                m_Cache.Add(enumType, values);
                return values;
            }
        }
        
        static readonly EnumValuesCache s_EnumValuesCache = new EnumValuesCache();
        
        Type m_EnumType;
        Array m_EnumValues;

        public override bool IsValid => base.IsValid && m_EnumType != null && m_EnumValues.Length > 0;

        public override void SetTarget(UnityEngine.Object sourceObject, MemberInfo memberInfo)
        {
            base.SetTarget(sourceObject, memberInfo);

            var enumType = GetEnumType(memberInfo);

            if (enumType.IsEnum)
            {
                m_EnumType = enumType;
                m_EnumValues = s_EnumValuesCache.GetValues(m_EnumType);
            }
            else
            {
                m_EnumType = null;
                m_EnumValues = Array.Empty<object>();
                
                throw new NotSupportedException($"Type {enumType.Name} is not an enum");
            }
        }

        protected virtual Type GetEnumType(MemberInfo memberInfo)
        {
            return ReflectionHelper.ResolveFieldOrPropertyType(memberInfo);
        }
        
        public override void ApplyData(SceneCPUData data)
        {
            var ordinalValue = Convert.ToInt32(data.Numeric[0]);
            var enumValue = m_EnumValues.GetValue(ordinalValue);
            SetValue(enumValue);
        }

        public override void ApplyData(SceneGPUData data)
        {
            
        }
        
#if UNITY_EDITOR
        public override IList<DisguiseRemoteParameter> GetParametersForSchema()
        {
            var enumValue = GetValue();
            var ordinalValue = Array.IndexOf(m_EnumValues, enumValue);
            var names = ReflectionHelper.GetEnumDisplayNames(m_EnumType).ToArray();
            
            return new[]
            {
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_NUMBER, ordinalValue, 0f, names.Length - 1, names)
            };
        }
#endif
    }
    
    [RemoteParameterWrapper(typeof(Transform))]
    class TransformRemoteParameterWrapper : ObjectRemoteParameterWrapper<Transform>
    {
        public override void ApplyData(SceneCPUData data)
        {
            var matrix = Matrix4x4RemoteParameterWrapper.ParseData(data.Numeric);

            var transform = GetValue();
            transform.localPosition = new Vector3(matrix[0, 3], matrix[1, 3], matrix[2, 3]);
            transform.localScale = matrix.lossyScale;
            transform.localRotation = matrix.rotation;
        }

        public override void ApplyData(SceneGPUData data)
        {
            
        }
        
#if UNITY_EDITOR
        public override IList<DisguiseRemoteParameter> GetParametersForSchema()
        {
            return new[]
            {
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_TRANSFORM, 0f, 0f, 255f),
            };
        }
#endif
    }
    
    [RemoteParameterWrapper(typeof(Texture))]
    [RemoteParameterWrapper(typeof(Texture2D))]
    class TextureRemoteParameterWrapper : RemoteParameterWrapper<Texture>
    {
        RenderTexture m_Data;
        
        public override void ApplyData(SceneCPUData data)
        {
            
        }

        public override void ApplyData(SceneGPUData data)
        {
            var disguiseTexture = data.Textures[0];

            var needsToUpdateBackingRT = m_Data != null && !DisguiseTextures.RenderTextureMatchesTexture(m_Data, disguiseTexture);
            
            if (m_Data == null || needsToUpdateBackingRT)
            {
                m_Data = new RenderTexture(disguiseTexture.width, disguiseTexture.height, 0, disguiseTexture.graphicsFormat);
            }
            
            SetValue(m_Data);
            
            DisguiseTextures.ConvertDisguiseTexture(disguiseTexture, m_Data, data.CommandBuffer);
        }
        
#if UNITY_EDITOR
        public override IList<DisguiseRemoteParameter> GetParametersForSchema()
        {
            return new[]
            {
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_IMAGE, 0f, 0f, 255f),
            };
        }
#endif
    }
    
    [RemoteParameterWrapper(typeof(RenderTexture))]
    class RenderTextureRemoteParameterWrapper : ObjectRemoteParameterWrapper<RenderTexture>
    {
        RenderTexture m_Data;
        
        public override void ApplyData(SceneCPUData data)
        {
            
        }

        public override void ApplyData(SceneGPUData data)
        {
            var disguiseTexture = data.Textures[0];
            var renderTexture = GetValue();

            var needsToUpdateBackingRT = m_Data != null && !DisguiseTextures.RenderTextureMatchesTexture(m_Data, disguiseTexture);
            
            if (renderTexture == null || needsToUpdateBackingRT)
            {
                m_Data = new RenderTexture(disguiseTexture.width, disguiseTexture.height, 0, disguiseTexture.graphicsFormat);
                SetValue(m_Data);
            }

            DisguiseTextures.ConvertDisguiseTexture(disguiseTexture, renderTexture, data.CommandBuffer);
        }
        
#if UNITY_EDITOR
        public override IList<DisguiseRemoteParameter> GetParametersForSchema()
        {
            return new[]
            {
                new DisguiseRemoteParameter(RemoteParameterType.RS_PARAMETER_IMAGE, 0f, 0f, 255f),
            };
        }
#endif
    }
}
