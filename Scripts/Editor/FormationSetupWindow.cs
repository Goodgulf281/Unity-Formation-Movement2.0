using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Goodgulf.Formation;

#if GDG_A
    using Pathfinding;
#endif

/*
 * This is a custom editor windows which will do the basic setup for your Formation.
 * The Formation and key components will be setup at the middle of the screen.
 * 
 * It creates a menu item: Window/Goodgulf/Formation Setup
 *  
 */

public class FormationSetupWindow : EditorWindow
{

    [MenuItem("Window/Goodgulf/Formation Setup")]
    public static void CustomEditorWindow()
    {
        GetWindow<FormationSetupWindow>("Formation Setup");
    }


    private void OnGUI()
    {
        GUILayout.Label("This is the Formation Movement 2.0 Setup", EditorStyles.largeLabel);
        GUILayout.Label("Select one of below buttons to set the #define for the pathfinding method of you choice.");

        if (GUILayout.Button("Set Navigation to Unity Navmesh"))
        {
            SetNavigationUnity();
        }

        if (GUILayout.Button("Set Navigation to A*Pathfinding"))
        {
            SetNavigationAStar();
        }

        GUILayout.Label("Create a new formation (in the center of the screen) by clicking on this button:");

        if (GUILayout.Button("Create a new formation"))
        {
            CreateNewFormation();
        }

    }

    private void SetNavigationUnity()
    {
        string currBuildSettings = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

        if (currBuildSettings.Contains("GDG_A"))
        {
            string newBuildSettings = currBuildSettings.Replace(";GDG_A", "");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, newBuildSettings);
        }
    }

    private void SetNavigationAStar()
    {
        string currBuildSettings = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

        if (!currBuildSettings.Contains("GDG_A"))
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, currBuildSettings + ";GDG_A");
        }
    }

    private void CreateNewFormation()
    {
        // Use a Ray / Raycast from the middle of the Scene View to place the formation and its anchor on the plane/terrain/object it hits
        //
        // http://answers.unity3d.com/questions/48979/how-can-i-find-the-world-position-of-the-center-of.html
        Ray worldRay = SceneView.lastActiveSceneView.camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 1.0f));

        // http://josbalcaen.com/unity-editor-place-objects-by-raycast/
        Vector3 newPosition;
        RaycastHit hitInfo;
        // Shoot this ray. check in a distance of 10000.
        if (Physics.Raycast(worldRay, out hitInfo, 10000))
        {
            newPosition = hitInfo.point;
        }
        else newPosition = new Vector3(0, 0, 0);


        // We'll use the time to name the Formation
        float t = Time.time;
        // Time to string see: http://answers.unity3d.com/questions/45676/making-a-timer-0000-minutes-and-seconds.html


        // Create the Leader
        GameObject leader = new GameObject("Leader " + string.Format("{0:0}:{1:00}.{2:0}",
                                        Mathf.Floor(t / 60),
                                        Mathf.Floor(t) % 60,
                                        Mathf.Floor((t * 10) % 10))
                                        );
        leader.transform.position = newPosition;
        FormationLeader fl = leader.AddComponent<FormationLeader>();

        // Create the Formation
        GameObject formationObject = new GameObject("Formation " + string.Format("{0:0}:{1:00}.{2:0}",
                                        Mathf.Floor(t / 60),
                                        Mathf.Floor(t) % 60,
                                        Mathf.Floor((t * 10) % 10))
                                        );
        formationObject.transform.position = newPosition;

        Formation formation = formationObject.AddComponent<Formation>();
        formation.formationLeader = fl;
#if GDG_A

        formationObject.AddComponent<AIPathWithEvents>();
        formationObject.AddComponent<AIDestinationSetter>();
#else

        formationObject.AddComponent<FormationAgent>();
#endif

    }

}
