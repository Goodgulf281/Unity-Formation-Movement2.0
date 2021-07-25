using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Goodgulf.Formation
{

    /*
    * The FormationAddFollwerTrigger is a utility class which can be used to trigger a ChangeFormation call
    * and add a follwer when the formation enters the trigger collider. Note: at least one of the formation components
    * (for example: FormationLeader or FormationFollower) needs to have a Collider and a RigidBody.
    * You can set the IsKinematic of the Rigidbody to true.
    *  
    * Add this to a collider which has been set as a Trigger=true and the below OnTriggerEnter event gets called
    * which will result in the formation being changed to the changeToType shape.
    */

    public class FormationAddFollowerTrigger : MonoBehaviour
    {
        [Header("Formation")]
        [Tooltip("This is the reference to the formation object in the hierarchy.")]

        public Formation formation;

        public FormationTypes changeToType;         // What will be the new Formation Grid Shape?
        public float changeToGridDistance = 1.5f;   // What will be the new GridDistance?
        public int changeToSecondParameter = 1;     // What will be the new secondParameter?

        [Header("New Follower")]
        [Tooltip("This is the prefab which will be instantiated when the trigger is entered.")]
        public GameObject followerPrefab;

        private bool triggered = false;

        private void OnTriggerEnter(Collider other)
        {
            if (formation.debugMode)
                Debug.Log("FormationChangeTrigger.FormationAddFollowerTrigger(): triggered");

            if (formation && !triggered)
            {
                triggered = true; // Only trigger once

                GameObject go = Instantiate(followerPrefab, this.transform.position, Quaternion.identity); // Instantiate the follower
                FormationFollower follow = go.GetComponent<FormationFollower>();                                 // Get its FormationFollower object

                if (follow)
                {
                    follow.enabled = true;
                    follow.lookedAtGridPoint = true;
                    follow.SetFormation(formation);

                    // AddFormationObject is a newly added function which adds a follower to the formation.
                    // In this example code a prefab is instantiated however an existing object can be added too.
                    formation.AddFormationObject(go);
                    formation.ChangeFormation(formation, changeToType, changeToGridDistance, changeToSecondParameter);

                }
                else Debug.LogError("FormationChangeTrigger.FormationAddFollowerTrigger(): object does not have a FormationFollower component.");
            }

        }
    }
}
