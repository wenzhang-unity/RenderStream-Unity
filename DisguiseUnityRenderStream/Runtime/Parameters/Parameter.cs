using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Disguise.RenderStream.Parameters
{
    [Serializable]
    class Parameter
    {
        public const string KeySeparator = " ";
        
        [SerializeField]
        bool m_Enabled = true;
        
        [SerializeField]
        int m_ID;
        
        [SerializeField]
        MemberInfoForRuntime m_MemberInfoForRuntime = new MemberInfoForRuntime();

        [SerializeReference]
        IRemoteParameterWrapper m_RemoteParameterWrapper;
        
        public bool Enabled
        {
            get => m_Enabled;
#if UNITY_EDITOR
            set => m_Enabled = value;
#endif
        }
        
        public int ID
        {
            get => m_ID;
#if UNITY_EDITOR
            set => m_ID = value;
#endif
        }
        
        public IRemoteParameterWrapper RemoteParameterWrapper => m_RemoteParameterWrapper;

        public void OnEnable()
        {
            m_MemberInfoForRuntime.Apply(m_RemoteParameterWrapper);
        }

        public static int ResolveIDFromSchema(ManagedRemoteParameter schemaParameter)
        {
            var key = schemaParameter.key;

            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Empty parameter key");
            
            var separatorIndex = key.IndexOf(KeySeparator);
            if (separatorIndex <= 0)
                throw new InvalidOperationException("Incorrect key format: no separator found");
            
            var idString = key.Substring(0, separatorIndex + 1);

            if (int.TryParse(idString, out int id))
                return id;
            else
                throw new InvalidOperationException("Incorrect key format: first word was not an integer");
        }
        
#if UNITY_EDITOR
        [SerializeField]
        string m_Name;
        
        [SerializeField]
        public bool m_HasCustomName;

        [SerializeField]
        UnityEngine.Object m_Object;
        
        [SerializeField]
        Component m_Component;
        
        [SerializeField]
        MemberInfoForEditor m_MemberInfoForEditor;
        
        public Parameter Clone()
        {
            return new Parameter
            {
                m_Name = m_Name,
                m_Enabled = m_Enabled,
                m_HasCustomName = m_HasCustomName,
                m_Component = m_Component,
                m_Object = m_Object,
                m_MemberInfoForEditor = m_MemberInfoForEditor,
                m_MemberInfoForRuntime = m_MemberInfoForRuntime,
                m_RemoteParameterWrapper = m_RemoteParameterWrapper
            };
        }
        
        public string Name
        {
            get => m_Name;
            set => m_Name = value;
        }
        
        public UnityEngine.Object Object
        {
            get => m_Object;
            set
            {
                if (value != m_Object)
                {
                    var resetMemberInfo = true;
                    Component newComponent = null;
                    
                    if (value != null && m_Object != null)
                    {
                        if (m_Component == null)
                        {
                            // A new Asset or GameObject of the same type?
                            resetMemberInfo = !m_Object.GetType().IsInstanceOfType(value);
                        }
                        else if (value is GameObject newGameObject)
                        {
                            // A new GameObject that has the same Component?
                            foreach (var component in newGameObject.GetComponents<Component>())
                            {
                                if (m_Component.GetType().IsInstanceOfType(component))
                                {
                                    resetMemberInfo = false;
                                    newComponent = component;
                                    break;
                                }
                            }
                        }
                    }
                    
                    m_Object = value;
                    m_Component = newComponent;
                    
                    if (!RefreshExtendedInfoIfNeeded() && resetMemberInfo)
                        MemberInfoForEditor = default;
                    
                    ApplyMemberInfo(MemberInfoForEditor);

                    if (!m_HasCustomName)
                    {
                        AutoAssignName();
                    }
                }
            }
        }
        
        public Component Component
        {
            get => m_Component;
            set
            {
                if (value != m_Component)
                {
                    var oldComponent = m_Component;
                    m_Component = value;
                    m_Object = m_Component.gameObject;
                    
                    // A new Component of a different type?
                    if (!RefreshExtendedInfoIfNeeded() &&
                        m_Component != null && oldComponent != null &&
                        !oldComponent.GetType().IsInstanceOfType(m_Component))
                    {
                        MemberInfoForEditor = default;
                    }
                    
                    ApplyMemberInfo(MemberInfoForEditor);

                    if (!m_HasCustomName)
                    {
                        AutoAssignName();
                    }
                }
            }
        }
        
        public UnityEngine.Object ReflectedObject => m_Object is GameObject ? m_Component : m_Object;
        
        public MemberInfoForEditor MemberInfoForEditor
        {
            get => m_MemberInfoForEditor;
            set
            {
                m_MemberInfoForEditor = value;
                ApplyMemberInfo(m_MemberInfoForEditor);
                
                if (!m_HasCustomName)
                {
                    AutoAssignName();
                }
            }
        }

        public MemberInfo MemberInfo => m_MemberInfoForRuntime.Target.MemberInfo;

        bool RefreshExtendedInfoIfNeeded()
        {
            if (ReflectedObject == null || m_MemberInfoForRuntime.Target.Object == null)
                return false;
            
            // This parameter was discovered by MemberInfoCollector, we need to recover its group information
            // and validate that its target object hasn't changed externally.
            if (ReflectedObject != m_MemberInfoForRuntime.Target.Object)
            {
                var (_, extendedInfo) = ReflectionHelper.GetSupportedMemberInfos(ReflectedObject);
                foreach (var info in extendedInfo)
                {
                    if (info.Target == m_MemberInfoForRuntime.Target &&
                        info.MemberType == m_MemberInfoForRuntime.Type)
                    {
                        m_MemberInfoForEditor = info;
                        return true;
                    }
                    
                    // We haven't found an exact match, but try auto-assign to an object of the same type
                    if (m_MemberInfoForRuntime.Target.Object.GetType().IsInstanceOfType(info.Target.Object) &&
                        info.MemberType == m_MemberInfoForRuntime.Type &&
                        Target.Equals(info.Target.MemberInfo, m_MemberInfoForRuntime.Target.MemberInfo))
                    {
                        m_MemberInfoForEditor = info;
                        return true;
                    }
                }
            }

            return false;
        }

        public void RefreshMemberInfoForEditor()
        {
            if (!RefreshExtendedInfoIfNeeded())
            {
                if (MemberInfoForEditor.TryCreateFromRuntimeInfo(m_MemberInfoForRuntime, out var editorInfo))
                {
                    m_MemberInfoForEditor = editorInfo;
                }
            }
        }

        static string NicifyMemberInfoName(MemberInfoForEditor memberInfo)
        {
            var name = string.IsNullOrWhiteSpace(memberInfo.DisplayName)
                ? ObjectNames.NicifyVariableName(memberInfo.RealName)
                : memberInfo.DisplayName;

            if (!string.IsNullOrWhiteSpace(memberInfo.GroupPrefix))
                name = $"{memberInfo.GroupPrefix}/{name}";

            return name;
        }
        
        public void AutoAssignName()
        {
            if (m_Object != null)
            {
                if (m_Component != null)
                {
                    Name = (MemberInfoForEditor.IsValid() && MemberInfoForEditor.MemberType != MemberInfoForRuntime.MemberType.This)
                        ? $"{m_Object.name} {ObjectNames.NicifyVariableName(m_Component.GetType().Name)} {NicifyMemberInfoName(MemberInfoForEditor)}"
                        : $"{m_Object.name} {ObjectNames.NicifyVariableName(m_Component.GetType().Name)}";
                }
                else
                {
                    Name = (MemberInfoForEditor.IsValid() && MemberInfoForEditor.MemberType != MemberInfoForRuntime.MemberType.This)
                        ? $"{m_Object.name} {NicifyMemberInfoName(MemberInfoForEditor)}"
                        : m_Object.name;
                }
            }
            else
            {
                Name = "New Parameter";
            }
        }

        void ApplyMemberInfo(MemberInfoForEditor memberInfo)
        {
            if (memberInfo.IsValid() && ReflectedObject != null)
            {
                var remoteParameterWrapper = Activator.CreateInstance(memberInfo.GetterSetterType) as IRemoteParameterWrapper;

                m_MemberInfoForRuntime = memberInfo.ToRuntimeInfo();
                m_MemberInfoForRuntime.Apply(remoteParameterWrapper);
                m_RemoteParameterWrapper = remoteParameterWrapper;
            }
            else
            {
                m_MemberInfoForRuntime.Assign(default);
                m_RemoteParameterWrapper = null;
            }
        }

        public bool TryGetParametersForSchema(ParameterGroup group, out IList<ManagedRemoteParameter> parametersForSchema)
        {
            OnEnable();
            
            if (RemoteParameterWrapper is { IsValid: true } wrapper)
            {
                var parameters = new List<ManagedRemoteParameter>();
                
                foreach (var parameterDesc in wrapper.GetParametersForSchema())
                {
                    var schemaParameter = CreateManagedRemoteParameter(parameterDesc, group.DisguiseSchemaName, Name, Key);
                    parameters.Add(schemaParameter);
                }

                parametersForSchema = parameters;
                return true;
            }

            parametersForSchema = default;
            return false;
        }
        
        public static ManagedRemoteParameter CreateManagedRemoteParameter(DisguiseRemoteParameter parameterDesc, string group, string displayName, string key)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(group));
            Debug.Assert(!string.IsNullOrWhiteSpace(displayName));
            Debug.Assert(!string.IsNullOrWhiteSpace(key));
            
            var options = parameterDesc.Options ?? Array.Empty<string>();
            
            var processedKey = key + (String.IsNullOrEmpty(parameterDesc.Suffix) ? string.Empty : "_" + parameterDesc.Suffix);
            var processedDisplayName = displayName + (String.IsNullOrEmpty(parameterDesc.Suffix) ? string.Empty : " " + parameterDesc.Suffix);
            
            var parameter = new ManagedRemoteParameter
            {
                group = group,
                displayName = processedDisplayName,
                key = processedKey,
                type = parameterDesc.Type,
                min = parameterDesc.DefaultMin,
                max = parameterDesc.DefaultMax,
                defaultValue = parameterDesc.DefaultValue,
                options = options,
                dmxOffset = -1,
                dmxType = RemoteParameterDmxType.RS_DMX_16_BE
            };

            // Could have UI in the future
            parameter.step = (parameter, parameterDesc.IntegralTypeHint) switch
            {
                { parameter: { type: RemoteParameterType.RS_PARAMETER_IMAGE } } => 1f,
                { parameter: { type: RemoteParameterType.RS_PARAMETER_POSE } } => 1f,
                { parameter: { type: RemoteParameterType.RS_PARAMETER_TRANSFORM } } => 1f,
                { parameter: { type: RemoteParameterType.RS_PARAMETER_TEXT } } => 0f,
                { parameter: { options: { Length: > 0 } } } => 1f,
                { parameter: { type: RemoteParameterType.RS_PARAMETER_NUMBER }, IntegralTypeHint: true} => 1f,
                { parameter: { type: RemoteParameterType.RS_PARAMETER_NUMBER }, IntegralTypeHint: false} => 0.001f,
                _ => throw new NotSupportedException($"Can't determine a step value for parameter {displayName} in group {group}")
            };
            
            return parameter;
        }
        
        public string Key => ID.ToString() + KeySeparator + Name;
#endif
    }
}
