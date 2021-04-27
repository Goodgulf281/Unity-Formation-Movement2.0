using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if GDG_A
using Pathfinding;
#else
using UnityEngine.AI;
#endif

namespace Goodgulf.Formation
{
    /*
     * This is a utility class to set a new target for the formation once the previous target was reached.
     * Press the T key once the target is reached to walk towards another target.
     *  
     */

    public class FormationNewTarget : MonoBehaviour
    {
        public Formation formation;
        public Transform newTarget;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyUp(KeyCode.T))
            {
                // Debug.Log("Set new target");

#if GDG_A
                // Set a new target
                AIDestinationSetter aIDestinationSetter = GetComponent<AIDestinationSetter>();
                if(aIDestinationSetter)
                    aIDestinationSetter.target = newTarget;

                // Clear the eventFired flag otherwise the target reached event will not fire
                AIPathWithEvents aIPathWithEvents = GetComponent<AIPathWithEvents>();
                if(aIPathWithEvents)
                    aIPathWithEvents.ClearEventFlag();
#else
                // Set a new target
                NavMeshAgent navMeshAgent = GetComponent<NavMeshAgent>();
                if (navMeshAgent)
                {
                    navMeshAgent.destination = newTarget.position;
                }

                // Clear the eventFired flag otherwise the target reached event will not fire
                FormationAgent formationAgent = GetComponent<FormationAgent>();
                if(formationAgent)
                    formationAgent.ClearEventFlag();

#endif

                formation.targetReached = false;
            }
        }
    }

}
