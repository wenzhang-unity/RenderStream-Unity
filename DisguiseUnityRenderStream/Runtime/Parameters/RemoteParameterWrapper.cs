using System;
using System.Collections.Generic;
using System.Reflection;
using Disguise.RenderStream.Utils;

namespace Disguise.RenderStream.Parameters
{
#if UNITY_EDITOR
    /// <summary>
    /// Data to be assembled into a <see cref="ManagedRemoteParameter"/> instance at build-time.
    /// </summary>
    struct DisguiseRemoteParameter
    {
        /// <summary>
        /// Directly mapped to <see cref="ManagedRemoteParameter.type"/>.
        /// </summary>
        public RemoteParameterType Type;
        
        /// <summary>
        /// Directly mapped to <see cref="ManagedRemoteParameter.defaultValue"/>.
        /// This can be a <see langword="float"/> or a <see langword="string"/>.
        /// </summary>
        public object DefaultValue;
        
        /// <summary>
        /// Mapped to <see cref="ManagedRemoteParameter.min"/> but it can be later overriden by the user.
        /// </summary>
        public float DefaultMin;
        
        /// <summary>
        /// Mapped to <see cref="ManagedRemoteParameter.max"/> but it can be later overriden by the user.
        /// </summary>
        public float DefaultMax;
        
        /// <summary>
        /// Directly mapped to <see cref="ManagedRemoteParameter.options"/>.
        /// </summary>
        public string[] Options;
        
        /// <summary>
        /// Will be appended to <see cref="ManagedRemoteParameter.displayName"/> and <see cref="ManagedRemoteParameter.key"/>.
        /// </summary>
        public string Suffix;

        /// <summary>
        /// A hint for type RemoteParameterType.RS_PARAMETER_NUMBER when this parameter only contains whole numbers.
        /// </summary>
        public bool IntegralTypeHint;

        public static DisguiseRemoteParameter SetIntegralType(DisguiseRemoteParameter parameter)
        {
            parameter.IntegralTypeHint = true;
            return parameter;
        }
            
        public DisguiseRemoteParameter(RemoteParameterType type, object defaultValue, float defaultMin, float defaultMax, string[] options = null, string suffix = null)
        {
            Type = type;
            DefaultValue = defaultValue;
            DefaultMin = defaultMin;
            DefaultMax = defaultMax;
            Options = options;
            Suffix = suffix;
            IntegralTypeHint = false;
        }
    }
#endif
    
    /// <summary>
    /// An attribute placed on a class inheriting from <see cref="IRemoteParameterWrapper"/> to enable the provided
    /// <see cref="RemoteParameterWrapperAttribute.Type"/> to be used as a remote parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    class RemoteParameterWrapperAttribute : Attribute
    {
        /// <summary>
        /// The type of the remote parameter.
        /// </summary>
        public Type Type { get; }
        
        public bool SupportsThisMode { get; }

        /// <summary>
        /// If multiple argument outputs are used for the same <see cref="Type"/>, the argument output with the greater priority is used.
        /// </summary>
        /// <remarks>
        /// Use a priority greater than zero to override the default argument outputs with custom implementations.
        /// </remarks>
        public int Priority { get; set; } = 0;

        public RemoteParameterWrapperAttribute(Type type, bool supportsThisMode = false)
        {
            Type = type;
            SupportsThisMode = supportsThisMode;
        }
    }
    
    /// <summary>
    /// Represents a remote parameter at runtime.
    /// Applies data (<see cref="DataBlock{T}"/>) received from Disguise onto Unity objects.
    /// Also adds its representation to the Disguise schema at built-time. 
    /// </summary>
    interface IRemoteParameterWrapper
    {
        /// <summary>
        /// Returns true when the target has been resolved successfully.
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Sets the target on which data from Disguise will be applied.
        /// </summary>
        void SetTarget(Target target);
        
        /// <summary>
        /// Applies data from Disguise onto the target.
        /// <remarks>Called during <see cref="UnityEngine.PlayerLoop.TimeUpdate.WaitForLastPresentationAndUpdateTime"/></remarks>
        /// </summary>
        void ApplyData(SceneCPUData data);
        
        /// <summary>
        /// Applies data from Disguise onto the target.
        /// <remarks>Called during <see cref="UnityEngine.Rendering.RenderPipelineManager.beginContextRendering"/></remarks>
        /// </summary>
        void ApplyData(SceneGPUData data);
        
#if UNITY_EDITOR
        /// <summary>
        /// Returns a representation of a remote parameter of this type as a combination of 1 or more <see cref="DisguiseRemoteParameter">Disguise parameters</see>.
        /// </summary>
        /// <returns>An ordered list of Disguise parameters. This order should be conserved in the schema.</returns>
        IList<DisguiseRemoteParameter> GetParametersForSchema();
#endif
    }
    
    abstract class GetterSetter<T>
    {
        public abstract bool IsValid { get; }
        public abstract void SetTarget(Target target);
        public abstract T Get();
        public abstract void Set(T value);
    }
    
    class DefaultGetterSetter<T> : GetterSetter<T>
    {
        public override bool IsValid => m_Target.Object != null && m_Target.MemberInfo != null;

        Target m_Target;
        
#if !ENABLE_IL2CPP
        Action<UnityEngine.Object, T> m_DynamicSetter;
#endif
        
        public override void SetTarget(Target target)
        {
            m_Target = target;
            
#if !ENABLE_IL2CPP
            if (m_Target.MemberInfo != null)
                m_DynamicSetter = DynamicSetterCache<T>.GetSetter(m_Target.MemberInfo);
#endif
        }

        public override T Get()
        {
            var value = GetValueByReflection();
            return value;
        }

        public override void Set(T value)
        {
#if ENABLE_IL2CPP
            SetValueByReflection(value);
#else
            m_DynamicSetter.Invoke(m_Target.Object, value);
#endif
        }
        
        T GetValueByReflection()
        {
            return m_Target.MemberInfo switch
            {
                FieldInfo field => (T)field.GetValue(m_Target.Object),
                PropertyInfo property => (T)property.GetValue(m_Target.Object),
                _ => throw new ArgumentOutOfRangeException(nameof(m_Target.MemberInfo), $"Invalid member type {m_Target.MemberInfo?.GetType().Name}.")
            };
        }

        void SetValueByReflection(T value)
        {
            if (m_Target.MemberInfo is FieldInfo field)
            {
                field.SetValue(m_Target.Object, value);
            }
            else if (m_Target.MemberInfo is PropertyInfo property)
            {
                property.SetValue(m_Target.Object, value);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(m_Target.MemberInfo), $"Invalid member type {m_Target.MemberInfo?.GetType().Name}.");
            }
        }
    }
    
    class ThisGetterSetter<T> : GetterSetter<T> where T : UnityEngine.Object
    {
        public override bool IsValid => m_Target.Object != null && m_Target.MemberInfo == null;

        Target m_Target;
        
        public override void SetTarget(Target target)
        {
            m_Target = target;
        }

        public override T Get()
        {
            return (T)m_Target.Object;
        }

        public override void Set(T value)
        {
            throw new InvalidOperationException("Cannot set the value on a 'this' object");
        }
    }
    
    /// <summary>
    /// An implementation of <see cref="IRemoteParameterWrapper"/> that provides optimized access to a reflected member.
    /// </summary>
    /// <typeparam name="T">The C# type of the remote parameter.</typeparam>
    [Serializable]
    abstract class RemoteParameterWrapper<T> : IRemoteParameterWrapper
    {
        GetterSetter<T> m_GetterSetter = new DefaultGetterSetter<T>();

        public GetterSetter<T> GetterSetter
        {
            get => m_GetterSetter;
            set => m_GetterSetter = value;
        }

        /// <inheritdoc/>
        public virtual bool IsValid => m_GetterSetter.IsValid;

        /// <inheritdoc/>
        public virtual void SetTarget(Target target)
        {
            m_GetterSetter.SetTarget(target);
        }

        /// <summary>
        /// Returns the current value of the target object and member.
        /// </summary>
        public T GetValue()
        {
            return m_GetterSetter.Get();
        }

        /// <summary>
        /// Applies data to the target object and member.
        /// </summary>
        public void SetValue(T value)
        {
            m_GetterSetter.Set(value);
        }

        /// <inheritdoc/>
        public abstract void ApplyData(SceneCPUData data);
        
        /// <inheritdoc/>
        public abstract void ApplyData(SceneGPUData data);
        
#if UNITY_EDITOR
        /// <inheritdoc/>
        public abstract IList<DisguiseRemoteParameter> GetParametersForSchema();
#endif
    }
    
    [Serializable]
    abstract class ObjectRemoteParameterWrapper<T> : RemoteParameterWrapper<T> where T : UnityEngine.Object
    {
        DefaultGetterSetter<T> m_GetterSetter = new DefaultGetterSetter<T>();
        ThisGetterSetter<T> m_ThisGetterSetter = new ThisGetterSetter<T>();

        /// <inheritdoc/>
        public override void SetTarget(Target target)
        {
            if (target.IsThisMode)
            {
                GetterSetter = m_ThisGetterSetter;
            }
            else
            {
                GetterSetter = m_GetterSetter;
            }
            
            base.SetTarget(target);
        }
    }
}
