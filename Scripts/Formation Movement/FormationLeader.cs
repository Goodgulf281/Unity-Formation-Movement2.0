using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if GDG_A
using Pathfinding;
#endif

namespace Goodgulf.Formation
{
    /*
     * The FormationLocation is used to store the Formation Leader's position as he moves. This necessary for the followers to follow more naturally
     * along the path the Leader follows: a follower without a x-offset will follow exactly in the path of the Leader albeit just behind him.
     * 
     * The Formation Locations are stored in a Queue to make it easier to remove items First-In-First-Out wise.
     */
    public class FormationLocation
    {
        public Vector3 position;        // The Leader's position
        public float time;              // At what time did we store the Leader's position
        public float rotationY;         // What was the Leader's rotation
        public GameObject debugObject;  // Also store a reference to the debug object (a colored sphere) if Formation.debugMode == true
    }


    /*
     * This is the class defining the Formation Leader who moves along with the Formation.
     * 
     * The core functionality of the Leader is:
     * 
     * - Store the path it takes in the formationLocations Queue.
     * - Call the formation.UpdateFormationPositions method to recalculate the GridPoints based on the Leader's current position
     * 
     */
    public class FormationLeader : FormationBase
    {
        private Formation formation;                            // Cache the formation

        public float formationCheckInterval = 1.0f;             // This is the threshold value used in the Update() method to check how often to place the debug sphere
        private float timer = 0.0f;                             // This is the timer for placing the debug sphere

#if GDG_A
        private AIBase aibase;
#endif
        private Queue<FormationLocation> formationLocations;    // This is the queue used for keeping track the Leader's Location (FormationLocation)

        private Transform tr;                                   // Cache the Transform of the FormationLeader
        private Transform trFormation;                          // Cache the Transform of the Formation

        private Vector3 oldPosition;                            // The FormationLeader's previous position
        [SerializeField]
        private Vector3 velocity;                               // The FormationLeader's velocity
        [SerializeField]
        private float velocityMagnitude;                         // The FormationLeader's velocity magnitude
        private float velocityAverage;                           // The FormationLeader's average velocity from the star of the movement


        // Called from Formation.Awake() to initialise/cache formation and transforms
        public void SetFormation(Formation f)
        {
            formation = f;

            tr.position = formation.transform.position;
            tr.rotation = formation.transform.rotation;
        }

        // When recalculating the FormationGridPoint positions they'll need to FormationLeader's position since their positions are relative to it.
        public Vector3 GetPosition()
        {
            return tr.position;
        }

        private void Awake()
        {
            formationLocations = new Queue<FormationLocation>();    // Initialize the Queue

            tr = transform;                                         // Cache the object's transform for performance reasons
            oldPosition = tr.position;                              // Set the initial previous position to this position to prevent an initial velocity spike
        }


        void Start()
        {
#if GDG_A
            // Todo: this code can potentially fail if FormationLeader.Start() is called before Formation.Start()
            
            aibase = formation.GetAIBase();
            if (aibase == null)
            {
                Debug.LogError("<b>FormationLeader.Awake():</b> Could not find AIBase component.");
            }
#endif
            // This code can potentially fail if FormationLeader.Start() is called before Formation.Start()
            // trFormation = formation.transform;
        }


        void Update()
        {
            timer += Time.deltaTime;

            // Check if we have reached beyond formationCheckInterval.
            // Subtracting formationCheckInterval is more accurate over time than resetting to zero.
            if (timer > formationCheckInterval)
            {
                timer -= formationCheckInterval;

                // Place a red debug sphere at the FormationLeader's current position
                if (formation.debugMode && !formation.targetReached)
                {
                    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    sphere.transform.name = "Path";
                    sphere.transform.position = this.transform.position;
                    Utilities.SetSphereColor(sphere, Color.red);
                    Utilities.DisableSphereCollider(sphere);
                }
            }
        }

        private void FixedUpdate()
        {

            // Step 1: Place the FormationLeader at the exact position of the Formation (which is following along the A*Pathfinding OR Unity NavMesh path)
            if (trFormation)
            {
                tr.position = trFormation.position;
                tr.rotation = trFormation.rotation;
            }
            else
            {
                // This is a fix for the potential issue which arises when FormationLeader.Start() is called before Formation.Start()
                if(formation)
                {
                    trFormation = formation.transform;
                    return; // We'll just start one frame later
                }
                else
                {
                    // ERROR
                    Debug.LogError("<b>FormationLeader.FixedUpdate():</b> formation reference is zero.");
                }
            }


            if (formation.targetReached)
                return; // We arrived at destination so stop calculating


            float deltaTime = Time.deltaTime;

            // Step 2: Calculate the leader velocity based on distance travelled/deltaTime
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

            // Step 3: Calculate the leader average velocity based on distance travelled (lookup of Queue oldest location and its time stamp)
            if (formationLocations.Count > 1 && deltaTime > 0.0001f)
            {
                FormationLocation firstLocation = formationLocations.Peek();    // Look at the oldest Queue item without removing it

                velocityAverage = (tr.position - firstLocation.position).magnitude / (Time.time - firstLocation.time);
            }
            else velocityAverage = 0.0f;

            // Step 4: Queue every position of the FormationLeader
            FormationLocation formationLocation = new FormationLocation();
            formationLocation.position = tr.position;
            formationLocation.time = Time.time;
            formationLocation.rotationY = tr.rotation.eulerAngles.y;            // Store the rotation along the y-axis (up); this is where the FormationLeader is looking at
            if(formation.debugMode)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);   // Add a black sphere for this FormationLocation if debugMode==true
                sphere.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                sphere.transform.name = "LeaderP";
                sphere.transform.position = tr.position;
                Utilities.SetSphereColor(sphere, Color.black);
                Utilities.DisableSphereCollider(sphere);
                formationLocation.debugObject = sphere;
            }
            formationLocations.Enqueue(formationLocation);

            // Step 5: Now cleanup old queue item
            //
            // The number of FormationLocations we'll need to keep is is based on:
            //
            //  The FormationFollower furthest away (behind) from the FormationLeader = MaxYOffset calculated in Formation.AddFormationGridPoint
            //  Divide this by the fixedDeltaTime to get to the number of FormationLeader's FormationLocations stored before the last FormationFollower gets to that point.
            //
            //  However we'll potentially need more FormationLocations for FormationFollowers who get stuck. See FormationFollower.MoveTowardsGridPoint() step 2b

            float maxItemsF = -1 * formation.GetMaxYOffset();
            float fixedDeltaTime = Time.fixedDeltaTime;
            int numberOfItemsToStore = (int)(3.0f * maxItemsF / fixedDeltaTime); // Safety margin = store 3 x more
            
            // As soon as we have more items then delete the first one in the queue
            if(formationLocations.Count > numberOfItemsToStore)
            {
                FormationLocation lastLocation = formationLocations.Dequeue();
                if(lastLocation.debugObject!=null)
                {
                    Destroy(lastLocation.debugObject);
                }
                if(formation.debugMode)
                {
                    Debug.Log("Max Queue Items =" + numberOfItemsToStore + " items left after dequeue = "+formationLocations.Count);
                }
            }


            // Step 6: now update the positions of the FormationGridPoints based on the FormationLeader's position
            formation.UpdateFormationPositions(tr);

            // Step 7: store the FormationLeader's current position in the oldPosition for the next iteration
            oldPosition = tr.position;
        }

        // Point the object to look at a point
        void LookAtPoint(Vector3 point)
        {
            Vector3 target = point;
            target.y = tr.position.y;
            transform.LookAt(target);
        }

        public Vector3 GetVelocity()
        {
            return velocity;
        }

        public float GetVelocityMagnitude()
        {
            return velocityMagnitude;
        }

        public float GetAverageVelocity()
        {
            return velocityAverage;
        }


        // This method tries to find the FormationLocation stored deltaTime ago
        public FormationLocation FindClosestInTime(float deltaTime)
        {
            if (formationLocations != null)
            {
                FormationLocation result = null;

                float searchForTime = Time.time - deltaTime; // we're looking back in time from Now to -deltaTime

                foreach (FormationLocation fl in formationLocations.Reverse())
                {
                    if (fl.time <= searchForTime)
                    {
                        result = fl;
                        break;
                    }
                }
                return result;
            }
            else return null;
        }


        // This method tries to find the FormationLocation stored in the queue which is closet to a position.
        // This is used when a FormationFollower is stuck and needs to move towards the FormationLeader's path.
        // THIS IS A VERY COSTLY FUNCTION BECAUSE IT ITERATES THOUGH THE WHOLE QUEUE! TODO: OPTIMIZE
        public FormationLocation FindClosest(Vector3 position)
        {
            if (formationLocations != null)
            {
                FormationLocation result = null;

                float smallestDistance = 1000.0f;   // Start with a huge distance, any new value found in the queue should be smaller
                float currentDistance = 0.0f;

                foreach (FormationLocation fl in formationLocations.Reverse())
                {
                    currentDistance = Vector3.Distance(fl.position, position);  // Calculate the distance between position and FormationLocation in the queue

                    if (currentDistance < smallestDistance)
                    {
                        result = fl;                                            // This one is smaller then the previous smallest distance
                        smallestDistance = currentDistance;
                    }
                }
                return result;
            }
            else return null;
        }

    }

}
