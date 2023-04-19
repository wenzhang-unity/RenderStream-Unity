using System;
using System.Collections.Generic;
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
        struct RenderStreamGfxUpdate { }

        protected virtual void Initialize()
        {
            PlayerLoopExtensions.RegisterUpdate<TimeUpdate.WaitForLastPresentationAndUpdateTime, RenderStreamUpdate>(AwaitFrame);
            PlayerLoopExtensions.RegisterUpdate<Update.ScriptRunBehaviourUpdate, RenderStreamGfxUpdate>(UpdateGfxResources);
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
                
                if (DisguiseRenderStreamSettings.GetOrCreateSettings().enableUnityDebugWindowPresenter)
                {
                    var presenterGO = UnityDebugWindowPresenter.Create();
                    var presenter = presenterGO.GetComponent<UnityDebugWindowPresenter>();
                    var presenterRemoteParameters = presenter.GetRemoteParameterWrappers();
                    remoteParameters.InsertRange(0, presenterRemoteParameters);
                }
                
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

            using var parameters = new NativeArray<float>(sceneData.Numeric.Length, Allocator.Temp);
            if (PluginEntry.instance.GetFrameParameters(spec.hash, parameters.AsSpan()) == RS_ERROR.RS_ERROR_SUCCESS)
            {
                sceneData.Numeric.SetData(parameters.GetEnumerator(), parameters.Length);

                for (uint i = 0; i < sceneData.Text.Length; i++)
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
        void UpdateGfxResources()
        {
            if (!HasNewFrameData)
                return;
            
            if (LatestFrameData.scene >= m_Schema.scenes.Length)
                return;
            
            var spec = m_Schema.scenes[LatestFrameData.scene];
            var sceneData = m_SceneData[LatestFrameData.scene];
            
            using var imageData = new NativeArray<ImageFrameData>(sceneData.Textures.Length, Allocator.Temp);
            if (PluginEntry.instance.GetFrameImageData(spec.hash, imageData.AsSpan()) != RS_ERROR.RS_ERROR_SUCCESS)
                return;
            
            CommandBuffer cmd = null;
            for (var i = 0; i < sceneData.Textures.Length; i++)
            {
                var info = imageData[i];
                if (info.format == RSPixelFormat.RS_FMT_INVALID)
                {
                    Debug.LogError($"DisguiseRenderStream: skipping input texture {i} with {nameof(RSPixelFormat)}.{nameof(RSPixelFormat.RS_FMT_INVALID)}");
                    continue;
                }

                // We may be temped to use RenderTexture instead of Texture2D for the shared textures.
                // RenderTextures are always stored as typeless texture resources though, which aren't supported
                // by CUDA interop (used by Disguise under the hood):
                // https://docs.nvidia.com/cuda/cuda-runtime-api/group__CUDART__D3D11.html#group__CUDART__D3D11_1g85d07753780643584b8febab0370623b
                // Texture2D apply their GraphicsFormat to their texture resources.

                var sharedTexture = sceneData.Textures.GetOrCreateTexture(i, info);

                NativeRenderingPlugin.InputImageData data = new NativeRenderingPlugin.InputImageData()
                {
                    m_rs_getFrameImage = PluginEntry.instance.rs_getFrameImage_ptr,
                    m_ImageId = info.imageId,
                    m_Texture = sharedTexture.GetNativeTexturePtr()
                };

                if (NativeRenderingPlugin.InputImageDataPool.TryPreserve(data, out var dataPtr))
                {
                    cmd ??= CommandBufferPool.Get($"Receiving Disguise Image Parameters");

                    cmd.IssuePluginEventAndData(
                        NativeRenderingPlugin.GetRenderEventCallback(),
                        NativeRenderingPlugin.GetEventID(NativeRenderingPlugin.EventID.InputImage),
                        dataPtr);
                    cmd.IncrementUpdateCount(sharedTexture);

                    sceneData.Textures.SetChangedValue(i, sharedTexture);
                }
                else
                {
                    Debug.LogError($"DisguiseRenderStream: {nameof(NativeRenderingPlugin)}.{nameof(NativeRenderingPlugin.InputImageData)} pool exceeded, skipping input texture {i}");
                }
            }

            if (cmd != null)
            {
                Graphics.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
                
                var gpuDataCmd = CommandBufferPool.Get($"Applying Disguise GPU data");
                sceneData.ApplyGPUData(gpuDataCmd);
                Graphics.ExecuteCommandBuffer(gpuDataCmd);
            }
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
        
        public ReadOnlyMemory<Texture> InputTextures
        {
            get
            {
                if (LatestFrameData.scene > m_SceneData.Length)
                    return Array.Empty<Texture>();
                
                return m_SceneData[LatestFrameData.scene].Textures.Data;
            }
        }

        public bool Awaiting { get; private set; }

        public FrameData LatestFrameData { get; protected set; }

        public bool HasNewFrameData { get; protected set; }
        
        GameObject[] m_Cameras = { };
        ManagedSchema m_Schema = new ();
        
        SceneData[] m_SceneData;
        DisguiseRenderStreamSettings m_Settings;
    }
}