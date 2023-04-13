#if UNITY_EDITOR
using System;
using System.Reflection;

namespace Disguise.RenderStream.Parameters
{
    /// <summary>
    /// Represents cached <see cref="MemberInfo"/> data in the editor context.
    /// </summary>
    struct MemberInfoForEditor
    {
        public Target Target;
        public string RealName;
        public string DisplayName;
        public string GroupPrefix;
        public Type ValueType;
        public Type GetterSetterType;
        public MemberInfoForRuntime.MemberType MemberType;

        public string UIName => string.IsNullOrWhiteSpace(DisplayName)
            ? RealName
            : DisplayName;

        public string UINameWithGroupPrefix => string.IsNullOrWhiteSpace(GroupPrefix)
            ? UIName
            : $"{GroupPrefix}/{UIName}";

        public bool IsValid()
        {
            return Target.Object != null &&
                   !string.IsNullOrWhiteSpace(RealName) &&
                   (Target.MemberInfo != null || MemberType == MemberInfoForRuntime.MemberType.This);
        }

        public MemberInfoForRuntime ToRuntimeInfo()
        {
            var runtimeInfo = new MemberInfoForRuntime();
            runtimeInfo.Assign(Target);
            return runtimeInfo;
        }
        
        public static bool TryCreateFromRuntimeInfo(MemberInfoForRuntime runtimeInfo, out MemberInfoForEditor editorInfo)
        {
            if (runtimeInfo.Type == MemberInfoForRuntime.MemberType.This)
            {
                var obj = runtimeInfo.Target.Object;
                if (obj == null)
                {
                    editorInfo = default;
                    return false;
                }
                
                return ReflectionHelper.TryCreateThisMemberInfo(obj, out editorInfo);
            }
            
            return ReflectionHelper.TryCreateMemberInfo(runtimeInfo.Target.Object, runtimeInfo.Target.MemberInfo, out editorInfo);
        }
        
        public override bool Equals(object obj) => obj is MemberInfoForEditor other && this.Equals(other);

        public static bool operator ==(MemberInfoForEditor lhs, MemberInfoForEditor rhs) => lhs.Equals(rhs);

        public static bool operator !=(MemberInfoForEditor lhs, MemberInfoForEditor rhs) => !(lhs == rhs);

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool Equals(MemberInfoForEditor other)
        {
            return Target == other.Target &&
                   RealName == other.RealName &&
                   DisplayName == other.DisplayName &&
                   GroupPrefix == other.GroupPrefix &&
                   ValueType == other.ValueType &&
                   GetterSetterType == other.GetterSetterType &&
                   MemberType == other.MemberType;
        }
    }
}
#endif
