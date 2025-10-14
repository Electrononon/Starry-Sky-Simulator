using UnityEngine;
using UnityEngine.InputSystem;  

public class MouseLookAroundScript : MonoBehaviour {

    public float sensitivity;

    public float xRotation = 0f;
    public float yRotation = 0f;

    void Update() {
        if (Mouse.current == null || SidebarManager.isActive) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * 0.02f;

        xRotation += mouseDelta.y * sensitivity;
        yRotation -= mouseDelta.x * sensitivity;

        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        yRotation %= 360;

        transform.localEulerAngles = new Vector3(xRotation, yRotation, 0);
    }
}
