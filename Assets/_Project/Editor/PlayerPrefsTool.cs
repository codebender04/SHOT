using UnityEditor;
using UnityEngine;

public static class PlayerPrefsTool
{
    [MenuItem("Tools/Delete PlayerPrefs")]
    private static void DeletePlayerPrefs()
    {
        if (!EditorUtility.DisplayDialog(
            "Delete PlayerPrefs",
            "Are you sure you want to delete all PlayerPrefs?",
            "Delete",
            "Cancel"))
        {
            return;
        }

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Debug.Log("All PlayerPrefs deleted.");
    }
}