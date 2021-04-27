using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Goodgulf.Formation
{
    /*
     * The FormationGridPoint is used to create the shape of the Formation. Each FormationFollower is assigned to a FormationGridPoint and walks
     * towards it.
     * 
     * 
     * 
     */
    public class FormationGridPoint
    {

        private FormationLeader formationLeader;    // Cache the FormationLeader

        private Vector2 gridPointOffset;            // The offset behind the FormationLeader (x = left/right, y = behind)
        private int ID;                             // Currently not used.

        private Formation formation;                // Cache the Formation
        private Vector3 Position;                   // Track the current FormationGridPoint's position; this is what the FormationFollower moves towards to
        private LayerMask layerMask;                // The layermask with the terrain so we can make sure it's placed on the terrain, not an obstacle
        private FormationFollower follower;         // The FormationFollower assigned to this FormationGridPoint

        private int debugCounter = 0;               // A counter used for placement of debug spheres along the path of the FormationGridPoint

        public FormationGridPoint(int id, Formation f, Vector2 offset, LayerMask mask, FormationFollower _follower)
        {
            ID = id;
            formation = f;
            formationLeader = f.formationLeader;
            gridPointOffset = offset;
            layerMask = mask;
            follower = _follower;
            follower.formationGridPoint = this;
        }

        public Vector3 GetPosition()
        {
            return Position;
        }

        //
        // This is the key method used to calculate the position of the FormationGridPoint relative to the FormationLeader
        //
        //
        public void RecalculatePosition (Transform leaderTransform)
        {


            float deltaTimeY = 0;
            float leaderAverageVelocity = formationLeader.GetAverageVelocity();
            float leaderVelocity = formationLeader.GetVelocityMagnitude();

            if (leaderAverageVelocity > 0.0001f)
            {
                // The Leader is moving so the FormationGridPoint needs to move too
                // 
                // Step 1: find the queue item in formationLocations (=history of where the leader walked) based on the gridPointOffset.y = behind the leader

                deltaTimeY = - 1.0f * gridPointOffset.y / leaderAverageVelocity; // delta time = distance / velocity
                FormationLocation oldLeaderPosition = formationLeader.FindClosestInTime(deltaTimeY);

                // Step 2: an old position was found so we can calculate the offset based on where the FormationLeader was deltaTimeY ago

                if (oldLeaderPosition != null)
                {
                    // Set the position to this old Leader position assuming the gridPointOffset.x == 0
                    Position = oldLeaderPosition.position;

                    if (Mathf.Abs(gridPointOffset.x) > 0.001f)
                    {
                        // The gridPointOffset.x > 0

                        // Calculate the offset perpendicular to vector between leader current position and leader old position
                        // So we're using a small vector (current - previous position) and get a normalized vector perpendicular to it.
                        Vector3 right = Utilities.Rotate90CW(formationLeader.GetPosition() - oldLeaderPosition.position).normalized;
                        
                        // Then multiply by the gridPointOffset.x to get the actual offset:
                        Position = oldLeaderPosition.position + right * gridPointOffset.x;
                    }

                    // Place a green sphere tracking the FormationGridPoint if Formation.debugMode == true
                    if (formation.debugMode)
                    {
                        debugCounter += 1;

                        if (debugCounter > 25)
                        {
                            debugCounter = 0;
                            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            sphere.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                            sphere.transform.name = "Follower";

                            Vector3 newPosition;
                            bool result = Utilities.GetTerrainRaycastPosition(Position, out newPosition, layerMask);
                            if(result)
                                sphere.transform.position = newPosition;
                            else
                                sphere.transform.position = Position;

                            Utilities.SetSphereColor(sphere, Color.green);
                            Utilities.DisableSphereCollider(sphere);
                        }
                    }

                   }
                else
                {
                    // FindClosestInTime = null so we're early on in the movement of the formation
                    // Now we use the hard offset coordinates. This will look cluncky but we need to do something

                    float rotationY = leaderTransform.rotation.eulerAngles.y;

                    Vector3 positionOffset = new Vector3(gridPointOffset.x, 0, gridPointOffset.y);
                    Vector3 rotatedPositionOffset = Quaternion.Euler(0, rotationY, 0) * positionOffset;

                    Position = leaderTransform.position + rotatedPositionOffset;
                }
            }
        }
    }
}
