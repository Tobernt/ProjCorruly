using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

namespace CustomNamespace
{
    [AddComponentMenu("Custom/Player Camera")]
    public class PlayerCamera : NetworkBehaviour
    {
        private Camera mainCam;

        public Vector3 offset = new Vector3(0f, 3f, -8f); // Third-person offset
        public Vector3 rotation = new Vector3(10f, 0f, 0f); // Default rotation
        public float sensitivity = 2f; // Sensitivity for mouse movement
        private float verticalRotation = 0f; // Tracks up-and-down rotation

        void Awake()
        {
            mainCam = Camera.main;
        }


        public float GetVerticalRotation()
        {
            return verticalRotation;
        }


        public override void OnStartLocalPlayer()
        {
            if (mainCam != null)
            {
                // Attach the camera to the player and apply initial settings
                mainCam.transform.SetParent(transform);
                mainCam.transform.localPosition = offset;
                mainCam.transform.localEulerAngles = rotation;
                mainCam.orthographic = false;

                Cursor.lockState = CursorLockMode.Locked; // Lock cursor for camera control
                Cursor.visible = false;
            }
            else
            {
                Debug.LogWarning("PlayerCamera: Could not find a camera in the scene with 'MainCamera' tag.");
            }
        }

        private void Update()
        {
            if (!isLocalPlayer || mainCam == null) return;

            // Get mouse input for camera control
            float mouseX = Input.GetAxis("Mouse X") * sensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

            // Rotate the player horizontally
            transform.Rotate(Vector3.up * mouseX);

            // Adjust vertical rotation and clamp it to avoid over-rotating
            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -45f, 45f); // Limit pitch between -45° and 45°

            // Apply vertical rotation to the camera
            mainCam.transform.localEulerAngles = new Vector3(verticalRotation, 0f, 0f);
        }

        public override void OnStopLocalPlayer()
        {
            if (mainCam != null && mainCam.transform.parent == transform)
            {
                // Detach the camera and reset it for the scene
                mainCam.transform.SetParent(null);
                SceneManager.MoveGameObjectToScene(mainCam.gameObject, SceneManager.GetActiveScene());
                mainCam.orthographic = true;
                mainCam.orthographicSize = 15f;
                mainCam.transform.localPosition = new Vector3(0f, 70f, 0f);
                mainCam.transform.localEulerAngles = new Vector3(90f, 0f, 0f);

                Cursor.lockState = CursorLockMode.None; // Unlock the cursor
                Cursor.visible = true;
            }
        }
    }
}
