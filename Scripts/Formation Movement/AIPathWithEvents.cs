using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if GDG_A
using Pathfinding;

/* * 
 * This is a simple extension of the A*Pathfinding AIPath class to include an event when the target has been reached.
 * Add this to the Formation class instance instead of the regular AIPath.
 * 
 * Note: you can easily make tis work for the other A*Pathfinding classes by changing the : AIPath line below to the appropriate class.
 */

namespace Goodgulf.Formation
{

    public class AIPathWithEvents : AIPath
    {
        private bool eventFired = false;

        public UnityEvent onTargetReached;

        public override void OnTargetReached()
        {
            if(!eventFired)
                onTargetReached.Invoke();
            eventFired = true;
        }

        public void ClearEventFlag()
        {
            eventFired = false;
        }

    }

}
#else
    // Just a stub to prevent compilation errors if you don't have A* Pathfinding imported

namespace Goodgulf.Formation
{
    public class AIPathWithEvents : MonoBehaviour
    {
    }
}
#endif




