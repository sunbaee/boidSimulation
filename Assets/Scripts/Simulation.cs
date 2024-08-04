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

public class Simulation : MonoBehaviour {
    
    [SerializeField] Vector3 boxBounds;

    [SerializeField] private GameObject boidObject;

    [Header("Boid General")]
    [SerializeField] private uint boidAmount;
    [SerializeField] private float boidSpeed;

    [Header("Boid Behaviours")]
    [SerializeField] private float coherenceFactor;
    [SerializeField] private float avoidFactor;
    [SerializeField] private float alignFactor;
    [SerializeField] private float inertFactor;

    [Header("Boid Detection")]
    [SerializeField] [Range(0f, 180f)] private float boidAngle;
    [SerializeField] private float rayCastStepAngle;
    [SerializeField] private float boidVisionRadius;

    [Header("Sphere")]
    [SerializeField] private uint numPoints;
    [SerializeField] private float sphereRadius = 10f;
    [SerializeField] [Range(0f, (float) Math.PI * 2)] private float angle = 2.399963f;

    Vector3[] boidPositions;
    Vector3[] boidVelocities;
    Vector3[] boidAccelerations;

    GameObject[] boidObjects;

    Vector3[] spherePoints;

    readonly float GOLDEN_ANGLE = Mathf.PI * (3 - Mathf.Sqrt(5));

    private void OnDrawGizmos() {
        Gizmos.DrawWireCube(Vector3.zero, boxBounds);

        // if (spherePoints == null) return;

        // for (int i = 0; i < spherePoints.Length; i++) {
        //     Gizmos.DrawSphere(spherePoints[i], 0.1f);
        // }
    }
    
    private void Awake() {
        boidObjects = new GameObject[boidAmount];

        boidAccelerations = new Vector3[boidAmount];
        boidVelocities = new Vector3[boidAmount];
        boidPositions = new Vector3[boidAmount];       

        spherePoints = SpherePoints(200, boidVisionRadius, GOLDEN_ANGLE);

        for (uint i = 0; i < boidAmount; i++) {
            Vector3 startPosition = GetRandomPosition();

            boidObjects[i] = Instantiate(boidObject, startPosition, Quaternion.identity);
            boidVelocities[i] = GetRandomDirection() * boidSpeed;
            boidPositions[i] = startPosition;

            PaintBoid(i, new Color(.4f, .4f, 1f));
        }
    }

    private void FixedUpdate() {
        //spherePoints = SpherePoints(numPoints, sphereRadius, angle);

        for (uint i = 0; i < boidAmount; i++) {
            boidAccelerations[i] = CalculateAcceleration(i);

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

            closeBoidsAmount++;
        }

        Vector3 deviationAcceleration = GetBoundCollision(boidPositions[boidIndex], boidVelocities[boidIndex]) * avoidFactor;

        if (closeBoidsAmount == 0) return deviationAcceleration;

        Vector3 centerMassDir = ((totalPosition +  boidPositions[boidIndex]) / (closeBoidsAmount + 1)) - boidPositions[boidIndex];
        Vector3 alignVelocity = ((totalVelocity + boidVelocities[boidIndex]) / (closeBoidsAmount + 1)) - boidVelocities[boidIndex];
        Vector3 awayVelocity = totalVelocityAway / closeBoidsAmount;

        return awayVelocity * avoidFactor + centerMassDir.normalized * coherenceFactor + alignVelocity.normalized * alignFactor + deviationAcceleration;
    }

    private Vector3[] SpherePoints(uint pointsAmount, float sphereRadius, float customAngle) {
        Vector3[] points = new Vector3[pointsAmount];

        // Distance between each circle layer

        for (int i = 0; i < pointsAmount; i++) {
            // Calculate values for a sphere with radius = 1
            
            // Calculates z position with circle distance
            float z = 1f - (i / (float) pointsAmount * 2f),
                  innerCircleRadius = (float) Mathf.Sqrt(1f - Mathf.Pow(z, 2f));

            float stepAngle = customAngle * i;

            // Calculate x and y with step angle
            float x = innerCircleRadius * Mathf.Cos(stepAngle),
                  y = innerCircleRadius * Mathf.Sin(stepAngle);
            
            Vector3 basePoint = new(x, y, z);

            // Converts the sphere to its normal size
            points[i] = basePoint * sphereRadius;
            
        }

        return points;
    }

    private Vector3 SmoothVelocity(Vector3 boidVel) {
        if (boidVel.sqrMagnitude > Mathf.Pow(boidSpeed, 2)) return boidVel - inertFactor * Time.fixedDeltaTime * boidVel.normalized;

        return boidVel.normalized * boidSpeed;
    }

    private Vector3 GetBoundCollision(Vector3 boidPos, Vector3 boidVelocity) {
        //float stepValue = 0f;

        //while (!Physics.Raycast(boidPos, boidVelocity, boidVisionRadius, 6)) {
            
        //}
        Vector3 boidRay = boidPos + boidVelocity.normalized * boidVisionRadius;

        Vector3 halfBoxBound = boxBounds / 2f;

        bool xBool = boidRay.x >  halfBoxBound.x || 
                     boidRay.x < -halfBoxBound.x;
        bool yBool = boidRay.y >  halfBoxBound.y || 
                     boidRay.y < -halfBoxBound.y;
        bool zBool = boidRay.z >  halfBoxBound.z || 
                     boidRay.z < -halfBoxBound.z;

        //boidVelocity.

        //if (xBool || yBool || zBool) return 

        return Vector3.zero;
    }
    
    private Vector3 GetRandomPosition()
    {
        float xValue = Random.Range(-boxBounds.x / 2f, boxBounds.x / 2f),
              yValue = Random.Range(-boxBounds.y / 2f, boxBounds.y / 2f),
              zValue = Random.Range(-boxBounds.z / 2f, boxBounds.z / 2f);

        return new Vector3(xValue, yValue, zValue);
    }

    private Vector3 GetRandomDirection() {
        float[] vecComponents = new float[3];
        for (int i = 0; i < vecComponents.Length; i++) {
            vecComponents[i] = Random.Range(-1f, 1f);
        }

        return new Vector3(vecComponents[0], vecComponents[1], vecComponents[2]).normalized;
    }

    private void PaintBoid(uint boidIndex, Color color) {
        boidObjects[boidIndex].GetComponent<Renderer>().material.SetColor("_BaseColor", color);
    }
}
