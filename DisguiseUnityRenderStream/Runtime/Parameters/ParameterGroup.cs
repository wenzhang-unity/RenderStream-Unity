using System;
using System.Collections.Generic;
using UnityEngine;

namespace Disguise.RenderStream.Parameters
{
    [Serializable]
    class ParameterGroup
    {
        [SerializeField]
        public bool m_Enabled = true;
        
        [SerializeField]
        public List<Parameter> m_Parameters = new List<Parameter>();
        
#if UNITY_EDITOR
        const string DefaultGroupUnityDisplayName = "Default Group";
        const string DefaultGroupDisguiseSchemaName = "Properties";
        
        [SerializeField]
        string m_Name;
        
        [SerializeField]
        int m_ID;

        [SerializeField]
        bool m_IsDefaultGroup;

        public ParameterGroup(bool isDefaultGroup = false)
        {
            m_IsDefaultGroup = isDefaultGroup;

            if (m_IsDefaultGroup)
            {
                m_Name = DefaultGroupUnityDisplayName;
            }
        }

        public ParameterGroup Clone()
        {
            var clone = new ParameterGroup
            {
                m_Name = m_Name,
                m_Enabled = m_Enabled,
                m_IsDefaultGroup = false // There can only be one default group
            };

            foreach (var parameter in m_Parameters)
            {
                clone.m_Parameters.Add(parameter.Clone());
            }

            return clone;
        }

        public bool IsDefaultGroup => m_IsDefaultGroup;

        public string UnityDisplayName => m_IsDefaultGroup ? DefaultGroupUnityDisplayName : m_Name;
        
        public string DisguiseSchemaName => m_IsDefaultGroup ? DefaultGroupDisguiseSchemaName : m_Name;
        
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
#endif
    }
}
