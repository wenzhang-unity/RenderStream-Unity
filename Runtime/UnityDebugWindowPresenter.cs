using System;
using System.Collections.Generic;
using System.Linq;
using Disguise.RenderStream.Utils;
using UnityEngine;

namespace Disguise.RenderStream
{
    /// <summary>
    /// This component together with the prefab of the same name offer drop-in support for presenting any Disguise-related texture to the Unity window.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class has two responsibilities:
    /// </para>
    /// <para>
    /// 1. Generating the remote parameters for each scene - including the texture selection dropdown choices specific to each scene.
    /// </para>
    /// <para>
    /// 2. Presenting a texture to the screen according to <see cref="Selected"/> and <see cref="ResizeStrategy"/>.
    /// </para>
    /// </remarks>
    class UnityDebugWindowPresenter : MonoBehaviour
    {
        /// <summary>
        /// This is a user-friendly subset of <see cref="BlitStrategy.Strategy"/>.
        /// </summary>
        public enum PresenterResizeStrategies
        {
            /// <see cref="BlitStrategy.Strategy.NoResize"/>
            ActualSize,
            /// <see cref="BlitStrategy.Strategy.Stretch"/>
            Stretch,
            /// <see cref="BlitStrategy.Strategy.Fill"/>
            Fill,
            /// <see cref="BlitStrategy.Strategy.Letterbox"/>
            Fit,
            /// <see cref="BlitStrategy.Strategy.Clamp"/>
            Clamp
        }

        const string k_NoneTextureLabel = "None";

        /// <summary>
        /// The index of the selection in the texture dropdown to present to the screen.
        /// The dropdown choices are generated inside <see cref="GetManagedRemoteParameters"/>, as a concatenated list of:
        /// None + Channels (output) + Live textures (input).
        /// </summary>
        public int Selected
        {
            get => m_Selected;
            set => m_Selected = value;
        }

        string SelectedString
        {
            get
            {
                // Fill a dictionary to map from selected index to the displayed text (used to search in the scene)
                if (m_SelectedIndexToText == null)
                {
                    m_SelectedIndexToText = new();
                    int sceneIndex = gameObject.scene.buildIndex;
                    if (sceneIndex < DisguiseRenderStream.Instance.Schema.scenes.Length)
                    {
                        var remoteParameters = DisguiseRenderStream.Instance.Schema.scenes[sceneIndex].parameters;
                        var disguiseRemoteParameters = GetComponent<DisguiseRemoteParameters>();
                        if (disguiseRemoteParameters != null)
                        {
                            string selectedRemoteParameterKey = $"{disguiseRemoteParameters.prefix} {nameof(Selected)}";
                            var disguiseRemoteParameter = 
                                remoteParameters.FirstOrDefault(r => r.key == selectedRemoteParameterKey);
                            if (disguiseRemoteParameter != null)
                            {
                                // Skipping 0 since it is none which mean nothing is selected...
                                for (int i = 1; i < disguiseRemoteParameter.options.Length; ++i)
                                {
                                    m_SelectedIndexToText.Add(i, disguiseRemoteParameter.options[i]);
                                }
                            }
                        }
                    }
                }

                return m_SelectedIndexToText.TryGetValue(Selected, out var ret) ? ret : "";
            }
        }
        
        /// <summary>
        /// The <see cref="PresenterResizeStrategies">strategy</see> for resizing the selected texture to screen.
        /// </summary>
        public PresenterResizeStrategies ResizeStrategy
        {
            get => m_ResizeStrategy;
            set => m_ResizeStrategy = value;
        }
        
        [SerializeField]
        int m_Selected;

        [SerializeField]
        PresenterResizeStrategies m_ResizeStrategy = PresenterResizeStrategies.Fit;

        [SerializeField]
        CameraCapturePresenter m_OutputPresenter;
        
        [SerializeField]
        Presenter m_InputPresenter;

        /// <summary>
        /// Index of the last values of <see cref="m_Selected"/> that was processed during the <see cref="Update"/>
        /// method.
        /// </summary>
        int m_LastProcessSelected = -1;

        /// <summary>
        /// Map a <see cref="Selected"/> index value to the display text.
        /// </summary>
        Dictionary<int, string> m_SelectedIndexToText;

        static BlitStrategy.Strategy PresenterStrategyToBlitStrategy(PresenterResizeStrategies strategy) => strategy switch
        {
            PresenterResizeStrategies.ActualSize => BlitStrategy.Strategy.NoResize,
            PresenterResizeStrategies.Stretch => BlitStrategy.Strategy.Stretch,
            PresenterResizeStrategies.Fill => BlitStrategy.Strategy.Fill,
            PresenterResizeStrategies.Fit => BlitStrategy.Strategy.Letterbox,
            PresenterResizeStrategies.Clamp => BlitStrategy.Strategy.Clamp,
            _ => throw new ArgumentOutOfRangeException()
        };

        /// <summary>
        /// Instantiates a prefab with a GameObject hierarchy configured to be dropped into a scene.
        /// It contains the <see cref="UnityDebugWindowPresenter"/> and <see cref="DisguiseRemoteParameters"/> 
        /// components, as well as the necessary <see cref="m_OutputPresenter"/> and <see cref="m_InputPresenter"/>.
        /// </summary>
        /// <returns></returns>
        public static GameObject LoadPrefab()
        {
            return Resources.Load<GameObject>(nameof(UnityDebugWindowPresenter));
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// Returns the list of remote parameters to control the presenter.
        /// The parameters are pre-configured in the prefab used by <see cref="LoadPrefab"/>.
        /// </summary>
        /// <remarks>
        /// The choices for the texture selection dropdown are scene-specific and correspond to a concatenated list of:
        /// None + Channels (output) + Live textures (input).
        /// </remarks>
        public static List<ManagedRemoteParameter> GetManagedRemoteParameters(ManagedSchema schema, ManagedRemoteParameters sceneSchema)
        {
            var prefab = LoadPrefab();
            var parameters = prefab.GetComponent<DisguiseRemoteParameters>();
            var managedParameters = parameters.exposedParameters();

            foreach (var parameter in managedParameters)
            {
                // Discard the name of the GameObject, keep only the field ex:
                // "DisguisePresenter Mode" => "Mode"
                parameter.displayName = parameter.displayName.Substring(parameter.displayName.IndexOf(" ") + 1);

                // Generate dropdown choices as a concatenated list of: None + Channels (output) + Live textures (input)
                if (parameter.displayName == nameof(Selected))
                {
                    List<string> options = new List<string>();
                    options.Add(k_NoneTextureLabel);

                    foreach (var channel in schema.channels.OrderBy(s => s))
                    {
                        options.Add(channel);
                    }

                    var remoteParameters = FindObjectsByType<DisguiseRemoteParameters>(FindObjectsSortMode.None);
                    foreach (var sceneParameter in sceneSchema.parameters.OrderBy(p => p.displayName))
                    {
                        var remoteParams = Array.Find(remoteParameters, rp => sceneParameter.key.StartsWith(rp.prefix));
                        var field = new ObjectField();
                        field.info = remoteParams.GetMemberInfoFromManagedParameter(sceneParameter);

                        if (field.FieldType == typeof(Texture))
                        {
                            options.Add(sceneParameter.displayName);
                        }
                    }

                    parameter.options = options.ToArray();
                }
            }
            
            return managedParameters;
        }
#endif

        /// <remarks>
        /// This class should only be instantiated through <see cref="LoadPrefab"/>.
        /// </remarks>
        UnityDebugWindowPresenter()
        {
        }

        void OnEnable()
        {
            DisguiseRenderStream.SceneLoaded += ForceSelectedUpdate;
            DisguiseRenderStream.StreamsChanged += ForceSelectedUpdate;
        }
        
        void OnDisable()
        {
            DisguiseRenderStream.SceneLoaded -= ForceSelectedUpdate;
            DisguiseRenderStream.StreamsChanged -= ForceSelectedUpdate;
        }

        void Update()
        {
            m_OutputPresenter.strategy = m_InputPresenter.strategy = PresenterStrategyToBlitStrategy(m_ResizeStrategy);
            if (Selected == m_LastProcessSelected)
            {
                return;
            }

            if (TryGetSelectedCameraCapture(out var selectedCamera))
            {
                m_OutputPresenter.enabled = true;
                m_InputPresenter.enabled = false;
                m_OutputPresenter.cameraCapture = selectedCamera;
            }
            else if (TryGetSelectedRenderTexture(out var renderTexture))
            {
                m_InputPresenter.enabled = true;
                m_OutputPresenter.enabled = false;
                m_InputPresenter.source = renderTexture;
            }
            else
            {
                m_OutputPresenter.enabled = m_InputPresenter.enabled = false;
            }

            m_LastProcessSelected = Selected;
        }

        /// <summary>
        /// Check if <see cref="Selected"/> correspond to a CameraCapture and finds it.
        /// </summary>
        bool TryGetSelectedCameraCapture(out CameraCapture cameraCapture)
        {
            var cameraName = SelectedString;
            var cameras = FindObjectsByType<DisguiseCameraCapture>(FindObjectsSortMode.InstanceID)
                .Where(c => c.enabled) // Ignore disabled cameras
                .Where(c => c.transform.parent) // CameraCapture are attached to cameras created as a child of the user created scene camera (which is the one that has the name we are searching for)
                .Select(x => x.GetComponent<CameraCapture>())
                .Where(x => x.transform.parent.name == cameraName).ToArray();
            
            if (cameras.Length > 0)
            {
                cameraCapture = cameras.First();
                return true;
            }

            cameraCapture = null;
            return false;
        }

        /// <summary>
        /// Check if <see cref="Selected"/> correspond to a <see cref="RenderTexture"/> <see cref="RemoteParameter"/>
        /// and finds it.
        /// </summary>
        bool TryGetSelectedRenderTexture(out RenderTexture renderTexture)
        {
            var renderTextureName = SelectedString;
            int sceneIndex = gameObject.scene.buildIndex;
            var fields = DisguiseRenderStream.Instance.GetFieldsOfScene(sceneIndex);
            var selectedRenderTextureField = 
                fields.images.FirstOrDefault(f => f.remoteParameter.displayName == renderTextureName);
            renderTexture = selectedRenderTextureField?.GetValue() as RenderTexture;
            return renderTexture != null;
        }

        void ForceSelectedUpdate()
        {
            m_LastProcessSelected = -1;
        }
    }
}
