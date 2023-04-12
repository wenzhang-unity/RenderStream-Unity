using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace Disguise.RenderStream.Parameters
{
    // Remote parameter wrappers for SRP VolumeParameter.cs types

#if UNITY_EDITOR
    [ReflectionHelper.MemberInfoCollector(typeof(VolumeProfile))]
    // Expose the properties of every VolumeComponent in a VolumeProfile
    class VolumeProfileMemberInfoCollector : ReflectionHelper.MemberInfoCollector
    {
        public override IEnumerable<MemberInfoForEditor> GetSupportedMemberInfos(UnityEngine.Object obj)
        {
            var volumeProfile = obj as VolumeProfile;
            var infos = new List<MemberInfoForEditor>();
            
            foreach (var component in volumeProfile.components)
            {
                var (mainInfo, extendedInfo) = ReflectionHelper.GetSupportedMemberInfos(component);
                var componentProcessedInfos = mainInfo.Concat(extendedInfo).Select(x =>
                {
                    x.GroupPrefix = component.GetType().Name;
                    return x;
                });
                infos.AddRange(componentProcessedInfos);
            }

            return infos;
        }
    }
    
    [ReflectionHelper.MemberInfoCollector(typeof(Volume))]
    // Expose a Volume's profile's properties
    class VolumeMemberInfoCollector : VolumeProfileMemberInfoCollector
    {
        public override IEnumerable<MemberInfoForEditor> GetSupportedMemberInfos(UnityEngine.Object obj)
        {
            var volume = obj as Volume;
            
            var volumeProfile = volume.HasInstantiatedProfile()
                ? volume.profile
                : volume.sharedProfile;

            if (volumeProfile == null)
                return Enumerable.Empty<MemberInfoForEditor>();

            return base.GetSupportedMemberInfos(volumeProfile);
        }
    }
#endif
    
    static class VolumeMinMax
    {
        public const int IntMin = int.MinValue;
        public const int IntMax = int.MaxValue;
        
        public const float FloatMin = float.MinValue;
        public const float FloatMax = float.MaxValue;
    }
    
    // Our target is a VolumeParameter<T>, but what we really want to access is its VolumeParameter.value
    class VolumeParameterDataGetterSetter<TData, TBackingData> : GetterSetter<TBackingData>
        where TData : VolumeParameter<TBackingData>
    {
        readonly DefaultGetterSetter<TData> m_Target = new DefaultGetterSetter<TData>();

        public override bool IsValid => m_Target.IsValid;
        
        public override void SetTarget(UnityEngine.Object targetObject, MemberInfo memberInfo)
        {
            m_Target.SetTarget(targetObject, memberInfo);
        }

        public TData GetTarget()
        {
            return m_Target.Get();
        }

        public override TBackingData Get()
        {
            var volumeParameter = m_Target.Get();
            return volumeParameter.value;
        }

        public override void Set(TBackingData value)
        {
            var volumeParameter = m_Target.Get();
            volumeParameter.value = value;
        }
    }
    
    // Our target is a VolumeParameter, but what we really want to access is its VolumeParameter.overrideState
    class VolumeParameterOverrideFlagGetterSetter : GetterSetter<bool>
    {
        readonly DefaultGetterSetter<VolumeParameter> m_Target = new DefaultGetterSetter<VolumeParameter>();

        public override bool IsValid => m_Target.IsValid;
        
        public override void SetTarget(UnityEngine.Object targetObject, MemberInfo memberInfo)
        {
            m_Target.SetTarget(targetObject, memberInfo);
        }

        public override bool Get()
        {
            var volumeParameter = m_Target.Get();
            return volumeParameter.overrideState;
        }

        public override void Set(bool value)
        {
            var volumeParameter = m_Target.Get();
            volumeParameter.overrideState = value;
        }
    }

    abstract class VolumeParameterWrapper<TData, TBackingData, TImpl> : IRemoteParameterWrapper
        where TData : VolumeParameter<TBackingData>
        where TImpl : RemoteParameterWrapper<TBackingData>, new()
    {
        protected readonly BoolRemoteParameterWrapper m_OverrideFlagImpl = new BoolRemoteParameterWrapper();
        protected readonly TImpl m_DataImpl = new TImpl();

        readonly VolumeParameterOverrideFlagGetterSetter m_overrideFlagGetterSetter = new VolumeParameterOverrideFlagGetterSetter();
        readonly VolumeParameterDataGetterSetter<TData, TBackingData> m_DataGetterSetter = new VolumeParameterDataGetterSetter<TData, TBackingData>();

        public bool IsValid => m_DataImpl.IsValid && m_OverrideFlagImpl.IsValid;

        protected VolumeParameterWrapper()
        {
            m_OverrideFlagImpl.GetterSetter = m_overrideFlagGetterSetter;
            m_DataImpl.GetterSetter = m_DataGetterSetter;
        }

        /// <summary>
        /// Configure this remote parameter based on <paramref name="volumeParameter"/>'s configuration
        /// </summary>
        protected virtual void SetupVolumeParameter(TData volumeParameter)
        {
            
        }

        public virtual void SetTarget(UnityEngine.Object targetObject, MemberInfo memberInfo)
        {
            m_OverrideFlagImpl.SetTarget(targetObject, memberInfo);
            m_DataImpl.SetTarget(targetObject, memberInfo);
            
            if (m_DataImpl.IsValid)
            {
                var volumeParameter = m_DataGetterSetter.GetTarget();
                SetupVolumeParameter(volumeParameter);
            }
        }

        public void ApplyData(SceneCPUData data)
        {
            // The first numeric element is for the override flag, everything else is for the data
            
            var overrideFlagData = new SceneCPUData(data.Numeric.Slice(0, 1), ReadOnlySpan<string>.Empty);
            var dataData = new SceneCPUData(data.Numeric.Slice(1), data.Text);
            
            m_OverrideFlagImpl.ApplyData(overrideFlagData);
            m_DataImpl.ApplyData(dataData);
        }

        public void ApplyData(SceneGPUData data)
        {
            m_DataImpl.ApplyData(data);
        }
        
#if UNITY_EDITOR
        public IList<DisguiseRemoteParameter> GetParametersForSchema()
        {
            // First the override flag, then the data parameters
            
            var boolParams = m_OverrideFlagImpl.GetParametersForSchema();
            if (boolParams.Count != 1)
                throw new InvalidOperationException("Expected a single boolean parameter");

            var boolParam = boolParams[0];
            boolParam.Suffix = "Override";
            
            var dataParams = m_DataImpl.GetParametersForSchema();

            var parameters = new List<DisguiseRemoteParameter>(new []{ boolParam });
            parameters.AddRange(dataParams);
            return parameters;
        }
#endif
    }

    [RemoteParameterWrapper(typeof(BoolParameter))]
    class VolumeBoolParameterHandler : VolumeParameterWrapper<BoolParameter, bool, BoolRemoteParameterWrapper> { }

    [RemoteParameterWrapper(typeof(IntParameter))]
    [RemoteParameterWrapper(typeof(NoInterpIntParameter))]
    class VolumeIntParameterHandler : VolumeParameterWrapper<VolumeParameter<int>, int, IntRemoteParameterWrapper>
    {
        protected override void SetupVolumeParameter(VolumeParameter<int> volumeParameter)
        {
            m_DataImpl.Min = VolumeMinMax.IntMin;
            m_DataImpl.Max = VolumeMinMax.IntMax;
        }
    }

    [RemoteParameterWrapper(typeof(MinIntParameter))]
    class VolumeMinIntParameterHandler : VolumeParameterWrapper<MinIntParameter, int, IntRemoteParameterWrapper>
    {
        protected override void SetupVolumeParameter(MinIntParameter volumeParameter)
        {
            m_DataImpl.Min = volumeParameter.min;
            m_DataImpl.Max = VolumeMinMax.IntMax;
        }
    }
    
    [RemoteParameterWrapper(typeof(NoInterpMinIntParameter))]
    class VolumeNoInterpMinIntParameterHandler : VolumeParameterWrapper<NoInterpMinIntParameter, int, IntRemoteParameterWrapper>
    {
        protected override void SetupVolumeParameter(NoInterpMinIntParameter volumeParameter)
        {
            m_DataImpl.Min = volumeParameter.min;
            m_DataImpl.Max = VolumeMinMax.IntMax;
        }
    }
    
    [RemoteParameterWrapper(typeof(MaxIntParameter))]
    class VolumeMaxIntParameterHandler : VolumeParameterWrapper<MaxIntParameter, int, IntRemoteParameterWrapper>
    {
        protected override void SetupVolumeParameter(MaxIntParameter volumeParameter)
        {
            m_DataImpl.Min = VolumeMinMax.IntMin;
            m_DataImpl.Max = volumeParameter.max;
        }
    }
    
    [RemoteParameterWrapper(typeof(NoInterpMaxIntParameter))]
    class VolumeNoInterpMaxIntParameterHandler : VolumeParameterWrapper<NoInterpMaxIntParameter, int, IntRemoteParameterWrapper>
    {
        protected override void SetupVolumeParameter(NoInterpMaxIntParameter volumeParameter)
        {
            m_DataImpl.Min = VolumeMinMax.IntMin;
            m_DataImpl.Max = volumeParameter.max;
        }
    }
    
    [RemoteParameterWrapper(typeof(ClampedIntParameter))]
    class VolumeClampedIntParameterHandler : VolumeParameterWrapper<ClampedIntParameter, int, IntRemoteParameterWrapper>
    {
        protected override void SetupVolumeParameter(ClampedIntParameter volumeParameter)
        {
            m_DataImpl.Min = volumeParameter.min;
            m_DataImpl.Max = volumeParameter.max;
        }
    }
    
    [RemoteParameterWrapper(typeof(NoInterpClampedIntParameter))]
    class VolumeNoInterpClampedIntParameterHandler : VolumeParameterWrapper<NoInterpClampedIntParameter, int, IntRemoteParameterWrapper>
    {
        protected override void SetupVolumeParameter(NoInterpClampedIntParameter volumeParameter)
        {
            m_DataImpl.Min = volumeParameter.min;
            m_DataImpl.Max = volumeParameter.max;
        }
    }
    
    [RemoteParameterWrapper(typeof(FloatParameter))]
    [RemoteParameterWrapper(typeof(NoInterpFloatParameter))]
    class VolumeFloatParameterHandler : VolumeParameterWrapper<VolumeParameter<float>, float, FloatRemoteParameterWrapper>
    {
        protected override void SetupVolumeParameter(VolumeParameter<float> volumeParameter)
        {
            m_DataImpl.Min = VolumeMinMax.FloatMin;
            m_DataImpl.Max = VolumeMinMax.FloatMax;
        }
    }

    [RemoteParameterWrapper(typeof(MinFloatParameter))]
    class VolumeMinFloatParameterHandler : VolumeParameterWrapper<MinFloatParameter, float, FloatRemoteParameterWrapper>
    {
        protected override void SetupVolumeParameter(MinFloatParameter volumeParameter)
        {
            m_DataImpl.Min = volumeParameter.min;
            m_DataImpl.Max = VolumeMinMax.FloatMax;
        }
    }
    
    [RemoteParameterWrapper(typeof(NoInterpMinFloatParameter))]
    class VolumeNoInterpMinFloatParameterHandler : VolumeParameterWrapper<NoInterpMinFloatParameter, float, FloatRemoteParameterWrapper>
    {
        protected override void SetupVolumeParameter(NoInterpMinFloatParameter volumeParameter)
        {
            m_DataImpl.Min = volumeParameter.min;
            m_DataImpl.Max = VolumeMinMax.FloatMax;
        }
    }
    
    [RemoteParameterWrapper(typeof(MaxFloatParameter))]
    class VolumeMaxFloatParameterHandler : VolumeParameterWrapper<MaxFloatParameter, float, FloatRemoteParameterWrapper>
    {
        protected override void SetupVolumeParameter(MaxFloatParameter volumeParameter)
        {
            m_DataImpl.Min = VolumeMinMax.FloatMin;
            m_DataImpl.Max = volumeParameter.max;
        }
    }
    
    [RemoteParameterWrapper(typeof(NoInterpMaxFloatParameter))]
    class VolumeNoInterpMaxFloatParameterHandler : VolumeParameterWrapper<NoInterpMaxFloatParameter, float, FloatRemoteParameterWrapper>
    {
        protected override void SetupVolumeParameter(NoInterpMaxFloatParameter volumeParameter)
        {
            m_DataImpl.Min = VolumeMinMax.FloatMin;
            m_DataImpl.Max = volumeParameter.max;
        }
    }
    
    [RemoteParameterWrapper(typeof(ClampedFloatParameter))]
    class VolumeClampedFloatParameterHandler : VolumeParameterWrapper<ClampedFloatParameter, float, FloatRemoteParameterWrapper>
    {
        protected override void SetupVolumeParameter(ClampedFloatParameter volumeParameter)
        {
            m_DataImpl.Min = volumeParameter.min;
            m_DataImpl.Max = volumeParameter.max;
        }
    }
    
    [RemoteParameterWrapper(typeof(NoInterpClampedFloatParameter))]
    class VolumeNoInterpClampedFloatParameterHandler : VolumeParameterWrapper<NoInterpClampedFloatParameter, float, FloatRemoteParameterWrapper>
    {
        protected override void SetupVolumeParameter(NoInterpClampedFloatParameter volumeParameter)
        {
            m_DataImpl.Min = volumeParameter.min;
            m_DataImpl.Max = volumeParameter.max;
        }
    }
    
    [RemoteParameterWrapper(typeof(FloatRangeParameter))]
    class VolumeFloatRangeParameterHandler : VolumeParameterWrapper<FloatRangeParameter, Vector2, Vector2RemoteParameterWrapper>
    {
        protected override void SetupVolumeParameter(FloatRangeParameter volumeParameter)
        {
            m_DataImpl.Min = volumeParameter.min;
            m_DataImpl.Max = volumeParameter.max;
        }
    }
    
    [RemoteParameterWrapper(typeof(NoInterpFloatRangeParameter))]
    class VolumeNoInterpFloatRangeParameterHandler : VolumeParameterWrapper<NoInterpFloatRangeParameter, Vector2, Vector2RemoteParameterWrapper>
    {
        protected override void SetupVolumeParameter(NoInterpFloatRangeParameter volumeParameter)
        {
            m_DataImpl.Min = volumeParameter.min;
            m_DataImpl.Max = volumeParameter.max;
        }
    }
    
    [RemoteParameterWrapper(typeof(ColorParameter))]
    [RemoteParameterWrapper(typeof(NoInterpColorParameter))]
    class VolumeColorParameterHandler : VolumeParameterWrapper<VolumeParameter<Color>, Color, ColorRemoteParameterWrapper> { }
    
    [RemoteParameterWrapper(typeof(Vector2Parameter))]
    [RemoteParameterWrapper(typeof(NoInterpVector2Parameter))]
    class VolumeVector2ParameterHandler : VolumeParameterWrapper<VolumeParameter<Vector2>, Vector2, Vector2RemoteParameterWrapper> { }
    
    [RemoteParameterWrapper(typeof(Vector3Parameter))]
    [RemoteParameterWrapper(typeof(NoInterpVector3Parameter))]
    class VolumeVector3ParameterHandler : VolumeParameterWrapper<VolumeParameter<Vector3>, Vector3, Vector3RemoteParameterWrapper> { }
    
    [RemoteParameterWrapper(typeof(Vector4Parameter))]
    [RemoteParameterWrapper(typeof(NoInterpVector4Parameter))]
    class VolumeVector4ParameterHandler : VolumeParameterWrapper<VolumeParameter<Vector4>, Vector4, Vector4RemoteParameterWrapper> { }

    [RemoteParameterWrapper(typeof(TextureParameter))]
    [RemoteParameterWrapper(typeof(NoInterpTextureParameter))]
    [RemoteParameterWrapper(typeof(Texture2DParameter))]
    class VolumeTextureParameterHandler : VolumeParameterWrapper<VolumeParameter<Texture>, Texture, TextureRemoteParameterWrapper> { }
    
    [RemoteParameterWrapper(typeof(RenderTextureParameter))]
    [RemoteParameterWrapper(typeof(NoInterpRenderTextureParameter))]
    class VolumeRenderTextureParameterHandler : VolumeParameterWrapper<VolumeParameter<RenderTexture>, RenderTexture, RenderTextureRemoteParameterWrapper> { }
    
    // An alternative to VolumeParameterDataGetterSetter that doesn't cast VolumeParameter<ConcreteEnum> to any
    // more general VolumeParameter<T>, since no such cast is allowed in C#. The value is accessed through
    // reflection instead.
    class EnumVolumeParameterDataGetterSetter : GetterSetter<object>
    {
        readonly DefaultGetterSetter<VolumeParameter> m_Target = new DefaultGetterSetter<VolumeParameter>();
        MethodInfo m_Get;
        MethodInfo m_Set;

        public override bool IsValid => m_Target.IsValid;
        
        public override void SetTarget(UnityEngine.Object targetObject, MemberInfo memberInfo)
        {
            m_Target.SetTarget(targetObject, memberInfo);

            var targetType = ReflectionHelper.ResolveFieldOrPropertyType(memberInfo);
            
            // We know for sure every VolumeParameter<ConcreteEnum> inherits from VolumeParameter<T>
            var info = targetType.GetProperty(nameof(VolumeParameter<object>.value));

            if (info == null)
                throw new InvalidOperationException($"Type {targetType.Name} has no '{nameof(VolumeParameter<object>.value)}' property");
            
            m_Get = info.GetGetMethod();
            m_Set = info.GetSetMethod();
        }

        public override object Get()
        {
            var target = m_Target.Get();
            var value = m_Get.Invoke(target, new object[]{ });
            return value;
        }

        public override void Set(object value)
        {
            var target = m_Target.Get();
            m_Set.Invoke(target, new []{ value });
        }
    }

    class GenericEnumRemoteParameterWrapper : EnumRemoteParameterWrapper
    {
        // The enum type is the first generic argument type
        protected override Type GetEnumType(MemberInfo memberInfo)
        {
            var type = ReflectionHelper.ResolveFieldOrPropertyType(memberInfo);
            return type.BaseType.GenericTypeArguments[0];
        }
    }

    [RemoteParameterWrapper(typeof(VolumeParameter<Enum>))]
    // A version of VolumeParameterWrapper that uses EnumVolumeParameterDataGetterSetter to access the value
    class VolumeEnumParameterHandler : VolumeParameterWrapper<VolumeParameter<object>, object, GenericEnumRemoteParameterWrapper>
    {
        readonly EnumVolumeParameterDataGetterSetter m_EnumGetterSetter = new EnumVolumeParameterDataGetterSetter();
        
        public VolumeEnumParameterHandler()
        {
            m_DataImpl.GetterSetter = m_EnumGetterSetter;
        }
        
        public override void SetTarget(UnityEngine.Object targetObject, MemberInfo memberInfo)
        {
            m_OverrideFlagImpl.SetTarget(targetObject, memberInfo);
            m_DataImpl.SetTarget(targetObject, memberInfo);
            
            // Overriden to skip the SetupVolumeParameter step because it's troublesome to cast a
            // VolumeParameter<ConcreteEnum> to any more general VolumeParameter<T>
        }
    }
}
