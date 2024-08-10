using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class Boid : MonoBehaviour {

    private float coherenceFactor;
    private float alignFactor;
    private float avoidFactor;
    private float collideFactor;

    // boid vectors
    private Vector3 position;
    private Vector3 velocity;

    private Vector3 boidRandomDir;

    public void Initialize(float coherenceFactor, float alignFactor, float avoidFactor, float collideFactor, Color boidColor) {
        this.coherenceFactor = coherenceFactor;
        this.alignFactor = alignFactor;
        this.avoidFactor = avoidFactor;
        this.collideFactor = collideFactor;

        PaintBoid(boidColor);
    }

    public void UpdateBoid(Vector3 boidPosition, Vector3 boidVelocity) {
        // Updates position and velocity to calculate acceleration on the next frame.
        position = boidPosition;
        velocity = boidVelocity;
    
        transform.position = position;

        // Boids looks at the direction it's moving.
        transform.LookAt(position + velocity);
    }

    public Vector3 BoidAcceleration(Vector3[] addedVectors, Vector3 dodgeDir, Vector3 randomDir, uint closeBoidsAmount) {
        // Checks for collisions:
        Vector3 dodgeAcc = collideFactor * dodgeDir;

        if (randomDir != Vector3.zero) boidRandomDir = randomDir;

        // If theres no nearby boids, skip other calculations.
        if (closeBoidsAmount == 0) return dodgeAcc + boidRandomDir;

        // Calculate acceleration directions:
        Vector3 centerMassDir = (((addedVectors[0] + position) / (closeBoidsAmount + 1)) - position).normalized * coherenceFactor;
        Vector3 alignVelocity = (((addedVectors[1] + velocity) / (closeBoidsAmount + 1)) - velocity).normalized * alignFactor;

        Vector3 awayVelocity = addedVectors[2] / closeBoidsAmount * avoidFactor;
        
        return awayVelocity + centerMassDir + alignVelocity + dodgeAcc + boidRandomDir;
    }

    private void PaintBoid(Color color) {
        GetComponent<Renderer>().material.SetColor("_BaseColor", color);
    }
}
