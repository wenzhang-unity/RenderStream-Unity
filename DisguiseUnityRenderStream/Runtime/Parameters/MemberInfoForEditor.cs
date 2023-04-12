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
        public UnityEngine.Object Object;
        public MemberInfo MemberInfo;
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
            return Object != null &&
                   !string.IsNullOrWhiteSpace(RealName) &&
                   (MemberInfo != null || MemberType == MemberInfoForRuntime.MemberType.This);
        }

        public MemberInfoForRuntime ToRuntimeInfo()
        {
            var runtimeInfo = new MemberInfoForRuntime();
            
            if (MemberType == MemberInfoForRuntime.MemberType.This)
                runtimeInfo.Assign(Object);
            else
                runtimeInfo.Assign(Object, MemberInfo);
            
            return runtimeInfo;
        }
        
        public static bool CreateFromRuntimeInfo(MemberInfoForRuntime runtimeInfo, out MemberInfoForEditor editorInfo)
        {
            if (runtimeInfo.Type == MemberInfoForRuntime.MemberType.This)
            {
                if (runtimeInfo.Object == null)
                {
                    editorInfo = default;
                    return false;
                }
                
                return ReflectionHelper.TryCreateThisMemberInfo(runtimeInfo.Object, out editorInfo);
            }
            
            var memberInfo = runtimeInfo.MemberInfo;
            return ReflectionHelper.TryCreateMemberInfo(runtimeInfo.Object, memberInfo, out editorInfo);
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
            return Equals(MemberInfo, other.MemberInfo) &&
                   Object == other.Object &&
                   RealName == other.RealName &&
                   DisplayName == other.DisplayName &&
                   GroupPrefix == other.GroupPrefix &&
                   ValueType == other.ValueType &&
                   GetterSetterType == other.GetterSetterType &&
                   MemberType == other.MemberType;
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
}
#endif
