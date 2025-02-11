using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OWL
{
    [CreateAssetMenu(fileName = "Base P1_Movement Stats", menuName = "P1_Movement Stat")]

    public class PlayerMovemement_Stats : ScriptableObject
    {
        [Header("Ground/Collision Checks")]
        public LayerMask Groundlayer;
        public LayerMask WallLayer;
        public float HeadDetectionRayLength = 0.02f;
        public float GroundDetectionRayLength = 0.02f;
        [Range(0f, 1f)] public float HeadWidth = 0.75f;

        [Header("Walking")]
        [Range(1f, 100f)] public float MaxWalkSpeed = 12.5f;
        [Range(0.25f, 50f)] public float AirDeceleration = 5f;
        [Range(0.25f, 50f)] public float AirAcceleration = 5f;
        [Range(0.25f, 50f)] public float GroundAcceleration = 5f;
        [Range(0.25f, 50f)] public float GroundDeceleration = 20f;

        [Header("Running")]
        [Range(1f, 100f)] public float MaxRunSpeed = 20f;

        [Header("Jumping")]
        public float jumpHeight = 6.5f;
        public float maxFallSpeed = 25f;
        public float timeTillJumpApex = 0.35f;
        [Range(1, 5)] public int numberOfJumpsAllowed = 2;
        [Range(0.01f, 5f)] public float gravityOnReleaseMultiplier = 2f;
        [Range(1f, 1.1f)] public float jumpHeightCompensationFactor = 1.05f;

        [Header("Jump Cut")]
        [Range(0.02f, 0.3f)] public float timeForUpwardsCancel = 0.27f;

        [Header("Jump Apex")]
        [Range(0.5f, 1f)] public float apexThreshold = 0.95f;
        [Range(0.01f, 1f)] public float apexHangTime = 0.075f;

        [Header("Jump Buffer")]
        [Range(0f, 1f)] public float jumpBufferTime = 0.125f;

        [Header("Jump Coyote Time")]
        [Range(0f, 1f)] public float jumpCoyoteTime = 0.1f;

        [Header("Debug")]
        public bool debugShowIsgroundedBox;
        public bool debugShowHeadBumpBox;

        [Header("Jump Visualisation Tool")]
        public bool showWalkJumpArc = false;
        public bool showRunJumpArc = false;
        public bool stopOnCollision = true;
        public bool drawRight = true;
        [Range(5, 100)] public int arcResolution = 20;
        [Range(0, 500)] public int visualisationSteps = 90;

        public float Gravity { get; private set; }
        public float InitialJumpVelocity { get; private set; }
        public float adjustedJumpHeight { get; private set; }

        private void OnValidate()
        {
            CalculateValues();
        }
        private void OnEnable()
        {
            CalculateValues();
        }
        private void CalculateValues()
        {
            adjustedJumpHeight = jumpHeight * jumpHeightCompensationFactor;
            Gravity = -(2f * adjustedJumpHeight) / Mathf.Pow(timeTillJumpApex, 2f);
            InitialJumpVelocity = Mathf.Abs(Gravity) * timeTillJumpApex;
        }
    }
}

