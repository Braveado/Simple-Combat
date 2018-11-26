using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public BoxCollider2D wCollider;             // Reference to the collider of the weapon.

    [Header("Stats")]
    public float wDamage;                       // Amount of damage the weapon makes in a hit.
    public float wEnergy;                       // Amount of energy consumed per hit.
    public float wForce;                        // Force applied in the x axis when attacking.

    [HideInInspector]
    public int hitCount;                        // To prevent multiple collisions with the same object.
}
