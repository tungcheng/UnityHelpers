﻿using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace UnityHelpers
{
    public static class BoundsHelpers
    {
        /// <summary>
        /// Gets all the renderers or colliders in the transform and gets their total bounds.
        /// </summary>
        /// <param name="transform">The root transform of the object</param>
        /// <param name="worldSpace">An option to return the bounds' center to be relative or absolute</param>
        /// <param name="fromColliders">If set to true, will get the total bounds from colliders rather than renderers</param>
        /// <param name="includeDisabled">If set to true includes renderers or colliders that are on disabled gameobjects</param>
        /// <returns>A bounds that encapsulates the entire model</returns>
        [System.Obsolete("Please use the new GetTotalBounds functions which properly get local/world space bounds")]
        public static Bounds GetTotalBounds(this Transform transform, bool worldSpace = true, bool fromColliders = false, bool includeDisabled = false)
        {
            return GetTotalBounds(transform, ~0, worldSpace, fromColliders, includeDisabled);
        }
        /// <summary>
        /// Gets all the renderers or colliders in the transform and gets their total bounds.
        /// </summary>
        /// <param name="transform">The root transform of the object</param>
        /// <param name="layer">The layers to include in bounds calculation</param>
        /// <param name="worldSpace">An option to return the bounds' center to be relative or absolute</param>
        /// <param name="fromColliders">If set to true, will get the total bounds from colliders rather than renderers</param>
        /// <param name="includeDisabled">If set to true includes renderers or colliders that are on disabled gameobjects</param>
        /// <returns>A bounds that encapsulates the entire model</returns>
        [System.Obsolete("Please use the new GetTotalBounds functions which properly get local/world space bounds")]
        public static Bounds GetTotalBounds(this Transform transform, LayerMask layer, bool worldSpace = true, bool fromColliders = false, bool includeDisabled = false)
        {
            Bounds totalBounds = new Bounds();

            List<Bounds> innerBounds = new List<Bounds>();
            if (fromColliders)
            {
                foreach (var collider in transform.GetComponentsInChildren<Collider>(true))
                    if (((1 << collider.gameObject.layer) & layer.value) != 0 && (includeDisabled || collider.gameObject.activeSelf))
                        innerBounds.Add(collider.bounds);
            }
            else
            {
                foreach (var renderer in transform.GetComponentsInChildren<Renderer>(true))
                    if (((1 << renderer.gameObject.layer) & layer.value) != 0 && (includeDisabled || renderer.gameObject.activeSelf))
                        innerBounds.Add(renderer.bounds);
            }

            if (innerBounds.Count > 0)
                totalBounds = Combine(innerBounds.ToArray());
            else
                totalBounds = new Bounds(transform.position, Vector3.zero);

            if (!worldSpace)
                totalBounds.center = transform.InverseTransformPoint(totalBounds.center);

            return totalBounds;
        }
        /// <summary>
        /// Gets only the current transform's renderer's bounds.
        /// </summary>
        /// <param name="transform">The transform of the object</param>
        /// <param name="worldSpace">An option to return the bounds' center to be relative or absolute</param>
        /// <returns>A bounds that encapsulates only the given transform's model</returns>
        [System.Obsolete("Please use the new GetBounds functions which properly gets local/world space bounds")]
        public static Bounds GetBounds(this Transform transform, bool worldSpace = true)
        {
            Bounds singleBounds = new Bounds();

            Renderer renderer = transform.GetComponent<Renderer>();
            if (renderer != null)
                singleBounds = renderer.bounds;
            if (!worldSpace)
                singleBounds.center = transform.InverseTransformPoint(singleBounds.center);

            return singleBounds;
        }

        /// <summary>
        /// Gets the total bounds of an object including all it's children
        /// </summary>
        /// <param name="root">The root transform of the object</param>
        /// <param name="space">An option to return the bounds based on local or world space (local space would be relative to the root transform)</param>
        /// <param name="fromColliders">If set to true, will get the total bounds from colliders rather than renderers</param>
        /// <param name="includeDisabled">Includes disabled gameobjects if set to true</param>
        /// <returns>A bounds that encapsulates the entire model</returns>
        public static Bounds GetTotalBounds(this Transform root, Space space, bool fromColliders = false, bool includeDisabled = false)
        {
            return root.GetTotalBounds(space, ~0, fromColliders, includeDisabled);
        }
        /// <summary>
        /// Gets the total bounds of an object including all it's children
        /// </summary>
        /// <param name="root">The root transform of the object</param>
        /// <param name="space">An option to return the bounds based on local or world space (local space would be relative to the root transform)</param>
        /// <param name="layers">The layers to include in bounds calculation</param>
        /// <param name="fromColliders">If set to true, will get the total bounds from colliders rather than renderers</param>
        /// <param name="includeDisabled">Includes disabled gameobjects if set to true</param>
        /// <returns>A bounds that encapsulates the entire model</returns>
        public static Bounds GetTotalBounds(this Transform root, Space space, LayerMask layers, bool fromColliders = false, bool includeDisabled = false)
        {
            Bounds totalBounds = default;

            IEnumerable<GameObject> boundedObjects;
            if (fromColliders)
                boundedObjects = root.GetComponentsInChildren<Collider>(true).Select(collider => collider.gameObject);
            else
                boundedObjects = root.GetComponentsInChildren<Renderer>(true).Select(renderer => renderer.gameObject);
                
            List<Bounds> innerBounds = new List<Bounds>();
            foreach (var boundedObject in boundedObjects)
                if (((1 << boundedObject.layer) & layers.value) != 0 && (includeDisabled || boundedObject.activeSelf))
                {
                    var currentBounds = boundedObject.transform.GetBounds(space, fromColliders);
                    if (space == Space.Self && boundedObject.transform != root)
                    {
                        var adjustedMin = boundedObject.transform.TransformPointToAnotherSpace(root, currentBounds.min);
                        var adjustedMax = boundedObject.transform.TransformPointToAnotherSpace(root, currentBounds.max);
                        var adjustedCenter = boundedObject.transform.TransformPointToAnotherSpace(root, currentBounds.center);
                        currentBounds = new Bounds(adjustedCenter, Vector3.zero);
                        currentBounds.SetMinMax(adjustedMin, adjustedMax);
                    }
                    innerBounds.Add(currentBounds);
                }
            
            if (innerBounds.Count > 0)
                totalBounds = Combine(innerBounds.ToArray());
            else
                totalBounds = new Bounds(space == Space.Self ? root.localPosition : root.position, Vector3.zero);

            return totalBounds;
        }
        /// <summary>
        /// Gets only the current transform's bounds.
        /// </summary>
        /// <param name="transform">The transform of the object</param>
        /// <param name="space">An option to return the bounds based on local or world space</param>
        /// <param name="useCollider">Uses the collider instead of the renderer to calculate the bounds of the object</param>
        /// <returns>A bounds that encapsulates only the given transform's model</returns>
        public static Bounds GetBounds(this Transform transform, Space space, bool useCollider = false)
        {
            Bounds singleBounds = default;

            if (useCollider)
            {
                Collider collider = transform.GetComponent<Collider>();
                if (collider != null)
                {
                    var localBounds = collider.GetLocalBounds();
                    if (space == Space.World)
                        localBounds = transform.TransformBounds(localBounds);
                    singleBounds = localBounds;
                }
            }
            else
            {
                Renderer renderer = transform.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (space == Space.World)
                        singleBounds = renderer.bounds;
                    else
                    {
                        if (renderer is SpriteRenderer)
                            singleBounds = ((SpriteRenderer)renderer).sprite.bounds;
                        else if (renderer is SkinnedMeshRenderer)
                            singleBounds = ((SkinnedMeshRenderer)renderer).localBounds;
                        else
                        {
                            MeshFilter meshFilter = transform.GetComponent<MeshFilter>();
                            if (meshFilter != null)
                                singleBounds = meshFilter.sharedMesh.bounds;
                        }
                    }
                }
            }

            return singleBounds;
        }
        /// <summary>
        /// Gets the local bounds of the collider by figuring out which type it is (supported types: BoxCollider, SphereCollider, CapsuleCollider, and MeshCollider)
        /// Courtesy of eisenpony from https://forum.unity.com/threads/how-do-you-find-the-size-of-a-local-bounding-box.341007/
        /// </summary>
        /// <param name="collider">The collider whose bounding box is being queried</param>
        /// <returns>The bounding box</returns>
        public static Bounds GetLocalBounds(this Collider collider)
        {
            Vector3 center = Vector3.zero;
            Vector3 size = Vector3.zero;
            if (collider is BoxCollider)
            {
                var boxCollider = ((BoxCollider)collider);
                center = boxCollider.center;
                size = boxCollider.size;
            }
            else if (collider is SphereCollider)
            {
                var sphereCollider = ((SphereCollider)collider);
                var radius = sphereCollider.radius;
                center = sphereCollider.center;
                size = new Vector3(radius * 2, radius * 2, radius * 2);
            }
            else if (collider is CapsuleCollider)
            {
                var capsuleCollider = ((CapsuleCollider)collider);
                center = capsuleCollider.center;

                var radius = capsuleCollider.radius;
                var height = capsuleCollider.height;
                var direction = capsuleCollider.direction;

                var directionArray = new Vector3[] { Vector3.right, Vector3.up, Vector3.forward };
                var result = new Vector3();
                for (int i = 0; i < 3; i ++)
                {
                    if (i == direction)
                        result += directionArray[i] * height;
                    else
                        result += directionArray[i] * radius * 2;
                }
                size = result;
            }
            else if (collider is MeshCollider)
            {
                var meshCollider = ((MeshCollider)collider);
                center = meshCollider.sharedMesh.bounds.center;
                size = meshCollider.sharedMesh.bounds.size;
            }
            else
            {
                Debug.LogError("BoundsHelpers: Given collider was of an unsupported type");
            }

            return new Bounds(center, size);
        }
        /// <summary>
        /// Takes a bounds and transforms it from an objects local space to the world space
        /// Courtesy of benblo from https://answers.unity.com/questions/361275/cant-convert-bounds-from-world-coordinates-to-loca.html
        /// </summary>
        /// <param name="_transform">The transform that is 'parent' to the bounds</param>
        /// <param name="_localBounds">The bounds with local values to be transformed</param>
        /// <returns>A bounds transformed with values corresponding to the world space</returns>
        public static Bounds TransformBounds(this Transform _transform, Bounds _localBounds)
        {
            var center = _transform.TransformPoint(_localBounds.center);

            // transform the local extents' axes
            var extents = _localBounds.extents;
            var axisX = _transform.TransformVector(extents.x, 0, 0);
            var axisY = _transform.TransformVector(0, extents.y, 0);
            var axisZ = _transform.TransformVector(0, 0, extents.z);

            // sum their absolute value to get the world extents
            extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
            extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
            extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

            return new Bounds { center = center, extents = extents };
        }

        /// <summary>
        /// Combines any number of bounds
        /// </summary>
        /// <param name="bounds">The bounds to be combined</param>
        /// <returns>A bounds that encapsulates all the given bounds</returns>
        public static Bounds Combine(params Bounds[] bounds)
        {
            Bounds combined = new Bounds();
            if (bounds.Length > 0)
            {
                combined = bounds[0];
                for (int i = 1; i < bounds.Length; i++)
                    combined.Encapsulate(bounds[i]);
            }
            else
                Debug.LogError("BoundsHelpers: No bounds to combine");

            return combined;
        }
        /// <summary>
        /// Combines two bounds together
        /// </summary>
        /// <param name="current">The first bounds</param>
        /// <param name="other">The second bounds</param>
        /// <returns>The combined bounds</returns>
        public static Bounds Combine(this Bounds current, Bounds other)
        {
            return Combine(new Bounds[] { current, other });
        }
        /// <summary>
        /// Finds the objects the given point is in. Checks the children of the given object as well.
        /// </summary>
        /// <param name="rootObject">The root of the object to check</param>
        /// <param name="point">The point in world space</param>
        /// <returns>A list of all objects where the point is contained</returns>
        public static List<Transform> HasPointInTotalBounds(this Transform rootObject, Vector3 point)
        {
            List<Transform> pointPierces = new List<Transform>();

            foreach (Transform currentTransform in rootObject.GetComponentsInChildren<Transform>())
            {
                if (currentTransform.GetBounds(Space.World).Contains(point))
                    pointPierces.Add(currentTransform);
            }
            return pointPierces;
        }
        public static bool HasPointInBounds(this Transform currentTransform, Vector3 point)
        {
            bool hasPoint = false;
            if (currentTransform.GetBounds(Space.World).Contains(point))
                hasPoint = true;
            return hasPoint;
        }
    }
}