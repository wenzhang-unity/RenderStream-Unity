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
        const string KeySeparator = " ";
        
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
                m_Object = m_Object,
                m_Component = m_Component
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
                            resetMemberInfo = value.GetType() != m_Object.GetType();
                        }
                        else if (value is GameObject newGameObject)
                        {
                            // A new GameObject that has the same Component?
                            if (newGameObject.GetComponent(m_Component.GetType()) is { } matchingComponent)
                            {
                                resetMemberInfo = false;
                                newComponent = matchingComponent;
                            }
                        }
                    }
                    
                    if (resetMemberInfo)
                        MemberInfoForEditor = default;
                    
                    m_Object = value;
                    m_Component = newComponent;
                    
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
                    // A new Component of a different type?
                    if (value != null && m_Component != null &&
                        value.GetType() != m_Component.GetType())
                    {
                        MemberInfoForEditor = default;
                    }
                    
                    m_Component = value;
                    m_Object = m_Component.gameObject;
                    
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

        public MemberInfo MemberInfo => m_MemberInfoForRuntime.MemberInfo;

        static string NicifyMemberInfoName(MemberInfoForEditor memberInfo)
        {
            return string.IsNullOrWhiteSpace(memberInfo.DisplayName)
                ? ObjectNames.NicifyVariableName(memberInfo.RealName)
                : memberInfo.DisplayName;
        }
        
        public void AutoAssignName()
        {
            if (m_Object != null)
            {
                if (m_Component != null)
                {
                    Name = MemberInfoForEditor.IsValid()
                        ? $"{m_Object.name} {ObjectNames.NicifyVariableName(m_Component.GetType().Name)} {NicifyMemberInfoName(MemberInfoForEditor)}"
                        : $"{m_Object.name} {ObjectNames.NicifyVariableName(m_Component.GetType().Name)}";
                }
                else
                {
                    Name = MemberInfoForEditor.IsValid()
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
            if (memberInfo.IsValid())
            {
                var remoteParameterWrapper = Activator.CreateInstance(memberInfo.GetterSetterType) as IRemoteParameterWrapper;
            
                m_MemberInfoForRuntime.Assign(ReflectedObject, memberInfo.MemberInfo);
                m_MemberInfoForRuntime.Apply(remoteParameterWrapper);
                m_RemoteParameterWrapper = remoteParameterWrapper;
            }
            else
            {
                m_MemberInfoForRuntime.Assign(ReflectedObject, null);
                m_RemoteParameterWrapper = null;
            }
        }

        public bool TryGetParametersForSchema(ParameterGroup group, out IList<ManagedRemoteParameter> parametersForSchema)
        {
            if (RemoteParameterWrapper is { } wrapper)
            {
                var parameters = new List<ManagedRemoteParameter>();
                
                foreach (var p in wrapper.GetParametersForSchema())
                {
                    // TODO handle "step"

                    var options = p.Options ?? new string[]{};
                    parameters.Add(CreateManagedRemoteParameter(p.Type, group.DisguiseSchemaName, Name, Key, p.Suffix, p.DefaultMin, p.DefaultMax, 1f, p.DefaultValue, options));
                }

                parametersForSchema = parameters;
                return true;
            }

            parametersForSchema = default;
            return false;
        }
        
        ManagedRemoteParameter CreateManagedRemoteParameter(RemoteParameterType type, string group, string displayName, string key, string suffix, float min, float max, float step, object defaultValue, string[] options)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(group));
            Debug.Assert(!string.IsNullOrWhiteSpace(displayName));
            Debug.Assert(!string.IsNullOrWhiteSpace(key));
            
            var processedKey = key + (String.IsNullOrEmpty(suffix) ? string.Empty : "_" + suffix);
            var processedDisplayName = displayName + (String.IsNullOrEmpty(suffix) ? string.Empty : " " + suffix);
        
            var parameter = new ManagedRemoteParameter
            {
                group = group,
                displayName = processedDisplayName,
                key = processedKey,
                type = type,
                min = min,
                max = max,
                step = step,
                defaultValue = defaultValue,
                options = options,
                dmxOffset = -1,
                dmxType = RemoteParameterDmxType.RS_DMX_16_BE
            };
            
            return parameter;
        }
        
        public string Key => ID.ToString() + KeySeparator + Name;
#endif
    }
}
