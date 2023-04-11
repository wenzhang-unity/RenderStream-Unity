using System;
using System.Collections.Generic;
using System.Linq;
using Disguise.RenderStream.Parameters;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

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

        private const int k_SelectedParameterID = 0;
        private const int k_ResizeStrategyParameterID = 1;

        /// <summary>
        /// The index of the selection in the texture dropdown to present to the screen.
        /// The dropdown choices are generated inside <see cref="GetParametersOrderedForSchema"/>, as a concatenated list of:
        /// None + Channels (output) + Live textures (input).
        /// </summary>
        public int Selected
        {
            get => m_Selected;
            set => m_Selected = value;
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

        CameraCapture[] m_Outputs = {};
        ReadOnlyMemory<Texture> m_Inputs;

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
        /// It contains the <see cref="DisguisePresenter"/> and <see cref="DisguiseRemoteParameters"/> components,
        /// as well as the necessary <see cref="m_OutputPresenter"/> and <see cref="m_InputPresenter"/>.
        /// </summary>
        /// <returns></returns>
        public static GameObject LoadPrefab()
        {
            return Resources.Load<GameObject>(nameof(UnityDebugWindowPresenter));
        }

        public List<(IRemoteParameterWrapper parameter, int parameterID)> GetRemoteParameterWrappers()
        {
            var selected = new IntRemoteParameterWrapper();
            selected.SetTarget(this, typeof(UnityDebugWindowPresenter).GetProperty(nameof(Selected)));
            
            var resizeStrategy = new EnumRemoteParameterWrapper();
            resizeStrategy.SetTarget(this, typeof(UnityDebugWindowPresenter).GetProperty(nameof(ResizeStrategy)));

            return new List<(IRemoteParameterWrapper parameter, int parameterID)>
            {
                (selected, k_SelectedParameterID),
                (resizeStrategy, k_ResizeStrategyParameterID),
            };
        }

#if UNITY_EDITOR
        const string k_GroupName = "Unity Debug Window Presenter";
        const string k_NoneTextureLabel = "None";
        
        class MockUnityDebugWindowPresenter : UnityEngine.Object
        {
            public PresenterResizeStrategies ResizeStrategy = PresenterResizeStrategies.Fit;
            public int Selected;

            public MockUnityDebugWindowPresenter()
            {
                Debug.Assert(nameof(ResizeStrategy) == nameof(UnityDebugWindowPresenter.ResizeStrategy));
                Debug.Assert(nameof(Selected) == nameof(UnityDebugWindowPresenter.Selected));
            }
        }
        
        /// <summary>
        /// Returns the list of remote parameters to control the presenter.
        /// </summary>
        /// <remarks>
        /// The choices for the texture selection dropdown are scene-specific and correspond to a concatenated list of:
        /// None + Channels (output) + Live textures (input).
        /// </remarks>
        public static List<ManagedRemoteParameter> GetParametersOrderedForSchema(ManagedSchema schema, ManagedRemoteParameters sceneSchema)
        {
            var mockInstance = new MockUnityDebugWindowPresenter();
            
            var liveTextureNames = sceneSchema.parameters.Where(
                x => x.type == RemoteParameterType.RS_PARAMETER_IMAGE).Select(
                x => x.displayName
            );
            var options = new List<string> { k_NoneTextureLabel };
            options.AddRange(schema.channels);
            options.AddRange(liveTextureNames);
            
            var selected = new IntRemoteParameterWrapper();
            selected.SetTarget(mockInstance, mockInstance.GetType().GetField(nameof(MockUnityDebugWindowPresenter.Selected)));
            var selectedDesc = selected.GetParametersForSchema()[0];
            var selectedSchema = Parameter.CreateManagedRemoteParameter(
                selectedDesc,
                k_GroupName,
                ObjectNames.NicifyVariableName(nameof(MockUnityDebugWindowPresenter.Selected)),
                $"{k_SelectedParameterID}{Parameter.KeySeparator}{nameof(UnityDebugWindowPresenter)}.{nameof(MockUnityDebugWindowPresenter.Selected)}"
            );
            selectedSchema.options = options.ToArray();

            var resizeStrategy = new EnumRemoteParameterWrapper();
            resizeStrategy.SetTarget(mockInstance, mockInstance.GetType().GetField(nameof(MockUnityDebugWindowPresenter.ResizeStrategy)));
            var resizeStrategyDesc = resizeStrategy.GetParametersForSchema()[0];
            var resizeStrategySchema = Parameter.CreateManagedRemoteParameter(
                resizeStrategyDesc,
                k_GroupName,
                ObjectNames.NicifyVariableName(nameof(MockUnityDebugWindowPresenter.ResizeStrategy)),
                $"{k_ResizeStrategyParameterID}{Parameter.KeySeparator}{nameof(UnityDebugWindowPresenter)}.{nameof(MockUnityDebugWindowPresenter.ResizeStrategy)}"
            );

            return new List<ManagedRemoteParameter>{ selectedSchema, resizeStrategySchema };
        }
#endif

        /// <remarks>
        /// This class should only be instantiated through <see cref="LoadPrefab"/>.
        /// </remarks>
        private UnityDebugWindowPresenter()
        {
            
        }

        void OnEnable()
        {
            DisguiseRenderStream.SceneLoaded += RefreshInput;
            DisguiseRenderStream.StreamsChanged += RefreshOutput;
            
            RefreshOutput();
            RefreshInput();
        }
        
        void OnDisable()
        {
            DisguiseRenderStream.SceneLoaded -= RefreshInput;
            DisguiseRenderStream.StreamsChanged -= RefreshOutput;
        }

        void Update()
        {
            Assert.IsNotNull(m_OutputPresenter);
            Assert.IsNotNull(m_InputPresenter);

            if (VirtualIndexToOutputIndex(Selected) is { } outputIndex)
            {
                m_OutputPresenter.enabled = true;
                m_InputPresenter.enabled = false;
                m_OutputPresenter.cameraCapture = m_Outputs[outputIndex];
            }
            else if (VirtualIndexToInputIndex(Selected) is { } inputIndex &&
                     m_Inputs.Span[inputIndex] is { } inputTexture)
            {
                m_InputPresenter.enabled = true;
                m_OutputPresenter.enabled = false;
                m_InputPresenter.source = inputTexture;
            }
            else
            {
                m_OutputPresenter.enabled = m_InputPresenter.enabled = false;
                return;
            }
            
            m_OutputPresenter.strategy = m_InputPresenter.strategy = PresenterStrategyToBlitStrategy(m_ResizeStrategy);
        }

        /// <summary>
        /// Maps a virtual index like <see cref="Selected"/> to a real index into <see cref="m_Outputs"/>.
        /// The virtual list is described in <see cref="GetParametersOrderedForSchema"/>.
        /// </summary>
        /// <returns>
        /// An index into <see cref="m_Outputs"/>, or null when no output is selected.
        /// </returns>
        int? VirtualIndexToOutputIndex(int virtualIndex)
        {
            var outputIndex = virtualIndex - 1;

            if (outputIndex < 0 || outputIndex >= m_Outputs.Length)
                return null;

            return outputIndex;
        }
        
        /// <summary>
        /// Maps a virtual index like <see cref="Selected"/> to a real index into <see cref="m_Inputs"/>.
        /// The virtual list is described in <see cref="GetParametersOrderedForSchema"/>.
        /// </summary>
        /// <returns>
        /// An index into <see cref="m_Inputs"/>, or null when no input is selected.
        /// </returns>
        int? VirtualIndexToInputIndex(int virtualIndex)
        {
            var inputIndex = virtualIndex - 1 - m_Outputs.Length;
            
            if (inputIndex < 0 || inputIndex >= m_Inputs.Length)
                return null;
            
            return inputIndex;
        }

        void RefreshOutput()
        {
            m_Outputs = FindObjectsByType<DisguiseCameraCapture>(
                FindObjectsSortMode.InstanceID).Select(
                    x => x.GetComponent<CameraCapture>()).ToArray();
        }
        
        void RefreshInput()
        {
            m_Inputs = DisguiseRenderStream.Instance.InputTextures;
        }
    }
}
