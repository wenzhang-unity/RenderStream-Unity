using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace Disguise.RenderStream.Parameters
{
    static class ReflectionHelper
    {
        /// <summary>
        /// A filtered mapping of every <see cref="RemoteParameterWrapperAttribute.Type"/> to its <see cref="IRemoteParameterWrapper"/> implementation.
        /// </summary>
        static Dictionary<Type, Type> s_TypeToRemoteParameterWrapper = TypeCache.GetTypesWithAttribute<RemoteParameterWrapperAttribute>()
            .Where(t => typeof(IRemoteParameterWrapper).IsAssignableFrom(t))
            .Select(type => (type, type.GetCustomAttribute(typeof(RemoteParameterWrapperAttribute)) as RemoteParameterWrapperAttribute))
            .Where(tuple => tuple.Item2 != null)
            .GroupBy(tuple => tuple.Item2.Type)
            .ToDictionary(t => t.Key, t => t.OrderByDescending(tuple => tuple.Item2.Priority).First().type);

        /// <summary>
        /// Returns a user-friendly name of an enum value. Uses an associated <see cref="DisplayNameAttribute"/> directly
        /// or <see cref="ObjectNames.NicifyVariableName"/> of the real name when absent.
        /// </summary>
        public static IList<string> GetEnumDisplayNames(Type enumType)
        {
            var values = (Enum[]) Enum.GetValues(enumType);
            return values.Select(GetEnumDisplayName).ToArray();
        }
        
        /// <summary>
        /// Returns a user-friendly name of an enum value. Uses an associated <see cref="DisplayNameAttribute"/> directly
        /// or <see cref="ObjectNames.NicifyVariableName"/> of the real name when absent.
        /// </summary>
        public static string GetEnumDisplayName(Enum value)
        {
            var type = value.GetType();
            var memberInfos = type.GetMember(value.ToString());
            var memberInfo = memberInfos[0];

            return GetDisplayName(memberInfo) ?? ObjectNames.NicifyVariableName(memberInfo.Name);
        }

        /// <summary>
        /// Returns all the members available to be used as remote parameter targets for the provided Unity object type.
        /// </summary>
        public static MemberInfoForEditor[] GetSupportedMemberInfos(Type type)
        {
            // Includes public instance/static fields and properties.
            // Inherited members are included.
            // Only properties with public getters and setters are collected (the getter is used for the default parameter value).
            // Only members that have a type with a corresponding RemoteParameterWrapperAttribute are collected.
            
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Where(m => s_TypeToRemoteParameterWrapper.ContainsKey(m.FieldType))
                .Select(CreateMemberInfoFromField)
                .OrderBy(m => m.UIName)
                .ToArray();
            
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Where(m => s_TypeToRemoteParameterWrapper.ContainsKey(m.PropertyType) && m.GetGetMethod() != null && m.GetSetMethod() != null)
                .Select(CreateMemberInfoFromProperty)
                .OrderBy(m => m.UIName)
                .ToArray();
            
            return fields.Concat(properties).ToArray();
        }

        /// <summary>
        /// Creates a <see cref="MemberInfoForEditor"/> which contains cached data from the provided <see cref="MemberInfo"/>.
        /// </summary>
        public static bool TryCreateMemberInfo(MemberInfo memberInfo, out MemberInfoForEditor memberInfoForEditor)
        {
            if (memberInfo == null)
            {
                memberInfoForEditor = default;
                return false;
            }

            if (memberInfo is FieldInfo field)
            {
                if (!s_TypeToRemoteParameterWrapper.ContainsKey(field.FieldType))
                {
                    memberInfoForEditor = default;
                    return false;
                }
                
                memberInfoForEditor = CreateMemberInfoFromField(field);
            }
            else if (memberInfo is PropertyInfo property)
            {
                if (!s_TypeToRemoteParameterWrapper.ContainsKey(property.PropertyType))
                {
                    memberInfoForEditor = default;
                    return false;
                }
                
                memberInfoForEditor = CreateMemberInfoFromProperty(property);
            }
            else
            {
                memberInfoForEditor = default;
                return false;
            }

            return true;
        }

        static MemberInfoForEditor CreateMemberInfoFromField(FieldInfo field)
        {
            return new MemberInfoForEditor
            {
                MemberInfo = field,
                RealName = field.Name,
                DisplayName = GetDisplayName(field),
                ValueType = field.FieldType,
                GetterSetterType = s_TypeToRemoteParameterWrapper[field.FieldType]
            };
        }
        
        static MemberInfoForEditor CreateMemberInfoFromProperty(PropertyInfo property)
        {
            return new MemberInfoForEditor
            {
                MemberInfo = property,
                RealName = property.Name,
                DisplayName = GetDisplayName(property),
                ValueType = property.PropertyType,
                GetterSetterType = s_TypeToRemoteParameterWrapper[property.PropertyType]
            };
        }

        static string GetDisplayName(MemberInfo memberInfo)
        {
            var displayNameAttribute = memberInfo.GetCustomAttribute<DisplayNameAttribute>();
            return displayNameAttribute?.DisplayName;
        }
    }
}
