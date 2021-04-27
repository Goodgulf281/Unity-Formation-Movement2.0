using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Goodgulf.Formation
{
    /*
     * This Utilities class contains a series of static methods used in the Formation classes:
     * 
     * - Update Spheres for debug purposes
     * - Terrain Raycasts to make sure objects get placed on terrain, not underneath it.
     * - Some vector functions
     * 
     */

    public class Utilities
    {
        
        static public void SetSphereColor(GameObject go, Color color)
        {
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer)
            {
                renderer.material.color = color;
            }
        }

        static public void DisableSphereCollider(GameObject go)
        {
            SphereCollider collider = go.GetComponent<SphereCollider>();
            if (collider)
            {
                collider.enabled = false;
            }
        }

        // Find the exact terrain position height based on the approximate checkPosition and store it in positionOnTerrain if found (result = true)
        // If not found the result = false and position on terrain = (0,0,0).
        static public bool GetTerrainRaycastPosition(Vector3 checkPosition, out Vector3 positionOnTerrain, LayerMask layerMask)
        {
            Vector3 rayPosition = checkPosition;
            rayPosition.y += 1000;

            RaycastHit hit;
            if (Physics.Raycast(rayPosition, -Vector3.up, out hit, Mathf.Infinity, layerMask))
            {
                positionOnTerrain = hit.point;
                return true;
            }
            else
            {
                positionOnTerrain = Vector3.zero;
                return false;
            }
        }

        // Move the object to a terrain position at the appropriate terrain height.
        // Move from current position to offset position x,z coordinates only (so horizontal plane)
        static public bool MoveObjectOnTerrainByOffset(GameObject toMove, GameObject origin,  Vector2 offset, LayerMask layerMask)
        {
            bool result;
            Vector3 newPosition;

            float rotationY = origin.transform.rotation.eulerAngles.y;
            Vector3 positionOffset = new Vector3(offset.x, 0, offset.y);
            Vector3 rotatedPositionOffset = Quaternion.Euler(0, rotationY, 0) * positionOffset;
            Vector3 offsetPosition = origin.transform.position + rotatedPositionOffset;


            result = GetTerrainRaycastPosition(offsetPosition, out newPosition, layerMask);
            if(result)
            {
                toMove.transform.position = newPosition;
            }
            else
            {
                Debug.LogWarning("Utilities.MoveObjectOnTerrainByOffset(): could not find position on terrain for "+toMove.name);
            }

            return result;
        }

        // clockwise https://answers.unity.com/questions/1333667/perpendicular-to-a-3d-direction-vector.html
        static public Vector3 Rotate90CW(Vector3 aDir)
        {
            return new Vector3(aDir.z, 0, -aDir.x);
        }
        // counter clockwise https://answers.unity.com/questions/1333667/perpendicular-to-a-3d-direction-vector.html
        static public Vector3 Rotate90CCW(Vector3 aDir)
        {
            return new Vector3(-aDir.z, 0, aDir.x);
        }

        // https://answers.unity.com/questions/50279/check-if-layer-is-in-layermask.html
        public static bool IsInLayerMask(int layer, LayerMask layermask)
        {
            return layermask == (layermask | (1 << layer));
        }
    }
}
