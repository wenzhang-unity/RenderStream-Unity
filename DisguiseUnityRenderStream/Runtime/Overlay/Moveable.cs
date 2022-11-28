using UnityEngine;
using UnityEngine.UIElements;

namespace Disguise.RenderStream.Overlay
{
    class Moveable
    {
        public static class Styles
        {
            public const string MoveHitbox = "move-hitbox";
        }
        
        readonly VisualElement m_Target;
        readonly float m_ActivationDistance;

        bool m_IsDragging;
        bool m_IsActivated;
        Vector2 m_LastMousePosition;
        Vector2 m_PositionDelta;
        Vector2 m_TotalPositionDelta;
        Vector2 m_DesiredPosition;
        Rect m_TrueBounds;

        public Moveable(VisualElement target)
        {
            m_Target = target;
            m_ActivationDistance = 2.0f;

            m_DesiredPosition = m_Target.localBound.position;
            m_TrueBounds = m_Target.localBound;

            m_Target.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            m_Target.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            AttachToPanel(m_Target.panel);
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

            m_Target.Query<VisualElement>(null, Styles.MoveHitbox).ForEach(element =>
                element.RegisterCallback<MouseDownEvent>(OnMoveHitboxMouseDown)
            );
            m_Target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            m_Target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (evt.originPanel == null)
                return;

            m_Target.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            m_Target.Query<VisualElement>(null, Styles.MoveHitbox).ForEach(element =>
                element.UnregisterCallback<MouseDownEvent>(OnMoveHitboxMouseDown)
            );
            m_Target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            m_Target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            m_TrueBounds = evt.newRect;

            if (!m_IsDragging)
                m_DesiredPosition = m_TrueBounds.position;

            SetPosition(m_DesiredPosition);
        }

        void OnMoveHitboxMouseDown(MouseDownEvent evt)
        {
            // Using this instead of evt.mouseDelta because
            // the latter has weird scaling applied to it.
            m_LastMousePosition = m_Target.parent.WorldToLocal(evt.mousePosition);

            m_PositionDelta = new Vector2(0.0f, 0.0f);
            m_TotalPositionDelta = new Vector2(0.0f, 0.0f);

            m_IsDragging = true;
            m_Target.CaptureMouse();
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            if (m_IsDragging)
                m_Target.ReleaseMouse();
            m_IsDragging = false;
            m_IsActivated = false;
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            if (m_IsDragging)
            {
                var newMousePosition = m_Target.parent.WorldToLocal(evt.mousePosition);
                m_PositionDelta += newMousePosition - m_LastMousePosition;
                m_LastMousePosition = newMousePosition;

                if (m_PositionDelta.magnitude >= m_ActivationDistance || m_IsActivated)
                {
                    m_IsActivated = true;

                    m_TotalPositionDelta += m_PositionDelta;
                    m_DesiredPosition += m_PositionDelta;
                    SetPosition(m_DesiredPosition);

                    SetPositionInternal(m_DesiredPosition);

                    m_PositionDelta.Set(0.0f, 0.0f);
                }
            }
        }

        float ScreenWidth => m_Target.parent.localBound.width;

        float ScreenHeight => m_Target.parent.localBound.height;

        public Vector2 GetPosition()
        {
            return m_TrueBounds.position;
        }
        
        public void SetPositionNoClamp(Vector2 newPosition)
        {
            SetPositionInternal(newPosition);
        }

        public void SetPosition(Vector2 newPosition)
        {
            ClampPositionToBounds(ref newPosition);
            SetPositionInternal(newPosition);
        }

        void SetPositionInternal(Vector2 newPosition)
        {
            m_DesiredPosition = newPosition;
            m_Target.style.left = newPosition.x;
            m_Target.style.top = newPosition.y;
        }

        void ClampPositionToBounds(ref Vector2 position)
        {
            // Clamp to screen edges.
            position.x = Mathf.Clamp(position.x, 0, ScreenWidth - m_TrueBounds.width);
            position.y = Mathf.Clamp(position.y, 0, ScreenHeight - m_TrueBounds.height);
        }
    }
}
