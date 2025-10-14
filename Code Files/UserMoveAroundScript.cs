using UnityEngine;
using UnityEngine.InputSystem;

public class UserMoveAroundScript : MonoBehaviour {
    
    public float xRot;
    public float yRot;

    public float speed = 32f;

    private float inputTimer = 0.0f;

    public MouseLookAroundScript userCamera;
    
    public float boundary = 100000f;

    public bool canSpeedIncrease = true;

    void Start() {
        transform.position = new Vector3(0f, 0f, 0f);
    }

    void Update() {
        if (SidebarManager.isActive) return;
        
        inputTimer += Time.deltaTime;

        if (inputTimer > 1/60f) {
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
            if (Keyboard.current.qKey.isPressed && speed > 0.001f && canSpeedIncrease) { // Originally 0.5f
                speed /= 2f;
                canSpeedIncrease = false;
            }

            // Increase speed when E is pressed
            if (Keyboard.current.eKey.isPressed && speed < 8192f && canSpeedIncrease) {
                speed *= 2f;
                canSpeedIncrease = false;
            }

            // Allow user to speed up/slow down when they release a speed up/slow down key
            if (Keyboard.current.qKey.wasReleasedThisFrame || Keyboard.current.eKey.wasReleasedThisFrame) {
                canSpeedIncrease = true;
            }

            if (pos.magnitude > boundary) {
                pos = pos.normalized * boundary;
            }

            transform.position = pos;
            inputTimer = 0f;            
        }
    }
}
