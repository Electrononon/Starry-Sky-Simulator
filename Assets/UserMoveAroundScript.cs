using UnityEngine;
using UnityEngine.InputSystem;

public class UserMoveAroundScript : MonoBehaviour {
    
    public float xRot;
    public float yRot;

    public float speed = 32f;

    public MouseLookAroundScript userCamera;
    
    public float boundary = 100000f;

    public static bool isAtBoundary;

    void Start() {
        transform.position = new Vector3(0f, 0f, 0f);
    }

    void Update() {
        if (SidebarManager.isActive || SidebarManager.isOpen) return;

        xRot = Mathf.Deg2Rad * userCamera.yRotation;
        yRot = Mathf.Deg2Rad * userCamera.xRotation;

        Vector3 pos = transform.position;

        if (Keyboard.current.wKey.isPressed) {
            pos += speed * transform.forward * Time.deltaTime;
        }

        if (Keyboard.current.sKey.isPressed) {
            pos -= speed * transform.forward * Time.deltaTime;
        }

        if (Keyboard.current.aKey.isPressed) {
            pos -= speed * transform.right * Time.deltaTime;
        }

        if (Keyboard.current.dKey.isPressed) {
            pos += speed * transform.right * Time.deltaTime;
        }

        if (Keyboard.current.spaceKey.isPressed) {
            pos += speed * transform.up * Time.deltaTime;
        }

        if (Keyboard.current.leftShiftKey.isPressed) {
            pos -= speed * transform.up * Time.deltaTime;
        }

        // Decrease speed when Q is pressed
        if (Keyboard.current.qKey.wasPressedThisFrame && speed > 0.001f) { // Originally 0.5f
            speed /= 2f;
        }

        // Increase speed when E is pressed
        if (Keyboard.current.eKey.wasPressedThisFrame && speed < 8192f) {
            speed *= 2f;
        }

        if (pos.magnitude > boundary) {
            pos = pos.normalized * boundary;
        }

        isAtBoundary = pos.magnitude >= boundary * 0.999f;

        transform.position = pos;
    }
}