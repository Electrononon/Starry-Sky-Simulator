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
    public static bool isOpen;
    public static bool isAnimating;

    private RectTransform m_RT;
    private CanvasGroup canvasGroup;
    private float endTime;

    public Toggle starLabelToggle;
    public Toggle constellationLabelToggle;

    public static bool starLabelsShown;
    public static bool constellationLabelsShown;
    
    void Start()
    {
        FindLatestKeyFrame();
        m_RT = GetComponent<RectTransform>();
        closedPos = m_RT.anchoredPosition;

        // Used to disable the sidebar's buttons while it animates
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Immediately set the sidebar to the open position
        m_RT.anchoredPosition = openPos;
        isOpen = true;
        isAnimating = false;

        // Initialize boolean values
        starLabelsShown = starLabelToggle.isOn;
        constellationLabelsShown = constellationLabelToggle.isOn;

        // Add event listeners to label toggling
        starLabelToggle.onValueChanged.AddListener(OnStarLabelToggleChanged);
        constellationLabelToggle.onValueChanged.AddListener(OnConstellationLabelToggleChanged);

        isActive = true;
        UnlockCursor();
    }


    void Update()
    {
        // Ignore all input while the sidebar is opening or closing
        if (isAnimating) return;

        // If the player clicks outside of the UI, hide the mouse and UI
        if (Mouse.current.leftButton.wasPressedThisFrame && !EventSystem.current.IsPointerOverGameObject())
        {
            isActive = false;
            LockCursor();
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
            OpenSidebar();
        }
    }

    // Called by the web page when the browser releases the mouse lock (e.g. the player pressed [ESC])
    public void OnBrowserPointerUnlocked()
    {
        if (!isActive && !isAnimating)
        {
            OpenSidebar();
        }
    }

    void OpenSidebar()
    {
        isActive = true;
        UnlockCursor();
        ToggleSidebar();
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    
    public void ToggleSidebar()
    {
        // Stop any animation that's still running so two never fight each other
        StopAllCoroutines();

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
        SetAnimating(true);

        while (elapsedTime < endTime)
        {
            m_RT.anchoredPosition = Vector3.Lerp(closedPos, openPos, animationCurve.Evaluate(elapsedTime));
            yield return new WaitForEndOfFrame();
            elapsedTime += Time.deltaTime;
        }

        m_RT.anchoredPosition = openPos;
        SetAnimating(false);
    }

    IEnumerator CloseRoutine()
    {
        float elapsedTime = 0f;

        SetAnimating(true);
        
        while (elapsedTime < endTime)
        {
            m_RT.anchoredPosition = Vector3.Lerp(openPos, closedPos, animationCurve.Evaluate(elapsedTime));
            yield return new WaitForEndOfFrame();
            elapsedTime += Time.deltaTime;
        }

        m_RT.anchoredPosition = closedPos;
        isOpen = false;
        SetAnimating(false);
    }

    void SetAnimating(bool animating)
    {
        isAnimating = animating;

        // Disable clicks on the sidebar's buttons and toggles while it moves
        canvasGroup.interactable = !animating;
        canvasGroup.blocksRaycasts = !animating;
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