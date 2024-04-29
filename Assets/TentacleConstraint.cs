using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using Unity.Collections;
using System;
using Unity.Mathematics;

[Unity.Burst.BurstCompile]
public struct TentacleConstraintJob : IWeightedAnimationJob
{
    public ReadWriteTransformHandle componentTransform;
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

    // Control points
    public NativeArray<Vector3> controlPoints;

    public NativeArray<Quaternion> controlRotations;
    public NativeArray<Vector3> leftHandles;
    public NativeArray<Vector3> rightHandles;

    // Knot vector
    public NativeArray<float> knotVector;

    int Sampling {
        get {
            return 500;
        }
    }

    public void ProcessRootMotion(AnimationStream stream) { }

    public void ProcessAnimation(AnimationStream stream)
    {
        float w = jobWeight.Get(stream);

        if (w > 0f)
        {
            // Retrieve root and tip rotation.
            Quaternion rootRotation = rootTarget.GetRotation(stream);
            Quaternion tipRotation = tipTarget.GetRotation(stream);
            
            this.controlPoints[0] = rootTarget.GetPosition(stream);
            this.controlPoints[1] = midTarget.GetPosition(stream);
            this.controlPoints[2] = tipTarget.GetPosition(stream);

            this.leftHandles[0] = GetHandle(rootTarget, stream, -1);
            this.leftHandles[1] = GetHandle(midTarget, stream, -1);
            this.leftHandles[2] = GetHandle(tipTarget, stream, -1);

            this.rightHandles[0] = GetHandle(rootTarget, stream);
            this.rightHandles[1] = GetHandle(midTarget, stream);
            this.rightHandles[2] = GetHandle(tipTarget, stream);

            this.controlRotations[0] = rootTarget.GetRotation(stream);
            this.controlRotations[1] = midTarget.GetRotation(stream);
            this.controlRotations[2] = tipTarget.GetRotation(stream);

            Quaternion prevRot = Quaternion.identity;
            for (int i = 0; i < chain.Length; ++i)
            {
                Vector3 pos = GetPoint(weights[i]);
                Quaternion quat = GetRotation(stream, weights[i]);

                // Vector3 from = chain[i].GetRotation(stream) * Vector3.up;
                // Vector3 tangent = GetTangent(weights[i]);
                // Quaternion quat = Quaternion.FromToRotation(from, tangent);

                if (i > 0) {
                    Quaternion tempPrev = prevRot;
                    prevRot = quat;
                    quat = Quaternion.Inverse(tempPrev) * quat;
                }
                
                chain[i].SetLocalRotation(stream, quat);
                chain[i].SetPosition(stream, pos);
            }
        }
        else
        {
            for (int i = 0; i < chain.Length; ++i)
                AnimationRuntimeUtils.PassThrough(stream, chain[i]);
        }
    }
    
    public static Vector3 GetHandle(ReadWriteTransformHandle transform, AnimationStream stream, int i = 1) {
        Vector3 handle = Vector3.up * i;
        //handle = Vector3.Scale(transform.GetLocalScale(stream), handle);
        handle = transform.GetRotation(stream) * handle;
        handle = transform.GetPosition(stream) + handle;
        
        return handle;
    }

    public Vector3 GetPoint(float time)
    {
        // The evaluated points is between these two points
        Vector3 startPoint;
        Vector3 endPoint;
        Vector3 startHandle;
        Vector3 endHandle;
        Quaternion startRot;
        Quaternion endRot;
        float timeRelativeToSegment;

        GetCubicSegment(time, out startPoint, out endPoint, out startHandle, out endHandle, out startRot, out endRot, out timeRelativeToSegment);

        return GetPointOnCubicCurve(timeRelativeToSegment, startPoint, endPoint, startHandle, endHandle);
    }
    public Quaternion GetRotation(AnimationStream stream, float time)
    {
        Vector3 startPoint;
        Vector3 endPoint;
        Vector3 startHandle;
        Vector3 endHandle;
        Quaternion startRot;
        Quaternion endRot;
        float timeRelativeToSegment;

        GetCubicSegment(time, out startPoint, out endPoint, out startHandle, out endHandle, out startRot, out endRot, out timeRelativeToSegment);

        return GetRotationOnCubicCurve(stream, timeRelativeToSegment, startRot, startPoint, endPoint, startHandle, endHandle);
    }

    public Vector3 GetTangent(float time)
    {
        Vector3 startPoint;
        Vector3 endPoint;
        Vector3 startHandle;
        Vector3 endHandle;
        Quaternion startRot;
        Quaternion endRot;
        float timeRelativeToSegment;

        GetCubicSegment(time, out startPoint, out endPoint, out startHandle, out endHandle, out startRot, out endRot, out timeRelativeToSegment);

        return GetTangentOnCubicCurve(timeRelativeToSegment, startPoint, endPoint, startHandle, endHandle);
    }

    public Quaternion GetRotationOnCubicCurve(AnimationStream stream, float time, Quaternion rot, Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent)
    {
        Vector3 tangent = GetTangentOnCubicCurve(time, startPosition, endPosition, startTangent, endTangent);
        Vector3 binormal = Vector3.Cross(componentTransform.GetRotation(stream) * Vector3.forward, tangent).normalized;
        Vector3 normal = Vector3.Cross(tangent, binormal).normalized;

        // TODO: figure out issue with normal because that's whats flipping around.
        // really the only variable to compute normal is tangent. 
        // ALso: we aren't using rotation of the handles???

        // NOTE: problem here is that z-axis is what will be aligned to the forward param in LookRotation(). so 
        // instead we pass the normal as the forward param. 
        // NADIR: so how will x/y axis be aligned w.r.t the tangent vector?
        // Confirm Eg: binormal computes in view camera plane, it computes forward. In tentacle view plane, 
        // we should get Vector3.forward exactly 
        return Quaternion.LookRotation(normal, tangent);
    }

    public static Vector3 GetTangentOnCubicCurve(float time, Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent)
    {
        float t = time;
        float u = 1f - t;
        float u2 = u * u;
        float t2 = t * t;

        Vector3 tangent =
            (-u2) * startPosition +
            (u * (u - 2f * t)) * startTangent -
            (t * (t - 2f * u)) * endTangent +
            (t2) * endPosition;

        return tangent.normalized;
    }

    public static Vector3 GetPointOnCubicCurve(float time, Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent)
    {
        float t = time;
        float u = 1f - t;
        float t2 = t * t;
        float u2 = u * u;
        float u3 = u2 * u;
        float t3 = t2 * t;

        Vector3 result =
            (u3) * startPosition +
            (3f * u2 * t) * startTangent +
            (3f * u * t2) * endTangent +
            (t3) * endPosition;

        return result;
    }

    public void GetCubicSegment(float time, out Vector3 startPoint, out Vector3 endPoint, out Vector3 startHandle, out Vector3 endHandle, out Quaternion startRot, out Quaternion endRot, out float timeRelativeToSegment)
    {
        bool isSet = true;
        startPoint = Vector3.negativeInfinity;
        endPoint = Vector3.negativeInfinity;
        startHandle = Vector3.negativeInfinity;
        endHandle = Vector3.negativeInfinity;
        startRot = Quaternion.identity;
        endRot = Quaternion.identity;

        timeRelativeToSegment = 0f;

        float subCurvePercent = 0f;
        float totalPercent = 0f;
        float approximateLength = GetApproximateLength();
        int subCurveSampling = (this.Sampling / (this.controlPoints.Length - 1)) + 1;

        for (int i = 0; i < this.controlPoints.Length - 1; i++)
        {
            subCurvePercent = GetApproximateLengthOfCubicCurve(this.controlPoints[i], this.controlPoints[i + 1], this.rightHandles[i], this.leftHandles[i+1], subCurveSampling) / approximateLength;
            if (subCurvePercent + totalPercent > time)
            {
                startPoint = this.controlPoints[i];
                endPoint = this.controlPoints[i + 1];

                startHandle = this.rightHandles[i];
                endHandle = this.leftHandles[i + 1];

                startRot = this.controlRotations[i];
                endRot = this.controlRotations[i + 1];

                isSet = false;

                break;
            }

            totalPercent += subCurvePercent;
        }

        if (isSet)
        {
            // If the evaluated point is very near to the end of the curve we are in the last segment
            startPoint = this.controlPoints[this.controlPoints.Length - 2];
            endPoint = this.controlPoints[this.controlPoints.Length - 1];

            startHandle = this.rightHandles[this.controlPoints.Length - 2];
            endHandle = this.leftHandles[this.controlPoints.Length - 1];

            startRot = this.controlRotations[this.controlPoints.Length - 2];
            endRot = this.controlRotations[this.controlPoints.Length - 1];

            // We remove the percentage of the last sub-curve
            totalPercent -= subCurvePercent;
        }


        timeRelativeToSegment = (time - totalPercent) / subCurvePercent;
    }

    public float GetApproximateLength()
    {
        float length = 0;
        int subCurveSampling = (this.Sampling / (this.controlPoints.Length - 1)) + 1;
        for (int i = 0; i < this.controlPoints.Length - 1; i++)
        {
            length += GetApproximateLengthOfCubicCurve(this.controlPoints[i], this.controlPoints[i + 1], this.rightHandles[i], this.leftHandles[i + 1], subCurveSampling);
        }

        return length;
    }

    public float GetApproximateLengthOfCubicCurve(Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent, int sampling)
    {
        float length = 0f;
        Vector3 fromPoint = GetPointOnCubicCurve(0f, startPosition, endPosition, startTangent, endTangent);

        for (int i = 0; i < sampling; i++)
        {
            float time = (i + 1) / (float)sampling;
            Vector3 toPoint = GetPointOnCubicCurve(time, startPosition, endPosition, startTangent, endTangent);
            length += Vector3.Distance(fromPoint, toPoint);
            fromPoint = toPoint;
        }

        return length;
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

    public bool IsValid() {
        return !(midTarget == null || root == null || tip == null || !tip.IsChildOf(root) || rootTarget == null || tipTarget == null || curve == null);
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
        job.componentTransform = ReadWriteTransformHandle.Bind(animator, component.transform);
        job.chain = new NativeArray<ReadWriteTransformHandle>(chain.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        job.steps = new NativeArray<float>(chain.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        job.weights = new NativeArray<float>(chain.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        job.rotations = new NativeArray<Quaternion>(chain.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        job.rootTarget = ReadWriteTransformHandle.Bind(animator, data.rootTarget);
        job.tipTarget = ReadWriteTransformHandle.Bind(animator, data.tipTarget);
        job.midTarget = ReadWriteTransformHandle.Bind(animator, data.midTarget);
        job.knotVector = new NativeArray<float>(7, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        job.controlPoints = new NativeArray<Vector3>(3, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        job.leftHandles = new NativeArray<Vector3>(3, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        job.rightHandles = new NativeArray<Vector3>(3, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        job.controlRotations = new NativeArray<Quaternion>(3, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

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
        job.leftHandles.Dispose();
        job.rightHandles.Dispose();
        job.controlRotations.Dispose();

        // NADIR: MAKE SURE EVERYTHING GETS DISPOSED
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
