using UnityEngine;
using TMPro;

namespace Disguise.RenderStream
{
    // Uses world-space floating text (TextMeshPro), no relation to UI (TextMeshProUGUI).
    [ExecuteAlways]
    [RequireComponent(typeof(TextMeshPro))]
    public class DisguiseBurnIn : MonoBehaviour
    {
        TextMeshPro m_label;

        [SerializeField]
        Camera m_camera;
        
        [SerializeField]
        string m_text = "Burn-In";

        [SerializeField]
        float m_scale = 0.1f;

        public Camera targetCamera
        {
            get => m_camera;
            set => m_camera = value;
        }

        public string text
        {
            get => m_text;
            set => m_text = value;
        }
        
        void OnEnable()
        {
            m_label = GetComponent<TextMeshPro>();

            m_label.rectTransform.sizeDelta = new Vector2(60f, 60f);
            
            m_label.horizontalAlignment = HorizontalAlignmentOptions.Center;
            m_label.verticalAlignment = VerticalAlignmentOptions.Middle;
            
            m_label.color = Color.black;
        }

        void Update()
        {
            transform.localScale = new Vector3(m_scale, m_scale, 1f);
            
            m_label.text = m_text;
            
            if (m_camera == null)
                return;

            // Orient with camera and place 1 unit ahead.
            // Beware, can intersect with or be occluded by other objects inside the 1 unit range.
            var cameraTransform = m_camera.transform;
            var cameraForward = cameraTransform.forward;
            transform.position = cameraTransform.position + cameraForward * 1.0f;
            transform.forward = cameraForward;
        }
    }
}
