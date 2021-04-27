using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Goodgulf.Formation
{

    /*
     * This is a simple script which checks the NavMeshAgent's position and when it has reached its destination it invokes an event.
     * 
     */

    [RequireComponent(typeof(NavMeshAgent))]
    public class FormationAgent : MonoBehaviour
    {
        [SerializeField]
        private Transform destination;          // This is where the NavMeshAgent is heading towards.
        public UnityEvent onTargetReached;      // The event to be invoked when we reach the destination.
        private NavMeshAgent formationAgent;    // Store the reference to the Navmesh agent for performance reasons.

        private bool eventFired = false;

        void Start()
        {
            // Get the destination set in the NavMeshAgent then store it in the destination property.

            formationAgent = GetComponent<NavMeshAgent>();
            if (formationAgent)
            {
                formationAgent.destination = destination.position;
            }
            else Debug.LogError("FormationAgent.Start(): Could not find NavMeshAgent on Formation");
        }

        private void Update()
        {
            if (!formationAgent.pathPending)
            {
                if (formationAgent.remainingDistance <= formationAgent.stoppingDistance)
                {
                    if (!formationAgent.hasPath || formationAgent.velocity.sqrMagnitude == 0f)
                    {
                        // We have reached the destination so invoke the event:
                        if (!eventFired) 
                        {
                            onTargetReached.Invoke();
                            eventFired = true;          // make sure it only fires once
                        }
                    }
                }
            }
        }
        public void ClearEventFlag()
        {
            eventFired = false;
        }

    }

}
