using UnityEngine;
using UnityEngine.UI;

public class BoundaryWarning : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public RawImage edgeGlow;
    public float fadeSpeed = 4f;
    public float holdTime = 0.25f;

    public Color glowColour = new Color(0.82f, 0.16f, 0.16f, 1f);
    public float glowStrength = 0.2f;  // How strong the glow is at the very edge (0 to 1)
    public float glowStart = 0.8f;     // Where the glow begins (0 = centre of screen, 1 = edge)

    private float lastAtEdgeTime = -999f;

    void Start()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        CreateEdgeGlow();
    }

    void Update()
    {
        if (UserMoveAroundScript.isAtBoundary)
        {
            lastAtEdgeTime = Time.unscaledTime;
        }

        // Stay visible for a moment after leaving the edge, and hide while the sidebar is open
        bool show = Time.unscaledTime - lastAtEdgeTime < holdTime && !SidebarManager.isOpen;

        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, show ? 1f : 0f, fadeSpeed * Time.unscaledDeltaTime);
    }

    // Generate a red glow around the edges of the screen
    void CreateEdgeGlow()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                // Position from -1 to 1, with 0 at the centre
                float u = (x + 0.5f) / size * 2f - 1f;
                float v = (y + 0.5f) / size * 2f - 1f;

                // Rounded-rectangle distance from the centre, so the glow follows the screen edges
                float d = Mathf.Pow(Mathf.Pow(Mathf.Abs(u), 10f) + Mathf.Pow(Mathf.Abs(v), 10f), 0.25f);

                // float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(glowStart, 1f, d)) * glowStrength;
                float t = Mathf.InverseLerp(glowStart, 1f, d);
                float alpha = t * t * t * glowStrength;
                pixels[y * size + x] = new Color(glowColour.r, glowColour.g, glowColour.b, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        edgeGlow.texture = tex;
        edgeGlow.color = Color.white;
        edgeGlow.raycastTarget = false;
    }
}