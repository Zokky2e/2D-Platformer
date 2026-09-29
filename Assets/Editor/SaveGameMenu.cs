using System.IO;
using UnityEditor;
using UnityEngine;

// Editor helpers for the save file; the game continues an existing save automatically
public static class SaveGameMenu
{
    [MenuItem("Tools/Save Game/Delete Save File")]
    private static void DeleteSaveFile()
    {
        string path = SaveSystem.SavePath;
        if (!File.Exists(path))
        {
            Debug.Log($"No save file at {path}");
            return;
        }
        if (!EditorUtility.DisplayDialog("Delete save file?", path, "Delete", "Cancel"))
            return;
        File.Delete(path);
        Debug.Log($"Deleted {path}" + (Application.isPlaying ? " (the running game saves again at its next save point)" : ""));
    }

    [MenuItem("Tools/Save Game/Open Save Folder")]
    private static void OpenSaveFolder()
    {
        EditorUtility.RevealInFinder(File.Exists(SaveSystem.SavePath) ? SaveSystem.SavePath : Application.persistentDataPath);
    }

    [MenuItem("Tools/Save Game/Start New Game (Play mode)")]
    private static void StartNewGame()
    {
        SaveSystem.Instance.StartNewGame();
    }

    [MenuItem("Tools/Save Game/Start New Game (Play mode)", true)]
    private static bool CanStartNewGame() => Application.isPlaying;
}
