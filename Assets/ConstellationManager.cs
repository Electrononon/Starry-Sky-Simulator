using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class ConstellationManager : MonoBehaviour
{
    public StarManager starManager;
    public TextAsset constellationJson;
    public Material lineMaterial;
    public TMP_FontAsset labelFont;
    public Color labelColour;
    public float lineWidth;

    private List<(LineRenderer line, int starID1, int starID2)> constellationLines = new();
    private Dictionary<string, List<int>> constellationStars = new();
    private Dictionary<string, TextMeshPro> constellationLabels = new();
    public List<string> constellationNames = new();
    private Dictionary<string, float> avgConstellationStarSize = new();
    public Dictionary<string, Rect> constellationLabelRects = new();

    private Dictionary<(int, int), int> lineEndpointsToConstellationIndex = new();


    void Start()
    {
        JArray constellations = JArray.Parse(constellationJson.text);

        //foreach (JObject constellation in constellations)
        for (int c = 0; c < constellations.Count; c++)
        {
            JObject constellation = (JObject) constellations[c];

            string name = (string)constellation["name"];

            JArray connections = (JArray)constellation["connections"];

            constellationNames.Add(name);

            HashSet<int> stars = new();

            Vector3 userPos = starManager.user.position;

            foreach (JArray path in connections)
            {
                for (int i = 0; i < path.Count - 1; i++)
                {
                    int starID1 = (int)path[i];
                    int starID2 = (int)path[i + 1];

                    stars.Add(starID1);
                    stars.Add(starID2);

                    if (starManager.TryGetStarPosition(starID1, out Vector3 pos1) && starManager.TryGetStarPosition(starID2, out Vector3 pos2))
                    {
                        GameObject lineObj = new GameObject("ConstellationLine");
                        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
                        lr.widthMultiplier = lineWidth;
                        lr.material = lineMaterial;
                        lr.useWorldSpace = true;

                        /*lr.positionCount = 2;

                        lr.SetPosition(0, pos1);
                        lr.SetPosition(1, pos2);*/

                        // Make the line segments curved along celestial sphere to preserve apparent thickness
                        int numOfSegments = 16;
                        lr.positionCount = numOfSegments + 1;

                        for (int j = 0; j <= numOfSegments; j++)
                        {
                            float t = j / (float) numOfSegments;
                            Vector3 point = userPos + 100f * Vector3.Slerp((pos1 - userPos).normalized, (pos2 - userPos).normalized, t);
                            lr.SetPosition(j, point);
                        }


                        constellationLines.Add((lr, starID1, starID2));

                        lineEndpointsToConstellationIndex[(starID1, starID2)] = c;
                    }
                }
            }

            constellationStars[name] = stars.ToList();

            // Create label GameObject
            GameObject labelObj = new GameObject("Label_" + name);
            TextMeshPro text = labelObj.AddComponent<TextMeshPro>();
            text.text = name.ToUpper();
            text.fontSize = 25;
            text.alignment = TextAlignmentOptions.Center;
            text.font = labelFont;
            text.color = labelColour;
            text.GetComponent<MeshRenderer>().material = labelFont.material;
            text.enableWordWrapping = false;

            constellationLabels[name] = text;
        }
    }

    void LateUpdate()
    {
        // Update average star brightness for each constellation
        foreach (string name in constellationNames)
        {
            float avgSize = 0f;
            foreach (int starID in constellationStars[name]) {
                avgSize += Mathf.Max(starManager.starSizes[starManager.hipToIndex[starID]], 0f);
            }

            avgSize /= constellationStars[name].Count;

            avgConstellationStarSize[name] = avgSize;
        }
        
        // Update constellation lines and labels
        UpdateLines();
        UpdateLabels();
    }

    void UpdateLines()
    {
        Vector3 userPos = starManager.user.position;
        Quaternion userRot = starManager.user.rotation;

        foreach (var (lr, starID1, starID2) in constellationLines)
        {
            int constellationIndex = lineEndpointsToConstellationIndex[(starID1, starID2)];
            bool isVisible = ConstellationDropdownController.constellationLinesShown && ConstellationDropdownController.visibleConstellationLines[constellationIndex];

            lr.enabled = isVisible;  // Enable or disable the LineRenderer component

            if (!isVisible)
                continue;  // Skip position update when not visible

            Vector3 pos1 = starManager.GetClampedStarPosition(starID1, userPos);
            Vector3 pos2 = starManager.GetClampedStarPosition(starID2, userPos);

            // Make the line segments curved along celestial sphere to preserve apparent thickness
            int numOfSegments = 16;
            lr.positionCount = numOfSegments + 1;

            for (int i = 0; i <= numOfSegments; i++)
            {
                float t = i / (float) numOfSegments;
                Vector3 point = userPos + 100f * Vector3.Slerp((pos1 - userPos).normalized, (pos2 - userPos).normalized, t);
                lr.SetPosition(i, point);
            }

        }
    }

    void UpdateLabels()
    {
        Vector3 userPos = starManager.user.position;
        Quaternion userRot = starManager.user.rotation;

        foreach (var pair in constellationLabels)
        {
            string name = pair.Key;
            TextMeshPro label = pair.Value;

            // Find the constellation index for this name
            int constellationIndex = constellationNames.IndexOf(name);

            // Check if this constellation's lines are visible
            bool isVisible = false;
            if (constellationIndex >= 0 && constellationIndex < ConstellationDropdownController.visibleConstellationLines.Length)
            {
                isVisible = ConstellationDropdownController.constellationLinesShown && ConstellationDropdownController.visibleConstellationLines[constellationIndex];
            }

            // Enable or disable label based on global setting AND constellation visibility
            label.gameObject.SetActive(SidebarManager.constellationLabelsShown && isVisible);

            if (!SidebarManager.constellationLabelsShown || !isVisible)
                continue;

            // Position the label as before
            List<int> starList = constellationStars[name];
            Vector3 center = Vector3.zero;
            int count = 0;

            foreach (int starID in starList)
            {
                Vector3 pos = starManager.GetClampedStarPosition(starID, userPos);
                center += pos;
                count++;
            }

            if (count > 0)
                center /= (count);

            Vector3 dir = center - userPos;
            center = userPos + dir.normalized * 100f;

            label.transform.position = center;
            label.transform.rotation = Quaternion.LookRotation(starManager.user.forward, starManager.user.up);
        }

        // Deal with overlapping constellation labels
        ResolveOverlappingLabels();
    }
        
    void ResolveOverlappingLabels()
    {
        Camera cam = Camera.main;
        constellationLabelRects.Clear();
        List<string> visibleLabels = new();

        foreach (var kvp in constellationLabels)
        {
            string name = kvp.Key;
            TextMeshPro label = kvp.Value;

            if (!label.gameObject.activeInHierarchy) continue;

            Vector3 screenPos = cam.WorldToScreenPoint(label.transform.position);
            if (screenPos.z < 0) continue; // behind camera

            // Adjust bounds if constellation is composed of two words
            Vector2 bounds;
            if (label.text.Contains(" ") && false)
            {
                string longestWord = "";
                foreach (string word in label.text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (label.GetPreferredValues(word).x > label.GetPreferredValues(longestWord).x)
                    {
                        longestWord = word;
                    }
                }
                bounds = label.GetPreferredValues(longestWord);
                bounds.y *= 2f;
            }
            else {
                bounds = label.GetPreferredValues(label.text);
            }

            // Scale the rect for visibility
            Rect rect = new Rect(
                screenPos.x - 10f * bounds.x,
                screenPos.y - 9f * bounds.y,
                20f * bounds.x,
                18f * bounds.y
            );

            constellationLabelRects[name] = rect;
            visibleLabels.Add(name);
        }

        HashSet<string> labelsToHide = new();

        for (int i = 0; i < visibleLabels.Count - 1; i++)
        {
            for (int j = i + 1; j < visibleLabels.Count; j++)
            {
                string name1 = visibleLabels[i];
                string name2 = visibleLabels[j];

                if (constellationLabelRects[name1].Overlaps(constellationLabelRects[name2]))
                {
                    // Pick one to hide. Here we just keep the first alphabetically
                    if (avgConstellationStarSize[name1] >= avgConstellationStarSize[name2])
                    {
                        labelsToHide.Add(name2);
                    }
                    else
                    {
                        labelsToHide.Add(name1);
                    }
                }
            }
        }

        // Hide the overlapping labels
        foreach (string name in labelsToHide)
        {
            if (constellationLabels.TryGetValue(name, out var label))
            {
                label.gameObject.SetActive(false);
            }
        }
    }
}
