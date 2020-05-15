﻿using UnityEngine;

namespace UnityHelpers
{
    /// <summary>
    /// This is a script to be attached to the object whose collision events you want to listen to
    /// TreeCollider is buggy, so I'm making this script for now
    /// </summary>
    public class CollisionListener : MonoBehaviour
    {
        [SerializeField]
        TreeCollider.UnifiedCollisionEvent onCollisionStay;
        [SerializeField]
        TreeCollider.UnifiedCollisionEvent onTriggerStay;
        [SerializeField]
        TreeCollider.UnifiedCollisionEvent onTriggerEnter;
        [SerializeField]
        TreeCollider.UnifiedCollisionEvent onTriggerExit;


        private void OnCollisionStay(Collision collision)
        {
            onCollisionStay?.Invoke(new TreeCollider.CollisionInfo
            {
                collidedWith = collision.gameObject,
                collision = collision,
                collisionState = TreeCollider.CollisionInfo.CollisionState.stay,
                isTrigger = false,
                otherCollider = collision.collider,
                sender = gameObject
            });
        }
        private void OnTriggerStay(Collider other)
        {
            onTriggerStay?.Invoke(new TreeCollider.CollisionInfo
            {
                collidedWith = other.gameObject,
                collisionState = TreeCollider.CollisionInfo.CollisionState.stay,
                isTrigger = true,
                otherCollider = other,
                sender = gameObject
            });
        }
        private void OnTriggerEnter(Collider other)
        {
            onTriggerEnter?.Invoke(new TreeCollider.CollisionInfo
            {
                collidedWith = other.gameObject,
                collisionState = TreeCollider.CollisionInfo.CollisionState.stay,
                isTrigger = true,
                otherCollider = other,
                sender = gameObject
            });
        }
        private void OnTriggerExit(Collider other)
        {
            onTriggerExit?.Invoke(new TreeCollider.CollisionInfo
            {
                collidedWith = other.gameObject,
                collisionState = TreeCollider.CollisionInfo.CollisionState.stay,
                isTrigger = true,
                otherCollider = other,
                sender = gameObject
            });
        }
    }
}