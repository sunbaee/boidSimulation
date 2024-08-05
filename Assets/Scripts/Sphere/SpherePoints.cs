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

public class SpherePoints {

    private readonly Vector3[] points;

    // Golden angle to create uniform points in a sphere surface.
    public static readonly float GOLDEN_ANGLE = Mathf.PI * (3 - Mathf.Sqrt(5));

    public Vector3[] GetPoints => points;

    public SpherePoints(uint pointsAmount, float sphereRadius, float customAngle) {
        Vector3[] points = new Vector3[pointsAmount];

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

        this.points = points;
    }
}