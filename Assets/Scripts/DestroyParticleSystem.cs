using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Add this script to a parent GameObject with a ParticleSystem component as childs, to destroy the GameObject when all its child particle systems have stopped.
/// </summary>
public class DestroyParticleSystem : MonoBehaviour
{
    private ParticleSystem[] childParticleSystems;

    void Start()
    {
        // Get all ParticleSystem components in the parent and its children
        childParticleSystems = GetComponentsInChildren<ParticleSystem>();
    }

    void Update()
    {
        bool allStopped = true;

        foreach (ParticleSystem ps in childParticleSystems)
        {
            if (ps.IsAlive())
            {
                allStopped = false;
                break;
            }
        }

        if (allStopped)
        {
            Destroy(gameObject);
        }
    }
}
