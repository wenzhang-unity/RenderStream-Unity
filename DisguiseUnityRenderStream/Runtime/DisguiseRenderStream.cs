using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Disguise.RenderStream.Parameters;
using Disguise.RenderStream.Utils;
using Unity.Collections;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Disguise.RenderStream
{
    class DisguiseRenderStream
    {
        /// <summary>
        /// Initializes RenderStream objects.
        /// </summary>
        /// <remarks>
        /// Ensures that the RenderStream plugin is initialized, and then creates the <see cref="DisguiseRenderStream"/>
        /// singleton. This is called after Awake but before the scene is loaded.
        /// </remarks>
        [RuntimeInitializeOnLoadMethod]
        static void OnLoad()
        {
            if (Application.isEditor)
            {
                // No play in editor support currently
                return;
            }

            if (PluginEntry.instance.IsAvailable == false)
            {
                Debug.LogError("DisguiseRenderStream: RenderStream DLL not available");
                return;
            }

            string pathToBuiltProject = ApplicationPath.GetExecutablePath();;
            RS_ERROR error = PluginEntry.instance.LoadSchema(pathToBuiltProject, out var schema);
            if (error != RS_ERROR.RS_ERROR_SUCCESS)
            {
                Debug.LogError(string.Format("DisguiseRenderStream: Failed to load schema {0}", error));
            }

#if !ENABLE_CLUSTER_DISPLAY
            Instance = new DisguiseRenderStream(schema);
#else
            Instance = new DisguiseRenderStreamWithCluster(schema);
#endif
            
            Instance.Initialize();
            SceneManager.sceneLoaded += Instance.OnSceneLoaded;
            if (SceneManager.GetActiveScene() is { isLoaded: true } scene)
            {
                Instance.OnSceneLoaded(scene, LoadSceneMode.Single);
            }
        }

        struct RenderStreamUpdate { }

        protected virtual void Initialize()
        {
            PlayerLoopExtensions.RegisterUpdate<TimeUpdate.WaitForLastPresentationAndUpdateTime, RenderStreamUpdate>(AwaitFrame);
            RenderPipelineManager.beginContextRendering += OnBeginContextRendering;
        }

        protected DisguiseRenderStream(ManagedSchema schema)
        {
            if (schema != null)
            {
                m_SceneData = new SceneData[schema.scenes.Length];
            }
            else
            {
                schema = new ManagedSchema();
                schema.channels = new string[0];
                schema.scenes = new ManagedRemoteParameters[1];
                schema.scenes[0] = new ManagedRemoteParameters();
                schema.scenes[0].name = "Default";
                schema.scenes[0].parameters = new ManagedRemoteParameter[0];
                m_SceneData = new SceneData[1];
                CreateStreams();
            }
            
            m_Schema = schema;
            m_Settings = DisguiseRenderStreamSettings.GetOrCreateSettings();
            
            for (var i = 0; i < m_SceneData.Length; i++)
            {
                m_SceneData[i] = new SceneData();
            }
        }

        void OnSceneLoaded(Scene loadedScene, LoadSceneMode mode)
        {
            if (DisguiseRenderStreamSettings.GetOrCreateSettings().enableUnityDebugWindowPresenter)
            {
                // TODO fix presenter remote parameters
                // GameObject.Instantiate(UnityDebugWindowPresenter.LoadPrefab());
            }
            
            CreateStreams();

            var sceneIndex = DisguiseRenderStreamSettings.GetOrCreateSettings().sceneControl switch
            {
                DisguiseRenderStreamSettings.SceneControl.Selection => loadedScene.buildIndex,
                _ => 0
            };
            
            var spec = m_Schema.scenes[sceneIndex];
            var sceneData = m_SceneData[sceneIndex];

            if (DisguiseParameterList.FindInstance() is { } parameterList)
            {
                var remoteParameters = parameterList.GetRemoteParameterWrappers();
                sceneData.AssignRemoteParameters(remoteParameters, spec);
            }
            else
            {
                sceneData.AssignRemoteParameters(new List<(IRemoteParameterWrapper, int)>(), spec);
            }
            
            SceneLoaded?.Invoke();
        }

        protected void CreateStreams()
        {
            Debug.Log("CreateStreams");
            if (PluginEntry.instance.IsAvailable == false)
            {
                Debug.LogError("DisguiseRenderStream: RenderStream DLL not available");
                return;
            }

            do
            {
                RS_ERROR error = PluginEntry.instance.getStreams(out var streams);
                if (error != RS_ERROR.RS_ERROR_SUCCESS)
                {
                    Debug.LogError(string.Format("DisguiseRenderStream: Failed to get streams {0}", error));
                    return;
                }

                Debug.Assert(streams != null);
                Streams = streams;
                if (Streams.Length == 0)
                {
                    Debug.Log("Waiting for streams...");
                    Thread.Sleep(1000);
                }
            } while (Streams.Length == 0);

            Debug.Log(string.Format("Found {0} streams", Streams.Length));
            
            foreach (var camera in m_Cameras)
                UnityEngine.Object.Destroy(camera);
            m_Cameras = new GameObject[Streams.Length];

            // cache the template cameras prior to instantiating our instance cameras 
            Camera[] templateCameras = getTemplateCameras();
            const int cullUIOnly = ~(1 << 5);
            
            ScratchTexture2DManager.Instance.Clear();

            for (int i = 0; i < Streams.Length; ++i)
            {        
                StreamDescription stream = Streams[i];
                Camera channelCamera = DisguiseRenderStream.GetChannelCamera(stream.channel);
                if (channelCamera)
                {
                    m_Cameras[i] = UnityEngine.Object.Instantiate(channelCamera.gameObject, channelCamera.gameObject.transform);
                    m_Cameras[i].name = stream.name;
                }
                else if (Camera.main)
                {
                    m_Cameras[i] = UnityEngine.Object.Instantiate(Camera.main.gameObject, Camera.main.gameObject.transform);
                    m_Cameras[i].name = stream.name;
                }
                else
                {
                    m_Cameras[i] = new GameObject(stream.name);
                    m_Cameras[i].AddComponent<Camera>();
                }

                m_Cameras[i].transform.localPosition = Vector3.zero;
                m_Cameras[i].transform.localRotation = Quaternion.identity;
            
                GameObject cameraObject = m_Cameras[i];
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.enabled = true; // ensure the camera component is enable
                camera.cullingMask &= cullUIOnly; // cull the UI so RenderStream and other error messages don't render to RenderStream outputs
                DisguiseCameraCapture capture = cameraObject.GetComponent(typeof(DisguiseCameraCapture)) as DisguiseCameraCapture;
                if (capture == null)
                    capture = cameraObject.AddComponent(typeof(DisguiseCameraCapture)) as DisguiseCameraCapture;

                camera.enabled = true;
            }

            // stop template cameras impacting performance
            foreach (var templateCam in templateCameras)
            {
                templateCam.enabled = false; // disable the camera component on the template camera so these cameras won't render and impact performance
                // we don't want to disable the game object otherwise we won't be able to find the object again to instantiate instance cameras if we get a streams changed event
            }

            LatestFrameData = new FrameData();
            Awaiting = false;
            
            StreamsChanged?.Invoke();
        }
    
        protected void ProcessFrameData(in FrameData receivedFrameData)
        {
            if (receivedFrameData.scene >= m_Schema.scenes.Length)
                return;
        
            var spec = m_Schema.scenes[receivedFrameData.scene];
            var sceneData = m_SceneData[receivedFrameData.scene];
            var numNumericalParameters = 0;
            var numTextParameters = 0;
            
            foreach (var parameter in spec.parameters)
            {
                if (parameter.type == RemoteParameterType.RS_PARAMETER_NUMBER)
                    ++numNumericalParameters;
                else if (parameter.type == RemoteParameterType.RS_PARAMETER_POSE || parameter.type == RemoteParameterType.RS_PARAMETER_TRANSFORM)
                    numNumericalParameters += 16;
                else if (parameter.type == RemoteParameterType.RS_PARAMETER_TEXT)
                    ++numTextParameters;
                else
                    throw new NotSupportedException();
            }

            using var parameters = new NativeArray<float>(numNumericalParameters, Allocator.Temp);
            if (PluginEntry.instance.GetFrameParameters(spec.hash, parameters.AsSpan()) == RS_ERROR.RS_ERROR_SUCCESS)
            {
                sceneData.Numeric.SetData(parameters.GetEnumerator(), parameters.Length);

                for (uint i = 0; i < numTextParameters; i++)
                {
                    var value = "";
                    if (PluginEntry.instance.getFrameText(spec.hash, i, ref value) == RS_ERROR.RS_ERROR_SUCCESS)
                        sceneData.Text.SetValue((int)i, value);
                }

                sceneData.ApplyCPUData();
            }
        }
    
        protected void AwaitFrame()
        {
            RS_ERROR error = PluginEntry.instance.awaitFrameData(500, out var frameData);
            LatestFrameData = frameData;
            m_HasUpdatedLiveTexturesThisFrame = false;
            
            if (error == RS_ERROR.RS_ERROR_QUIT)
                Application.Quit();
            if (error == RS_ERROR.RS_ERROR_STREAMS_CHANGED)
                CreateStreams();
            switch (m_Settings.sceneControl)
            {
                case DisguiseRenderStreamSettings.SceneControl.Selection:
                    if (SceneManager.GetActiveScene().buildIndex != LatestFrameData.scene)
                    {
                        HasNewFrameData = false;
                        SceneManager.LoadScene((int)LatestFrameData.scene);
                        return;
                    }
                    break;
            }
            HasNewFrameData = (error == RS_ERROR.RS_ERROR_SUCCESS);
            if (HasNewFrameData)
            {
                ProcessFrameData(LatestFrameData);
            }

            DisguiseFramerateManager.Update();
        }

        // Updates the RenderTextures assigned to image parameters on the render thread to avoid stalling the main thread
        void OnBeginContextRendering(ScriptableRenderContext context, List<Camera> cameras)
        {
            // TODO fix live texture parameters
            
            // if (!HasNewFrameData)
            //     return;
            //
            // if (LatestFrameData.scene >= m_Schema.scenes.Length)
            //     return;
            //
            // var spec = m_Schema.scenes[LatestFrameData.scene];
            // var images = m_SceneFields[LatestFrameData.scene].images;
            // if (images == null)
            //     return;
            //
            // // Only run once per frame for the main render context
            // if (m_HasUpdatedLiveTexturesThisFrame)
            //     return;
            // foreach (var camera in cameras)
            // {
            //     if (camera.cameraType != CameraType.Game)
            //         return;
            // }
            // m_HasUpdatedLiveTexturesThisFrame = true;
            //
            // var nImageParameters = spec.parameters.Count(t => t.type == RemoteParameterType.RS_PARAMETER_IMAGE);
            //
            // using var imageData = new NativeArray<ImageFrameData>(nImageParameters, Allocator.Temp);
            // if (PluginEntry.instance.GetFrameImageData(spec.hash, imageData.AsSpan()) != RS_ERROR.RS_ERROR_SUCCESS)
            //     return;
            //
            // CommandBuffer cmd = null;
            // var i = 0;
            // foreach (var field in images)
            // {
            //     if (field.GetValue() is RenderTexture renderTexture)
            //     {
            //         // We may be temped to use RenderTexture instead of Texture2D for the shared textures.
            //         // RenderTextures are always stored as typeless texture resources though, which aren't supported
            //         // by CUDA interop (used by Disguise under the hood):
            //         // https://docs.nvidia.com/cuda/cuda-runtime-api/group__CUDART__D3D11.html#group__CUDART__D3D11_1g85d07753780643584b8febab0370623b
            //         // Texture2D apply their GraphicsFormat to their texture resources.
            //
            //         var sharedTexture = TemporaryTexture2DManager.Instance.Get(new Texture2DDescriptor()
            //         {
            //             Width = (int)imageData[i].width,
            //             Height = (int)imageData[i].height,
            //             Format = imageData[i].format,
            //             Linear = true
            //         });
            //
            //         NativeRenderingPlugin.InputImageData data = new NativeRenderingPlugin.InputImageData()
            //         {
            //             m_rs_getFrameImage = PluginEntry.instance.rs_getFrameImage_ptr,
            //             m_ImageId = imageData[i].imageId,
            //             m_Texture = sharedTexture.GetNativeTexturePtr()
            //         };
            //
            //         if (NativeRenderingPlugin.InputImageDataPool.TryPreserve(data, out var dataPtr))
            //         {
            //             if (cmd == null)
            //                 cmd = CommandBufferPool.Get($"Receiving Disguise Image Parameters");
            //
            //             cmd.IssuePluginEventAndData(
            //                 NativeRenderingPlugin.GetRenderEventCallback(),
            //                 NativeRenderingPlugin.GetEventID(NativeRenderingPlugin.EventID.InputImage),
            //                 dataPtr);
            //             cmd.IncrementUpdateCount(sharedTexture);
            //
            //             cmd.Blit(sharedTexture, renderTexture, new Vector2(1.0f, -1.0f), new Vector2(0.0f, 1.0f));
            //             cmd.IncrementUpdateCount(renderTexture);
            //         }
            //         else
            //         {
            //             Debug.LogError($"DisguiseRenderStream: {nameof(NativeRenderingPlugin)}.{nameof(NativeRenderingPlugin.InputImageData)} pool exceeded, skipping input texture '{field.info.Name}'");
            //         }
            //     }
            //
            //     ++i;
            // }
            //
            // if (cmd != null)
            // {
            //     context.ExecuteCommandBuffer(cmd);
            //     CommandBufferPool.Release(cmd);
            // }
        }

        static Camera[] getTemplateCameras()
        {
            return Camera.allCameras;
        }

        static Camera GetChannelCamera(string channel)
        {
            try
            {
                return Array.Find(getTemplateCameras(), camera => camera.name == channel);
            }
            catch (ArgumentNullException)
            {
                return Camera.main;
            }
        }

        public static DisguiseRenderStream Instance { get; private set; }
        
        public static Action SceneLoaded { get; set; } = delegate { };
        
        public static Action StreamsChanged { get; set; } = delegate { };

        public StreamDescription[] Streams { get; private set; } = { };
        
        public IEnumerable<RenderTexture> InputTextures
        {
            get
            {
                // TODO fix live texture remote parameters
                return Enumerable.Empty<RenderTexture>();
                
                // if (LatestFrameData.scene > m_SceneFields.Length)
                //     return Enumerable.Empty<RenderTexture>();
                //
                // var images = m_SceneFields[LatestFrameData.scene].images;
                // if (images == null)
                //     return Enumerable.Empty<RenderTexture>();
                //
                // return images.Select(x => x.GetValue() as RenderTexture);
            }
        }

        public bool Awaiting { get; private set; }

        public FrameData LatestFrameData { get; protected set; }

        public bool HasNewFrameData { get; protected set; }
        
        GameObject[] m_Cameras = { };
        ManagedSchema m_Schema = new ();
        
        SceneData[] m_SceneData;
        DisguiseRenderStreamSettings m_Settings;
        bool m_HasUpdatedLiveTexturesThisFrame;
    }
}