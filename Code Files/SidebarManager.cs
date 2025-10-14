using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem; 
using UnityEngine.EventSystems; 

public class SidebarManager : MonoBehaviour
{
    public GameObject sidebar;
    public Vector2 openPos;
    public Vector2 closedPos;
    public AnimationCurve animationCurve;

    public static bool isActive;

    private RectTransform m_RT;
    private float endTime;
    private bool isOpen;

    public Toggle starLabelToggle;
    public Toggle constellationLabelToggle;

    public static bool starLabelsShown;
    public static bool constellationLabelsShown;
    
    void Start()
    {
        FindLatestKeyFrame();
        m_RT = GetComponent<RectTransform>();
        closedPos = m_RT.anchoredPosition;

        // Immediately set the sidebar to the open position
        m_RT.anchoredPosition = openPos;
        isOpen = true;

        // Initialize boolean values
        starLabelsShown = starLabelToggle.isOn;
        constellationLabelsShown = constellationLabelToggle.isOn;

        // Add event listeners to label toggling
        starLabelToggle.onValueChanged.AddListener(OnStarLabelToggleChanged);
        constellationLabelToggle.onValueChanged.AddListener(OnConstellationLabelToggleChanged);

        isActive = true;
        Cursor.visible = true;
    }


    void Update()
    {
         // If the player clicks outside of the UI, hide the mouse and UI
        if (Mouse.current.leftButton.wasPressedThisFrame && !EventSystem.current.IsPointerOverGameObject())
        {
            isActive = false;
            Cursor.visible = false;
            // Close sidebar if opened
            if (isOpen)
            {
                ToggleSidebar();
            }
        }

        // Set cursor visibility and activity to false if the sidebar is closed
        if (!isOpen && (Cursor.visible || isActive)) {
            Cursor.visible = false;
            isActive = false;
        }

        // If the player presses [ESC], then show the mouse and UI
        if (Keyboard.current.escapeKey.wasPressedThisFrame && !isActive)
        {
            isActive = true;
            Cursor.visible = true;
            ToggleSidebar();
        }
    }

    
    public void ToggleSidebar()
    {
        if (isOpen)
        {
            StartCoroutine(CloseRoutine());
        }
        else
        {
            StartCoroutine(OpenRoutine());
        }
    }

    IEnumerator OpenRoutine()
    {
        float elapsedTime = 0f;
        
        isOpen = true;

        while (elapsedTime < endTime)
        {
            m_RT.anchoredPosition = Vector3.Lerp(closedPos, openPos, animationCurve.Evaluate(elapsedTime));
            yield return new WaitForEndOfFrame();
            elapsedTime += Time.deltaTime;
        }

    }

    IEnumerator CloseRoutine()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < endTime)
        {
            m_RT.anchoredPosition = Vector3.Lerp(openPos, closedPos, animationCurve.Evaluate(elapsedTime));
            yield return new WaitForEndOfFrame();
            elapsedTime += Time.deltaTime;
        }

        isOpen = false;
    }


    void FindLatestKeyFrame()
    {
        float maxTime = Mathf.NegativeInfinity;

        foreach (Keyframe kf in animationCurve.keys) {
            maxTime = kf.time;
        }

        endTime = maxTime;
    }

    public void OnStarLabelToggleChanged(bool isOn) {
        starLabelsShown = isOn;
    }

    public void OnConstellationLabelToggleChanged(bool isOn) {
        constellationLabelsShown = isOn;
    }
}
