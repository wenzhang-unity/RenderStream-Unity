using System;
using System.Collections.Generic;
using System.Linq;
using Disguise.RenderStream.Overlay.Elements;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace Disguise.RenderStream.Overlay
{
    public class Overlay : MonoBehaviour
    {
        sealed class TextureWindow : Window
        {
            public Texture Texture
            {
                get => m_Image.image;
                set => m_Image.image = value;
            }
            
            Image m_Image;
            
            public TextureWindow() : base()
            {
                m_Image = new Image();
                m_Image.scaleMode = ScaleMode.ScaleToFit;
                m_Image.AddToClassList(Moveable.Styles.MoveHitbox);
                m_Image.style.flexGrow = 1f;
                m_Image.style.flexShrink = 1f;
                
                contentContainer.Add(m_Image);
                contentContainer.AddToClassList(Moveable.Styles.MoveHitbox);

                Resizeable = true;
            }
        }
        
        static class Names
        {
            public const string WindowsContainer = "windows=container";
            public const string BackgroundImage = "background-image";
            public const string OutputStreams = "output-streams";
        }

        [Serializable]
        class OverlayState
        {
            public const string StorageKey = "Disguise.RenderStream.Overlay";

            public string SelectedStream;

            public List<string> TextureWindows = new List<string>();

            public void Save()
            {
                var serialized = JsonUtility.ToJson(this);
                PlayerPrefs.SetString(StorageKey, serialized);
            }
            
            public void Load()
            {
                var serialized = PlayerPrefs.GetString(StorageKey);
                if (!string.IsNullOrEmpty(serialized))
                {
                    var copy = JsonUtility.FromJson<OverlayState>(serialized);
                
                    SelectedStream = copy.SelectedStream;
                    TextureWindows = copy.TextureWindows;
                }
            }
        }
        
        public const string OutputStreamNone = "None";

        public bool ShowOnStart = true;
        
        UIDocument m_Document;

        VisualElement m_WindowsContainer;
        Image m_BackgroundImage;
        ExtendedList m_OutputStreams;
        List<TextureWindow> m_TextureWindows = new List<TextureWindow>();

        OverlayState m_State = new OverlayState();
        List<string> m_OutputStreamNames = new List<string>();

        void OnEnable()
        {
            m_Document = GetComponent<UIDocument>();
            
            if (ShowOnStart)
            {
                Enable();
            }
            else
            {
                m_Document.enabled = false;
            }
        }

        void Enable()
        {
            m_WindowsContainer = m_Document.rootVisualElement.Q<VisualElement>(Names.WindowsContainer);

            m_BackgroundImage = m_Document.rootVisualElement.Q<Image>(Names.BackgroundImage);

            m_OutputStreams = m_Document.rootVisualElement.Q<ExtendedList>(Names.OutputStreams);
            m_OutputStreams.List.itemsSource = m_OutputStreamNames;
            m_OutputStreams.List.makeItem = () => new TextureListItem();
            m_OutputStreams.List.bindItem = (element, index) =>
            {
                var item = element as TextureListItem;
                var streamName = m_OutputStreams.List.itemsSource[index] as string;
                
                item.IsInspectable = streamName != OutputStreamNone;
                item.Name = streamName;
                item.OnInspect.clicked += () => InspectTexture(streamName);
            };
            m_OutputStreams.List.onSelectionChange += OnOutputStreamSelection;
            
            PopulateOutputStreams();
            m_OutputStreams.ItemsChanged();
            
            m_State.Load();
            if (!TrySetStream(m_State.SelectedStream))
            {
                m_State.SelectedStream = OutputStreamNone;
            }
            m_OutputStreams.List.selectedIndex = m_OutputStreams.List.itemsSource.IndexOf(m_State.SelectedStream);

            foreach (var textureName in m_State.TextureWindows)
            {
                InspectTexture(textureName);
            }
        }

        void OnDisable()
        {
            Disable();
        }

        void Disable()
        {
            if (m_Document.enabled)
            {
                m_OutputStreams.List.onSelectionChange -= OnOutputStreamSelection;
            }

            m_State.TextureWindows = m_TextureWindows.Select(x => x.Title).ToList();
            m_State.Save();
            
            m_TextureWindows.Clear();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F12))
            {
                m_Document.enabled = !m_Document.enabled;
                
                if (m_Document.enabled)
                {
                    Enable();
                }
                else
                {
                    Disable();
                    ClearScreen();
                    ClearCursor();
                }
            }
        }

        void PopulateOutputStreams()
        {
            m_OutputStreamNames.Clear();
            m_OutputStreamNames.Add(OutputStreamNone);
            
            foreach (var camera in Camera.allCameras)
            {
                if (camera.targetTexture != null)
                {
                    m_OutputStreamNames.Add(camera.name);
                }
            }
        }

        void OnOutputStreamSelection(IEnumerable<object> selection)
        {
            foreach (var obj in selection)
            {
                var name = obj as string;
                if (TrySetStream(name))
                {
                    m_State.SelectedStream = name;
                }
            }
        }

        bool TrySetStream(string name)
        {
            if (name == OutputStreamNone)
            {
                SetNoneStream();
                return true;
            }
            
            foreach (var camera in Camera.allCameras)
            {
                if (camera.name == name && camera.targetTexture != null)
                {
                    m_BackgroundImage.image = camera.targetTexture;
                    return true;
                }
            }

            SetNoneStream();
            return false;
        }

        void SetNoneStream()
        {
            m_BackgroundImage.image = null;
        }

        void InspectTexture(string name)
        {
            foreach (var camera in Camera.allCameras)
            {
                var targetTexture = camera.targetTexture;
                
                if (camera.name == name && targetTexture != null)
                {
                    var textureWindow = m_TextureWindows.Find(x => x.Texture == targetTexture);

                    if (textureWindow != null)
                    {
                        textureWindow.Minimized = false;
                        textureWindow.BringToFront();
                    }
                    else
                    {
                        textureWindow = new TextureWindow();
                        textureWindow.Title = name;
                        textureWindow.Texture = targetTexture;
                        textureWindow.SaveKey = "Disguise.RenderStream.Overlay.Inspect." + name;
                        textureWindow.OnClose += () => CloseTextureWindow(textureWindow);
                        m_TextureWindows.Add(textureWindow);
                        
                        m_WindowsContainer.Add(textureWindow);
                    }
                }
            }
        }

        void CloseTextureWindow(TextureWindow window)
        {
            window.ResetState();
            window.SaveKey = null;
            
            m_TextureWindows.Remove(window);
            m_WindowsContainer.Remove(window);
        }
        
        void ClearScreen()
        {
            var restore = RenderTexture.active;
                
            RenderTexture.active = null;
            GL.Clear(true, true, Color.black);
                
            RenderTexture.active = restore;
        }

        void ClearCursor()
        {
            Cursor.SetCursor(null, default, CursorMode.Auto);
        }
    }
}
