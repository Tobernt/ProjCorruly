using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

namespace CustomNamespace
{
    [AddComponentMenu("Custom/Player Camera")]
    public class PlayerCamera : NetworkBehaviour
    {
        private Camera mainCam;

        [Header("Camera Settings")]
        public Vector3 offset = new Vector3(0f, 3f, -8f); // Third-person offset
        public float sensitivity = 2f; // Mouse sensitivity

        [Header("Local Model Rotation")]
        public Transform skinAndBones; // Assign this in Unity
        private Vector3 skinOffset = new Vector3(0.05f, -2.05f, 0f); // Custom offset

        private float verticalRotation = 0f;

        void Awake()
        {
            mainCam = Camera.main;
        }

        public override void OnStartLocalPlayer()
        {
            if (mainCam != null)
            {
                // Attach the camera to the player
                mainCam.transform.SetParent(transform);
                mainCam.transform.localPosition = offset;
                mainCam.orthographic = false;

                // Parent `SkinAndBones` to the camera and apply offset
                if (skinAndBones != null)
                {
                    skinAndBones.SetParent(mainCam.transform);
                    skinAndBones.localPosition = skinOffset;
                    skinAndBones.localRotation = Quaternion.identity;
                }

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Debug.LogWarning("🚨 PlayerCamera: No Main Camera found!");
            }
        }

        private void Update()
        {
            if (!isLocalPlayer || mainCam == null) return;

            float mouseX = Input.GetAxis("Mouse X") * sensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

            // Rotate the player root (networked left/right movement)
            transform.Rotate(Vector3.up * mouseX);

            // Rotate the camera & SkinAndBones up/down together
            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -80f, 80f);

            mainCam.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }

        public override void OnStopLocalPlayer()
        {
            if (mainCam != null && mainCam.transform.parent == transform)
            {
                // Detach the camera and reset it
                mainCam.transform.SetParent(null);
                SceneManager.MoveGameObjectToScene(mainCam.gameObject, SceneManager.GetActiveScene());
                mainCam.orthographic = true;
                mainCam.orthographicSize = 15f;
                mainCam.transform.localPosition = new Vector3(0f, 70f, 0f);
                mainCam.transform.localEulerAngles = new Vector3(90f, 0f, 0f);

                // Reset `SkinAndBones` to the player root when stopping
                if (skinAndBones != null)
                {
                    skinAndBones.SetParent(transform);
                    skinAndBones.localPosition = Vector3.zero;
                    skinAndBones.localRotation = Quaternion.identity;
                }

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}
