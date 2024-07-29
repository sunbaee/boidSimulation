using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class Simulation : MonoBehaviour {
    
    [SerializeField] Vector3 boxBounds;

    [SerializeField] private GameObject boidObject;

    [Header("Boid Movement")]
    [SerializeField] private uint boidAmount;
    [SerializeField] private float boidSpeed;
    [SerializeField] private float avoidFactor;

    [Header("Boid Detection")]
    [SerializeField] private float boidVisionRadius;
    [SerializeField] private float boidAngle;

    Vector3[] boidPositions;
    Vector3[] boidVelocities;
    Vector3[] boidAccelerations;

    GameObject[] boidObjects;

    private void OnDrawGizmos() {
        Gizmos.DrawWireCube(Vector3.zero, boxBounds);
    }
    
    private void Awake() {
        boidObjects = new GameObject[boidAmount];

        boidAccelerations = new Vector3[boidAmount];
        boidVelocities = new Vector3[boidAmount];
        boidPositions = new Vector3[boidAmount];       

        for (uint i = 0; i < boidAmount; i++) {
            boidObjects[i] = Instantiate(boidObject, GetRandomPosition(), Quaternion.identity);
            PaintBoid(i, new Color(.4f, .4f, 1f));
            if (i == 5) PaintBoid(i, new Color(1f, 0f, 0f));

            boidVelocities[i] = GetRandomDirection() * boidSpeed;
            boidPositions[i] = GetRandomPosition();
        }
    }

    private void FixedUpdate() {
        for (uint i = 0; i < boidAmount; i++) {
            boidAccelerations[i] = Separation(i);

            boidVelocities[i] += boidAccelerations[i] * Time.fixedDeltaTime;

            boidPositions[i] += boidVelocities[i] * Time.fixedDeltaTime;
            boidPositions[i] = GetBoundCollision(boidPositions[i]);

            boidObjects[i].GetComponent<Boid>().UpdateBoid(boidPositions[i], boidVelocities[i]);
        }
    }

    private Vector3 Separation(uint boidIndex) {
        Vector3 accelerationAway = Vector3.zero;

        for (uint i = 0; i < boidAmount; i++) {
            Vector3 boidVector = boidPositions[boidIndex] - boidPositions[i];
            
            if (boidVector.sqrMagnitude > Math.Pow(boidVisionRadius, 2)) continue;
            if (i == boidIndex) continue;
            
            accelerationAway += boidVector;
        }

        return accelerationAway.normalized * avoidFactor;
    }

    private Vector3 GetBoundCollision(Vector3 boidPos) {        
        Vector3 halfBoxBound = boxBounds / 2f;

        bool xBool = boidPos.x >  halfBoxBound.x || 
                     boidPos.x < -halfBoxBound.x;
        bool yBool = boidPos.y >  halfBoxBound.y || 
                     boidPos.y < -halfBoxBound.y;
        bool zBool = boidPos.z >  halfBoxBound.z || 
                     boidPos.z < -halfBoxBound.z;

        Vector3 newPos = boidPos;

        if (xBool) newPos.x *= -.95f;
        if (yBool) newPos.y *= -.95f;
        if (zBool) newPos.z *= -.95f;

        return newPos;
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
