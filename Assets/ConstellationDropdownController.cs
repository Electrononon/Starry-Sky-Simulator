using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ConstellationDropdownController : MonoBehaviour
{
    public ConstellationManager constellationManager;
    public Toggle dropdownHeaderToggle;
    public GameObject constellationListPanel;
    public Transform contentParent;
    public GameObject togglePrefab;

    private List<Toggle> constellationToggles = new List<Toggle>();

    public static bool[] visibleConstellationLines = new bool[89]; // Each entry represents whether or not that constellation should be shown
    public static bool constellationLinesShown;  // Master switch for all constellation lines

    IEnumerator Start()
    {
        constellationLinesShown = dropdownHeaderToggle.isOn;
        while (constellationManager.constellationNames == null || constellationManager.constellationNames.Count == 0)
        {
            yield return null; // wait for next frame
        }

        dropdownHeaderToggle.onValueChanged.AddListener(OnDropdownToggleChanged);
        PopulateConstellationToggles();

        // Initalize boolean values
        for (int i = 0; i < 89; i++)
        {
            visibleConstellationLines[i] = dropdownHeaderToggle.isOn;
        }
    }


    void OnDropdownToggleChanged(bool isOn)
    {
        constellationListPanel.SetActive(isOn);
        constellationLinesShown = isOn;
    }
    
    void PopulateConstellationToggles()
    {
        constellationToggles.Clear();

        List<string> constellationNames = new List<string>(constellationManager.constellationNames);
        constellationNames.Insert(0, "All Constellations");

        for (int i = 0; i < constellationNames.Count; i++)
        {
            string name = constellationNames[i];

            // Deal with Serpens exception
            if (name == "Serpens Caput") {
                name = "Serpens";
            }
            else if (name == "Serpens Cauda") {
                continue;
            }

            GameObject toggleGO = Instantiate(togglePrefab, contentParent);
            toggleGO.SetActive(true);
            Toggle toggle = toggleGO.GetComponent<Toggle>();
            Text label = toggleGO.GetComponentInChildren<Text>();
            label.text = name;

            int index = i;

            toggle.onValueChanged.AddListener(isOn =>
            {
                OnConstellationLinesToggleChanged(index, isOn);
            });

            toggle.isOn = dropdownHeaderToggle.isOn;
            constellationToggles.Add(toggle);
        }

    }

    public void OnConstellationLinesToggleChanged(int index, bool isOn) 
    {
        if (index == 0) // "All Constellations" toggle
        {
            // Update the bool array
            for (int i = 0; i < visibleConstellationLines.Length; i++) 
            {
                visibleConstellationLines[i] = isOn;
            }

            // Update each toggle's visual state except index 0
            for (int i = 1; i < constellationToggles.Count; i++) 
            {
                // Remove listener to avoid recursive call
                constellationToggles[i].onValueChanged.RemoveAllListeners();

                // Set toggle state
                constellationToggles[i].isOn = isOn;

                // Re-add listener
                int closureIndex = i;
                constellationToggles[i].onValueChanged.AddListener(toggledOn => OnConstellationLinesToggleChanged(closureIndex, toggledOn));
            }

            return;
        }

        // Update bool array for individual toggles
        visibleConstellationLines[index - 1] = isOn; // Use index - 1 because index = 0 is for the "All Constellations" option

        // If the toggle changed was Serpens (index #76 in dropdown), then also change the other part of the constellation (index #76 in visibleConstellationLines)
        if (index == 76) 
        {
            visibleConstellationLines[76] = isOn;
        }

        // Update "All Constellations" toggle state based on all individual toggles
        bool allOn = true;
        for (int i = 0; i < visibleConstellationLines.Length; i++) 
        {
            if (!visibleConstellationLines[i]) 
            {
                allOn = false;
                break;
            }
        }
        // Remove listener to avoid recursion
        constellationToggles[0].onValueChanged.RemoveAllListeners();
        constellationToggles[0].isOn = allOn;
        constellationToggles[0].onValueChanged.AddListener(isOn => OnConstellationLinesToggleChanged(0, isOn));
    }

}
