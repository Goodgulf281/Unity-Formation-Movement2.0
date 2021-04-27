using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Goodgulf.Formation
{

    /*
     * These are the options when a Follower gets stuck:
     * 
     * - Do nothing (probably remain stuck).
     * - Move towards the center path follower by the FormationLeader.
     * - Try a Random Walk to see if the follower can unstuck himself. For small obstacles this is a fast method.
     */

    public enum FollowerStuckMode
    {
        DoNothing,
        MoveToLeaderPath,
        RandomWalk
    }

    /*
     * What's the follower doing, simple state machine used mostly for keeping evasion working.
     */

    public enum FollowerStatus
    {
        Stand,
        Move,
        Stuck,
        Evade
    }

    /*
     * The FormationFollower is the class you'll need to add to the objects/prefabs used as followers in the formation.
     * 
     * The FormationFollowers walk towards the FormationGridPoint they are linked to. If teh distance to the gridpoint is large
     * it will move more quickly.
     * 
     * If the follower gets stuck it will act based on the FollowerStuckMode
     * 
     */

    public class FormationFollower : FormationBase
    {
        private Formation formation;                                                // Cache the Formation
        public FormationGridPoint formationGridPoint;                               // Cache the FormationGridPoint linked to this follower
        public FormationLeader formationLeader;                                     // Cache the FormationLeader
        private Transform tr;                                                       // Cache the follower's Transform
        private int terrainLayer;                                                   // Cache the terrainLayer

        [Header("Movement")]
        private Vector3 oldPosition;                                                // The FormationFollower's previous position
        private Vector3 lastKnownSuccessfulRaycastHit;                              // The last succesfull Rayecast on terrain (so not on an obstacle)
        [SerializeField]
        private Vector3 velocity;                                                   // The FormationFollower's velocity vector
        [SerializeField]
        private float velocityMagnitude;                                            // The FormationFollower's velocity magnitude
        [SerializeField]
        private float velocityAverage;                                              // The FormationFollower's average velocity
        [SerializeField]
        private float maximumVelocityCoefficient = 2.0f;                            // This is the multiplier to get the FormationFollower's maximum velocity in case the distance to the FormationGridPoint is large; this is used to catch up.

        public bool lookedAtGridPoint = false;                                      // Currently assigned but not used

        [SerializeField]
        private FollowerStatus followerStatus = FollowerStatus.Stand;               // What's the FormationFollower doing?
        [SerializeField]
        private FollowerStuckMode followerStuckMode = FollowerStuckMode.DoNothing;  // Set the FormationFollower's mode when stuck
        [SerializeField]
        private float angleTowardsLeaderPath = 45.0f;                               // If the FollowerStuckMode == MoveToLeaderPath then
        [SerializeField]                                                            //  then move in a random maximum angle (angleTowardsLeaderPath) towards the FormationLeader's path
        private float maxObstacleEvasionTime = 2.0f;                                // If the FollowerStuckMode == RandomWalk then
                                                                                    //  then only try for maxObstacleEvasionTime seconds
        
        private Vector3 evasionTargetPosition;                                      // This is the target position where the stuck follower is moving towards to
        private float evasionStartTime = 0.0f;                                      // The timer for how long we're evading

        public void SetFormation (Formation f)
        {
            formation = f;
            formationLeader = f.formationLeader;

            terrainLayer = LayerMask.NameToLayer(formation.terrainLayerName);       // Cache the layer index for the terrain layer
        }

        public void ClearFormation ()
        {
            formationGridPoint = null;                                              // Clear the reference to the FormationGridPoint
        }

        private void Awake()
        {
            tr = transform;                 // Cache the Transform
            oldPosition = tr.position;      // Set the initial value of the oldPosition to the current position
        }

        
        // Update is called once per frame
        void FixedUpdate()
        {
            float deltaTime = Time.deltaTime;

            MoveTowardsGridPoint();         // This is where the heavy lifting is done for the FormationFollower

            // Calculate the follower velocity based on distance travelled/deltaTime
            if (deltaTime > 0.0001f)
            {
                velocity = (tr.position - oldPosition) / deltaTime;
                velocityMagnitude = velocity.magnitude;
            }
            else
            {
                velocity = Vector3.zero;
                velocityMagnitude = 0;
            }

            oldPosition = tr.position;
        }

        // Make the FormationFollower look at the FormationGridPoint (rotate along y-axis only)
        void LookAtFormationGridPoint()
        {
            Vector3 target = formationGridPoint.GetPosition();
            target.y = tr.position.y;
            transform.LookAt(target);
        }

        // Make the FormationFollower look at any point (rotate along y-axis only)
        void LookAtPoint(Vector3 point)
        {
            Vector3 target = point;
            target.y = tr.position.y;
            transform.LookAt(target);
        }

        // Place the FormationFollower on the FormationGridPoint, currently not used
        public void MoveToGridPoint (float rotationY)
        {
            if (formationGridPoint != null)
            {
                transform.position = formationGridPoint.GetPosition();

                Vector3 rotation = new Vector3(0, rotationY, 0);

                transform.eulerAngles= rotation;
            }
        }

        //
        // This is the method (called from the FixedUpdate) where the FormationFollower moves towards the FormationGridPoint which previously was updated by the FormationLeader's FixedUpdate
        //
        public void MoveTowardsGridPoint()
        {
            if (formationGridPoint != null)
            {
                float deltaTime = Time.deltaTime;

                // Get the psotion of the FormationGridPoint this FormationFollower is linked to and calculate the distance.
                Vector3 gridPointPosition = formationGridPoint.GetPosition();
                float distanceToGridPoint = Vector3.Distance(gridPointPosition, tr.position);

                Vector3 velocityLeader = formationLeader.GetVelocity();
                float leaderVelocity = formationLeader.GetVelocityMagnitude();

                if(formation.debugMode)
                    Debug.Log("FormationFollower.MoveTowardsGridPoint(): distance to grid point = "+distanceToGridPoint);

                // Step 1: look at the gridpoint before we move, if the distance is at least 0.1m otherwise the FormationFollower may start spinning along its y-axis
                if (distanceToGridPoint > 0.1f)
                {
                    if (followerStatus == FollowerStatus.Move) 
                    {
                        LookAtFormationGridPoint();
                    }
                    else if (followerStatus == FollowerStatus.Evade)
                    {
                        LookAtPoint(evasionTargetPosition);
                    }
                }

                // Step 2a: check if the leader stopped moving and we are close enough to the FormationGridPoint to stop too
                if (leaderVelocity < 0.0001f && distanceToGridPoint < 0.01f)
                {
                    if (formation.debugMode)
                        Debug.Log("FormationFollower.MoveTowardsGridPoint(): we're not moving, close to gridpoint and leader stopped");

                    followerStatus = FollowerStatus.Stand;

                    return; // leader is not moving and we are close (enough) to gridpoint
                }

                // Step 2b: What to do if we're stuck?
                //
                // The FollowerStatus is set to Stuck in Step 5 so we first have to have moved to get into this state.
                //
                if(followerStatus == FollowerStatus.Stuck)
                {
                    if (followerStuckMode == FollowerStuckMode.DoNothing)
                    {
                        if (formation.debugMode)
                            Debug.Log("FormationFollower.MoveTowardsGridPoint(): follower is stuck, do nothing");

                        return; // OK do nothing and stay where we are
                    }
                    else if (followerStuckMode == FollowerStuckMode.RandomWalk)
                    {
                        // In RandomWalk mode the follower walks in a random direction to see if it gets unstuck at some time
                        followerStatus = FollowerStatus.Evade;
                        evasionStartTime = Time.fixedTime;

                        // Calculate an alternative gridPointPosition with a distance (radius around us) based on max obstacle evasion time
                        float radius = deltaTime * leaderVelocity * maximumVelocityCoefficient * (maxObstacleEvasionTime / Time.fixedDeltaTime);

                        if (formation.debugMode)
                            Debug.Log("FormationFollower.MoveTowardsGridPoint(): follower is stuck, random walk with radius = " + radius);

                        // We know hoe far (radius) not pick a random direction:
                        Vector2 randomWalk = Random.insideUnitCircle.normalized * radius;

                        // So this is where we're heading:
                        gridPointPosition = tr.position + new Vector3(randomWalk.x, 0, randomWalk.y);

                        // Make sure we're on terrain by Raycasting the new gridPointPosition with the terrain layer mask
                        Vector3 terrainPosition;
                        bool result = Utilities.GetTerrainRaycastPosition(gridPointPosition, out terrainPosition, formation.layerMaskTerrain);
                        if (result)
                            gridPointPosition = terrainPosition;

                        distanceToGridPoint = Vector3.Distance(gridPointPosition, tr.position);

                        // TODO: there is a logic error here: if the Raycast hits something else then terrain the new point still gets used.
                        //          although the FormationFollower still will not walk through obstacles.

                        // Place a small blue sphere indicating the RandomWalk destination
                        if (formation.debugMode)
                        {
                            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            sphere.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
                            sphere.transform.name = "Stuck";
                            sphere.transform.position = gridPointPosition;
                            Utilities.SetSphereColor(sphere, Color.blue);
                            Utilities.DisableSphereCollider(sphere);
                        }

                        // Have the Follower look at the gridPointPosition
                        LookAtPoint(gridPointPosition);

                        // Store the evasion target
                        evasionTargetPosition = gridPointPosition;
                    }
                    else if (followerStuckMode == FollowerStuckMode.MoveToLeaderPath)
                    {
                        // In MoveToLeaderPath the follower tries to move towards the path the Leader took since that's known to be without obstacles due to the pathfinding
                        followerStatus = FollowerStatus.Evade;
                        evasionStartTime = Time.fixedTime;

                        // Get the closest location stored in the FormationLeader's history (stored in the queue).
                        FormationLocation fl = formationLeader.FindClosest(tr.position);
                        if (fl != null) 
                        {
                            if (formation.debugMode)
                                Debug.Log("FormationFollower.MoveTowardsGridPoint(): close leader path location found");

                            // Now randomize towards this new location
                            // https://gamedev.stackexchange.com/questions/151840/random-direction-vector-relative-to-current-direction
                            // https://answers.unity.com/questions/46770/rotate-a-vector3-direction.html

                            Vector3 direction = fl.position - tr.position;

                            //                        L
                            //                         o
                            //                    /     o           L = leader
                            //          Obstacle / a    o           o = FormationLocations
                            //                f ------->x           a = angleTowardsLeaderPath
                            //                   \ a    o           f = follower (is stuck)
                            //                    \    o            x = FindCloset / FormationLocation
                            //
                            
                            // Get an angle towards x +/- a
                            float angle = Random.Range(-1 * angleTowardsLeaderPath, angleTowardsLeaderPath);
                            Vector3 newDirection = Quaternion.Euler(0, angle, 0) * direction;

                            // This is the new temporary direction:
                            gridPointPosition = tr.position + newDirection;

                            // Make sure we're on terrain
                            Vector3 terrainPosition;
                            bool result = Utilities.GetTerrainRaycastPosition(gridPointPosition, out terrainPosition, formation.layerMaskTerrain);
                            if (result)
                                gridPointPosition = terrainPosition;

                            distanceToGridPoint = Vector3.Distance(gridPointPosition, tr.position);
                            evasionTargetPosition = gridPointPosition;

                            // Place a small blue sphere indicating the MoveToLeaderPath destination
                            if (formation.debugMode){
                                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                                sphere.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
                                sphere.transform.name = "Stuck";
                                sphere.transform.position = gridPointPosition;
                                Utilities.SetSphereColor(sphere, Color.blue);
                                Utilities.DisableSphereCollider(sphere);
                            }

                            LookAtPoint(gridPointPosition);
                        }
                        else if (formation.debugMode)
                            Debug.Log("FormationFollower.MoveTowardsGridPoint(): follower is stuck, cannot find close leader path location");

                    }
                }

                // Step 2c: What if we're evading?
                if (followerStatus == FollowerStatus.Evade)
                {
                    gridPointPosition = evasionTargetPosition;
                    distanceToGridPoint = Vector3.Distance(gridPointPosition, tr.position);

                    // Check if we are moving towards the temporary destination long enough
                    if(Time.fixedTime > evasionStartTime + maxObstacleEvasionTime) 
                    {
                        // Stop evading obstacles now
                        followerStatus = FollowerStatus.Move;
                        evasionStartTime = 0.0f;
                    }
                    else if (distanceToGridPoint < 0.1f)
                    {
                        // We have reached the alternative "temporary" grid point
                        followerStatus = FollowerStatus.Move;
                        evasionStartTime = 0.0f;
                    }
                }

                // Step 3: We should move, check distance to cover and velocity depending on distance
                float fractionOfDistanceToCover = 0f;

                if (distanceToGridPoint < 0.01f)
                {
                    // we're on/close enough to the gridpoint so no need to move
                    fractionOfDistanceToCover = 0.0f;
                    
                    if(formation.debugMode)
                        Debug.Log("FormationFollower.MoveTowardsGridPoint(): on the gridpoint ");

                    followerStatus = FollowerStatus.Stand;
                    return;
                }
                else
                {
                    if (distanceToGridPoint > velocityLeader.magnitude * deltaTime)
                    {
                        // we cannot cover this distance at normal speed so increase the speed using the maximumVelocityCoefficient
                        fractionOfDistanceToCover = (velocityLeader.magnitude * deltaTime * maximumVelocityCoefficient) / distanceToGridPoint;
                        
                        if (formation.debugMode)
                            Debug.Log("FormationFollower.MoveTowardsGridPoint(): fraction coeff = " + fractionOfDistanceToCover);
                    }
                    else
                    {
                        // we'll get there at regular speed
                        fractionOfDistanceToCover = (velocityLeader.magnitude * deltaTime) / distanceToGridPoint;

                        if (formation.debugMode)
                            Debug.Log("FormationFollower.MoveTowardsGridPoint(): fraction = " + fractionOfDistanceToCover);
                    }
                }

                // Step 4: Calculate the new position based on the velocity / fraction of distance to cover

                Vector3 newPosition = Vector3.Lerp(tr.position, gridPointPosition, fractionOfDistanceToCover);
                tr.position = newPosition;
                
                // Step 5: Check if this new position hits a terrain, obstacle or nothing. In case of the latter two stay in last known position which did hit terrain.

                // NOTE: 
                //          Make sure the terrain has been assigned a different layer than the character itself
                //          Also ensure the terrain layer is in the A*Pathfinding Height checking layer mask
                //

                Vector3 rayPosition = newPosition;
                rayPosition.y += 1000;

                RaycastHit hit;
                if (Physics.Raycast(rayPosition, -Vector3.up, out hit, Mathf.Infinity, formation.layerMaskTerrainAndObstacles))
                { 
                    // So we hit something, now check what we hit
                    if(hit.transform.gameObject.layer == terrainLayer)
                    {
                        // We hit terrain so move to where we hit the terrain
                        tr.position = hit.point;
                        // Store this succesful hit in case we hit an obstacle the next step we take.
                        lastKnownSuccessfulRaycastHit = tr.position;

                        if(followerStatus == FollowerStatus.Evade)
                        {
                            // we keep evading, keep the status we have
                        }
                        else followerStatus = FollowerStatus.Move;
                    }
                    else
                    {
                        // We hit an obstacle, so stick to the position we know was OK in the previous iteration
                        tr.position = lastKnownSuccessfulRaycastHit;

                        followerStatus = FollowerStatus.Stuck;
                        // In the next call we'll check what to do using the followerStuckMode
                    }
                }
                else
                {
                    // We hit nothing, treat as Stuck
                    if (formation.debugMode)
                        Debug.LogError("FormationFollower.MoveTowardsGridPoint(): Raycast hit nothing");

                    tr.position = lastKnownSuccessfulRaycastHit;
                    followerStatus = FollowerStatus.Stuck;
                }
            }
        }
    }
}
