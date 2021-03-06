﻿using MIConvexHull;
using UnityEngine;

namespace UnityHelpers
{
    /// <summary>
    /// This script has been tested and works well with NWH's WheelController. Make sure to set the center of mass to be closer to the ground so the car doesn't keep flipping.
    /// </summary>
    public class CarPhysics : MonoBehaviour
    {
        public Rigidbody vehicleRigidbody;
        public HealthController vehicleHealth;

        [Space(10)]
        public ParticleSystem smoke;
        public ParticleSystem fire;

        [Space(10)]
        public AbstractWheel wheelFL;
        public AbstractWheel wheelFR;
        public AbstractWheel wheelRL;
        public AbstractWheel wheelRR;
        [Tooltip("This value sets how far from the center of the rear wheels to check for the ground. If the ground is not being touched, the car won't accelerate.")]
        public float wheelGroundDistance = 1;

        [Space(10)]
        public CarStats vehicleStats;
        public bool healthAffectsAcceleration;
        public float forwardSpeedMod, reverseSpeedMod;
        public float accelerationMod, brakelerationMod;
        public float gripMod;
        public float armorMod;

        public float currentForwardSpeed { get; private set; }
        public float currentTotalSpeed { get; private set; }
        private float strivedSpeed;
        private float prevCurrentSpeed, prevStrivedSpeed;

        [Space(10), Range(-1, 1)]
        public float gas;
        [Range(0, 1)]
        public float brake;
        [Range(-1, 1)]
        public float steer;

        public bool castRays;
        private RaycastHitInfo[] forwardRayResults, leftRayResults, rightRayResults, rearRayResults;
        //private Vector3 prevVelocity;

        public event OnHitHandler onHit;
        public delegate void OnHitHandler(CarPhysics caller, Collision collision);
        public event OnTriggerHandler onTrigger;
        public delegate void OnTriggerHandler(CarPhysics caller, Collider other);

        private void Awake()
        {
            if (vehicleHealth != null)
                vehicleHealth.onValueChanged.AddListener(OnHealthValueChanged);
            else
                Debug.LogWarning("VehiclePhysics(" + gameObject.name + "): No HealthController provided.");
        }
        private void Update()
        {
            if (castRays)
                CastAllRays();
        }
        void FixedUpdate()
        {
            float acceleration = GetAcceleration();
            float brakeleration = GetBrakeleration();
            float grip = GetGrip();

            Vector3 vehicleProjectedForward = vehicleRigidbody.transform.forward.Planar(Vector3.up);
            //float forwardPercent = vehicleRigidbody.velocity.PercentDirection(vehicleProjectedForward);
            currentTotalSpeed = vehicleRigidbody.velocity.magnitude;
            //currentForwardSpeed = currentTotalSpeed * forwardPercent;
            Vector3 planarVelocityVector = vehicleRigidbody.velocity.Planar(Vector3.up);
            float direction = Mathf.Sign(planarVelocityVector.normalized.PercentDirection(vehicleProjectedForward));
            currentForwardSpeed = planarVelocityVector.magnitude * direction;
            float currentSpeedPercent = Mathf.Abs(currentForwardSpeed / vehicleStats.maxForwardSpeed);

            float currentMaxWheelAngle = vehicleStats.wheelAngleCurve.Evaluate(currentSpeedPercent) * Mathf.Abs(vehicleStats.slowWheelAngle - vehicleStats.fastWheelAngle) + Mathf.Min(vehicleStats.slowWheelAngle, vehicleStats.fastWheelAngle);
            //Debug.Log(currentMaxWheelAngle);
            Quaternion wheelRotation = Quaternion.Euler(0, currentMaxWheelAngle * steer, 0);
            wheelFL.transform.localRotation = wheelRotation;
            wheelFR.transform.localRotation = wheelRotation;

            wheelFL.SetGrip(grip);
            wheelFR.SetGrip(grip);
            wheelRL.SetGrip(grip);
            wheelRR.SetGrip(grip);

            //if (wheelRL.IsGrounded(-wheelRL.up, wheelGroundDistance) || wheelRR.IsGrounded(-wheelRR.up, wheelGroundDistance))
            if (wheelRL.IsGrounded() || wheelRR.IsGrounded())
            {
                gas = Mathf.Clamp(gas, -1, 1);
                brake = Mathf.Clamp(brake, 0, 1);
                steer = Mathf.Clamp(steer, -1, 1);

                float gasAmount = gas * (acceleration + (gas > 0 && currentForwardSpeed < 0 || gas < 0 && currentForwardSpeed > 0 ? brakeleration : 0));
                float brakeAmount = Mathf.Clamp01(brake + Mathf.Abs(steer) * vehicleStats.percentSteerEffectsBrake.Evaluate(currentSpeedPercent)) * brakeleration * (currentForwardSpeed >= 0 ? -1 : 1);
                float totalAcceleration = gasAmount + brakeAmount;
                if (totalAcceleration > -float.Epsilon && totalAcceleration < float.Epsilon && !(strivedSpeed > -float.Epsilon && strivedSpeed < float.Epsilon))
                    totalAcceleration = vehicleStats.deceleration * (currentForwardSpeed >= 0 ? -1 : 1);
                float deltaSpeed = totalAcceleration * Time.fixedDeltaTime;

                SetStrivedSpeed(strivedSpeed + deltaSpeed);

                float nextCurrentSpeed = currentForwardSpeed + deltaSpeed;
                //If there is too high a difference between the strived speed and the expected next speed then set strived speed to the expected
                if (Mathf.Abs(strivedSpeed - nextCurrentSpeed) > 0)
                    SetStrivedSpeed(nextCurrentSpeed);

                vehicleRigidbody.AddForce(PhysicsHelpers.CalculateRequiredForceForSpeed(vehicleRigidbody.mass, currentForwardSpeed * vehicleRigidbody.transform.forward, strivedSpeed * vehicleRigidbody.transform.forward), ForceMode.Force);
            }

            prevCurrentSpeed = currentForwardSpeed;
            prevStrivedSpeed = strivedSpeed;
            //prevVelocity = vehicleRigidbody.velocity;
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (vehicleHealth != null)
            {
                armorMod = Mathf.Clamp(armorMod, -1, 1);
                float percentDamage = (collision.impulse.magnitude / vehicleRigidbody.mass / 100) * (1 - armorMod);
                vehicleHealth.HurtPercent(percentDamage);
                //Debug.Log(collision.gameObject.name + " impulse: " + collision.impulse + " percent damage: " + percentDamage);
            }

            onHit?.Invoke(this, collision);
        }
        private void OnTriggerEnter(Collider other)
        {
            //Debug.Log("Triggered " + other.name);
            onTrigger?.Invoke(this, other);
        }

        private void OnHealthValueChanged(float value)
        {
            if (value <= 0.5f)
            {
                smoke.Play();
                if (value <= 0.25f)
                    fire.Play();
                else
                    fire.Stop();
            }
            else
            {
                smoke.Stop();
                fire.Stop();
            }
            //Debug.Log("Vehicle health: " + value);
        }

        public float GetAcceleration()
        {
            float percent = 1;
            if (healthAffectsAcceleration && vehicleHealth != null)
                percent = Mathf.Clamp(vehicleHealth.value, 0, 0.5f) / 0.5f;

            return (vehicleStats.acceleration + accelerationMod) * percent;
        }
        public float GetBrakeleration()
        {
            return vehicleStats.brakeleration + brakelerationMod;
        }
        public float GetMaxForwardSpeed()
        {
            return vehicleStats.maxForwardSpeed + forwardSpeedMod;
        }
        public float GetMaxReverseSpeed()
        {
            return vehicleStats.maxReverseSpeed + reverseSpeedMod;
        }
        public float GetGrip()
        {
            return vehicleStats.grip + gripMod;
        }

        public float GetSpeedInKMH()
        {
            return currentForwardSpeed * MathHelpers.MPS_TO_KMH;
        }
        public float GetSpeedInMPH()
        {
            return currentForwardSpeed * MathHelpers.MPS_TO_MPH;
        }

        #region Ray Casting
        private void CastAllRays()
        {
            int forwardRayCount = MathHelpers.GetOddNumber((int)vehicleStats.forwardRays);
            if (forwardRayResults == null || forwardRayResults.Length != forwardRayCount)
                forwardRayResults = new RaycastHitInfo[forwardRayCount];

            int leftRayCount = MathHelpers.GetOddNumber((int)vehicleStats.leftRays);
            if (leftRayResults == null || leftRayResults.Length != leftRayCount)
                leftRayResults = new RaycastHitInfo[leftRayCount];

            int rightRayCount = MathHelpers.GetOddNumber((int)vehicleStats.rightRays);
            if (rightRayResults == null || rightRayResults.Length != rightRayCount)
                rightRayResults = new RaycastHitInfo[rightRayCount];

            int rearRayCount = MathHelpers.GetOddNumber((int)vehicleStats.rearRays);
            if (rearRayResults == null || rearRayResults.Length != rearRayCount)
                rearRayResults = new RaycastHitInfo[rearRayCount];

            CastRays(forwardRayResults, vehicleStats.forwardDistanceObstacleCheck, vehicleRigidbody.transform.forward, 0, 1);
            CastRays(leftRayResults, vehicleStats.leftDistanceObstacleCheck, -vehicleRigidbody.transform.right, -1, 0);
            CastRays(rightRayResults, vehicleStats.rightDistanceObstacleCheck, vehicleRigidbody.transform.right, 1, 0);
            CastRays(rearRayResults, vehicleStats.rearDistanceObstacleCheck, -vehicleRigidbody.transform.forward, 0, -1);
        }
        /// <summary>
        /// Casts rays in a direction and outputs the results to the given array.
        /// </summary>
        /// <param name="rayResults">The results of the casts</param>
        /// <param name="distanceObstacleCheck">How far to send out the rays</param>
        /// <param name="rayDirection">The direction the rays shoot</param>
        /// <param name="xBorder">Where on the vehicle border in the x direction to shoot rays from (-1 .. 1)</param>
        /// <param name="zBorder">Where on the vehicle border in the z direction to shoot rays from (-1 .. 1)</param>
        private void CastRays(RaycastHitInfo[] rayResults, float distanceObstacleCheck, Vector3 rayDirection, float xBorder, float zBorder)
        {
            float extentPercent = 0.9f;
            xBorder = Mathf.Clamp(xBorder, -extentPercent, extentPercent);
            zBorder = Mathf.Clamp(zBorder, -extentPercent, extentPercent);

            Vector3 vehicleRayStart;
            int rayCount = rayResults.Length;
            int extents = rayCount / 2;
            float step = 1f / rayCount;
            RaycastHit rayhitInfo;
            for (int i = 0; i < rayCount; i++)
            {
                int offsetIndex = i - extents;
                float currentOffset = extents != 0 ? (step * offsetIndex) / (step * extents) : 0;
                vehicleRayStart = vehicleRigidbody.transform.GetPointInBounds(new Vector3((Mathf.Abs(zBorder) > Mathf.Epsilon ? extentPercent : 0) * currentOffset + xBorder, -0.5f, (Mathf.Abs(xBorder) > Mathf.Epsilon ? extentPercent : 0) * currentOffset + zBorder));

                bool rayhit = Physics.Raycast(vehicleRayStart, rayDirection, out rayhitInfo, distanceObstacleCheck);
                rayResults[i] = new RaycastHitInfo() { hit = rayhit, info = rayhitInfo, rayStart = vehicleRayStart, rayStartDirection = rayDirection, rayMaxDistance = distanceObstacleCheck };

                Debug.DrawRay(vehicleRayStart, rayDirection * (rayhit ? rayhitInfo.distance : distanceObstacleCheck), rayhit ? Color.green : Color.red);
            }
        }

        private static RaycastHitInfo GetClosestHitInfo(RaycastHitInfo[] directionRayResults)
        {
            RaycastHitInfo bestRay = default;
            if (directionRayResults != null)
            {
                float closestRay = float.MaxValue;
                for (int i = 0; i < directionRayResults.Length; i++)
                {
                    var currentRay = directionRayResults[i];
                    if (currentRay.hit && currentRay.info.distance < closestRay)
                        bestRay = currentRay;
                }
            }
            return bestRay;
        }
        /// <summary>
        /// Gets the closest raycast hit info that was hit. If no rays were hit, then returns the default value.
        /// </summary>
        /// <returns>Raycast hit info.</returns>
        public RaycastHitInfo GetForwardHitInfo()
        {
            return GetClosestHitInfo(forwardRayResults);
        }
        /// <summary>
        /// Gets the closest raycast hit info that was hit. If no rays were hit, then returns the default value.
        /// </summary>
        /// <returns>Raycast hit info.</returns>
        public RaycastHitInfo GetLeftHitInfo()
        {
            return GetClosestHitInfo(leftRayResults);
        }
        /// <summary>
        /// Gets the closest raycast hit info that was hit. If no rays were hit, then returns the default value.
        /// </summary>
        /// <returns>Raycast hit info.</returns>
        public RaycastHitInfo GetRightHitInfo()
        {
            return GetClosestHitInfo(rightRayResults);
        }
        /// <summary>
        /// Gets the closest raycast hit info that was hit. If no rays were hit, then returns the default value.
        /// </summary>
        /// <returns>Raycast hit info.</returns>
        public RaycastHitInfo GetRearHitInfo()
        {
            return GetClosestHitInfo(rearRayResults);
        }
        /// <summary>
        /// Gives the angle between the raycast direction and the direction of the hit on the car's up axis.
        /// </summary>
        /// <param name="raycastInfo">The info of the raycast.</param>
        /// <returns>The signed angle between the two directions.</returns>
        public float GetHitAngle(RaycastHitInfo raycastInfo)
        {
            return vehicleRigidbody.position.SignedAngle(raycastInfo.info.point, raycastInfo.rayStartDirection, vehicleRigidbody.transform.up);
        }
        #endregion

        private void SetStrivedSpeed(float value)
        {
            float maxForwardSpeed = GetMaxForwardSpeed();
            float maxReverseSpeed = GetMaxReverseSpeed();
            strivedSpeed = Mathf.Clamp(value, -maxReverseSpeed, maxForwardSpeed);
        }

        public void Match(CarPhysics other)
        {
            if (other != null)
            {
                gas = other.gas;
                brake = other.brake;
                steer = other.steer;

                Teleport(other.vehicleRigidbody.transform.position, other.vehicleRigidbody.transform.rotation);
                vehicleRigidbody.velocity = other.vehicleRigidbody.velocity;
                vehicleRigidbody.angularVelocity = other.vehicleRigidbody.angularVelocity;
            }
        }
        public void SetVisible(bool onOff)
        {
            vehicleRigidbody.gameObject.SetActive(onOff);
        }
        public void Teleport(Vector3 position, Quaternion rotation, float speed = 0)
        {
            SetStrivedSpeed(speed);
            currentForwardSpeed = strivedSpeed;

            vehicleRigidbody.transform.position = position;
            vehicleRigidbody.transform.rotation = rotation;
            vehicleRigidbody.velocity = vehicleRigidbody.transform.forward * currentForwardSpeed;
            vehicleRigidbody.angularVelocity = Vector3.zero;
        }
    }

    public struct RaycastHitInfo
    {
        public bool hit;
        public Vector3 rayStart, rayStartDirection;
        public float rayMaxDistance;
        public RaycastHit info;
    }
}