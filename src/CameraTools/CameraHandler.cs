using Configgy;
using HydraDynamics.CameraTools;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Ultracam.CameraTools
{
    public class CameraHandler : MonoBehaviour
    {
        //Config

        //General binds
        [Configgable("Binds", "Toggle Freecam Button")]
        public static ConfigKeybind toggleCameraEnabled = new ConfigKeybind(KeyCode.F5);
        [Configgable("Binds", "Toggle Light")]
        public static ConfigKeybind toggleLight = new ConfigKeybind(KeyCode.F);
        
        [Configgable("Binds")]
        public static ConfigKeybind toggleOnScreenDisplay = new ConfigKeybind(KeyCode.Semicolon);
        [Configgable("Binds")]
        public static ConfigKeybind resetCameraRotation = new ConfigKeybind(KeyCode.F7);
        [Configgable("Binds", "Reset Camera To Player Position")]
        public static ConfigKeybind resetCameraPositionToPlayer = new ConfigKeybind(KeyCode.F8);
        [Configgable("Binds", displayName: "Show all binds in OSD")]
        public static ConfigKeybind displayAllBinds = new ConfigKeybind(KeyCode.LeftAlt);

        //Camera settings
        [Configgable("Settings/Camera")]
        private static ConfigInputField<float> cameraNearClipPlane = new ConfigInputField<float>(0.01f);
        [Configgable("Settings/Camera")]
        private static ConfigInputField<float> cameraFarClipPlane = new ConfigInputField<float>(1000f);
        [Configgable("Settings/Camera")]
        private static ConfigInputField<float> cameraFieldOfView = new ConfigInputField<float>(77f, (v) => (v >= minZoom && v <= maxZoom));
        [Configgable("Settings/Camera")]
        private static ConfigInputField<int> cameraCullingMask = new ConfigInputField<int>(-1, (v) => (v >= -1));
        [Configgable("Settings/Camera")]
        private static ConfigToggle copyExistingCameraCullingMask = new ConfigToggle(true);

        //Light settings
        [Configgable("Settings/Light")]
        private static ConfigInputField<float> lightIntensity = new ConfigInputField<float>(1.0f, (v) => (v >= 0f));
        [Configgable("Settings/Light")]
        private static ConfigColor lightColor = new ConfigColor(Color.white);
        [Configgable("Settings/Light")]
        private static ConfigInputField<int> lightCullingMask = new ConfigInputField<int>(-1, (v) => (v >= -1));

        //General settings
        [Configgable("Settings/General")]
        public static ConfigToggle stopTimeDuringFreecam = new ConfigToggle(true);

        [Configgable("Settings/General")]
        private static ConfigDropdown<CameraType> cameraType = new ConfigDropdown<CameraType>((CameraType[])Enum.GetValues(typeof(CameraType)).Cast<CameraType>().ToArray());

        [Configgable("Settings/General", "Position Freecam To Player On Enable")]
        public static ConfigToggle positionFreecamToPlayerOnEnable = new ConfigToggle(true);

        private static float minZoom = 1e-05f, maxZoom = 179f;
        public static bool Enabled { get; private set; }
        public static bool ShowHelpMenu { get; private set; } = true;

        private GameObject cameraObject;
        private Camera camera;
        private AudioListener listener;
        private Light light;

        private Freecam freecam;
        private EditorCam sceneViewCam;

        private static bool loaded;

        public static void Load()
        {
            if (loaded)
                return;

            loaded = true;
            GameObject cameraHandler = new GameObject("FreecamHandler");
            DontDestroyOnLoad(cameraHandler);
            CameraHandler handler = cameraHandler.AddComponent<CameraHandler>();
            handler.Init();

            stopTimeDuringFreecam.OnValueChanged += (v) =>
            {
                if (Enabled)
                    Time.timeScale = v ? 0f : 1f;
            };
        }

        private void Init()
        {
            cameraObject = new GameObject("Camera");
            cameraObject.transform.parent = transform;

            //Construct the light
            light = cameraObject.AddComponent<Light>();
            light.type = LightType.Directional;

            light.intensity = lightIntensity.Value;
            lightIntensity.OnValueChanged += (v) => light.intensity = v;

            light.shadows = LightShadows.None;
            light.enabled = false;

            light.color = lightColor.Value;
            lightColor.OnValueChanged += (v) => light.color = v;

            light.cullingMask = lightCullingMask.Value;
            lightCullingMask.OnValueChanged += (v) => light.cullingMask = v;

            //Listener
            listener = cameraObject.AddComponent<AudioListener>();
            listener.enabled = true;

            //Construct the camera
            camera = cameraObject.AddComponent<Camera>();

            camera.fieldOfView = cameraFieldOfView.Value;
            cameraFieldOfView.OnValueChanged += (v) => camera.fieldOfView = v;

            camera.nearClipPlane = cameraNearClipPlane.Value;
            cameraNearClipPlane.OnValueChanged += (v) => camera.nearClipPlane = v;

            camera.farClipPlane = cameraFarClipPlane.Value;
            cameraFarClipPlane.OnValueChanged += (v) => camera.farClipPlane = v;

            camera.cullingMask = cameraCullingMask.Value;
            cameraCullingMask.OnValueChanged += (v) =>
            {
                if (!copyExistingCameraCullingMask.Value)
                    camera.cullingMask = v;
            };

            copyExistingCameraCullingMask.OnValueChanged += (v) =>
            {
                if (v)
                    camera.cullingMask = CameraController.Instance.cam.cullingMask;
                else
                    camera.cullingMask = cameraCullingMask.Value;
            };

            //Depth is 1 so it takes priority over the hud camera
            camera.depth = 1;

            camera.useOcclusionCulling = false;
            camera.tag = "MainCamera";
            camera.enabled = true;
            
            cameraObject.SetActive(false);

            CameraType type = cameraType.Value;

            freecam = cameraObject.AddComponent<Freecam>();
            freecam.Init(this);
            freecam.enabled = false;
            
            sceneViewCam = cameraObject.AddComponent<EditorCam>();
            sceneViewCam.enabled = false;

            Action<CameraType> onCameraTypeChanged = (v) =>
            {
                Debug.Log($"Changing Freecam to type {v}");
                freecam.enabled = false;
                sceneViewCam.enabled = false;

                switch (v)
                {
                    case CameraType.Standard:
                        freecam.enabled = true;
                        break;
                    case CameraType.SceneView:
                        sceneViewCam.enabled = true;
                        break;
                }
            };

            cameraType.OnValueChanged += onCameraTypeChanged;
            onCameraTypeChanged(type);
        }


        private void Update()
        {
            Inputs();
            
            if (!Enabled)
                return;
         
            Time.timeScale = (CameraHandler.stopTimeDuringFreecam.Value) ? 0f : 1f;
        }

        private void Inputs()
        {
            if (toggleCameraEnabled.WasPeformed())
                SetEnabled(!Enabled);

            if (!Enabled)
                return;

            if (toggleLight.WasPeformed())
                light.enabled = !light.enabled;

            if (toggleOnScreenDisplay.WasPeformed())
                ShowHelpMenu = !ShowHelpMenu;
        }

        public void SetEnabled(bool enabled)
        {
            SetExternalStates(!enabled);
            cameraObject.SetActive(enabled);
            Enabled = enabled;
            Debug.LogWarning($"Freecam {(Enabled ? "Enabled" : "Disabled")}");
        }

        private static FieldInfo hudCameraField = typeof(CameraController).GetField("hudCamera", BindingFlags.NonPublic | BindingFlags.Instance);
        private static GameState camState;

        //Disable and enable the game components that would interfere with the freecam
        private void SetExternalStates(bool freeCamDisabled)
        {
            //Disable the players camera and hud camera
            CameraController.Instance.cam.enabled = freeCamDisabled;
            CameraController.Instance.enabled = freeCamDisabled;
            CameraController.Instance.gameObject.GetComponent<AudioListener>().enabled = freeCamDisabled;
            Camera hudCam = (Camera)hudCameraField.GetValue(CameraController.Instance);
            hudCam.enabled = freeCamDisabled;

            if (copyExistingCameraCullingMask.Value)
                camera.cullingMask = CameraController.Instance.cam.cullingMask;

            //Disable movement
            NewMovement.Instance.enabled = freeCamDisabled;

            //Set UI visibility
            float canvasAlpha = freeCamDisabled ? 1f : 0f;
            if (!CanvasController.Instance.TryGetComponent<CanvasGroup>(out CanvasGroup canvasGroup))
                canvasGroup = CanvasController.Instance.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = canvasAlpha;

            //Set the game state... even though some of it doesnt work as intended...
            camState ??= new GameState("Freecam")
            {
                cameraInputLock = LockMode.Lock,
                playerInputLock = LockMode.Lock,
                priority = 200,
                cursorLock = LockMode.Lock,
            };

            if (freeCamDisabled)
                GameStateManager.Instance.PopState("Freecam");
            else
                GameStateManager.Instance.RegisterState(camState);

            if (stopTimeDuringFreecam.Value)
                Time.timeScale = (freeCamDisabled) ? 1f : 0f;
        }
    }

    public enum CameraType
    {
        Standard,
        SceneView,
    }
}
