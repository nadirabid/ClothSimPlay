using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using UnityEngine.Splines;
using Unity.Collections;
using System;
using Unity.Mathematics;

[Unity.Burst.BurstCompile]
public struct TentacleConstraintJob : IWeightedAnimationJob
{
    /// <summary>The Transform handle for the root target Transform.</summary>
    public ReadWriteTransformHandle rootTarget;
    /// <summary>The Transform handle for the tip target Transform.</summary>
    public ReadWriteTransformHandle tipTarget;

    public ReadWriteTransformHandle midTarget;

    /// <summary>An array of Transform handles that represents the Transform chain.</summary>
    public NativeArray<ReadWriteTransformHandle> chain;

    public FloatProperty jobWeight { get; set; }

    /// <summary>An array of interpolant values used to reevaluate the weights.</summary>
    public NativeArray<float> steps;
    /// <summary>An array of weight values used to adjust how twist is distributed along the chain.</summary>
    public NativeArray<float> weights;
    /// <summary>An array of rotation offsets to maintain the chain initial shape.</summary>
    public NativeArray<Quaternion> rotations;

    public void ProcessRootMotion(AnimationStream stream) { }

    public void ProcessAnimation(AnimationStream stream)
    {
        float w = jobWeight.Get(stream);

         if (w > 0f)
        {
            // Retrieve root and tip rotation.
            Quaternion rootRotation = rootTarget.GetRotation(stream);
            Quaternion tipRotation = tipTarget.GetRotation(stream);

            this.degree = 2;
            
            this.controlPoints[0] = rootTarget.GetPosition(stream);
            this.controlPoints[1] = midTarget.GetPosition(stream);
            this.controlPoints[2] = tipTarget.GetPosition(stream);

            // Interpolate rotation on chain.
            
            //chain[0].SetRotation(stream, Quaternion.Slerp(chain[0].GetRotation(stream), rootRotation, w));
            for (int i = 0; i < chain.Length - 2; ++i)
            {
                
                Vector3 pos = Evaluate(weights[i]);
                Vector3 tan = EvaluateTangent(weights[i]);

                Quaternion quat = Quaternion.Euler(tan.x, tan.y, tan.z);

                //Debug.Log($"chain[{i}]");
                //Debug.Log($"QUAT <{quat.w}, {quat.x}, {quat.y}, {quat.z}>");
                //Debug.Log($"POS <{pos.x}, {pos.y}, {pos.z}>");
                
                chain[i].SetRotation(stream, Quaternion.Slerp(chain[0].GetRotation(stream), quat, w));
                chain[i].SetPosition(stream, pos);
            }
            
        }
        else
        {
            for (int i = 0; i < chain.Length; ++i)
                AnimationRuntimeUtils.PassThrough(stream, chain[i]);
        }
    }

    // Control points
    public NativeArray<Vector3> controlPoints;
    // Knot vector
    public NativeArray<float> knotVector;
    // Degree of the spline
    public int degree;

    public Vector3 Evaluate(float t)
    {
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        int degree = controlPoints.Length - 1;
        Vector3 result = Vector3.zero;

        for (int i = 0; i <= degree; i++)
        {
            float blend = BinomialCoefficient(degree, i) * Mathf.Pow(oneMinusT, degree - i) * Mathf.Pow(t, i);
            result += blend * controlPoints[i];
        }

        return result;
    }

    public Vector3 EvaluateTangent(float t)
    {
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        int degree = controlPoints.Length - 1;
        Vector3 tangent = Vector3.zero;

        for (int i = 0; i < degree; i++)
        {
            float blend = BinomialCoefficient(degree - 1, i) * Mathf.Pow(oneMinusT, degree - i - 1) * Mathf.Pow(t, i);
            tangent += blend * (controlPoints[i + 1] - controlPoints[i]);
        }

        return tangent.normalized;
    }

    public float min;
    public float max;

     public Vector3 GetTangent(float t)
    {
        int n = controlPoints.Length - 1;
        Vector3 tangent = Vector3.zero;

        for (int i = 0; i <= n; i++)
        {
            float binomial = CalculateBinomialCoefficient(n, i) * (n - i);
            float basis = Mathf.Pow(1f - t, n - i - 1) * Mathf.Pow(t, i);
            tangent.x += binomial * controlPoints[i].x * basis;
            tangent.y += binomial * controlPoints[i].y * basis;
            tangent.z += binomial * controlPoints[i].z * basis;
        }

        tangent.x = Mathf.Clamp(tangent.x, min, max);
        tangent.y = Mathf.Clamp(tangent.y, min, max);
        tangent.z = Mathf.Clamp(tangent.z, min, max);

        return tangent.normalized;
    }

    private int BinomialCoefficient(int n, int k)
    {
        int result = 1;
        for (int i = 1; i <= k; i++)
        {
            result *= n - (k - i);
            result /= i;
        }
        return result;
    }

    private float CalculateBinomialCoefficient(int n, int k)
    {
        return Factorial(n) / (Factorial(k) * Factorial(n - k));
    }

    private int Factorial(int n)
    {
        if (n <= 0) return 1;
        return n * Factorial(n - 1);
    }
}

[Serializable]
public struct TentacleConstraintData : IAnimationJobData
{
    /// <summary>The root Transform of the TwistChain hierarchy.</summary>
    [SerializeField] public Transform root;

    /// <summary>The tip Transform of the TwistChain hierarchy.</summary>
    [SerializeField] public Transform tip;

    /// <summary>The TwistChain root target Transform.</summary>
    [SyncSceneToStream, SerializeField] public Transform rootTarget;
    /// <summary>The TwistChain tip target Transform.</summary>
    [SyncSceneToStream, SerializeField] public Transform tipTarget;

    [SyncSceneToStream, SerializeField] public Transform midTarget;

    [SerializeField] public AnimationCurve curve;

    [SerializeField] public SplineContainer spline;

    public bool IsValid() {
        return !(midTarget == null || spline == null || root == null || tip == null || !tip.IsChildOf(root) || rootTarget == null || tipTarget == null || curve == null);
    }

    public void SetDefaultValues() {
        curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        root = tip = rootTarget = midTarget = tipTarget = null;
    }
}

public class TentacleConstraintBinder : AnimationJobBinder<TentacleConstraintJob, TentacleConstraintData> 
{
    public override TentacleConstraintJob Create(Animator animator, ref TentacleConstraintData data, Component component)
    {
        // Retrieve chain in-between root and tip transforms.
        Transform[] chain = ConstraintsUtils.ExtractChain(data.root, data.tip);

        // Extract steps from chain.
        float[] steps = ConstraintsUtils.ExtractSteps(chain);

        var job = new TentacleConstraintJob();
        job.chain = new NativeArray<ReadWriteTransformHandle>(chain.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        job.steps = new NativeArray<float>(chain.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        job.weights = new NativeArray<float>(chain.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        job.rotations = new NativeArray<Quaternion>(chain.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        job.rootTarget = ReadWriteTransformHandle.Bind(animator, data.rootTarget);
        job.tipTarget = ReadWriteTransformHandle.Bind(animator, data.tipTarget);
        job.midTarget = ReadWriteTransformHandle.Bind(animator, data.midTarget);
        job.knotVector = new NativeArray<float>(7, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        job.controlPoints = new NativeArray<Vector3>(3, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        job.min = -100.00001f;
        job.max = 100.00001f;

        for (int i = 0; i < chain.Length; ++i)
        {
            job.chain[i] = ReadWriteTransformHandle.Bind(animator, chain[i]);
            job.steps[i] = steps[i];
            job.weights[i] = Mathf.Clamp01(data.curve.Evaluate(steps[i]));
        }

        job.rotations[0] = Quaternion.identity;
        job.rotations[chain.Length - 1] = Quaternion.identity;
        for (int i = 1; i < chain.Length - 1; ++i)
        {
            // inverse(lerp(chain.first.rot, chain.last.rot, job.weights[i]) * chain[i].rot)
            job.rotations[i] = Quaternion.Inverse(Quaternion.Lerp(chain[0].rotation, chain[chain.Length - 1].rotation, job.weights[i])) * chain[i].rotation;
        }

        return job;
    }

    public override void Destroy(TentacleConstraintJob job) {
        job.chain.Dispose();
        job.weights.Dispose();
        job.steps.Dispose();
        job.rotations.Dispose();
        job.knotVector.Dispose();
        job.controlPoints.Dispose();
    }

#if UNITY_EDITOR
    /// <inheritdoc />
    public override void Update(TentacleConstraintJob job, ref TentacleConstraintData data)
    {
        // Update weights based on curve.
        // NOTE(NADIR): This is basically if we want the unity curve editor
        // to "hot load" the new weights
    }
#endif
}

[DisallowMultipleComponent, AddComponentMenu("Animation Tentacle Constraint")]
public class TentacleConstraint : RigConstraint<
    TentacleConstraintJob,
    TentacleConstraintData,
    TentacleConstraintBinder
    >
{
}
