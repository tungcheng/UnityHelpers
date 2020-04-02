﻿using UnityEngine;
using System.Collections.Generic;

namespace UnityHelpers
{
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicsTransform : MonoBehaviour
    {
        /// <summary>
        /// Only used for local position and local rotation calculations, does not actively anchor self unless anchorPosition/anchorRotation is greater than 0. Inspector values are in world coordinates, use scripting to access local position and local rotation.
        /// </summary>
        [Tooltip("Only used for local position and local rotation calculations, does not actively anchor self unless anchorPosition/anchorRotation is greater than 0. Inspector values are in world coordinates, use scripting to access local position and local rotation.")]
        public Transform parent;
        [Range(0, 1), Tooltip("0 means don't anchor at all, 1 means anchor completely")]
        public float anchorPositionPercent, anchorRotationPercent;
        public bool anchorByDistance;
        public float minAnchorDistance = 0.01f, maxAnchorDistance = 0.04f;
        private Vector3 originalLocalPosition;
        private Quaternion originalLocalRotation;
        [Range(0, float.MaxValue), Tooltip("The max distance the object can be from original local position, or if there is no parent then world position")]
        public float localLinearLimit = float.MaxValue;

        public ConfigurableJoint joint; //Temporary will move later

        /// <summary>
        /// Sets striveForPosition and striveForOrientation simultaneously.
        /// </summary>
        public bool strive { set { striveForPosition = value; striveForOrientation = value; } }
        /// <summary>
        /// Only counteracts gravity if rigidbody is affected by gravity and not kinematic
        /// </summary>
        [Tooltip("Only counteracts gravity if rigidbody is affected by gravity and is not kinematic")]
        public bool counteractGravity = true; //Suzan told me about PID controllers and how they work, so maybe in the future I can add the I to positional strivingness to counteract gravity/friction automatically.

        [Space(10)]
        public bool striveForPosition = true;
        public Vector3 position;
        /// <summary>
        /// Sets position relative to parent.
        /// </summary>
        public Vector3 localPosition
        {
            set
            {
                if (parent != null)
                    position = parent.TransformPoint(value);
                else
                    position = value;
            }
        }
        [Tooltip("The final multiplier or coefficient of the calculated force")]
        public float strength = 1;
        [Tooltip("In kg * m/s^2 (newtons)")]
        public float maxForce = 500;
        [Tooltip("If set to true, will dynamically set the strength value to be based on distance from given position")]
        public bool calculateStrengthByDistance = false;
        [Tooltip("This will be what the current distance is divided by to make a strength percentage (measured in meters)")]
        public float distanceDivisor = 5;
        public AnimationCurve strengthGraph = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
        [Tooltip("If set to true, will clamp the strength value to between 0 and 1")]
        public bool clampStrength = true;

        [Space(10)]
        public bool striveForOrientation = true;
        public Quaternion rotation = Quaternion.identity;
        /// <summary>
        /// Sets rotation relative to parent.
        /// </summary>
        public Quaternion localRotation
        {
            set
            {
                if (parent != null)
                    rotation = parent.TransformRotation(value);
                else
                    rotation = value;
            }
        }
        [Tooltip("Frequency is the speed of convergence. If damping is 1, frequency is the 1/time taken to reach ~95% of the target value. i.e. a frequency of 6 will bring you very close to your target within 1/6 seconds.")]
        public float frequency = 6;
        [Tooltip("damping = 1, the system is critically damped\ndamping is greater than 1 the system is over damped(sluggish)\ndamping is less than 1 the system is under damped(it will oscillate a little)")]
        public float damping = 1;

        [Space(10)]
        public bool striveForVelocity = false;
        public Vector3 velocity;
        public Vector3 localVelocity
        {
            set
            {
                if (parent != null)
                    velocity = parent.TransformDirection(value);
                else
                    velocity = value;
            }
        }
        public float velStrength = 1;
        [Tooltip("In kg * m/s^2 (newtons)")]
        public float velMaxForce = 500;

        public Rigidbody AffectedBody { get { if (_affectedBody == null) _affectedBody = GetComponent<Rigidbody>(); return _affectedBody; } }
        private Rigidbody _affectedBody;

        void Awake()
        {
            if (parent != null)
            {
                originalLocalPosition = parent.InverseTransformPoint(transform.position);
                originalLocalRotation = parent.InverseTransformRotation(transform.rotation);
            }
        }
        void FixedUpdate()
        {
            if (striveForPosition)
                AffectedBody.AddForce(CalculatePushForceVector(), ForceMode.Force);

            if (striveForVelocity)
            {
                Vector3 boneForce = AffectedBody.CalculateRequiredForceForSpeed(velocity, Time.deltaTime, velMaxForce) * velStrength;
                AffectedBody.AddForce(boneForce, ForceMode.Force);
            }

            if (striveForOrientation)
            {
                Quaternion strivedOrientation = Quaternion.Lerp(rotation, parent != null ? parent.TransformRotation(originalLocalRotation) : rotation, anchorRotationPercent);
                Vector3 rotationTorque = AffectedBody.CalculateRequiredTorque(strivedOrientation, frequency, damping);
                AffectedBody.AddTorque(rotationTorque);
            }

            if (counteractGravity && AffectedBody.useGravity && !AffectedBody.isKinematic)
                AffectedBody.AddForce(-Physics.gravity * AffectedBody.mass);
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, localLinearLimit);
        }

        public Vector3 CalculatePushForceVector()
        {
            Vector3 local = parent != null ? parent.TransformPoint(originalLocalPosition) : position; //Get original local position or strive position if no parent
            Vector3 strivedPosition = Vector3.Lerp(position, local, anchorPositionPercent); //Get interpolated position

            Vector3 delta = strivedPosition - local; //Get difference in position
            if (delta.sqrMagnitude > localLinearLimit * localLinearLimit) //If greater than limit
                strivedPosition = local + delta.normalized * localLinearLimit; //Set to limit

            if (anchorByDistance)
            {
                Vector3 strivedDifference = strivedPosition - local;
                float strivedDistance = strivedDifference.magnitude;
                if (strivedDistance > minAnchorDistance)
                {
                    float offsetDistance = Mathf.Abs(maxAnchorDistance - minAnchorDistance);
                    float lerpAmount = Mathf.Clamp01((strivedDistance - minAnchorDistance) / offsetDistance);
                    Vector3 minStrivePosition = local + strivedDifference.normalized * minAnchorDistance;
                    strivedPosition = Vector3.Lerp(strivedPosition, minStrivePosition, lerpAmount);
                }
            }

            if (calculateStrengthByDistance)
            {
                float currentDistance = (strivedPosition - AffectedBody.position).magnitude;
                strength = currentDistance;
                if (distanceDivisor != 0)
                    strength /= distanceDivisor;

                strength = strengthGraph.Evaluate(strength);
            }

            if (clampStrength)
                strength = Mathf.Clamp01(strength);

            return AffectedBody.CalculateRequiredForceForPosition(strivedPosition, Time.fixedDeltaTime, maxForce) * strength;
        }
    }
}