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
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
        public class MemberInfoCollectorAttribute : Attribute
        {
            public Type Type { get; }

            public int Priority { get; set; } = 0;

            public MemberInfoCollectorAttribute(Type type)
            {
                Type = type;
            }
        }
        
        public abstract class MemberInfoCollector
        {
            public abstract IEnumerable<MemberInfoForEditor> GetSupportedMemberInfos(UnityEngine.Object obj);
        }
        
        /// <summary>
        /// A filtered mapping of every <see cref="RemoteParameterWrapperAttribute.Type"/> to its <see cref="IRemoteParameterWrapper"/> implementation.
        /// </summary>
        private static readonly Dictionary<Type, Type> s_TypeToRemoteParameterWrapper = TypeCache.GetTypesWithAttribute<RemoteParameterWrapperAttribute>()
            .Where(t => typeof(IRemoteParameterWrapper).IsAssignableFrom(t))
            .SelectMany(type =>
            {
                return type.GetCustomAttributes(typeof(RemoteParameterWrapperAttribute))
                    .Select(attribute => (type, attribute as RemoteParameterWrapperAttribute));
            })
            .Where(tuple => tuple.Item2 != null)
            .GroupBy(tuple => tuple.Item2.Type)
            .ToDictionary(t => t.Key, t => t.OrderByDescending(tuple => tuple.Item2.Priority).First().type);

        private static readonly Dictionary<Type, MemberInfoCollector> s_TypeToCollector = TypeCache.GetTypesWithAttribute<MemberInfoCollectorAttribute>()
            .Where(t => typeof(MemberInfoCollector).IsAssignableFrom(t))
            .SelectMany(type =>
            {
                var instance = Activator.CreateInstance(type) as MemberInfoCollector;
                
                return type.GetCustomAttributes(typeof(MemberInfoCollectorAttribute))
                    .Select(attribute => (instance, type, attribute as MemberInfoCollectorAttribute));
            })
            .Where(tuple => tuple.Item3 != null)
            .GroupBy(tuple => tuple.Item3.Type)
            .ToDictionary(t => t.Key, t => t.OrderByDescending(tuple => tuple.Item3.Priority).First().instance);
        
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
        public static (IList<MemberInfoForEditor> mainInfo, IList<MemberInfoForEditor> extendedInfo) GetSupportedMemberInfos(UnityEngine.Object obj)
        {
            // Includes public instance/static fields and properties.
            // Inherited members are included.
            // Only properties with public getters and setters are collected (the getter is used for the default parameter value).
            // Only members that have a type with a corresponding RemoteParameterWrapperAttribute are collected.

            var type = obj.GetType();
            
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Where(m => TargetTypeIsSupported(m.FieldType))
                .Select(m => CreateMemberInfoFromField(obj, m))
                .OrderBy(m => m.UIName)
                .ToList();
            
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Where(m => TargetTypeIsSupported(m.PropertyType) && m.GetGetMethod() != null && m.GetSetMethod() != null)
                .Select(m => CreateMemberInfoFromProperty(obj, m))
                .OrderBy(m => m.UIName)
                .ToList();
            
            var mainInfos = fields.Concat(properties).ToList();
            var extendedInfos = new List<MemberInfoForEditor>();

            if (s_TypeToCollector.TryGetValue(type, out var collector))
            {
                var extended = collector.GetSupportedMemberInfos(obj);
                extended = extended.OrderBy(m => m.GroupPrefix).ThenBy(m => m.UIName);
                extendedInfos.AddRange(extended);
            }

            if (TryCreateThisMemberInfo(obj, out var thisInfo))
            {
                mainInfos.Insert(0, thisInfo);
            }
            
            return (mainInfos, extendedInfos);
        }
        
        static Type GetSearchType(Type type)
        {
            if (s_TypeToRemoteParameterWrapper.ContainsKey(type))
                return type;
            
            // Funnel the unregistered enum type into our generic Enum handler
            if (type.IsEnum)
                return typeof(Enum);
            
            // Funnel an unregistered enum generic into a user handler
            var curType = type;
            while (curType != null) // Walk up the inheritance chain
            {
                if (curType.IsGenericType)
                {
                    var args = curType.GenericTypeArguments;
                    if (args.Length > 0 && args[0].IsEnum)
                    {
                        return curType.GetGenericTypeDefinition().MakeGenericType(typeof(Enum));
                    }
                }

                curType = curType.BaseType;
            }

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
        public static bool TryCreateMemberInfo(UnityEngine.Object obj, MemberInfo memberInfo, out MemberInfoForEditor memberInfoForEditor)
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
                
                memberInfoForEditor = CreateMemberInfoFromField(obj, field);
            }
            else if (memberInfo is PropertyInfo property)
            {
                if (!TargetTypeIsSupported(property.PropertyType))
                {
                    memberInfoForEditor = default;
                    return false;
                }
                
                memberInfoForEditor = CreateMemberInfoFromProperty(obj, property);
            }
            else
            {
                memberInfoForEditor = default;
                return false;
            }

            return true;
        }

        public static bool TryCreateThisMemberInfo(UnityEngine.Object obj, out MemberInfoForEditor memberInfoForEditor)
        {
            var thisType = obj.GetType();
            
            if (!TargetTypeIsSupported(thisType))
            {
                memberInfoForEditor = default;
                return false;
            }
            
            memberInfoForEditor = new MemberInfoForEditor
            {
                Object = obj,
                RealName = "this",
                DisplayName = string.Empty,
                GetterSetterType = GetGetterSetterType(thisType),
                MemberInfo = null,
                ValueType = thisType,
                MemberType = MemberInfoForRuntime.MemberType.This
            };
            return true;
        }

        static MemberInfoForEditor CreateMemberInfoFromField(UnityEngine.Object obj, FieldInfo field)
        {
            return new MemberInfoForEditor
            {
                Object = obj,
                MemberInfo = field,
                RealName = field.Name,
                DisplayName = GetDisplayName(field),
                ValueType = field.FieldType,
                GetterSetterType = GetGetterSetterType(field.FieldType),
                MemberType = MemberInfoForRuntime.MemberType.Field
            };
        }
        
        static MemberInfoForEditor CreateMemberInfoFromProperty(UnityEngine.Object obj, PropertyInfo property)
        {
            return new MemberInfoForEditor
            {
                Object = obj,
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
