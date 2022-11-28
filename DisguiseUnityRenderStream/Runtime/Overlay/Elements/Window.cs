using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Disguise.RenderStream.Overlay.Elements
{
    public class Window : VisualElement
    {
        static class ResourcePaths
        {
            public const string Layout = OverlayConstants.LayoutsPath + "/Window";
        }

        static class StyleClass
        {
            public const string Window = "window";
            public const string Minimized = "window-minimized";
            public const string NotMinimized = "window-not-minimized";
            public const string Maximized = "window-maximized";
            public const string NotMaximized = "window-not-maximized";
            public const string Resizeable = "window-resizeable";
            public const string NotResizeable = "window-not-resizeable";
            
            public const string TitleBar = "window-title-bar";
            public const string Title = "window-title";
            public const string ButtonMinimize = "window-button-minimize";
            public const string ButtonMaximize = "window-button-maximize";
            public const string ButtonClose = "window-button-close";
            public const string InnerContent = "window-inner-content";
        }
        
        public Action OnMinimize = delegate { };
        public Action OnMaximize = delegate { };
        public Action OnClose = delegate { };

        public bool Minimized
        {
            get => m_State.Minimized;
            set
            {
                if (m_State.Minimized != value)
                {
                    Minimize();
                }
            }
        }
        public bool Maximized
        {
            get => m_State.Maximized;
            set
            {
                if (m_State.Maximized != value)
                {
                    Maximize();
                }
            }
        }

        [Serializable]
        public struct Layout
        {
            public Vector2 Position;
            public Vector2 Size;
        }

        State m_State = new State();
        
        [Serializable]
        public struct State
        {
            public float PreMinimizedHeight;
            public Layout PreMaximizedLayout;
            public Layout Layout;
            public bool Minimized;
            public bool Maximized;
        }

        bool m_IsOpen = true;
        public bool IsOpen
        {
            get => m_IsOpen;
            set
            {
                if (m_IsOpen && !value)
                    Close();
                else if (!m_IsOpen && value)
                    Open();
            }
        }
        
        public string SaveKey
        {
            get;
            set;
        }

        string m_Title;
        public string Title
        {
            get => m_Title;
            set
            {
                m_Title = value;
                m_TitleLabel.text = m_Title;
            }
        }

        bool m_ShowMinimizeButton;
        public bool ShowMinimizeButton
        {
            get => m_ShowMinimizeButton;
            set
            {
                m_ShowMinimizeButton = value;
                UIUtils.ShowElement(m_ButtonMinimize, m_ShowMinimizeButton);
            }
        }
        
        bool m_ShowMaximizeButton;
        public bool ShowMaximizeButton
        {
            get => m_ShowMaximizeButton;
            set
            {
                m_ShowMaximizeButton = value;
                UIUtils.ShowElement(m_ButtonMaximize, m_ShowMaximizeButton);
            }
        }

        bool m_ShowCloseButton;
        public bool ShowCloseButton
        {
            get => m_ShowCloseButton;
            set
            {
                m_ShowCloseButton = value;
                UIUtils.ShowElement(m_ButtonClose, m_ShowCloseButton);
            }
        }
        
        bool m_IsResizeable;
        public bool Resizeable
        {
            get => m_IsResizeable;
            set
            {
                m_IsResizeable = value;
                
                if (m_IsResizeable && m_Resizeable == null)
                {
                    m_Resizeable = new Resizeable(this);
                }
                else if (!m_IsResizeable && m_Resizeable != null)
                {
                    m_Resizeable.Unregister();
                    m_Resizeable = null;
                }
                
                EnableInClassList(StyleClass.Resizeable, m_IsResizeable);
                EnableInClassList(StyleClass.NotResizeable, !m_IsResizeable);
            }
        }

        Label m_TitleLabel;
        Button m_ButtonMinimize;
        Button m_ButtonMaximize;
        Button m_ButtonClose;
        VisualElement m_InnerContent;

        Moveable m_Moveable;
        Resizeable m_Resizeable;

        public Window()
        {
            var doc = Resources.Load<VisualTreeAsset>(ResourcePaths.Layout);
            doc.CloneTree(this);
            
            AddToClassList(StyleClass.Window);
            AddToClassList(StyleClass.NotMinimized);
            AddToClassList(StyleClass.NotMaximized);
            AddToClassList(StyleClass.NotResizeable);
            AddToClassList(OverlayConstants.LayoutAlignStart);
            AddToClassList(OverlayConstants.LayoutNoShrink);

            m_TitleLabel = this.Q<Label>(null, StyleClass.Title);
            m_ButtonMinimize = this.Q<Button>(null, StyleClass.ButtonMinimize);
            m_ButtonMaximize = this.Q<Button>(null, StyleClass.ButtonMaximize);
            m_ButtonClose = this.Q<Button>(null, StyleClass.ButtonClose);
            m_InnerContent = this.Q<VisualElement>(null, StyleClass.InnerContent);

            m_ButtonMinimize.clicked += Minimize;
            m_ButtonMaximize.clicked += Maximize;
            m_ButtonClose.clicked += Close;

            m_Moveable = new Moveable(this);
            
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<MouseDownEvent>(OnMouseDown);

            SetMaxSize(parent.localBound.size);
            parent.RegisterCallback<GeometryChangedEvent>(OnParentGeometryChanged);
        }
        
        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            ViewData.Save(SaveKey, this);
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            ViewData.Load(SaveKey, this);
            
            UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            BringToFront();
        }
        
        void OnParentGeometryChanged(GeometryChangedEvent evt)
        {
            SetMaxSize(evt.newRect.size);
        }

        void SetMaxSize(Vector2 maxSize)
        {
            style.maxWidth = maxSize.x;
            style.maxHeight = maxSize.y;
        }

        public override VisualElement contentContainer
        {
            get
            {
                return m_InnerContent;
            }
        }
        
        void Minimize()
        {
            var skipSaveHeight = false;
            
            if (m_State.Maximized)
            {
                skipSaveHeight = true;
                Maximize();
            }
            
            m_State.Minimized = !m_State.Minimized;
            UIUtils.ShowElement(m_InnerContent, !m_State.Minimized);

            if (m_IsResizeable)
            {
                if (m_State.Minimized)
                {
                    if (!skipSaveHeight)
                    {
                        m_State.PreMinimizedHeight = m_Resizeable.GetSize().y;
                    }
                    m_Resizeable.SetHeight(0f);
                }
                else
                {
                    m_Resizeable.SetHeight(m_State.PreMinimizedHeight);
                }
            }

            m_ButtonMinimize.text = m_State.Minimized ? ">" : "▼";
            EnableInClassList(StyleClass.Minimized, m_State.Minimized);
            EnableInClassList(StyleClass.NotMinimized, !m_State.Minimized);
            
            OnMinimize();
        }

        void Maximize()
        {
            if (m_State.Minimized)
            {
                Minimize();
            }
            
            m_State.Maximized = !m_State.Maximized;
            
            if (m_State.Maximized)
            {
                SaveLayout(ref m_State.PreMaximizedLayout);
                
                style.width = Length.Percent(100);
                style.height = Length.Percent(100);
                
                BringToFront();
            }
            else
            {
                style.width = StyleKeyword.Auto;
                style.height = StyleKeyword.Auto;
                
                SetLayout(m_State.PreMaximizedLayout);
            }

            m_ButtonMaximize.text = m_State.Maximized ? "[]" : "[ ]";
            EnableInClassList(StyleClass.Maximized, m_State.Maximized);
            EnableInClassList(StyleClass.NotMaximized, !m_State.Maximized);

            OnMaximize();
        }

        void Close()
        {
            m_IsOpen = false;
            UIUtils.ShowElement(this, false);

            OnClose();
        }

        void Open()
        {
            m_IsOpen = true;
            UIUtils.ShowElement(this, true);
        }

        void SaveLayout(ref Layout layout)
        {
            layout.Position = m_Moveable.GetPosition();
            if (m_IsResizeable)
            {
                layout.Size = m_Resizeable.GetSize();
            }
        }

        void SetLayout(Layout layout)
        {
            m_Moveable.SetPositionNoClamp(layout.Position);
            if (m_IsResizeable)
            {
                m_Resizeable.SetSize(layout.Size);
            }
        }

        public State GetState()
        {
            if (!m_State.Maximized)
            {
                SaveLayout(ref m_State.Layout);
            }
            return m_State;
        }

        public void SetState(State state)
        {
            if (m_State.Minimized != state.Minimized)
            {
                Minimize();
            }

            if (m_State.Maximized != state.Maximized)
            {
                Maximize();
            }

            m_State = state;
            if (!m_State.Maximized)
            {
                SetLayout(m_State.Layout);
            }
        }

        public void ResetState()
        {
            ViewData.Reset(SaveKey);
            m_State = new State();
            SetState(m_State);
        }

        public new class UxmlFactory : UxmlFactory<Window, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_SaveKey = new UxmlStringAttributeDescription
            { name = "save-key", defaultValue = "" };
            
            UxmlStringAttributeDescription m_Title = new UxmlStringAttributeDescription
            { name = "title", defaultValue = "" };

            UxmlBoolAttributeDescription m_ShowMinimizeButton = new UxmlBoolAttributeDescription
            { name = "show-minimize-button", defaultValue = true };
            
            UxmlBoolAttributeDescription m_ShowMaximizeButton = new UxmlBoolAttributeDescription
            { name = "show-maximize-button", defaultValue = true };

            UxmlBoolAttributeDescription m_ShowCloseButton = new UxmlBoolAttributeDescription
            { name = "show-close-button", defaultValue = true };
            
            UxmlBoolAttributeDescription m_Resizeable = new UxmlBoolAttributeDescription
            { name = "resizeable", defaultValue = false };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                Window window = (Window)ve;

                window.SaveKey = m_SaveKey.GetValueFromBag(bag, cc);
                window.Title = m_Title.GetValueFromBag(bag, cc);
                window.ShowMinimizeButton = m_ShowMinimizeButton.GetValueFromBag(bag, cc);
                window.ShowMaximizeButton = m_ShowMaximizeButton.GetValueFromBag(bag, cc);
                window.ShowCloseButton = m_ShowCloseButton.GetValueFromBag(bag, cc);
                window.Resizeable = m_Resizeable.GetValueFromBag(bag, cc);
            }
        }
        
        class ViewData
        {
            public static void Load(string key, Window window)
            {
                if (string.IsNullOrEmpty(key))
                {
                    return;
                }
                
                var text = PlayerPrefs.GetString(key);
                if (!string.IsNullOrEmpty(text))
                {
                    var state = JsonUtility.FromJson<State>(text);
                    window.SetState(state);
                }
            }

            public static void Save(string key, Window window)
            {
                if (string.IsNullOrEmpty(key))
                {
                    return;
                }
                
                var state = window.GetState();
                var serialized = JsonUtility.ToJson(state);
                PlayerPrefs.SetString(key, serialized);
            }

            public static void Reset(string key)
            {
                if (string.IsNullOrEmpty(key))
                {
                    return;
                }
                
                PlayerPrefs.DeleteKey(key);
            }
        }
    }
}
