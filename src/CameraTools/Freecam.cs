using Configgy;
using Ultracam.CameraTools;
using UnityEngine;

namespace HydraDynamics.CameraTools
{
    public class Freecam : MonoBehaviour
    {
        
        [Configgable("Binds/Control/StandardCamera")]
        private static ConfigKeybind toggleSmoothing = new ConfigKeybind(KeyCode.F6);

        //Translation
        [Configgable("Binds/Control/StandardCamera")]
        private static ConfigKeybind speedModifierButton = new ConfigKeybind(KeyCode.LeftShift);
        [Configgable("Binds/Control/StandardCamera")]
        private static ConfigKeybind moveForward = new ConfigKeybind(KeyCode.W);
        [Configgable("Binds/Control/StandardCamera")]
        private static ConfigKeybind moveBack = new ConfigKeybind(KeyCode.S);
        [Configgable("Binds/Control/StandardCamera")]
        private static ConfigKeybind moveLeft = new ConfigKeybind(KeyCode.A);
        [Configgable("Binds/Control/StandardCamera")]
        private static ConfigKeybind moveRight = new ConfigKeybind(KeyCode.D);
        [Configgable("Binds/Control/StandardCamera")]
        private static ConfigKeybind moveUp = new ConfigKeybind(KeyCode.Space);
        [Configgable("Binds/Control/StandardCamera")]
        private static ConfigKeybind moveDown = new ConfigKeybind(KeyCode.LeftControl);

        //Rotation
        [Configgable("Binds/Control/StandardCamera")]
        private static ConfigKeybind rollLeft = new ConfigKeybind(KeyCode.Q);
        [Configgable("Binds/Control/StandardCamera")]
        private static ConfigKeybind rollRight = new ConfigKeybind(KeyCode.E);
        [Configgable("Binds/Control/StandardCamera")]
        private static ConfigKeybind pitchUp = new ConfigKeybind(KeyCode.UpArrow);
        [Configgable("Binds/Control/StandardCamera")]
        private static ConfigKeybind pitchDown = new ConfigKeybind(KeyCode.DownArrow);
        [Configgable("Binds/Control/StandardCamera")]
        private static ConfigKeybind yawLeft = new ConfigKeybind(KeyCode.LeftArrow);
        [Configgable("Binds/Control/StandardCamera")]
        private static ConfigKeybind yawRight = new ConfigKeybind(KeyCode.RightArrow);

        //Zoom
        [Configgable("Binds/Control/StandardCamera")]
        private static ConfigKeybind zoomIn = new ConfigKeybind(KeyCode.Equals);
        [Configgable("Binds/Control/StandardCamera")]
        private static ConfigKeybind zoomOut = new ConfigKeybind(KeyCode.Minus);

        //Control settings
        [Configgable("Settings/Control/StandardCamera")]
        private static ConfigInputField<float> zoomSpeed = new ConfigInputField<float>(60.0f);
        [Configgable("Settings/Control/StandardCamera")]
        private static ConfigInputField<float> smoothingSpeed = new ConfigInputField<float>(1.5f);
        [Configgable("Settings/Control/StandardCamera")]
        private static ConfigInputField<float> lookSpeed = new ConfigInputField<float>(128f);
        [Configgable("Settings/Control/StandardCamera")]
        private static ConfigInputField<float> mouseLookSpeedMultiplier = new ConfigInputField<float>(1f);
        [Configgable("Settings/Control/StandardCamera")]
        private static ConfigInputField<float> rollSpeed = new ConfigInputField<float>(40f);
        [Configgable("Settings/Control/StandardCamera")]
        private static ConfigInputField<float> moveSpeed = new ConfigInputField<float>(12f);
        [Configgable("Settings/Control/StandardCamera")]
        private static ConfigInputField<float> speedModifierMultiplier = new ConfigInputField<float>(2f);

        //Component stuff
        private Transform cameraTransform;
        private Camera camera;

        private static float minZoom = 1e-05f, maxZoom = 179f;
        private float currentZoom = 77.0f;

        private Vector3 currentLocalRotation;

        private Vector3 moveInput, lookInput;
        private float zoomInput;
        private float smoothedZoom;

        private bool speedModifierPressed = false;
        private bool smoothing = false;

        Transform cameraTarget;

        public void Init(CameraHandler handler)
        {
            cameraTransform = transform;
            camera = GetComponent<Camera>();
            cameraTarget = new GameObject("FreecamTarget").transform;
            cameraTarget.transform.parent = handler.transform;
        }

        private void Update()
        {
            GetInputs();
            Look();
            Move();
            Zoom();
            UpdateCameraTransform();
        }

        private void OnEnable()
        {
            if (CameraHandler.positionFreecamToPlayerOnEnable.Value)
            {
                cameraTransform.position = CameraController.Instance.transform.position;
                cameraTarget.position = CameraController.Instance.transform.position;

                currentLocalRotation = CameraController.Instance.transform.eulerAngles;
                cameraTarget.rotation = Quaternion.Euler(currentLocalRotation);
            }
        }

        //Check inputs and perform actions
        private void GetInputs()
        {
            if(CameraHandler.resetCameraPositionToPlayer.WasPeformed())
            {
                cameraTarget.position = CameraController.Instance.transform.position;
                cameraTransform.position = CameraController.Instance.transform.position;
            }

            if (CameraHandler.resetCameraRotation.WasPeformed())
            {
                currentLocalRotation = Vector3.zero;
                cameraTarget.rotation = Quaternion.identity;
            }

            if (toggleSmoothing.WasPeformed())
                smoothing = !smoothing;

            //Translation
            moveInput.z = (moveForward.IsPressed() ? 1 : 0) + (moveBack.IsPressed() ? -1 : 0);
            moveInput.x = (moveLeft.IsPressed() ? -1 : 0) + (moveRight.IsPressed() ? 1 : 0);
            moveInput.y = (moveUp.IsPressed() ? 1 : 0) + (moveDown.IsPressed() ? -1 : 0);

            speedModifierPressed = speedModifierButton.IsPressed();

            Vector2 mouseDelta = InputManager.Instance.InputSource.Look.ReadValue<Vector2>() * mouseLookSpeedMultiplier.Value;
            
            //Rotation
            lookInput.x = (mouseDelta.x) + (yawLeft.IsPressed() ? -1 : 0) + (yawRight.IsPressed() ? 1 : 0);
            lookInput.y = (-mouseDelta.y) + (pitchUp.IsPressed() ? -1 : 0) + (pitchDown.IsPressed() ? 1 : 0);
            lookInput.z = (rollLeft.IsPressed() ? 1 : 0) + (rollRight.IsPressed() ? -1 : 0);

            zoomInput = -Input.mouseScrollDelta.y + ((zoomIn.IsPressed() ? 1 : 0) + (zoomOut.IsPressed() ? -1 : 0)) * 0.15f;
        }

        private void UpdateCameraTransform()
        {
            Vector3 targetPosition = smoothing ? Vector3.Lerp(cameraTransform.position, cameraTarget.position, smoothingSpeed.Value * Time.unscaledDeltaTime) : cameraTarget.position;
            Quaternion targetRotation = smoothing ? Quaternion.Slerp(cameraTransform.rotation, cameraTarget.rotation, smoothingSpeed.Value * Time.unscaledDeltaTime) : cameraTarget.rotation;

            cameraTransform.position = targetPosition;
            cameraTransform.rotation = targetRotation;
        }

        private void Look()
        {
            currentLocalRotation.x += lookInput.y * lookSpeed.Value * Time.unscaledDeltaTime;
            currentLocalRotation.y += lookInput.x * lookSpeed.Value * Time.unscaledDeltaTime;
            currentLocalRotation.z += lookInput.z * rollSpeed.Value * Time.unscaledDeltaTime;

            cameraTarget.rotation = Quaternion.Euler(currentLocalRotation);
        }

        private void Move()
        {
            Vector3 moveVector = moveInput * moveSpeed.Value * Time.unscaledDeltaTime * (speedModifierPressed ? speedModifierMultiplier.Value : 1f);
            cameraTarget.position += cameraTarget.rotation * moveVector;
        }

        private void Zoom()
        {
            currentZoom = Mathf.Clamp(currentZoom + zoomInput * zoomSpeed.Value * Time.unscaledDeltaTime, minZoom, maxZoom);
            float zoom = 0;

            if (!smoothing)
                zoom = currentZoom;
            else
                zoom = smoothedZoom = Mathf.Lerp(smoothedZoom, currentZoom, smoothingSpeed.Value * Time.unscaledDeltaTime);

            camera.fieldOfView = zoom;
        }

        private void OnGUI()
        {
            if (!CameraHandler.ShowHelpMenu)
                return;

            GUI.skin.box.fontSize = 35;
            GUI.skin.box.normal.textColor = Color.white;
            GUI.skin.box.alignment = TextAnchor.UpperLeft;
            GUILayout.Box(GetHelpMenuText().TrimEnd('\n', '\r'));
        }

        private string GetHelpMenuText()
        {
            string helpMenu = $"HUD TOGGLE - (<color=orange>{CameraHandler.toggleOnScreenDisplay.Value}</color>)\n";
            helpMenu += $"SHOW BINDS: (<color=orange>{CameraHandler.displayAllBinds.Value}</color>)\n\n";

            if (CameraHandler.displayAllBinds.IsPressed())
            {
                helpMenu += $"MOVE: ( <color=orange>{moveForward.Value}{moveLeft.Value}{moveBack.Value}{moveRight.Value}</color> )\n";
                helpMenu += $"VERTICAL: ( <color=orange>-{moveDown.Value} {moveUp.Value}+</color> )\n";
                helpMenu += $"LOOK: ( <color=orange>{pitchUp.Value}{yawLeft.Value}{pitchDown.Value}{yawRight.Value}</color> )\n";
                helpMenu += $"ZOOM: ( +<color=orange>{zoomIn.Value} {zoomOut.Value}</color>- )\n";
                helpMenu += $"ROLL: ( <color=orange><{rollLeft.Value} {rollRight.Value}></color> )\n";
                helpMenu += $"SPEED MODIFIER: (<color=orange>{speedModifierButton.Value}</color>)\n";
                helpMenu += $"SMOOTHING: <color=orange>{smoothing}</color> (<color=orange>{toggleSmoothing.Value}</color>)\n";
                helpMenu += $"RESET ROTATION: (<color=orange>{CameraHandler.resetCameraRotation.Value}</color>)\n";
                helpMenu += $"RESET POSITION: (<color=orange>{CameraHandler.resetCameraPositionToPlayer.Value}</color>)\n";
                helpMenu += $"TOGGLE LIGHT: (<color=orange>{CameraHandler.toggleLight.Value}</color>)\n";
            }

            return helpMenu;
        }

    }
}
