using UnityEditor;
using UnityEngine;
using System.IO;

public class CreateProjectFolders
{
    [MenuItem("Tools/Create Default Project Structure")]
    public static void CreateFolders()
    {
        string assetsPath = Application.dataPath;

        // Root folders
        CreateFolder(assetsPath, "000 Sandbox");
        CreateFolder(assetsPath, "001 _Proyecto");
        CreateFolder(assetsPath, "002 Plugin");
        CreateFolder(assetsPath, "003 Resources");

        // Subfolders inside 001 _Proyecto
        string proyectoPath = Path.Combine(assetsPath, "001 _Proyecto");
        CreateFolder(proyectoPath, "3D");
        CreateFolder(proyectoPath, "Prefabs");
        CreateFolder(proyectoPath, "Scripts");
        CreateFolder(proyectoPath, "SFX");
        CreateFolder(proyectoPath, "VFX");
        CreateFolder(proyectoPath, "Fonts");
        CreateFolder(proyectoPath, "UI");
        CreateFolder(proyectoPath, "Scenes");

        // Scenes subfolders
        string scenesPath = Path.Combine(proyectoPath, "Scenes");
        CreateFolder(scenesPath, "1 Experimental");
        CreateFolder(scenesPath, "2 WIP");
        CreateFolder(scenesPath, "3 Completed");
	CreateFolder(scenesPath, "4 Archived");

        // Scripts subfolders
        string scriptsPath = Path.Combine(proyectoPath, "Scripts");

	CreateFolder(scriptsPath, "Runtime");
	CreateFolder(scriptsPath, "Editor");
	CreateFolder(scriptsPath, "ScriptableObjects");

	string runtimePath = Path.Combine(scriptsPath, "Runtime");
        CreateFolder(runtimePath, "Controllers");
        CreateFolder(runtimePath, "Handlers");
        CreateFolder(runtimePath, "Interfaces");
        CreateFolder(runtimePath, "Managers");
        CreateFolder(runtimePath, "Spawners");
        CreateFolder(runtimePath, "Factories");
        CreateFolder(runtimePath, "AI_Controllers");
        CreateFolder(runtimePath, "Utilities");
        CreateFolder(runtimePath, "Services");

        AssetDatabase.Refresh();
        Debug.Log("✅ Project folder structure created successfully!");
    }

    private static void CreateFolder(string parentPath, string folderName)
    {
        string fullPath = Path.Combine(parentPath, folderName);

        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
            Debug.Log("📁 Created folder: " + fullPath);
        }
        else
        {
            Debug.Log("⚠️ Already exists: " + fullPath);
        }
    }
}
