using System;
using System.Collections.Generic;
using UnityEngine;

public class BSpline
{
    private List<Vector3> controlPoints;
    private int degree;
    
    public BSpline(List<Vector3> controlPoints, int degree)
    {
        if (controlPoints.Count < degree + 1)
            throw new ArgumentException("Number of control points must be at least degree + 1");

        this.controlPoints = controlPoints;
        this.degree = degree;
    }

    private float BasisFunction(int i, int k, float t)
    {
        if (k == 1)
        {
            if (t >= KnotVector[i] && t < KnotVector[i + 1])
                return 1;
            else
                return 0;
        }

        float factor1 = 0;
        float factor2 = 0;

        if (KnotVector[i + k - 1] != KnotVector[i])
            factor1 = (t - KnotVector[i]) / (KnotVector[i + k - 1] - KnotVector[i]) * BasisFunction(i, k - 1, t);

        if (KnotVector[i + k] != KnotVector[i + 1])
            factor2 = (KnotVector[i + k] - t) / (KnotVector[i + k] - KnotVector[i + 1]) * BasisFunction(i + 1, k - 1, t);

        return factor1 + factor2;
    }

    public Vector3 Evaluate(float t)
    {
        Vector3 result = Vector3.zero;

        for (int i = 0; i < controlPoints.Count; i++)
        {
            float basis = BasisFunction(i, degree + 1, t);
            result += controlPoints[i] * basis;
        }

        return result;
    }

    public Vector3 EvaluateTangent(float t)
    {
        Vector3 tangent = Vector3.zero;

        for (int i = 0; i < controlPoints.Count; i++)
        {
            float basisDerivative = BasisFunctionDerivative(i, degree + 1, t);
            tangent += controlPoints[i] * basisDerivative;
        }

        return tangent;
    }

    private float BasisFunctionDerivative(int i, int k, float t)
    {
        if (k == 1)
        {
            return (KnotVector[i + 1] - KnotVector[i]) > 0 ? 1 : 0;
        }

        float factor1 = 0;
        float factor2 = 0;

        float den1 = KnotVector[i + k - 1] - KnotVector[i];
        float den2 = KnotVector[i + k] - KnotVector[i + 1];

        if (den1 > 0)
            factor1 = k / den1 * BasisFunctionDerivative(i, k - 1, t);

        if (den2 > 0)
            factor2 = k / den2 * BasisFunctionDerivative(i + 1, k - 1, t);

        return factor1 - factor2;
    }

    private List<float> KnotVector
    {
        get
        {
            List<float> knots = new List<float>();
            int n = controlPoints.Count;
            int m = n + degree + 1;

            for (int i = 0; i <= m; i++)
            {
                if (i <= degree)
                    knots.Add(0);
                else if (i >= n)
                    knots.Add(1);
                else
                    knots.Add((float)(i - degree) / (n - degree));
            }

            return knots;
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        // Example usage:
        List<Vector3> controlPoints = new List<Vector3>
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 3, 2),
            new Vector3(2, -1, 1),
            new Vector3(3, 2, 4),
            new Vector3(4, 0, 3)
        };

        int degree = 3;

        BSpline spline = new BSpline(controlPoints, degree);

        float t = 0.5f;
        Vector3 point = spline.Evaluate(t);
        Vector3 tangent = spline.EvaluateTangent(t);

        Debug.Log($"Point at t={t}: ({point.x}, {point.y}, {point.z})");
        Debug.Log($"Tangent at t={t}: ({tangent.x}, {tangent.y}, {tangent.z})");
    }
}
