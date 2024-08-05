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

public class DisplaySphere : MonoBehaviour {

    [Header("Sphere Configuration")]
    [SerializeField] private uint pointsAmount;
    [SerializeField] private float sphereRadius;
    [SerializeField] [Range(0f, Mathf.PI * 2f)] private float stepAngle = SpherePoints.GOLDEN_ANGLE;

    private Vector3[] displayPoints;

    private void OnDrawGizmos() {
        // Show points on screen.

        if (displayPoints == null) return;
        for (int i = 0; i < displayPoints.Length; i++) {
            Gizmos.DrawSphere(displayPoints[i], .1f);
        }
    }

    private void Update() {
        SpherePoints spherePoints = new(pointsAmount, sphereRadius, stepAngle);
        displayPoints = spherePoints.GetPoints;
    }
}