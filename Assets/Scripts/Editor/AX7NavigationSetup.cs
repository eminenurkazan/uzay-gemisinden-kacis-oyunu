using System;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Yalnızca Unity editöründe çalışır; oyunda otomatik görev başlatmaz.
public static class AX7NavigationSetup
{
    private const string MenuPath = "Uzay Oyunu/AX-7 NavMesh Hazırla";
    private const string DataPath = "Assets/Navigation/AX7NavMesh.asset";

    [MenuItem(MenuPath)]
    private static void Prepare()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode
            || scene.path != "Assets/Scenes/EscapeScene.unity")
        {
            Debug.LogWarning("Play'i durdurup EscapeScene sahnesini açmalısın.");
            return;
        }

        GameObject robot = FindRoot("AX-7");
        GameObject player = FindRoot("Astronot");
        if (robot == null || player == null || FindRoot("KontrolPaneli") == null)
        {
            Debug.LogError("AX-7, Astronot veya KontrolPaneli bulunamadı.");
            return;
        }

        int agentType = ConfigureAgentType();
        // Hareketli karakter gövdeleri zemindeki sabit engeller olarak pişirilmez.
        IgnoreCharacter(robot);
        IgnoreCharacter(player);
        ConfigureDoor(FindRoot("FizikselKapi"));
        ConfigureDoor(FindRoot("BiyometrikKapi"));

        GameObject surfaceObject = FindRoot("AX7Navigation");
        if (surfaceObject == null)
        {
            surfaceObject = new GameObject("AX7Navigation");
            Undo.RegisterCreatedObjectUndo(surfaceObject, "AX-7 gezinme yüzeyini oluştur");
        }
        var surface = surfaceObject.GetComponent<NavMeshSurface>();
        if (surface == null)
            surface = Undo.AddComponent<NavMeshSurface>(surfaceObject);
        Undo.RecordObject(surface, "AX-7 NavMesh ayarlarını düzenle");
        surface.agentTypeID = agentType;
        surface.collectObjects = CollectObjects.Volume;
        surface.center = new Vector3(1.5f, 1f, 0f);
        surface.size = new Vector3(10f, 4f, 30f);
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~0;
        surface.overrideVoxelSize = true;
        surface.voxelSize = 0.05f;
        surface.minRegionArea = 0.05f;

        if (!AssetDatabase.IsValidFolder("Assets/Navigation"))
            AssetDatabase.CreateFolder("Assets", "Navigation");
        NavMeshData existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(DataPath);
        if (existing == null && AssetDatabase.LoadMainAssetAtPath(DataPath) != null)
        {
            Debug.LogError("NavMesh kayıt yolunda farklı türde bir dosya var.");
            return;
        }

        surface.BuildNavMesh();
        NavMeshData generated = surface.navMeshData;
        if (generated == null)
        {
            Debug.LogError("NavMesh oluşturulamadı; Console mesajlarını kontrol et.");
            return;
        }
        if (existing == null)
            AssetDatabase.CreateAsset(generated, DataPath);
        else
        {
            surface.RemoveData();
            EditorUtility.CopySerialized(generated, existing);
            EditorUtility.SetDirty(existing);
            surface.navMeshData = existing;
            surface.AddData();
            UnityEngine.Object.DestroyImmediate(generated);
        }

        EditorUtility.SetDirty(surface);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        bool routeValid = ValidateRoute(agentType);
        bool hasKaelArea = FindRoot("Kael") != null;
        bool kaelRouteValid = !hasKaelArea || ValidateKaelArea(agentType);
        Selection.activeGameObject = surfaceObject;
        SceneView.RepaintAll();
        if (!EditorSceneManager.SaveScene(scene))
        {
            Debug.LogError("NavMesh hesaplandı ancak sahne kaydedilemedi.");
            return;
        }
        if (routeValid && kaelRouteValid)
        {
            string message = "AX-7 NavMesh hazır: bakım yolu üzerinden panele tam rota bulundu.";
            if (hasKaelArea)
                message += " Panelden Kael alanına kesintisiz rota da doğrulandı.";
            Debug.Log(message, surface);
        }
    }

    private static GameObject FindRoot(string name)
    {
        foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            if (root.name == name)
                return root;
        return null;
    }

    private static void IgnoreCharacter(GameObject character)
    {
        var modifier = character.GetComponent<NavMeshModifier>();
        if (modifier == null)
            modifier = Undo.AddComponent<NavMeshModifier>(character);
        Undo.RecordObject(modifier, "Karakteri NavMesh hesabından çıkar");
        modifier.ignoreFromBuild = true;
        modifier.applyToChildren = true;
        EditorUtility.SetDirty(modifier);
    }

    private static void ConfigureDoor(GameObject door)
    {
        if (door == null)
            return;
        var box = door.GetComponent<BoxCollider>();
        if (box == null)
            throw new InvalidOperationException("Kapının BoxCollider bileşeni eksik: " + door.name);

        // Kapalı kapı sabit geometriye pişirilmez; geçişi çalışma zamanı engeli keser.
        var modifier = door.GetComponent<NavMeshModifier>();
        if (modifier == null)
            modifier = Undo.AddComponent<NavMeshModifier>(door);
        Undo.RecordObject(modifier, "Hareketli kapıyı sabit NavMesh geometrisinden çıkar");
        modifier.ignoreFromBuild = true;
        modifier.applyToChildren = true;
        EditorUtility.SetDirty(modifier);

        var obstacle = door.GetComponent<NavMeshObstacle>();
        if (obstacle == null)
            obstacle = Undo.AddComponent<NavMeshObstacle>(door);
        Undo.RecordObject(obstacle, "Kapının dinamik NavMesh engelini hazırla");
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = box.center;
        obstacle.size = box.size;
        obstacle.carving = true;
        obstacle.carveOnlyStationary = false;
        obstacle.carvingMoveThreshold = 0.01f;
        obstacle.enabled = true;
        EditorUtility.SetDirty(obstacle);
    }

    private static int ConfigureAgentType()
    {
        int id = -1;
        for (int i = 0; i < NavMesh.GetSettingsCount(); i++)
        {
            var settings = NavMesh.GetSettingsByIndex(i);
            if (NavMesh.GetSettingsNameFromID(settings.agentTypeID) == "AX-7")
                id = settings.agentTypeID;
        }
        if (id == -1)
            id = NavMesh.CreateSettings().agentTypeID;

        // Kurulu AI Navigation paketinin NavigationWindow ile aynı kayıt arayüzü.
        var settingsObject = new SerializedObject(
            Unsupported.GetSerializedAssetInterfaceSingleton("NavMeshProjectSettings"));
        settingsObject.Update();
        var entries = settingsObject.FindProperty("m_Settings");
        var names = settingsObject.FindProperty("m_SettingNames");
        for (int i = 0; i < entries.arraySize; i++)
        {
            var entry = entries.GetArrayElementAtIndex(i);
            if (entry.FindPropertyRelative("agentTypeID").intValue != id)
                continue;
            entry.FindPropertyRelative("agentRadius").floatValue = 0.35f;
            entry.FindPropertyRelative("agentHeight").floatValue = 0.8f;
            entry.FindPropertyRelative("agentClimb").floatValue = 0.1f;
            entry.FindPropertyRelative("agentSlope").floatValue = 45f;
            names.GetArrayElementAtIndex(i).stringValue = "AX-7";
            settingsObject.ApplyModifiedProperties();
            return id;
        }
        throw new InvalidOperationException("AX-7 ajan ayarları kaydedilemedi.");
    }

    private static bool ValidateKaelArea(int agentType)
    {
        var filter = new NavMeshQueryFilter { agentTypeID = agentType, areaMask = NavMesh.AllAreas };
        Vector3[] points =
        {
            new Vector3(1.8f, 0f, -1.9f),
            new Vector3(0f, 0f, 3f),
            new Vector3(0f, 0f, 6f)
        };
        var path = new NavMeshPath();
        for (int i = 0; i < points.Length; i++)
        {
            if (!NavMesh.SamplePosition(points[i], out var hit, 0.4f, filter))
            {
                Debug.LogError("Kael alanında yürünebilir zemin bulunamadı: " + points[i]);
                return false;
            }
            points[i] = hit.position;
            if (i > 0 && (!NavMesh.CalculatePath(points[i - 1], points[i], filter, path)
                          || path.status != NavMeshPathStatus.PathComplete))
            {
                Debug.LogError("Panel ile Kael alanı arasında kesintisiz rota oluşmadı.");
                return false;
            }
        }
        return true;
    }

    private static bool ValidateRoute(int agentType)
    {
        var filter = new NavMeshQueryFilter { agentTypeID = agentType, areaMask = NavMesh.AllAreas };
        Vector3[] checkpoints =
        {
            new Vector3(1.5f, 0f, -12f),
            new Vector3(3f, 0f, -10f),
            new Vector3(4.5f, 0f, -6f),
            new Vector3(3f, 0f, -2f),
            new Vector3(1.8f, 0f, -1.9f)
        };
        var path = new NavMeshPath();
        for (int i = 0; i < checkpoints.Length; i++)
        {
            if (!NavMesh.SamplePosition(checkpoints[i], out var hit, 0.4f, filter))
            {
                Debug.LogError("Bakım yolunun bir bölümünde yürünebilir alan bulunamadı: " + checkpoints[i]);
                return false;
            }
            checkpoints[i] = hit.position;
            if (i > 0 && (!NavMesh.CalculatePath(checkpoints[i - 1], checkpoints[i], filter, path)
                          || path.status != NavMeshPathStatus.PathComplete))
            {
                Debug.LogError("Bakım yolunun giriş ve çıkışı arasında kesintisiz rota oluşmadı.");
                return false;
            }
        }
        if (!NavMesh.CalculatePath(checkpoints[0], checkpoints[checkpoints.Length - 1], filter, path)
            || path.status != NavMeshPathStatus.PathComplete)
        {
            Debug.LogError("Robotun başlangıcından panele tam rota bulunamadı.");
            return false;
        }
        return true;
    }
}
