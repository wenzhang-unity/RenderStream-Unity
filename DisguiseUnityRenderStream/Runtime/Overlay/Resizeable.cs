using UnityEngine;
using UnityEngine.UIElements;

namespace Disguise.RenderStream.Overlay.Elements
{
    class Resizeable
    {
        static class Styles
        {
            public const string ResizeVerticalHitbox = "resize-vertical-hitbox";
            public const string ResizeHorizontalHitbox = "resize-horizontal-hitbox";
            public const string ResizeDiagonalHitbox = "resize-diagonal-hitbox";
        }
        
        readonly VisualElement m_Target;
        
        bool m_IsDragging;
        Vector2 m_ResizeMask;
        Vector2 m_LastMousePosition;
        Vector2 m_PositionDelta;
        Vector2 m_DesiredSize;
        Rect m_TrueBounds;
        bool m_Initialized;
        
        public Resizeable(VisualElement target)
        {
            m_Target = target;

            m_DesiredSize = m_Target.localBound.size;
            m_TrueBounds = m_Target.localBound;

            m_Target.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            m_Target.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            AttachToPanel(m_Target.panel);
        }

        public void Unregister()
        {
            DetachFromPanel(m_Target.panel);
        }
        
        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            AttachToPanel(evt.destinationPanel);
        }
        
        void AttachToPanel(IPanel panel)
        {
            if (panel == null)
                return;

            m_Target.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            m_Target.Q<VisualElement>(null, Styles.ResizeVerticalHitbox).RegisterCallback<MouseDownEvent>(OnMouseDownVertical);
            m_Target.Q<VisualElement>(null, Styles.ResizeHorizontalHitbox).RegisterCallback<MouseDownEvent>(OnMouseDownHorizontal);
            m_Target.Q<VisualElement>(null, Styles.ResizeDiagonalHitbox).RegisterCallback<MouseDownEvent>(OnMouseDownDiagonal);
            m_Target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            m_Target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        }
        
        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            DetachFromPanel(evt.originPanel);
        }

        void DetachFromPanel(IPanel panel)
        {
            if (panel == null)
                return;
            
            m_Target.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            m_Target.Q<VisualElement>(null, Styles.ResizeVerticalHitbox).UnregisterCallback<MouseDownEvent>(OnMouseDownVertical);
            m_Target.Q<VisualElement>(null, Styles.ResizeHorizontalHitbox).UnregisterCallback<MouseDownEvent>(OnMouseDownHorizontal);
            m_Target.Q<VisualElement>(null, Styles.ResizeDiagonalHitbox).UnregisterCallback<MouseDownEvent>(OnMouseDownDiagonal);
            m_Target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            m_Target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            m_TrueBounds = evt.newRect;
        }

        void OnMouseDownBase(MouseDownEvent evt)
        {
            m_DesiredSize = m_TrueBounds.size;
            
            // Using this instead of evt.mouseDelta because
            // the latter has weird scaling applied to it.
            m_LastMousePosition = m_Target.parent.WorldToLocal(evt.mousePosition);

            m_PositionDelta = new Vector2(0.0f, 0.0f);

            m_IsDragging = true;
            m_Target.CaptureMouse();
        }

        void OnMouseDownVertical(MouseDownEvent evt)
        {
            m_ResizeMask = new Vector2(0f, 1f);
            OnMouseDownBase(evt);
        }
        
        void OnMouseDownHorizontal(MouseDownEvent evt)
        {
            m_ResizeMask = new Vector2(1f, 0f);
            OnMouseDownBase(evt);
        }
        
        void OnMouseDownDiagonal(MouseDownEvent evt)
        {
            m_ResizeMask = new Vector2(1f, 1f);
            OnMouseDownBase(evt);
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            if (m_IsDragging)
                m_Target.ReleaseMouse();
            m_IsDragging = false;
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            if (m_IsDragging)
            {
                var newMousePosition = m_Target.parent.WorldToLocal(evt.mousePosition);
                m_PositionDelta += newMousePosition - m_LastMousePosition;
                m_LastMousePosition = newMousePosition;

                m_DesiredSize += m_PositionDelta * m_ResizeMask;
                SetSize(m_DesiredSize);

                m_PositionDelta.Set(0.0f, 0.0f);
            }
        }
        
        float ScreenWidth => m_Target.parent.localBound.width;

        float ScreenHeight => m_Target.parent.localBound.height;

        public Vector2 GetSize()
        {
            return m_TrueBounds.size;
        }

        public void SetSize(Vector2 newSize)
        {
            if (m_IsDragging)
                ClampSizeToBounds(ref newSize);
            SetSizeInternal(newSize);
        }

        void SetSizeInternal(Vector2 newSize)
        {
            m_DesiredSize = newSize;

            if (Mathf.Approximately(0f, m_DesiredSize.x))
                m_Target.style.width = StyleKeyword.Auto;
            else
                m_Target.style.width = m_DesiredSize.x;
            
            if (Mathf.Approximately(0f, m_DesiredSize.y))
                m_Target.style.height = StyleKeyword.Auto;
            else
                m_Target.style.height = m_DesiredSize.y;
        }

        void ClampSizeToBounds(ref Vector2 size)
        {
            // Clamp to screen edges.
            size.x = Mathf.Clamp(size.x, 0, ScreenWidth - m_TrueBounds.x);
            size.y = Mathf.Clamp(size.y, 0, ScreenHeight - m_TrueBounds.y);
        }
        
        public void SetHeight(float newHeight)
        {
            if (m_IsDragging)
                ClampHeightToBounds(ref newHeight);
            SetHeightInternal(newHeight);
        }

        void SetHeightInternal(float newSize)
        {
            m_DesiredSize.y = newSize;

            if (Mathf.Approximately(0f, m_DesiredSize.y))
                m_Target.style.height = StyleKeyword.Auto;
            else
                m_Target.style.height = m_DesiredSize.y;
        }

        void ClampHeightToBounds(ref float height)
        {
            // Clamp to screen edges.
            height = Mathf.Clamp(height, 0, ScreenHeight - m_TrueBounds.y);
        }
    }
}
