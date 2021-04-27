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
     * A FormationBase class in case we want to move methods and properties into a common base class.
     * Currently not used.
     */

    public class FormationBase : MonoBehaviour
    {

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
