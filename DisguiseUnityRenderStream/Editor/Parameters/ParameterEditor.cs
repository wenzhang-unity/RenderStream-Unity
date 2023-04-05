using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Disguise.RenderStream.Parameters
{
    [Flags]
    enum ParameterEditorFlags
    {
        IntMinMax,
        FloatMinMax,
        FloatStep,
        Options
    }
    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    class ParameterEditorAttribute : Attribute
    {
        public ParameterEditorFlags Flags { get; }

        public int Priority { get; set; } = 0;

        public ParameterEditorAttribute(ParameterEditorFlags flags)
        {
            Flags = flags;
        }
    }
    
    /// <summary>
    /// Not yet implemented.
    /// The idea is to display parameter details and for users to be able to override the default min/max/step fields of remote parameters.
    /// Each <see cref="IRemoteParameterWrapper"/> type would be able to specify which UI sections to display through <see cref="ParameterEditorFlags"/>.
    /// User-provided custom sections would technically be possible with a similar attribute approach but out of scope for now.
    /// </summary>
    class ParameterEditor : ScriptableObject
    {
        [SerializeField]
        VisualTreeAsset m_Layout;

        Parameter m_Parameter;

        VisualElement m_IntMinMaxSection;
        VisualElement m_FloatMinMaxSection;
        VisualElement m_StepSection;
        VisualElement m_OptionsSection;

        ListView m_OptionsList;

        public VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            m_Layout.CloneTree(root);

            m_IntMinMaxSection = root.Q<VisualElement>("int-minmax-section");
            m_FloatMinMaxSection = root.Q<VisualElement>("float-minmax-section");
            m_StepSection = root.Q<VisualElement>("step-section");
            m_OptionsSection = root.Q<VisualElement>("options-section");

            m_OptionsList = m_OptionsSection.Q<ListView>("options-list");
            
            return root;
        }

        public void Bind(Parameter parameter)
        {
            if (parameter == m_Parameter)
                return;
            
            m_Parameter = parameter;
            
            m_IntMinMaxSection.SetDisplay(false);
            m_FloatMinMaxSection.SetDisplay(false);
            m_StepSection.SetDisplay(false);
            m_OptionsSection.SetDisplay(false);
            
            m_OptionsList.Clear();
            
            // TODO
        }
    }
    
    /// <summary>
    /// Displays a <see cref="Toggle"/> before its content.
    /// The toggle's value sets the content's enabled state (using <see cref="VisualElement.SetEnabled"/>).
    /// </summary>
    class OverrideBlock : VisualElement
    {
        public static class Style
        {
            public const string OverrideBlock = "override-block";
            public const string OverrideBlockContainer = "override-block-container";
        }
        
        public new class UxmlFactory : UxmlFactory<OverrideBlock> {}
        
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlBoolAttributeDescription m_OverrideEnabled = new UxmlBoolAttributeDescription { name = "override-enabled" };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var target = ve as OverrideBlock;
                target.OverrideEnabled = m_OverrideEnabled.GetValueFromBag(bag, cc);
            }
        }
        
        public override VisualElement contentContainer { get; }

        public bool OverrideEnabled
        {
            get => m_Toggle.value;
            set => m_Toggle.value = value;
        }

        readonly Toggle m_Toggle;

        public OverrideBlock()
        {
            AddToClassList(Style.OverrideBlock);
            
            m_Toggle = new Toggle();
            hierarchy.Insert(0, m_Toggle);

            contentContainer = new VisualElement();
            hierarchy.Insert(1, contentContainer);
            contentContainer.AddToClassList(Style.OverrideBlockContainer);

            m_Toggle.RegisterValueChangedCallback(OnToggleValueChanged);
            
            if (!OverrideEnabled)
                contentContainer.SetEnabled(false);
        }

        void OnToggleValueChanged(ChangeEvent<bool> evt)
        {
            contentContainer.SetEnabled(evt.newValue);
        }
    }
}
