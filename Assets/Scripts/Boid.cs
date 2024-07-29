using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class Boid : MonoBehaviour {

    public void UpdateBoid(Vector3 boidPosition, Vector3 boidVelocity) {
        transform.position = boidPosition;
        transform.LookAt(boidPosition + boidVelocity);
    }    
}
