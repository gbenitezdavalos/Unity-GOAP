using UnityEngine;
using UnityEditor;

public class FullertonStructureCreator
{
    [MenuItem("Tools/Create Fullerton Structure", false, 10)]
    static void CreateFullertonStructure()
    {
        // Check if a root already exists
        GameObject root = GameObject.Find("Game Structure");
        if (root != null)
        {
            Debug.LogWarning("Ya existe una estructura de 'Game Structure' en la escena.");
            return;
        }

        // Create root
        root = new GameObject("Game Structure");

        // ---- Main Hierarchy ----
        CreateChild("_Systems", root);
        CreateChild("_Scenario", root);
        CreateChild("_Characters", root);
        CreateChild("_UI", root);
        CreateChild("_Lighting", root);
        CreateChild("_Cameras", root);
        CreateChild("_Dev", root);

        // Select root when finished
        Selection.activeGameObject = root;

        Debug.Log("✅ Nueva estructura de escena creada con éxito.");
    }

    private static GameObject CreateChild(string name, GameObject parent)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent.transform);
        return go;
    }
}
