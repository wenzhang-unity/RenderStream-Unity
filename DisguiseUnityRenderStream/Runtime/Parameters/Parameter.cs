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
        [SerializeField]
        bool m_Enabled = true;
        
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
        
        public IRemoteParameterWrapper RemoteParameterWrapper => m_RemoteParameterWrapper;

        public void OnEnable()
        {
            m_MemberInfoForRuntime.Apply(m_RemoteParameterWrapper);
        }
        
#if UNITY_EDITOR
        [SerializeField]
        string m_Name;

        [SerializeField]
        int m_ID;
        
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

        public int ID
        {
            get => m_ID;
            set => m_ID = value;
        }
        
        public UnityEngine.Object Object
        {
            get => m_Object;
            set
            {
                if (value != m_Object)
                {
                    m_Object = value;
                    m_Component = null;
                    
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
                    m_Component = value;
                    m_Object = m_Component.gameObject;
                    
                    MemberInfoForEditor = default;
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
                    parameters.Add(CreateManagedRemoteParameter(p.Type, group.DisguiseSchemaName, Name, ID.ToString(), p.Suffix, p.DefaultMin, p.DefaultMax, 1f, p.DefaultValue, options));
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
#endif
    }
}
