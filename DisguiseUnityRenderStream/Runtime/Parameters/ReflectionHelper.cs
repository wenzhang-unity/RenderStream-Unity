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
        /// A shortcut to get the type of the field or property described by <paramref name="memberInfo"/>.
        /// </summary>
        /// <exception cref="NotSupportedException">If <paramref name="memberInfo"/> is not a <see cref="FieldInfo"/> or a <see cref="PropertyInfo"/></exception>
        public static Type ResolveFieldOrPropertyType(MemberInfo memberInfo)
        {
            return memberInfo switch
            {
                FieldInfo fieldInfo => fieldInfo.FieldType,
                PropertyInfo propertyInfo => propertyInfo.PropertyType,
                null => null,
                _ => throw new NotSupportedException()
            };
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// A filtered mapping of every <see cref="RemoteParameterWrapperAttribute.Type"/> to its <see cref="IRemoteParameterWrapper"/> implementation.
        /// </summary>
        static readonly Dictionary<Type, Type> s_TypeToRemoteParameterWrapper = TypeCache.GetTypesWithAttribute<RemoteParameterWrapperAttribute>()
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
            return Enum.GetNames(enumType).Select(name =>
            {
                var memberInfo = enumType.GetField(name);
                return GetDisplayName(memberInfo) ?? ObjectNames.NicifyVariableName(memberInfo.Name);
            }).ToList();
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
                .Where(m => TargetTypeIsSupported(m.FieldType))
                .Select(CreateMemberInfoFromField)
                .OrderBy(m => m.UIName)
                .ToArray();
            
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Where(m => TargetTypeIsSupported(m.PropertyType) && m.GetGetMethod() != null && m.GetSetMethod() != null)
                .Select(CreateMemberInfoFromProperty)
                .OrderBy(m => m.UIName)
                .ToArray();
            
            var infos = fields.Concat(properties).ToList();

            if (TryCreateThisMemberInfo(type, out var thisInfo))
            {
                infos.Insert(0, thisInfo);
            }
            
            return infos.ToArray();
        }
        
        static Type GetSearchType(Type type)
        {
            if (s_TypeToRemoteParameterWrapper.ContainsKey(type))
                return type;
            
            // Funnel the unregistered enum type into our generic Enum handler
            if (type.IsEnum)
                return typeof(Enum);

            return type;
        }

        static bool TargetTypeIsSupported(Type type)
        {
            var searchType = GetSearchType(type);
            return s_TypeToRemoteParameterWrapper.ContainsKey(searchType);
        }
        
        static Type GetGetterSetterType(Type type)
        {
            var searchType = GetSearchType(type);
            return s_TypeToRemoteParameterWrapper[searchType];
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
                if (!TargetTypeIsSupported(field.FieldType))
                {
                    memberInfoForEditor = default;
                    return false;
                }
                
                memberInfoForEditor = CreateMemberInfoFromField(field);
            }
            else if (memberInfo is PropertyInfo property)
            {
                if (!TargetTypeIsSupported(property.PropertyType))
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

        public static bool TryCreateThisMemberInfo(Type thisType, out MemberInfoForEditor memberInfoForEditor)
        {
            if (!TargetTypeIsSupported(thisType))
            {
                memberInfoForEditor = default;
                return false;
            }
            
            memberInfoForEditor = new MemberInfoForEditor
            {
                RealName = "this",
                DisplayName = string.Empty,
                GetterSetterType = GetGetterSetterType(thisType),
                MemberInfo = null,
                ValueType = thisType,
                MemberType = MemberInfoForRuntime.MemberType.This
            };
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
                GetterSetterType = GetGetterSetterType(field.FieldType),
                MemberType = MemberInfoForRuntime.MemberType.Field
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
                GetterSetterType = GetGetterSetterType(property.PropertyType),
                MemberType = MemberInfoForRuntime.MemberType.Property
            };
        }

        static string GetDisplayName(MemberInfo memberInfo)
        {
            var displayNameAttribute = memberInfo.GetCustomAttribute<DisplayNameAttribute>();
            return displayNameAttribute?.DisplayName;
        }
#endif
    }
}
