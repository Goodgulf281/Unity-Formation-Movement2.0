using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Goodgulf.Formation
{

   /*
    * The FormationChangeTrigger is a utility class which can be used to trigger a ChangeFormation call
    * when the formation enters the trigger collider. Note: at least one of the formation components
    * (for example: FormationLeader or FormationFollower) needs to have a Collider and a RigidBody.
    * You can set the IsKinematic of the Rigidbody to true.
    *  
    * Add this to a collider which has been set as a Trigger=true and the below OnTriggerEnter event gets called
    * which will result in the formation being changed to the changeToType shape.
    */
    public class FormationChangeTrigger : MonoBehaviour
    {
        public Formation formation;
        public FormationTypes changeToType;         // What will be the new Formation Grid Shape?
        public float changeToGridDistance = 1.5f;   // What will be the new GridDistance?
        public int changeToSecondParameter = 1;     // What will be the new secondParameter?

        private bool triggered = false;

        
        private void OnTriggerEnter(Collider other)
        {
            if(formation.debugMode)
                Debug.Log("FormationChangeTrigger.OnTriggerEnter(): triggered");

            if (formation && !triggered)
            {
                triggered = true; // Only trigger once

                formation.ChangeFormation(formation, changeToType, changeToGridDistance, changeToSecondParameter);
            }

        }
    }

}
