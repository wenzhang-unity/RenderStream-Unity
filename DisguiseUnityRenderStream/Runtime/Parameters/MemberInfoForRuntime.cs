using System;
using System.Reflection;
using UnityEngine;

namespace Disguise.RenderStream.Parameters
{
    struct Target
    {
        public bool IsValid => Object != null;
        
        public bool IsThisMode => MemberInfo == null;
        
        public UnityEngine.Object Object;
        public MemberInfo MemberInfo;
        
        public override bool Equals(object obj) => obj is Target other && Equals(other);

        public static bool operator ==(Target lhs, Target rhs) => lhs.Equals(rhs);

        public static bool operator !=(Target lhs, Target rhs) => !(lhs == rhs);

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool Equals(Target other)
        {
            return Object == other.Object &&
                   Equals(MemberInfo, other.MemberInfo);
        }

        public static bool Equals(MemberInfo lhs, MemberInfo rhs)
        {
            if (lhs == null && rhs == null)
                return true;

            if (lhs == null || rhs == null)
                return false;

            return lhs.Name == rhs.Name &&
                   lhs.DeclaringType == rhs.DeclaringType;
        }
    }
    
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
            Property = 2,
            This = 4
        }
        
        [SerializeField]
        MemberType m_MemberType = MemberType.Invalid;
        [SerializeField]
        UnityEngine.Object m_Object;
        [SerializeField]
        string m_MemberName;

        MemberInfo m_CachedMemberInfo;
        bool m_SourceDirty;

        public MemberType Type => m_MemberType;

        public Target Target
        {
            get
            {
                if (!m_SourceDirty)
                {
                    return new Target
                    {
                        Object = m_Object,
                        MemberInfo = m_CachedMemberInfo
                    };
                }
            
                if (m_Object == null)
                {
                    return default;
                }

                var type = m_Object.GetType();

                try
                {
                    m_CachedMemberInfo = m_MemberType switch
                    {
                        MemberType.Field => type.GetField(m_MemberName),
                        MemberType.Property => type.GetProperty(m_MemberName),
                        MemberType.Invalid or MemberType.This => null,
                        _ => throw new ArgumentOutOfRangeException(nameof(m_MemberType), $"Invalid member type {m_MemberType}."),
                    };
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                    return default;
                }

                m_SourceDirty = false;
                return new Target
                {
                    Object = m_Object,
                    MemberInfo = m_CachedMemberInfo
                };
            }
        }

#if UNITY_EDITOR
        public void Assign(Target target)
        {
            Reset();

            if (!target.IsValid)
                return;

            if (target.IsThisMode)
            {
                m_MemberType = MemberType.This;
                m_Object = target.Object;
            }
            else if (target.MemberInfo != null)
            {
                switch (target.MemberInfo)
                {
                    case FieldInfo field:
                        m_MemberType = MemberType.Field;
                        break;
                    
                    case PropertyInfo property:
                        if (property.GetGetMethod() == null)
                            throw new NotSupportedException($"Property {target.MemberInfo.Name} has no getter");
                        if (property.GetSetMethod() == null)
                            throw new NotSupportedException($"Property {target.MemberInfo.Name} has no setter");
                        m_MemberType = MemberType.Property;
                        break;
                    
                    default:
                        throw new NotSupportedException($"Unsupported member type: {target.MemberInfo.GetType().Name}");
                }
                
                m_Object = target.Object;
                m_MemberName = target.MemberInfo.Name;
                m_CachedMemberInfo = target.MemberInfo;
            }
            
            m_SourceDirty = false;
        }
        
        void Reset()
        {
            m_MemberType = MemberType.Invalid;
            m_Object = null;
            m_MemberName = string.Empty;

            m_CachedMemberInfo = null;
            m_SourceDirty = false;
        }
#endif

        public void Apply(IRemoteParameterWrapper remoteParameterWrapper)
        {
            remoteParameterWrapper?.SetTarget(Target);
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
