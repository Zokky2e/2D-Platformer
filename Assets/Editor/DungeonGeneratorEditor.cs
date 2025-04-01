using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DungeonGenerator))]
public class DungeonGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DungeonGenerator generator = (DungeonGenerator)target;
        if (GUILayout.Button("Expand Dungeon"))
        {
            generator.ExpandDungeon();
        }
        if (GUILayout.Button("Expand Dungeon To Max"))
        {
            generator.ExpandToMaxDungeon();
        }
    }
}