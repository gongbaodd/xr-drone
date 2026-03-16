using UnityEngine;


namespace YueUltimateDronePhysics
{
    public class YueDroneSound : MonoBehaviour
    {
        [SerializeField]
        private float pitchFactor = 1f;
        [SerializeField]
        private float volumeFactor = 1f;

        [SerializeField]
        private float pitchOffset = 1f;
        [SerializeField]
        private float volumeOffset = 1f;

        [SerializeField]
        private AudioClip motorSound;
        [SerializeField]
        private YueDronePhysics dronePhysics;
        [SerializeField]
        private AudioSource source;

        private void Start()
        {
            if(!source)
                source = GetComponent<AudioSource>();

            if(!dronePhysics)
                dronePhysics = GetComponent<YueDronePhysics>();
        }
        void Update()
        {
            if(dronePhysics.armed)
            {
                if(!source.isPlaying)
                    source.PlayOneShot(motorSound);

                source.pitch = pitchOffset + (dronePhysics.appliedForce.magnitude / dronePhysics.physicsConfig.thrust) * pitchFactor;
                source.volume = volumeOffset + (dronePhysics.appliedForce.magnitude / dronePhysics.physicsConfig.thrust) * volumeFactor;
            }
        }
    }
}