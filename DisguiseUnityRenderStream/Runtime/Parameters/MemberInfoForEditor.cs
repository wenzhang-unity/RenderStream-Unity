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
        public MemberInfo MemberInfo;
        public string RealName;
        public string DisplayName;
        public Type ValueType;
        public Type GetterSetterType;
        public MemberInfoForRuntime.MemberType MemberType;

        public string UIName => string.IsNullOrWhiteSpace(DisplayName)
            ? RealName
            : DisplayName;

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(RealName) &&
                   (MemberInfo != null || MemberType == MemberInfoForRuntime.MemberType.This);
        }

        public MemberInfoForRuntime ToRuntimeInfo(UnityEngine.Object obj)
        {
            var runtimeInfo = new MemberInfoForRuntime();
            
            if (MemberType == MemberInfoForRuntime.MemberType.This)
                runtimeInfo.Assign(obj);
            else
                runtimeInfo.Assign(obj, MemberInfo);
            
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
                
                return ReflectionHelper.TryCreateThisMemberInfo(runtimeInfo.Object.GetType(), out editorInfo);
            }
            
            var memberInfo = runtimeInfo.MemberInfo;
            return ReflectionHelper.TryCreateMemberInfo(memberInfo, out editorInfo);
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
                   RealName == other.RealName &&
                   DisplayName == other.DisplayName &&
                   ValueType == other.ValueType &&
                   GetterSetterType == other.GetterSetterType &&
                   MemberType == other.MemberType;
        }

        bool Equals(MemberInfo lhs, MemberInfo rhs)
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
