using Configgy;
using System.Collections.Generic;
using UnityEngine;

namespace Ultracam.CameraTools
{
    public class EditorCam : MonoBehaviour
    {
        //Translation
        [Configgable("Binds/Control/SceneViewCamera")]
        private static ConfigKeybind unlockCamera = new ConfigKeybind(KeyCode.Mouse1);
        [Configgable("Binds/Control/SceneViewCamera")]
        private static ConfigKeybind dragCamera = new ConfigKeybind(KeyCode.Mouse2);

        [Configgable("Binds/Control/SceneViewCamera")]
        private static ConfigKeybind speedModifierButton = new ConfigKeybind(KeyCode.LeftShift);
        [Configgable("Binds/Control/SceneViewCamera")]
        private static ConfigKeybind moveForward = new ConfigKeybind(KeyCode.W);
        [Configgable("Binds/Control/SceneViewCamera")]
        private static ConfigKeybind moveBack = new ConfigKeybind(KeyCode.S);
        [Configgable("Binds/Control/SceneViewCamera")]
        private static ConfigKeybind moveLeft = new ConfigKeybind(KeyCode.A);
        [Configgable("Binds/Control/SceneViewCamera")]
        private static ConfigKeybind moveRight = new ConfigKeybind(KeyCode.D);
        [Configgable("Binds/Control/SceneViewCamera")]
        private static ConfigKeybind moveUp = new ConfigKeybind(KeyCode.E);
        [Configgable("Binds/Control/SceneViewCamera")]
        private static ConfigKeybind moveDown = new ConfigKeybind(KeyCode.Q);

        //Zoom
        [Configgable("Binds/Control/SceneViewCamera")]
        private static ConfigKeybind zoomIn = new ConfigKeybind(KeyCode.Equals);
        [Configgable("Binds/Control/SceneViewCamera")]
        private static ConfigKeybind zoomOut = new ConfigKeybind(KeyCode.Minus);

        //Control settings
        [Configgable("Settings/Control/SceneViewCamera")]
        private static ConfigInputField<float> zoomSensitivity = new ConfigInputField<float>(75.0f);
        [Configgable("Settings/Control/SceneViewCamera")]
        private static ConfigInputField<float> lookSensitivity = new ConfigInputField<float>(256f);
        [Configgable("Settings/Control/SceneViewCamera")]
        private static ConfigInputField<float> dragSensitivity = new ConfigInputField<float>(256f);

        [Configgable("Settings/Control/SceneViewCamera")]
        private static ConfigInputField<float> maxMoveSpeed = new ConfigInputField<float>(20f);
        [Configgable("Settings/Control/SceneViewCamera")]
        private static ConfigInputField<float> moveAcceleration = new ConfigInputField<float>(0.225f);
        [Configgable("Settings/Control/SceneViewCamera")] 
        private static ConfigInputField<float> moveDeceleration = new ConfigInputField<float>(3.5f);

        [Configgable("Settings/Control/SceneViewCamera")]
        private static ConfigInputField<float> speedModifierMultiplier = new ConfigInputField<float>(2f);

        //Component stuff
        private Vector3 lastMoveDirection;
        private Vector3 currentLocalRotation;
        private Vector3 moveInput, lookInput;

        private float zoomInput;
        private bool speedModifierPressed = false;
        private bool cameraLooking;
        private bool cameraDragging;
        private float currentSpeed;

        private void Update()
        {
            GetInputs();

            if (cameraDragging)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Drag();
            }
            else if (cameraLooking)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Look();
                Move();
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
            }

            Zoom();
        }


        //Check inputs and perform actions
        private void GetInputs()
        {
            if (CameraHandler.resetCameraPositionToPlayer.WasPeformed())
                transform.position = CameraController.Instance.transform.position;

            if (CameraHandler.resetCameraRotation.WasPeformed())
                currentLocalRotation = Vector3.zero;

            //Translation
            moveInput.z = (moveForward.IsPressed() ? 1 : 0) + (moveBack.IsPressed() ? -1 : 0);
            moveInput.x = (moveLeft.IsPressed() ? -1 : 0) + (moveRight.IsPressed() ? 1 : 0);
            moveInput.y = (moveUp.IsPressed() ? 1 : 0) + (moveDown.IsPressed() ? -1 : 0);

            speedModifierPressed = speedModifierButton.IsPressed();
            cameraDragging = dragCamera.IsPressed();
            cameraLooking = unlockCamera.IsPressed();

            //Rotation
            if(cameraDragging || cameraLooking)
            {
                Vector2 mouseDelta = InputManager.Instance.InputSource.Look.ReadValue<Vector2>();
                lookInput.x = (mouseDelta.x);
                lookInput.y = (-mouseDelta.y);
            }
            else
            {
                lookInput = Vector3.zero;
            }

            zoomInput = Input.mouseScrollDelta.y + ((zoomIn.IsPressed() ? 1 : 0) + (zoomOut.IsPressed() ? -1 : 0)) * 0.15f;
        }

        private void Look()
        {
            currentLocalRotation.x += lookInput.y * lookSensitivity.Value * Time.unscaledDeltaTime;
            currentLocalRotation.y += lookInput.x * lookSensitivity.Value * Time.unscaledDeltaTime;
            transform.localRotation = Quaternion.Euler(currentLocalRotation);
        }

        private void Move()
        {
            float targetSpeed;
            float deltaModifier;
            Vector3 moveVector = moveInput;

            if (moveVector.magnitude > 0f)
            {
                lastMoveDirection = moveVector;
                targetSpeed = maxMoveSpeed.Value;
                deltaModifier = moveAcceleration.Value * ((speedModifierPressed) ? speedModifierMultiplier.Value : 1f);
            }
            else
            {
                deltaModifier = moveDeceleration.Value;
                targetSpeed = 0f;
            }

            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, Time.unscaledDeltaTime * deltaModifier);
            Vector3 translation = lastMoveDirection * currentSpeed;
            transform.position += transform.rotation * translation;
        }


        //drag camera around with Middle Mouse
        private void Drag()
        {
            Vector3 moveDirection = transform.rotation * new Vector3(-lookInput.x, lookInput.y, 0f);
            moveDirection *= dragSensitivity.Value * Time.unscaledDeltaTime * (speedModifierPressed ? speedModifierMultiplier.Value : 1f);
            transform.position += moveDirection;
        }

        private void Zoom()
        {
            //translate camera on local z axis
            if (zoomInput == 0f)
                return;

            transform.position += transform.rotation * (new Vector3(0, 0, zoomInput) * Time.unscaledDeltaTime * zoomSensitivity.Value);
        }

        private void OnEnable()
        {
            currentSpeed = 0f;
            if (CameraHandler.positionFreecamToPlayerOnEnable.Value)
            {
                transform.position = CameraController.Instance.transform.position;
                currentLocalRotation = CameraController.Instance.transform.eulerAngles;
            }
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
            helpMenu += $"TOGGLE CAMERA: (<color=orange>{CameraHandler.toggleCameraEnabled.Value}</color>)\n";
            helpMenu += $"SHOW BINDS: (<color=orange>{CameraHandler.displayAllBinds.Value}</color>)\n\n";

            if (CameraHandler.displayAllBinds.IsPressed())
            {
                helpMenu += $"TRANSLATE: ( <color=orange>{moveForward.Value}{moveLeft.Value}{moveBack.Value}{moveRight.Value}</color> )\n";
                helpMenu += $"TRANSLATE VERTICAL: ( <color=orange>-{moveDown.Value} {moveUp.Value}+</color> )\n";
                helpMenu += $"LOOK: ( <color=orange>MOUSE</color> )\n";
                helpMenu += $"ZOOM: ( +<color=orange>{zoomIn.Value} SCROLL {zoomOut.Value}</color>- )\n";
                helpMenu += $"DRAG: ( <color=orange>{dragCamera.Value}</color> )\n";
                helpMenu += $"SPEED MODIFIER: (<color=orange>{speedModifierButton.Value}</color>)\n";
                helpMenu += $"RESET ROTATION: (<color=orange>{CameraHandler.resetCameraRotation.Value}</color>)\n";
                helpMenu += $"RESET POSITION: (<color=orange>{CameraHandler.resetCameraPositionToPlayer.Value}</color>)\n";
                helpMenu += $"TOGGLE LIGHT: (<color=orange>{CameraHandler.toggleLight.Value}</color>)\n";
            }

            return helpMenu;
        }

    }
}
