using UnityEngine;
using UnityEngine.InputSystem;  

public class MouseLookAroundScript : MonoBehaviour {

    public float sensitivity;
    public float webGLSensitivityMultiplier = 0.1f;

    public float xRotation = 0f;
    public float yRotation = 0f;

    private Vector2 lastMouseDelta;

    void Update() {
        if (Mouse.current == null || SidebarManager.isActive || SidebarManager.isOpen) {
            lastMouseDelta = Vector2.zero;
            return;
        }

        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * 0.02f * sensitivity;

        // Browsers report larger mouse movement
        if (Application.platform == RuntimePlatform.WebGLPlayer) {
            mouseDelta *= webGLSensitivityMultiplier;
        }

        // Average with the previous frame to even out uneven mouse input without adding lag
        Vector2 smoothedDelta = (mouseDelta + lastMouseDelta) * 0.5f;
        lastMouseDelta = mouseDelta;

        xRotation += smoothedDelta.y;
        yRotation -= smoothedDelta.x;

        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        yRotation %= 360;

        transform.localEulerAngles = new Vector3(xRotation, yRotation, 0);
    }
}