using System;
using System.Reflection;
using UnityEngine;

namespace Disguise.RenderStream.Parameters
{
    /// <summary>
    /// Represents serialized and cached <see cref="MemberInfo"/> data in the runtime context.
    /// </summary>
    [Serializable]
    class MemberInfoForRuntime : ISerializationCallbackReceiver
    {
        public enum MemberType
        {
            Invalid = 0,
            Field = 1,
            Property = 2
        }
        
        [SerializeField]
        UnityEngine.Object m_Object = null;
        [SerializeField]
        MemberType m_MemberType = MemberType.Invalid;
        [SerializeField]
        string m_MemberValueType = null;
        [SerializeField]
        string m_MemberName = null;

        MemberInfo m_CachedMemberInfo;
        bool m_SourceDirty;

        public MemberInfo MemberInfo => GetMemberInfo();

        public void Assign(UnityEngine.Object obj, MemberInfo memberInfo)
        {
            m_Object = obj;
            
            m_MemberType = MemberType.Invalid;
            m_MemberValueType = string.Empty;
            m_MemberName = string.Empty;
            
            if (memberInfo == null)
                return;

            if (memberInfo is FieldInfo field)
            {
                m_MemberType = MemberType.Field;
                m_MemberValueType = field.FieldType.Name;
            }
            else if (memberInfo is PropertyInfo property)
            {
                if (property.GetGetMethod() == null)
                    throw new NotSupportedException($"Property {memberInfo.Name} has no getter");
                
                if (property.GetSetMethod() == null)
                    throw new NotSupportedException($"Property {memberInfo.Name} has no setter");
                
                m_MemberType = MemberType.Property;
                m_MemberValueType = property.PropertyType.Name;
            }
            else
            {
                throw new NotSupportedException($"Unsupported member type: {memberInfo.GetType().Name}");
            }

            m_MemberName = memberInfo.Name;
            m_CachedMemberInfo = memberInfo;
            m_SourceDirty = false;
        }
        
        MemberInfo GetMemberInfo()
        {
            if (!m_SourceDirty)
            {
                return m_CachedMemberInfo;
            }
            
            if (m_Object == null)
            {
                return null;
            }

            var type = m_Object.GetType();

            try
            {
                m_CachedMemberInfo = m_MemberType switch
                {
                    MemberType.Field => type.GetField(m_MemberName),
                    MemberType.Property => type.GetProperty(m_MemberName),
                    MemberType.Invalid => null,
                    _ => throw new ArgumentOutOfRangeException(nameof(m_MemberType), $"Invalid member type {m_MemberType}."),
                };
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
                return null;
            }

            m_SourceDirty = false;
            return m_CachedMemberInfo;
        }

        public void Apply(IRemoteParameterWrapper remoteParameterWrapper)
        {
            if (remoteParameterWrapper == null)
                return;
            
            var memberInfo = GetMemberInfo();
            remoteParameterWrapper.SetTarget(m_Object, memberInfo);
        }
        
        /// <inheritdoc />
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            
        }

        /// <inheritdoc />
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // We can't find the member on deserialization since it requires access to a UnityEngine.Object
            // which may not be used in this callback, so we defer initialization.
            m_SourceDirty = true;
        }
    }
}
