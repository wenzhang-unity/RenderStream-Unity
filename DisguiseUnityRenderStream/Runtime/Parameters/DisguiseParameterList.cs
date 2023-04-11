using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Disguise.RenderStream.Parameters
{
    [Serializable]
    class DisguiseParameterList : MonoBehaviour
    {
        [SerializeField]
        public List<ParameterGroup> m_Groups = new List<ParameterGroup>();
        
        public static DisguiseParameterList FindInstance()
        {
            var parameterLists = FindObjectsByType<DisguiseParameterList>(FindObjectsSortMode.None);
            if (parameterLists.Length == 1)
            {
                var parameterList = parameterLists[0];
                return parameterList;
            }
            else if (parameterLists.Length == 0)
            {
                Debug.LogError($"Failed to find a {nameof(DisguiseParameterList)} instance in the current scene");
            }
            else
            {
                Debug.LogError($"Found multiple {nameof(DisguiseParameterList)} instances in the current scene, but expected a single instance");
            }

            return null;
        }
        
        public IList<(ParameterGroup group, Parameter parameter)> GetParametersOrderedForSchema(bool excludeDisabled = true)
        {
            var parameters = new List<(ParameterGroup group, Parameter parameter)>();
            
            foreach (var group in m_Groups)
            {
                if (excludeDisabled && !group.Enabled)
                    continue;

                foreach (var parameter in group.m_Parameters)
                {
                    if (excludeDisabled && !parameter.Enabled)
                        continue;

                    parameters.Add((group, parameter));
                }
            }

            return parameters;
        }
        
        public List<(IRemoteParameterWrapper parameter, int parameterID)> GetRemoteParameterWrappers()
        {
            // Ensure that the execution order will be the same as the order defined in the schema:
            var parameters = GetParametersOrderedForSchema();
            
            var wrappers = new List<(IRemoteParameterWrapper parameter, int parameterID)>();

            foreach (var (_, parameter) in parameters)
            {
                if (parameter.RemoteParameterWrapper is { IsValid: true } validWrapper)
                    wrappers.Add((validWrapper, parameter.ID));
            }

            return wrappers;
        }

        void OnEnable()
        {
            foreach (var group in m_Groups)
            {
                if (!group.Enabled)
                    continue;
                
                foreach (var parameter in group.m_Parameters)
                {
                    if (!parameter.Enabled)
                        continue;
                    
                    parameter.OnEnable();
                }
            }
        }
        
#if UNITY_EDITOR
        public const int NumIDsForInternalUse = 100;
        
        public UnityEditor.IMGUI.Controls.TreeViewState TreeViewState => m_TreeViewState;
        
        [SerializeField]
        UnityEditor.IMGUI.Controls.TreeViewState m_TreeViewState = new UnityEditor.IMGUI.Controls.TreeViewState();

        [SerializeField]
        int m_IDCounter = NumIDsForInternalUse;

        [SerializeField]
        Guid m_GUID = Guid.NewGuid();

        public Guid GUID => m_GUID;
        
        public ParameterGroup DefaultGroup => m_Groups[0];
        
        void OnValidate()
        {
            // Create the default group if it isn't already there
            if (m_Groups.Count == 0 || !m_Groups[0].IsDefaultGroup)
            {
                m_Groups.Insert(0, new ParameterGroup(true) { ID = ReserveID() });
            }
        }

        public int ReserveID()
        {
            checked
            {
                m_IDCounter++;
                return m_IDCounter;
            }
        }
#endif
    }
}
