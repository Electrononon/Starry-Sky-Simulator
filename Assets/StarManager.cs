using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using TMPro;

public class StarManager : MonoBehaviour
{
    public Mesh starMesh;
    public Material starMaterial;
    public TextAsset csvFile;
    public Transform user;
    public ConstellationManager constellationManager;

    private const int MAX_INSTANCES = 1023;

    public float clampedRadius;

    private float[,] data;
    private string[] starNames;
    private Matrix4x4[] matrices;
    private Vector4[] baseColors;
    private Vector4[] cellColors;
    private float[] absMags;
    public float[] starSizes;
    private Vector3[] pastDirs;

    List<Matrix4x4> visibleMatrices;
    List<Vector4> visibleBaseColors;
    List<Vector4> visibleCellColors;

    private Dictionary<int, Vector3> hipToPosition = new Dictionary<int, Vector3>();
    public Dictionary<int, int> hipToIndex = new Dictionary<int, int>();
    private int starCount;
    private MaterialPropertyBlock propBlock;

    [Range(0.1f, 5f)]
    public float brightnessMultiplier = 2f;

    public TMP_FontAsset labelFont;
    public float apparentMagThreshold = 2.5f;
    public Color textColour;
    private bool wereStarLabelsShown;

    private Dictionary<int, TextMeshPro> activeLabels = new Dictionary<int, TextMeshPro>();
    private Queue<TextMeshPro> labelPool = new Queue<TextMeshPro>();
    private const int initialPoolSize = 50;

    private Dictionary<int, bool> isLabelShown;
    private Dictionary<int, float> labelOpacities;
    public float fadeSpeed = 10f; // Speed in frames
    
    void Start()
    {
        var result = LoadCSVData(csvFile.text);
        data = result.data;
        starNames = result.names;
        starCount = data.GetLength(0);

        matrices = new Matrix4x4[starCount];
        baseColors = new Vector4[starCount];
        cellColors = new Vector4[starCount];
        absMags = new float[starCount];
        propBlock = new MaterialPropertyBlock();
        starSizes = new float[starCount];

        visibleMatrices = new List<Matrix4x4>(starCount);
        visibleBaseColors = new List<Vector4>(starCount);
        visibleCellColors = new List<Vector4>(starCount);

        isLabelShown = new Dictionary<int, bool>();
        labelOpacities = new Dictionary<int, float>();

        for (int i = 0; i < starCount; i++)
        {
            int hipID = (int)data[i, 0];
            Vector3 pos = new Vector3(data[i, 1], data[i, 2], data[i, 3]) * clampedRadius;
            hipToPosition[hipID] = pos;
            hipToIndex[hipID] = i;

            absMags[i] = data[i, 4];

            // Initialize label status and colour for each hip ID
            isLabelShown[hipID] = false;
            labelOpacities[hipID] = 0f;

            Color baseCol = new Color(data[i, 5] / 255f, data[i, 6] / 255f, data[i, 7] / 255f, 1f);

            Color baseColScaled = baseCol * 2f;
            baseColScaled.r = Mathf.Clamp01(baseColScaled.r);
            baseColScaled.g = Mathf.Clamp01(baseColScaled.g);
            baseColScaled.b = Mathf.Clamp01(baseColScaled.b);

            baseColors[i] = new Vector4(baseColScaled.r, baseColScaled.g, baseColScaled.b, 1f);

            Color.RGBToHSV(baseCol, out float H, out float S, out float V);

            if (H < 0.1f && S > 0.32f)
            {
                H -= 0.0032f * Mathf.Pow(1.206f, data[i, 4]);
            }

            S = Mathf.Min(1f, Mathf.Pow(S, 0.6f) * 1.8f);
            V *= 0.9f;
            Color cellCol = Color.HSVToRGB(H, S, V, true);

            cellCol.r = Mathf.Clamp01(cellCol.r) * 8f;
            cellCol.g = Mathf.Clamp01(cellCol.g) * 8f;
            cellCol.b = Mathf.Clamp01(cellCol.b) * 8f;

            cellColors[i] = new Vector4(cellCol.r, cellCol.g, cellCol.b, 1f);
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            TextMeshPro label = CreateLabelObject();
            label.gameObject.SetActive(false);
            labelPool.Enqueue(label);
        }

        pastDirs = new Vector3[starCount];
        wereStarLabelsShown = true;

        UpdateLabels();
    }

    void LateUpdate()
    {
        UpdateVisibleStars();
        RenderStarsInstanced(visibleMatrices, visibleBaseColors, visibleCellColors);

        if (SidebarManager.starLabelsShown)
            UpdateLabels();
        else
            HideAllLabels();
    }

    void UpdateVisibleStars()
    {
        Vector3 userPos = user.position;
        Vector3 userForward = user.forward;
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

        visibleMatrices.Clear();
        visibleBaseColors.Clear();
        visibleCellColors.Clear();

        for (int i = 0; i < starCount; i++)
        {
            Vector3 starPos = new Vector3(data[i, 1], data[i, 2], data[i, 3]) * clampedRadius;
            Vector3 dir = starPos - userPos;
            float dist = Mathf.Max(1e-10f, dir.magnitude);

            Vector3 finalPos = userPos + dir.normalized * clampedRadius;

            float size = clampedRadius / 100f * (0.54f - 0.09f * (absMags[i] + 5f * Mathf.Log10(dist / (10f * 3.26156f * clampedRadius))));
            if (size < 0.06f) continue;

            starSizes[i] = size;

            Bounds starBounds = new Bounds(finalPos, Vector3.one * size);
            if (!GeometryUtility.TestPlanesAABB(frustumPlanes, starBounds)) continue;

            //Matrix4x4 matrix = Matrix4x4.TRS(finalPos, Quaternion.identity, Vector3.one * size);
            Vector3 toCamera = finalPos - Camera.main.transform.position;
            Quaternion rotation = Quaternion.identity;

            if (toCamera != Vector3.zero)
            {
                rotation = Quaternion.LookRotation(toCamera);
            }
            Matrix4x4 matrix = Matrix4x4.TRS(finalPos, rotation, Vector3.one * size);


            Color baseCol = new Color(baseColors[i].x, baseColors[i].y, baseColors[i].z, 1f);
            Color.RGBToHSV(baseCol, out float H, out float S, out float V);
            S = Mathf.Clamp01(1.8f * Mathf.Pow(S, 0.6f));
            V = Mathf.Clamp01(V * brightnessMultiplier);
            Color adjustedBaseCol = Color.HSVToRGB(H, S, V, true);

            visibleMatrices.Add(matrix);
            visibleBaseColors.Add(new Vector4(adjustedBaseCol.r, adjustedBaseCol.g, adjustedBaseCol.b, 1f));
            visibleCellColors.Add(cellColors[i]);
        }
    }

    void UpdateLabels()
    {
        Vector3 userPos = user.position;
        Vector3 userForward = user.forward;
        HashSet<int> starsToLabel = new HashSet<int>();

        for (int i = 0; i < starCount; i++)
        {
            int hipID = (int)data[i, 0];
            Vector3 starPos = new Vector3(data[i, 1], data[i, 2], data[i, 3]) * clampedRadius;

            float distance = Vector3.Distance(starPos, userPos) / clampedRadius / 3.26156f;
            if (distance < 1e-10f) continue;

            float apparentMag = absMags[i] + 5f * (Mathf.Log10(distance) - 1f);
            if (apparentMag < apparentMagThreshold)
                starsToLabel.Add(hipID);
        }

        var keys = new List<int>(activeLabels.Keys);
        foreach (int hipID in keys)
        {
            if (!starsToLabel.Contains(hipID))
                RemoveLabel(hipID);
        }

        foreach (int hipID in starsToLabel)
        {
            if (!activeLabels.ContainsKey(hipID))
                AddLabel(hipID);

            UpdateLabelPosition(hipID);
        }

        ResolveOverlappingLabels();
    }

    void AddLabel(int hipID)
    {
        TextMeshPro label = labelPool.Count > 0 ? labelPool.Dequeue() : CreateLabelObject();
        label.gameObject.SetActive(true);
        int idx = hipToIndex[hipID];
        label.text = starNames[idx];
        activeLabels[hipID] = label;
    }

    void RemoveLabel(int hipID)
    {
        if (activeLabels.TryGetValue(hipID, out TextMeshPro label))
        {
            label.gameObject.SetActive(false);
            labelPool.Enqueue(label);
            activeLabels.Remove(hipID);
        }
    }

    void UpdateLabelPosition(int hipID)
    {
        if (!hipToPosition.TryGetValue(hipID, out Vector3 starPos)) return;

        Vector3 dir = starPos - user.position;
        float starSize = starSizes[hipToIndex[hipID]];
        Vector3 labelPos = user.position + dir.normalized * clampedRadius + user.up * (starSize + 1f * clampedRadius / 100f);
        bool inFront = Vector3.Dot(user.forward, dir.normalized) > 0f;

        TextMeshPro label = activeLabels[hipID];
        label.transform.position = labelPos;
        label.transform.rotation = Quaternion.LookRotation(user.forward, user.up);
        label.gameObject.SetActive(inFront);
    }

void UpdateLabelsColors(int hipID) {
    TextMeshPro label = activeLabels[hipID];
    float opacity = labelOpacities[hipID];
    bool show = isLabelShown[hipID];

    if (show && opacity < 1f) {
        opacity = Mathf.Clamp01(opacity + Time.deltaTime / fadeSpeed);
    }
    else if (!show && opacity > 0f) {
        opacity = Mathf.Clamp01(opacity - Time.deltaTime / fadeSpeed);
    }

    labelOpacities[hipID] = opacity;
    label.color = new Color(label.color.r, label.color.g, label.color.b, opacity);

    if (opacity == 0f && !show) {
        label.gameObject.SetActive(false);
        labelPool.Enqueue(label);
        activeLabels.Remove(hipID);
        labelOpacities.Remove(hipID);
        isLabelShown.Remove(hipID);
    }
}

    TextMeshPro CreateLabelObject()
    {
        GameObject labelObj = new GameObject("StarLabel");
        TextMeshPro text = labelObj.AddComponent<TextMeshPro>();

        text.font = labelFont;
        text.fontSize = 20 * clampedRadius / 100f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = textColour;

        text.enableWordWrapping = false;
        text.GetComponent<Renderer>().material = labelFont.material;

        return text;
    }

    void ResolveOverlappingLabels()
    {
        Camera cam = Camera.main;
        Dictionary<int, Rect> labelRects = new Dictionary<int, Rect>();
        HashSet<int> labelsToRemove = new HashSet<int>();

        foreach (var kvp in activeLabels)
        {
            int curID = kvp.Key;
            TextMeshPro curLabel = kvp.Value;

            Vector3 screenPos = cam.WorldToScreenPoint(curLabel.transform.position);
            
            if (screenPos.z < 0) continue; // behind camera

            Vector2 bounds = curLabel.GetPreferredValues(curLabel.text);

            // Rescale bounds to fit the labels
            bounds = new Vector2(22 * bounds.x * 100f/clampedRadius, 20 * bounds.y * 100f/clampedRadius);

            // Center rect on screenPos:
            Rect rect = new Rect(screenPos.x - bounds.x/2, screenPos.y - bounds.y/2, bounds.x, bounds.y); // Scale up the preferred values

            labelRects[curID] = rect;            
        }

        List<int> labelIDs = new List<int>(labelRects.Keys);

        for (int i = 0; i < labelIDs.Count - 1; i++)
        {
            for (int j = i + 1; j < labelIDs.Count; j++)
            {
                if (labelRects[labelIDs[i]].Overlaps(labelRects[labelIDs[j]]))
                {
                    if (starSizes[hipToIndex[labelIDs[i]]] >= starSizes[hipToIndex[labelIDs[j]]])
                        labelsToRemove.Add(labelIDs[j]);
                    else
                        labelsToRemove.Add(labelIDs[i]);
                }
            }
        }

        // Hide any star labels overlapping with constellation labels
        for (int i = 0; i <  constellationManager.constellationNames.Count; i++)
        {
            string constellationName = constellationManager.constellationNames[i];

            if (!constellationManager.constellationLabelRects.TryGetValue(constellationName, out Rect constRect)) continue;

            for (int j = 0; j < labelIDs.Count; j++) {
                if (constellationManager.constellationLabelRects[constellationName].Overlaps(labelRects[labelIDs[j]]))
                {
                    labelsToRemove.Add(labelIDs[j]);
                }
            }
        }

        foreach (int labelID in labelsToRemove)
        {
            RemoveLabel(labelID);
        }
    }

    void HideAllLabels()
    {
        foreach (var labelEntry in activeLabels)
        {
            labelEntry.Value.gameObject.SetActive(false);
        }
    }

    void RenderStarsInstanced(List<Matrix4x4> matricesToRender, List<Vector4> baseColorsToRender, List<Vector4> cellColorsToRender)
    {
        int count = matricesToRender.Count;
        int batchSize = MAX_INSTANCES;

        for (int i = 0; i < count; i += batchSize)
        {
            int batchCount = Mathf.Min(batchSize, count - i);
            Matrix4x4[] batchMatrices = new Matrix4x4[batchCount];
            Vector4[] batchBaseColors = new Vector4[batchCount];
            Vector4[] batchCellColors = new Vector4[batchCount];

            matricesToRender.CopyTo(i, batchMatrices, 0, batchCount);
            baseColorsToRender.CopyTo(i, batchBaseColors, 0, batchCount);
            cellColorsToRender.CopyTo(i, batchCellColors, 0, batchCount);

            propBlock.Clear();
            propBlock.SetVectorArray("_BaseColour", batchBaseColors);
            propBlock.SetVectorArray("_CellColour", batchCellColors);

            Graphics.DrawMeshInstanced(starMesh, 0, starMaterial, batchMatrices, batchCount, propBlock);
        }
    }

    (float[,] data, string[] names) LoadCSVData(string csvText)
    {
        string[] lines = csvText.Split('\n');
        List<string> validLines = new List<string>();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (!string.IsNullOrEmpty(line))
            {
                string[] values = line.Split(',');
                if (values.Length >= 9)
                    validLines.Add(line);
            }
        }

        int rowCount = validLines.Count;
        float[,] result = new float[rowCount, 8];
        string[] starNames = new string[rowCount];

        for (int i = 0; i < rowCount; i++)
        {
            string[] values = validLines[i].Split(',');

            for (int j = 0; j < 8; j++)
            {
                if (float.TryParse(values[j], NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
                    result[i, j] = parsed;
                else
                    result[i, j] = 0f;
            }

            starNames[i] = values[8].Trim();
        }

        return (result, starNames);
    }

    public Vector3 GetClampedStarPosition(int hipID, Vector3 userPos)
    {
        if (!hipToPosition.TryGetValue(hipID, out Vector3 starPos)) return Vector3.zero;
        Vector3 dir = starPos - userPos;
        return userPos + dir.normalized * clampedRadius;
    }

    public bool TryGetStarPosition(int hipID, out Vector3 pos) => hipToPosition.TryGetValue(hipID, out pos);
}
