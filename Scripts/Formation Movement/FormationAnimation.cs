using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Goodgulf.Formation
{
    /*
     * This is the generic Formation Animation script which can be used wit any of the pathfinding solutions.
     * Based on the velocity of the objects in the formation it changes the animator variables.
     * Also it randomizes the animations to prevent a synced animation for all followers.
     * Next it starts and stops the "marching" sound depending on the movement of the object it is attached to.
     * 
     * 1. Attach this script to the prefabs / objects which are part of the formation.
     * 2. Make sure there's an audio source attached to the prefab / object.
     * 3. Setup the Animation Controller with velx, vely, move variables as per https://docs.unity3d.com/Manual/nav-CouplingAnimationAndNavigation.html
     */


    [RequireComponent(typeof(Animator))]
    public class FormationAnimation : MonoBehaviour
    {
        protected Animator  anim;                                   // Reference to the Animator
        [Header("Movement")]
        private Transform   tr;                                     // Reference to the objects transform (for performance reasons)
        private Vector3     oldPosition;                            // The object's position in the previous FixedUpdate call
        [SerializeField]
        private Vector3     velocity3D;                             // The object's velocity in 3D
        private Vector2     velocity2D = Vector2.zero;              // The object's velocity on the horizontal plane
        [SerializeField]
        private float       velocityMagnitude;                      // The magnitude of the velocity

        private Vector2     smoothDeltaPosition = Vector2.zero;

        [Header("Animation")]
        [SerializeField]
        private bool randomizeAnimation = true;                     // Enable randomized animation if set to true
        private bool animations = false;                            // Set to true if animations are running

        [Header("Sound")]
        [SerializeField] 
        private bool hasSound = true;                               // Enable if "marching" sound is available for the object
        [SerializeField] 
        private bool randomStartSound = false;                      // Set to true to enable a randomized sound start

        protected   AudioSource audioSource;                        // Cached AudioSource component
        private     bool        audioIsPlaying = false;

        private void Awake()
        {
            tr          = transform;                                // Cache the object's transform
            oldPosition = tr.position;                              // Start with oldPostion = current position

            anim = GetComponent<Animator>();                        // Cache the Animator component
            if (anim == null)
            {
                Debug.LogError("FormationAnimation.Awake(): no animator controller found.");
                return;
            }

            if (hasSound)
            {
                audioSource = GetComponent<AudioSource>();          // Cache the AudioSource component
                if (audioSource == null)
                {
                    Debug.LogError("FormationAnimation.Awake(): AudioSource component missing");
                    return;
                }
            }

        }

        void Start()
        {
            Random.InitState((int)System.DateTime.Now.Ticks);
            StartAnimations();
        }

        private void FixedUpdate()
        {
            float deltaTime = Time.deltaTime;

            // Calculate the leader velocity based on distance travelled/deltaTime
            if (deltaTime > 0.0001f)
            {
                velocity3D = (tr.position - oldPosition) / deltaTime;
                velocityMagnitude = velocity3D.magnitude;
            }
            else
            {
                // Prevent a division by zero in case deltaTime is very small or zero for some reason.
                velocity3D = Vector3.zero;
                velocityMagnitude = 0;
            }

            UpdateAnimations();             // Update the animations in every FixedUpdate call

            oldPosition = tr.position;      // Store the current position as the old position for the next iteration
        }

        /*
         * This code is based on https://docs.unity3d.com/Manual/nav-CouplingAnimationAndNavigation.html
         * 
         * It basically calculates the Animator variables for:
         * 
         * - Velocity in x direction (left, right)
         * - Velocity in y direction (forwards, backwards)
         * - Are we even moving?
         * 
         * The blend tree in the Animator combines the walking, running into a smooth movement based on the velocity in x and y directions
         * 
         */
        protected void UpdateAnimations()
        {

            Vector3 worldDeltaPosition = tr.position - oldPosition;

            // Map 'worldDeltaPosition' to local space
            float dx = Vector3.Dot(transform.right, worldDeltaPosition);
            float dy = Vector3.Dot(transform.forward, worldDeltaPosition);
            Vector2 deltaPosition = new Vector2(dx, dy);

            // Low-pass filter the deltaMove
            float smooth = Mathf.Min(1.0f, Time.deltaTime / 0.15f);
            smoothDeltaPosition = Vector2.Lerp(smoothDeltaPosition, deltaPosition, smooth);


            // Update velocity if delta time is safe
            if (Time.deltaTime > 1e-5f)
                velocity2D = smoothDeltaPosition / Time.deltaTime;

            bool shouldMove = velocity2D.magnitude > 0.5f;

            // Update animation parameters
            anim.SetBool("move", shouldMove);
            anim.SetFloat("velx", velocity2D.x);
            anim.SetFloat("vely", velocity2D.y);

            // If we're moving we should also make a sound
            SetSoundState(shouldMove);
        }


        public virtual void SetSoundState(bool state)
        {
            // Do nothing if sounds is playing and is asked to play
            if (state == true && audioIsPlaying)
                return;

            if (hasSound && audioSource!= null)
            {
                if (state)
                {
                    if (!audioSource.isPlaying)
                    {
                        // Start playing sounde
                        if (randomStartSound)
                        {
                            // Randomized start of sound
                            audioSource.PlayDelayed(Random.Range(0.0f, 0.2f));
                        }
                        else
                        {
                            audioSource.Play();
                        }
                    }
                    audioSource.mute = false;
                    audioIsPlaying = true;
                }
                else
                {
                    // Stop playing sound
                    audioSource.Stop();
                    audioIsPlaying = false;
                }
            }
        }

        // Enable the animations
        public virtual void StartAnimations()
        {
            animations = true;

            if (randomizeAnimation)
            {
                if (anim)
                {
                    anim.Play(0, -1, Random.value);
                }
            }
            else
            {
                if (anim)
                {
                    anim.Play(0);
                }
            }
        }

        public bool GetAnimationsFlag()
        {
            return animations;
        }

        // Disable the animations and stop them immediately
        public virtual void IdleAnimations()
        {
            anim.SetBool("move", false);
            anim.SetFloat("velx", 0f);
            anim.SetFloat("vely", 0f);

            anim.Rebind();
            // Randomize the Idle animation, doesn't work at the moment
            anim.Play("Idle", -1, Random.value);

            // Also stop the sound playing
            animations = false;
            SetSoundState(false);
        }

    }

}
