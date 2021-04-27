using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Goodgulf.Utilities
{
    public class CameraUtility : MonoBehaviour
    {
        public Transform pointAt;
        public bool pointAtStart = true;
        public bool trackPointAt = true;

        void Start()
        {
            if(pointAtStart)
                transform.LookAt(pointAt);
        }

        void Update()
        {
            if(trackPointAt)
                transform.LookAt(pointAt);
        }
    }
}
