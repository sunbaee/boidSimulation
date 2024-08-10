using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
using Debug = UnityEngine.Debug;
using Unity.VisualScripting.Dependencies.NCalc;
using System.Linq;
using TMPro;

public class Simulation : MonoBehaviour {
    
    [SerializeField] private GameObject boidObject;

    [Header("Bound Configuration")]
    [SerializeField] Vector3 boxBounds;
    [SerializeField] float boxThickness;

    [Header("Boid General")]
    [SerializeField] private uint boidAmount;
    [SerializeField] private float boidSpeed;

    [Header("Boid Behaviours")]
    [SerializeField] private float coherenceFactor;
    [SerializeField] private float avoidFactor;
    [SerializeField] private float alignFactor;
    [SerializeField] private float collideFactor;
    [SerializeField] private float randomFactor;
    [SerializeField] private float inertFactor;

    [Header("Boid Detection")]
    [SerializeField] [Range(0f, 180f)] private float boidAngle;
    [SerializeField] private float boidVisionRadius;

    [Header("Boid Predators")]
    [SerializeField] private uint predatorsAmount;
    [SerializeField] private float escapeFactor;

    // [Header("Sphere")]
    // [SerializeField] private uint numPoints;
    // [SerializeField] private float sphereRadius = 10f;
    // [SerializeField] [Range(0f, (float) Math.PI * 2)] private float angle = 2.399963f;

    GameObject[] boidObjects;

    // Predator boids Indexes
    uint[] predatorsIndexes;

    // boid vectors.
    Vector3[] boidPositions;
    Vector3[] boidVelocities;
    Vector3[] boidAccelerations;

    // Vector for boxThickness float.
    Vector3 boxThicknessVector;

    // points in a sphere surface.
    Vector3[] spherePoints;

    // Switch between random flock vectors.
    bool isSleeping;

    private void OnDrawGizmos() {
        Gizmos.color = new Color(.45f, .45f, 1f);
        Gizmos.DrawWireCube(Vector3.zero, boxBounds);
    }
    
    private void Awake() {
        StartCoroutine(StartTimer(3f));
        boidObjects = new GameObject[boidAmount];

        boidAccelerations = new Vector3[boidAmount];
        boidVelocities = new Vector3[boidAmount];
        boidPositions = new Vector3[boidAmount];       

        boxThicknessVector = new(boxThickness, boxThickness, boxThickness);
        
        SpherePoints sphereObj = new(100, boidVisionRadius, SpherePoints.GOLDEN_ANGLE);
        spherePoints = sphereObj.GetPoints;

        predatorsIndexes = new uint[predatorsAmount];
        for (uint i = 0; i < predatorsAmount; i++) predatorsIndexes[i] = (uint) Random.Range(0, boidAmount - 1);

        for (uint i = 0; i < boidAmount; i++) {    
            boidVelocities[i] = GetRandomDirection() * boidSpeed;
            boidPositions[i] = GetRandomPosition(boxBounds - boxThicknessVector);

            boidObjects[i] = Instantiate(boidObject, boidPositions[i], Quaternion.identity);

            Boid currentBoid = boidObjects[i].GetComponent<Boid>();

            if (predatorsIndexes.Contains(i)) currentBoid.Initialize(coherenceFactor, 0, 0, collideFactor, new Color(1f, 0f, 0f));
            else currentBoid.Initialize(coherenceFactor, alignFactor, avoidFactor, collideFactor, new Color(.4f, .4f, 1f));
            
            currentBoid.UpdateBoid(boidPositions[i], boidVelocities[i]);
        }
    }

    private void FixedUpdate() {
        for (uint i = 0; i < boidAmount; i++) {
            
            boidAccelerations[i] = CalculateAcceleration(i);

            // Updates velocities with time
            boidVelocities[i] += boidAccelerations[i] * Time.fixedDeltaTime;
            boidVelocities[i] = SmoothVelocity(boidVelocities[i]);

            boidPositions[i] += boidVelocities[i] * Time.fixedDeltaTime;

            boidObjects[i].GetComponent<Boid>().UpdateBoid(boidPositions[i], boidVelocities[i]);
        }
    }   

    private Vector3 CalculateAcceleration(uint boidIndex) {
        Vector3 totalVelocityAway = Vector3.zero;
        Vector3 totalVelocity = Vector3.zero;
        Vector3 totalPosition = Vector3.zero;

        uint closeBoidsAmount = 0;
        
        for (uint i = 0; i < boidAmount; i++) {
            
            // Check distance from another boids and calculate factors if the distance is less than the boid vision radius.
            Vector3 boidVector = boidPositions[boidIndex] - boidPositions[i];
            
            if (boidVector.sqrMagnitude > Math.Pow(boidVisionRadius, 2) || 
                Mathf.Abs(Vector3.Angle(boidVelocities[boidIndex], -boidVector)) > boidAngle ||
                i == boidIndex) continue;
            
            // Calculate factors: 
            // Coherence - Boid fly towards other boids.
            // Alignment - Boid tries to match its speed and direction with nearby boids.
            // Separation - Boid avoid running into other.
            
            totalPosition += boidPositions[i];
            totalVelocity += boidVelocities[i];
            
            totalVelocityAway += boidVector.normalized * (boidVector.magnitude > 0 ? (boidVisionRadius / boidVector.magnitude) : avoidFactor * boidSpeed);

            if (predatorsIndexes.Contains(i)) totalVelocityAway *= escapeFactor;

            closeBoidsAmount++;
        }

        Vector3[] addedVectors = { totalPosition, totalVelocity, totalVelocityAway };
        
        Vector3 dodgeDirection = GetCollisions(boidPositions[boidIndex], boidVelocities[boidIndex]);

        Vector3 randomDir = RandomizeDirection();
        Vector3 randomAcc = randomDir != Vector3.zero ? (randomDir - boidVelocities[boidIndex].normalized) * randomFactor : Vector3.zero;
        
        Boid currentBoid = boidObjects[boidIndex].GetComponent<Boid>();
        return currentBoid.BoidAcceleration(addedVectors, dodgeDirection, randomAcc, closeBoidsAmount);
    }
    
    private Vector3 RandomizeDirection() {
        if (isSleeping) return Vector3.zero;
        return GetRandomDirection();
    }

    private IEnumerator StartTimer(float seconds) {
        while (true) {
            isSleeping = false;
            yield return new WaitForFixedUpdate();
            isSleeping = true;
            yield return new WaitForSeconds(seconds);
        }
    }

    private Vector3 SmoothVelocity(Vector3 boidVel) {
        // Slows velocity if its faster than the normal boid speed
        if (boidVel.sqrMagnitude > Mathf.Pow(boidSpeed, 2)) return boidVel - inertFactor * Time.fixedDeltaTime * boidVel.normalized;

        return boidVel.normalized * boidSpeed;
    }

    private Vector3 GetCollisions(Vector3 boidPos, Vector3 boidVelocity) {
        // Adjusts sphere points with the direction of the boid (base sphere points towards z = 1f).
        Quaternion offSetRotation = Quaternion.FromToRotation(Vector3.forward, boidVelocity);
        
        // Loops through predefined points around a sphere, the center of the sphere is the boid.
        for (int i = 0; i < spherePoints.Length; i++) {
            // Vector from the boid to a sphere point.
            Vector3 castVector = offSetRotation * spherePoints[i];
            
            // Looks for clear direction if it collided with an object or the bound box.
            if (Physics.Raycast(boidPos, castVector.normalized, boidVisionRadius) || CheckBounds(boidPos + castVector, boxBounds - boxThicknessVector)) continue;
            
            // If a clear direction is found, creates acceleration to the clear direction.
            if (i > 0) return (castVector - boidVelocity).normalized;
            
            // Returns no acceleration if it didn't collide.
            return Vector3.zero;
        }

        return Vector3.zero;
    }
    
    private bool CheckBounds(Vector3 pos, Vector3 boundPos) {
        // Compares if any component of pos is bigger than the corresponding component of boundPos.
        // Also compares if pos is smaller than -boundPos.

        // Divides bound by 2 to correspond with center of bound box.
        Vector3 halfBound = boundPos / 2f;

        float[] posArray = { pos.x, pos.y, pos.z },
                boundArray = { halfBound.x, halfBound.y, halfBound.z };
        
        // Loops through all pos components and compares them with all halfBound components, returns true if any comparation is true.
        for (int i = 0; i < 3; i++) {
            if (posArray[i] > boundArray[i] || posArray[i] < -boundArray[i]) return true;
        }

        // Returns false otherwise.
        return false;
    }

    private Vector3 GetRandomPosition(Vector3 boundPos) {
        // Creates a random position inside the bound box.

        float xValue = Random.Range(-boundPos.x / 2f, boundPos.x / 2f),
              yValue = Random.Range(-boundPos.y / 2f, boundPos.y / 2f),
              zValue = Random.Range(-boundPos.z / 2f, boundPos.z / 2f);

        return new Vector3(xValue, yValue, zValue);
    }

    private Vector3 GetRandomDirection() {
        // Creates a random normalized direction.

        int pointIndex = Random.Range(0, spherePoints.Length - 1);

        return spherePoints[pointIndex].normalized;
    }
}
