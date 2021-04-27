using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if GDG_A
using Pathfinding;
#else
using UnityEngine.AI;
#endif

/*
 * The Formation class contains the necessary information about the formation:
 * - How does the formation look like? (FormationTypes)
 * - What opject is the formation leader? (A formation always needs a leader since his travelled path determines the path of the followers.
 * - A list of the grid points which together create the formation with the followers each linked to a gridpoint.
 * - The layers with the obstacles and with the terrain
 * 
 * The formation is the actual object to which the navigation component is attached to (A*Pathfinding's AIPath or Unity's NavMeshAgent).
 * So this is the object which actuallya traverses the path on your terrain or mesh.
 * 
 * The FormationLeader is linked to the formation so it effectively follows the Formation object.
 * The GridPoints are created with an offset (x,y) to the Formation creating the formation shape.
 * 
 * 
 *                          F(L)
 * 
 *                          G(f)
 * 
 *                      G(f)    G(f)
 * 
 *                  G(f)            G(f)
 * 
 * F = Formation object. This object has the navigation component attached to it.
 * L = FormationLeader. The history of the FormationLeader's positions is kept for a limited time.
 * G = Gridpoint. Each gridpoint has an offset (x,y) always behind the Formation.
 * f = FormationFollower. Each FormationFollower tries to keep up with its Gridpoint
 * 
 * Why have the Formation Leader as a seperate object? This will make it easier to exchange a follower for a leader (not yet implemented).
 * 
 */

namespace Goodgulf.Formation
{


    /*
     * The FormationTypes are the preset forms supported by the Formation class. 
     * Check the Formationm.CreateFormation for more details on each preset.
     */

    public enum FormationTypes
    {
        SingleFile,
        Wedge,
        Circle,
        Square,
        Triangle,
        Lines,
        Rhomb,
        Snake
    }


    public class Formation : MonoBehaviour
    {
        [Header("Debug")]
        [Tooltip("Enabling the debug mode will show the path of the leader and formation gridpoints. It will also set a more verbose debug in the console.")]
        public bool debugMode = true;
        [Tooltip("Enabling test mode will automatically instantiate follower prefabs in the Formation.Start() method.")]
        public bool testMode = true;

        [Header("Demo")]
        [Tooltip("This is the prefab used when testMode is enabled.")]
        public GameObject followerPrefab;

        [Header("Formation")]
        [Tooltip("Select the preset formation shape.")]
        public FormationTypes formationType = FormationTypes.SingleFile;
        [Tooltip("Link to the object whic contains the FormationLeader component.")]
        public FormationLeader formationLeader;

        private List<FormationGridPoint> formationGridPoints = new List<FormationGridPoint>(); // List of all the gridpoints which make up the formation
        private List<FormationFollower> formationFollowers = new List<FormationFollower>();    // List of all the formation followers which are linked to the Gridpoints
        [SerializeField]
        private List<GameObject> formationObjects = new List<GameObject>();                    // List of the GameObjects added to the formation as FormationFollowers.
                                                                                               // This list is kept in order to facilitate a Formation change at runtime.
        
        public bool targetReached = false;                                                     // For checks whether the target has been reached

        [Tooltip("Apply randomization to each Gridpoint's offset when creating the formation. This makes the formation look a bit less perfect.")]
        [SerializeField]
        private bool randomizeGridPoints = false;

        [Tooltip("Strength of the Gridpoint randomization.")]
        [SerializeField]
        private float randomizeStrength = 0.2f;

        [Header("Layers")]
        [Tooltip("Name of the layer to which the base terrain or mesh -where the formation will walk on- has been assigned.")]
        public string terrainLayerName;
        [Tooltip("Only include the layer to which the base terrain or mesh has been assigned to.")]
        public LayerMask layerMaskTerrain;                  // Layermask includes Terrain only
        [Tooltip("Include both the layer to which the base terrain or mesh has been assigned to AND the layers to which obstacles are assigned to.")]
        public LayerMask layerMaskTerrainAndObstacles;      // Layermask includes Terrain and Obstacles


        private float maxYOffset = 0.0f;                    // Largest distance behind the leader, calculated when the formation is created
        private int nextID = 1;                             // Used to assign IDs to FormationFollowers

#if GDG_A
        private AIBase aibase;                              // A*Pathfinding base object
#endif

        void Awake()
        {
#if GDG_A
            aibase = GetComponent<AIBase>();
            if (aibase == null)
            {
                Debug.LogError("<b>Formation.Awake():</b> Could not find AIBase component.");
            }
#endif
            if (formationLeader != null)
            {
                formationLeader.SetFormation(this);
            }
            else Debug.LogWarning("<b>Formation.Awake():</b> No formation leader has been assigned.");
        }

#if GDG_A
        public AIBase GetAIBase()
        {
            return aibase;
        }
#endif
        public float GetMaxYOffset()
        {
            return maxYOffset;
        }

        // Returns the destination of the formation
        public Vector3 GetTargetPosition()
        {
#if GDG_A
            return aibase.destination;

#else
            NavMeshAgent navMeshAgent = GetComponent<NavMeshAgent>();
            if (navMeshAgent)
            {
                return navMeshAgent.destination;
            }
            else return new Vector3(0, 0, 0);
#endif
         }

        // Start is called before the first frame update
        void Start()
        {
#if GDG_A
            // Add event when target is reached. This is the best way to stop the followers moving/animation when the formation reached its destination.
            // For this to work I created a new class AIPathWithEvents which inherits from AIPath and implements the event.
            AIPathWithEvents aIPathWithEvents = (AIPathWithEvents)aibase;
            aIPathWithEvents.onTargetReached.AddListener(TargetReached);
#else
            // Add event when target is reached. For this I created a simple script FormationAgent which checks if the destination is reached then invokes the event.
            FormationAgent formationAgent = GetComponent<FormationAgent>();
            formationAgent.onTargetReached.AddListener(TargetReached);
#endif
            Debug.Log("Formation.Start(): debugMode="+debugMode+", testMode="+testMode);

            if (testMode)
            {
                // If the testmode is enabled we create a series of objects by instantiating the followerPrefab.
                // Add each object to the formation and create the formation.

                List<GameObject> gameObjects = new List<GameObject>();

                for (int i=0;i<7;i++)
                {
                    if (debugMode)
                        Debug.Log("Formation.Start(): Creating follower #" +i);

                    GameObject go = Instantiate(followerPrefab, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity); // Instantiate the follower
                    FormationFollower follow = go.GetComponent<FormationFollower>();                                 // Get its FormationFollower object
                    follow.lookedAtGridPoint = true;                                                                 
                    follow.SetFormation(this);                                                                       // Make sure the FormationFollower is linked to this formation.
                    gameObjects.Add(go);                                                                             // Add it to the temporary list so we can pass all followers in one go to the CreateFormation method
                }

                CreateFormation(this, 2.0f, 30, gameObjects, true);                                                  // Create the test formation
                
                foreach (GameObject go in gameObjects)                                                               // Add each object to the formationObjects so we keep track of the gameObjects for a later formation change
                    formationObjects.Add(go);

            }
        }

        // Update is called once per frame
        void FixedUpdate()
        {

        }

        // We have two ChangeFormation methods: one has a new list of GameObjects for the formation and the other one reuses the existing formationObjects (added to the formation befor changing it).
        //
        // This method creates a new formation shape and used a new list of GameObjects.
        // The new shape is passed on as newFormationType.
        public void ChangeFormation(Formation f, FormationTypes newFormationType, float gridDistance, int secondParameter, List<GameObject> gameObjects)
        {
            if(debugMode)
                Debug.Log("Formation.ChangeFormation(): Change Formation!");

            // First cleanup the formation:
            foreach (FormationFollower follower in formationFollowers)
                follower.ClearFormation();  // Clear the link to the FormationGridPoint within each of the Followers

            formationGridPoints.Clear();
            formationFollowers.Clear();

            // Next create the new formation shape:
            f.formationType = newFormationType;
            f.CreateFormation(f, gridDistance, secondParameter, gameObjects, false);

            // Copy the list of gameobjects from the passed on parameter into the internal list of GameObject. This will make a formation change easier.
            foreach (GameObject go in gameObjects)
                formationObjects.Add(go);
        }

        // This method creates a new formation shape and used the existing list of GameObjects.
        // The new shape is passed on as newFormationType.
        public void ChangeFormation(Formation f, FormationTypes newFormationType, float gridDistance, int secondParameter)
        {
            if (debugMode)
                Debug.Log("Formation.ChangeFormation(): Change Formation! objects count = "+formationObjects.Count);

            foreach (FormationFollower follower in formationFollowers)
                follower.ClearFormation();

            formationGridPoints.Clear();
            formationFollowers.Clear();

            f.formationType = newFormationType;

            f.CreateFormation(f, gridDistance, secondParameter, formationObjects, false);
        }

        // Create a formation with distance between (leader and) gridpoints of gridDistance
        // Use secondParameter for specific shapes, examples: angle of the wedge or the number of number of parallel lines in the formation 
        public void CreateFormation(Formation f, float gridDistance, int secondParameter, List<GameObject> gameObjects, bool RepositionFollowers)
        {
            if(formationGridPoints.Count>0)
            {
                Debug.LogError("Formation.CreateFormation(): use this function only on empty formation gridpoints.");
                return;
            }

            int N = gameObjects.Count;

            /*
             * At this stage we have the following variables/parameters we can use to setup the shapes:
             * 
             * N                = number of followers (so does not include the leader)
             * gridDistance     = used to determine the distance between the followers
             * secondParameter  = used as a parameter in the shapes
             *
             * The basic shape "routine" is to create an offset vector (x,y) for each object 1..N
             * The y coordinate of the offset is always 0 or negative since the followers are not allowed to walk in front of the leader.
             * Although it could technically be implemented this would result in gridpoints moving abruptly/no smoothly.
             *
             */


            switch (f.formationType)
            {
                case FormationTypes.SingleFile:

                        for(int i=0;i<N;i++)
                        {
                            // The offset is simple (x,y) = 0, -1 * gridDistance * follower (1..N)

                            Vector2 offset = new Vector2(0,-1f * gridDistance * (i+1));

                            // Get the FormationFollower component:
                            FormationFollower follow = gameObjects[i].GetComponent<FormationFollower>();
                            if(follow==null)
                            {
                                Debug.LogError("Formation.CreateFormation(): follower object "+i+" does not have FormationFollower Component.");
                                return;
                            }

                            // Add the gridpoint based on the offset:
                            AddFormationGridPoint(f, offset, layerMaskTerrain, follow);

                            // If the RepositionFollowers paramater is true then we'll move the followers to the gridpoint immediately.
                            // This is not recommended when you change the formation at runtime (other then at the Start) because the objects will jump to this position.
                            if(RepositionFollowers)
                                Utilities.MoveObjectOnTerrainByOffset(gameObjects[i], f.gameObject, offset, layerMaskTerrain);
                        }

                    break;

                case FormationTypes.Wedge:

                    if(secondParameter==0)
                    {
                        Debug.LogError("Formation.CreateFormation(): Wedge angle cannot be zero.");
                        return;
                    }

                    // The secondParameter is used as the angle (a) in degrees for the wedge:
                    //
                    //                 x=-1     x=1             L = leader, g = gridpoint
                    //                      L                   a = angle for the triangle
                    //                  g   |   g               y = 1
                    //              g       |  /    g           y = 2
                    //          g           |-a         g       y = 3
                    //                      |

                    float beta = (float)secondParameter * Mathf.PI / 180f;  // Convert angle to radians
                    float adjacent = gridDistance * Mathf.Cos(beta);        // https://www.mathsisfun.com/sine-cosine-tangent.html
                    float opposite = gridDistance * Mathf.Sin(beta);

                    for (int i = 0; i < N; i++)
                    {
                        int y = 1+ i / 2;               // Increase y for every two follewers by 1
                        int x = (i % 2 == 0) ? 1 : -1;  // Switch between -1 and 1 on even and odd follower index
                        
                                                        // The x coordinate increases with every increase of y to get the wedge shape 
                        Vector2 offset = new Vector2(x * opposite * y, -1 * y * adjacent);

                        FormationFollower follow = gameObjects[i].GetComponent<FormationFollower>();
                        if (follow == null)
                        {
                            Debug.LogError("Formation.CreateFormation(): follower object " + i + " does not have FormationFollower Component.");
                            return;
                        }

                        AddFormationGridPoint(f, offset, layerMaskTerrain, follow);
                        if (RepositionFollowers)
                            Utilities.MoveObjectOnTerrainByOffset(gameObjects[i], f.gameObject, offset, layerMaskTerrain);
                    }

                    break;

                case FormationTypes.Circle:

                    // For the circle we don't need the secondParameter
                    //
                    // The number of sectors (https://en.wikipedia.org/wiki/Circular_sector) is determined by the number of followers + 1 (=leader)
                    // The radius of the circle is determined by the number of sectors * gridDistance
                    // The leader is positioned in the front:
                    //
                    //                          L
                    //                       g  |  g
                    //                    g     |a /  g     a = angle for a sector
                    //                    g     |-/   g  
                    //                      g       g
                    //                         g g
                    //

                    int numberOfSectors = N + 1;
                    float radius = numberOfSectors * gridDistance / Mathf.PI;

                    for (int i = 0; i < N; i++)
                    {
                        float angle = (i + 1) * 2 * Mathf.PI / numberOfSectors;

                        Vector2 offset = new Vector2(radius * Mathf.Sin(angle) , -radius + radius * Mathf.Cos(angle));

                        FormationFollower follow = gameObjects[i].GetComponent<FormationFollower>();
                        if (follow == null)
                        {
                            Debug.LogError("Formation.CreateFormation(): follower object " + i + " does not have FormationFollower Component.");
                            return;
                        }

                        AddFormationGridPoint(f, offset, layerMaskTerrain, follow);
                        if (RepositionFollowers)
                            Utilities.MoveObjectOnTerrainByOffset(gameObjects[i], f.gameObject, offset, layerMaskTerrain);
                    }

                    break;

                case FormationTypes.Lines:

                    // secondParamater = number of parallel lines in the formation
                    //
                    //              L
                    //            g g g
                    //            g g g
                    //            g g g
                    //            g g g

                    float half = (secondParameter - 1)  / 2;

                    for (int i = 0; i < N; i++)
                    {
                        int row = i / secondParameter;
                        int x = i % secondParameter;

                        Vector2 offset = new Vector2((x - half) * gridDistance, -1 * (row+1) * gridDistance);

                        FormationFollower follow = gameObjects[i].GetComponent<FormationFollower>();
                        if (follow == null)
                        {
                            Debug.LogError("Formation.CreateFormation(): follower object " + i + " does not have FormationFollower Component.");
                            return;
                        }

                        AddFormationGridPoint(f, offset, layerMaskTerrain, follow);
                        if (RepositionFollowers)
                            Utilities.MoveObjectOnTerrainByOffset(gameObjects[i], f.gameObject, offset, layerMaskTerrain);
                    }

                    break;

                case FormationTypes.Square:

                    // secondParamater = length of the side 

                    //               L
                    //            g g g g
                    //            g g g g
                    //            g g g g
                    //            g g g g

                    float halfSide = (secondParameter - 1) / 2;

                    int M = secondParameter * secondParameter;

                    if(N < M)
                    {
                        Debug.LogWarning("Formation.CreateFormation(): not enough objects to fill formation.");
                    }

                    if (M > N) 
                    {
                        Debug.LogWarning("Formation.CreateFormation(): more objects than allowed in ("+secondParameter+"x"+secondParameter+") square.");
                        M = N; 
                    }

                    for (int i = 0; i < M; i++)
                    {
                        int row = i / secondParameter;
                        int x = i % secondParameter;

                        Vector2 offset = new Vector2((x - halfSide) * gridDistance, -1 * (row + 1) * gridDistance);

                        FormationFollower follow = gameObjects[i].GetComponent<FormationFollower>();
                        if (follow == null)
                        {
                            Debug.LogError("Formation.CreateFormation(): follower object " + i + " does not have FormationFollower Component.");
                            return;
                        }

                        AddFormationGridPoint(f, offset, layerMaskTerrain, follow);
                        if (RepositionFollowers)
                            Utilities.MoveObjectOnTerrainByOffset(gameObjects[i], f.gameObject, offset, layerMaskTerrain);
                    }

                    break;
                case FormationTypes.Snake:

                    // secondParamater = number of snake wave repeats
                    //
                    //              L
                    //                  g            y = 1
                    //                   g           y = 2
                    //                  g
                    //              g
                    //          g
                    //         g
                    //          g
                    //              g

                    float wavelength = (float) N / secondParameter;

                    for (int i = 0; i < N; i++)
                    {

                        float y = gridDistance * (i + 1);

                        float x = gridDistance * Mathf.Sin( Mathf.PI * 2 * secondParameter * (y / N) );

                        Vector2 offset = new Vector2(x, -1f * y);

                        FormationFollower follow = gameObjects[i].GetComponent<FormationFollower>();
                        if (follow == null)
                        {
                            Debug.LogError("Formation.CreateFormation(): follower object " + i + " does not have FormationFollower Component.");
                            return;
                        }

                        AddFormationGridPoint(f, offset, layerMaskTerrain, follow);
                        if (RepositionFollowers)
                            Utilities.MoveObjectOnTerrainByOffset(gameObjects[i], f.gameObject, offset, layerMaskTerrain);
                    }

                    break;

                case FormationTypes.Rhomb:

                    // https://en.wikipedia.org/wiki/Rhombus
                    //
                    //                 x=-1     x=1             L = leader, g = gridpoint
                    //                      L                   a = angle for triangle shape
                    //                  g   |   g               y = 1
                    //              g       |  /    g           y = 2
                    //          g           |-a         g       y = 3
                    //              g               g
                    //                  g       g
                    //                      g
                    //
                    //  For this shape to work the unit count should be uneven
                    


                    if (secondParameter == 0)
                    {
                        Debug.LogError("Formation.CreateFormation(): Rhomb angle cannot be zero.");
                        return;
                    }

                    if(N % 2 == 0)
                    {
                        Debug.LogWarning("Formation.CreateFormation(): Rhomb unit count should not be even.");
                    }

                    float betaR = (float)secondParameter * Mathf.PI / 180f;  // Convert angle to radians
                    float adjacentR = gridDistance * Mathf.Cos(betaR);        // https://www.mathsisfun.com/sine-cosine-tangent.html
                    float oppositeR = gridDistance * Mathf.Sin(betaR);

                    int converge = 0;
                    int maxRows = (N + 1) / 4;

                    for (int i = 0; i < N; i++)
                    {
                        int y = 1 + i / 2;
                        int x = (i % 2 == 0) ? 1 : -1;

                        Vector2 offset;

                        if (i > (N / 2))
                        {
                            // Lines now need to converge again when we end the shape
                            converge = -x ;
                            int newRow =  ( N - i ) / 2;
                            offset = new Vector2(x * oppositeR * ( maxRows - (y - maxRows) ) , -1 * y * adjacentR);
                        }
                        else offset = new Vector2(x * oppositeR * y, -1 * y * adjacentR); // diverging lines, this is how we start


                        FormationFollower follow = gameObjects[i].GetComponent<FormationFollower>();
                        if (follow == null)
                        {
                            Debug.LogError("Formation.CreateFormation(): follower object " + i + " does not have FormationFollower Component.");
                            return;
                        }

                        AddFormationGridPoint(f, offset, layerMaskTerrain, follow);
                        if (RepositionFollowers)
                            Utilities.MoveObjectOnTerrainByOffset(gameObjects[i], f.gameObject, offset, layerMaskTerrain);
                    }

                    break;

                /*
                 *         Triangle: not yet implemented
                 */

                default:
                    Debug.LogError("Formation.CreateFormation(): unsupported Formation Type.");
                    break;
            }
        }

        // This method adds a grid point to the formation to create the desired shape. Followers always walk towards the gridpoint they are assigned to.
        // Note that a grid point Y should also be zero or smaller than zero.
        public void AddFormationGridPoint(Formation f, Vector2 offset, LayerMask layerMask, FormationFollower follower)
        {
            if (offset.y > 0)
            {
                Debug.LogError("Formation.AddFormationGridPoint(): Offset Y coordinate should always be < 0." + offset.y);
            }
            else
            {
                Vector2 newOffset = new Vector2(offset.x, offset.y);

                // If the formation has randomizeGridPoints set to true we'll add a small variation to the coordinates
                // in order for the shape to be imperfect. Note the randomization of the Y coordinate is limited to ensure it never becomes positive.
                if(randomizeGridPoints)
                {
                    newOffset.y -= Random.Range(0, randomizeStrength);
                    newOffset.x -= Random.Range(-randomizeStrength, randomizeStrength);
                }

                FormationGridPoint formationGridPoint = new FormationGridPoint(nextID, f, newOffset, layerMask, follower);
                nextID++;
                
                formationGridPoints.Add(formationGridPoint);
                follower.formationGridPoint = formationGridPoint;
                formationFollowers.Add(follower);
            }
            // find the largest negative y offset (we'll need that in the FormationLeader.FixedUpdate to cleanup the queue).
            if (offset.y < maxYOffset)
                maxYOffset = offset.y;
        }

        // This method gets called from teh FormationLeader.FixedUpdate() method to recalculate the gridpoints after the leader moves.
        public void UpdateFormationPositions(Transform leaderTransform)
        {
            foreach (FormationGridPoint formationGridPoint in formationGridPoints)
            {
                formationGridPoint.RecalculatePosition(leaderTransform);
            }
        }

        // This method is invoked by the event assigned in the Formation.Start() method.
        // This method is used to mark that the target destination has been reached.
        public void TargetReached()
        {
            if(debugMode)
                Debug.Log("Formation.TargetReached(): <b>Target reached.</b>");

            targetReached = true;

            if(formationLeader!=null)
            {
                FormationAnimation fal = formationLeader.GetComponentInChildren<FormationAnimation>();
                if (fal != null)
                    fal.IdleAnimations();
            }

            foreach(FormationFollower f in formationFollowers)
            {
                FormationAnimation faf = f.GetComponentInChildren<FormationAnimation>();
                if (faf != null)
                    faf.IdleAnimations();

            }
        }

    }
}