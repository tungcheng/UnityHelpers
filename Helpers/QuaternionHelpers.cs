﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityHelpers
{
    public static class QuaternionHelpers
    {
        /// <summary>
        /// Source: https://stackoverflow.com/questions/3684269/component-of-a-quaternion-rotation-around-an-axis
        /// Gets angle of quaternion only along the specified axis
        /// </summary>
        /// <param name="rot">The rotation quaternion</param>
        /// <param name="axis">The axis to be polled</param>
        /// <param name="axisNormal">The orthogonal vector of the axis</param>
        /// <returns>The angle around the given axis</returns>
        public static float PollAxisAngle(this Quaternion rot, Vector3 axis, Vector3 axisNormal)
        {
            axis.Normalize();

            Vector3 transformed = rot * axisNormal;

            // Project transformed vector onto plane
            Vector3 flattened = transformed - (Vector3.Dot(transformed, axis) * axis);
            flattened.Normalize();

            // Get angle between original vector and projected transform to get angle around normal
            float a = (float)Mathf.Acos(Vector3.Dot(axisNormal, flattened));

            return a;
        }

        /// <summary>
        /// If the quaternion is going the long way around the axis, then this function will
        /// find the complementary shorter angle on the axis
        /// </summary>
        /// <param name="value">The original quaternion value</param>
        /// <returns>The shortened quaternion value</returns>
        public static Quaternion Shorten(this Quaternion value)
        {
            //Source: https://answers.unity.com/questions/147712/what-is-affected-by-the-w-in-quaternionxyzw.html
            //"If w is -1 the quaternion defines +/-2pi rotation angle around an undefined axis"
            //So by doing this we check to see if that is true, and if so turn it the other way around
            if (value.w < 0)
            {
                value.x = -value.x;
                value.y = -value.y;
                value.z = -value.z;
                value.w = -value.w;
            }
            return value;
        }

        /// <summary>
        /// Transform rotation from local space to world space.
        /// </summary>
        /// <param name="transform">The anchor.</param>
        /// <param name="localRotation">The rotation in the anchor's local space.</param>
        /// <returns>The rotation in world space.</returns>
        public static Quaternion TransformRotation(this Transform transform, Quaternion localRotation)
        {
            return transform.rotation * localRotation;
        }
        /// <summary>
        /// Transform rotation from world space to local space.
        /// </summary>
        /// <param name="transform">The anchor.</param>
        /// <param name="worldRotation">The rotation in world space.</param>
        /// <returns>The rotation in the anchor's local space.</returns>
        public static Quaternion InverseTransformRotation(this Transform transform, Quaternion worldRotation)
        {
            return Quaternion.Inverse(transform.rotation) * worldRotation;
        }
        /// <summary>
        /// Determines whether the quaternion is safe for interpolation or use with transform.rotation.
        /// </summary>
        /// <returns><c>false</c> if using the quaternion in Quaternion.Lerp() will result in an error (eg. NaN values or zero-length quaternion).</returns>
        /// <param name="quaternion">Quaternion.</param>
        public static bool IsValid(this Quaternion quaternion)
        {
            bool isNaN = float.IsNaN(quaternion.x + quaternion.y + quaternion.z + quaternion.w);

            bool isZero = quaternion.x == 0 && quaternion.y == 0 && quaternion.z == 0 && quaternion.w == 0;

            return !(isNaN || isZero);
        }
        /// <summary>
        /// Turns all NaN values to 0
        /// </summary>
        /// <param name="quaternion">The quaternion in question</param>
        /// <returns>The fixed quaternion</returns>
        public static Quaternion FixNaN(this Quaternion quaternion)
        {
            if (float.IsNaN(quaternion.x))
                quaternion.x = 0;
            if (float.IsNaN(quaternion.y))
                quaternion.y = 0;
            if (float.IsNaN(quaternion.z))
                quaternion.z = 0;
            if (float.IsNaN(quaternion.w))
                quaternion.w = 0;

            return quaternion;
        }
        /// <summary>
        /// Sets the x, y, z, and w values' decimal places
        /// </summary>
        /// <param name="quaternion">The original quaternion rotation</param>
        /// <param name="places">Number of decimal places to keep</param>
        /// <returns>The quaternion with it's values' decimal places adjusted</returns>
        public static Quaternion SetDecimalPlaces(this Quaternion quaternion, uint places)
        {
            return new Quaternion(MathHelpers.SetDecimalPlaces(quaternion.x, places), MathHelpers.SetDecimalPlaces(quaternion.y, places), MathHelpers.SetDecimalPlaces(quaternion.z, places), MathHelpers.SetDecimalPlaces(quaternion.w, places));
        }
        /// <summary>
        /// Gets the average difference between all the quaternion's values.
        /// ((x1-x2)+(y1-y2)+(z1-z2)+(w1-w2))/4
        /// If you'd like to compare two rotations Quaternion.Angle might be good for you.
        /// </summary>
        /// <param name="rot1">The first rotation quaternion</param>
        /// <param name="rot2">The second rotation quaternion</param>
        /// <returns>The average difference</returns>
        public static float Difference(this Quaternion rot1, Quaternion rot2)
        {
            return ((rot1.x - rot2.x) + (rot1.y - rot2.y) + (rot1.z - rot2.z) + (rot1.w - rot2.w)) / 4;
        }
        /// <summary>
        /// Adds two quaternions together value for value
        /// </summary>
        /// <param name="rot1">The first rotation quaternion</param>
        /// <param name="rot2">The second rotation quaternion</param>
        /// <returns>A quaternion whose values are the sum of the given two's values</returns>
        public static Quaternion Add(this Quaternion rot1, Quaternion rot2)
        {
            return new Quaternion(rot1.x + rot2.x, rot1.y + rot2.y, rot1.z + rot2.z, rot1.w + rot2.w);
        }
        /// <summary>
        /// Divides all the quaternion's values by another value
        /// </summary>
        /// <param name="quaternion">The original quaternion</param>
        /// <param name="value">The value to divide by</param>
        /// <returns>The result quaternion</returns>
        public static Quaternion DivideBy(this Quaternion quaternion, float value)
        {
            return new Quaternion(quaternion.x / value, quaternion.y / value, quaternion.z / value, quaternion.w / value);
        }
        /// <summary>
        /// Adds all the quaternions then divides the final values by number of quaternions given
        /// </summary>
        /// <param name="quaternions">A list of quaternions to average</param>
        /// <returns>An averaged quaternion</returns>
        public static Quaternion Average(this IEnumerable<Quaternion> quaternions)
        {
            return quaternions.Aggregate(Add).DivideBy(quaternions.Count());
        }
        /// <summary>
        /// Adds all the quaternions then divides the final values by number of quaternions given
        /// </summary>
        /// <param name="quaternions">A list of quaternions to average</param>
        /// <returns>An averaged quaternion</returns>
        public static Quaternion Average(params Quaternion[] quaternions)
        {
            return quaternions.Average();
        }
    }
}