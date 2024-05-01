using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class Tentacle {
    public Transform t;

    public Transform mesh;
    public Transform p0;
    public Transform p1;
    public Transform p2;
}

class Interpolations {
    public Vector3 p1Vec;
    public Vector3 p2Vec;

    public float p1Rot;
    public float p2Rot;

    public Quaternion p1Quat;
    public Quaternion p2Quat;
}

public class TentacleController : MonoBehaviour
{
    // Start is called before the first frame update

    Tentacle[] tentacles;
    Interpolations[] interpolations;

    private float timeToMove = 1f;
    private float elapsedTime = 0f;

    private int currentInterp = 0;

    void Start()
    {
        InitializeInterpolations();


        tentacles = new Tentacle[15];
        for (int i = 0; i < tentacles.Length; i++)
        {
            tentacles[i] = GetTentacle(i);
            SetDefaultTentacleTransforms(i);
            ApplyInterpolation(tentacles[i], interpolations[0]);
        }
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;

        int prevInterp = currentInterp;

        if (elapsedTime > timeToMove)
        {
            elapsedTime = 0;
            currentInterp = (currentInterp + 1) % interpolations.Length;
        }

        float t = elapsedTime / timeToMove;

        Interpolations curr = interpolations[currentInterp];
        Interpolations prev = interpolations[prevInterp];

        for (int i = 0; i < tentacles.Length; i++)
        {
            Tentacle tent = tentacles[i];

            tent.p1.Translate(Vector3.Lerp(prev.p1Vec, curr.p1Vec, t));
            tent.p1.RotateAround(tent.p1.position, tent.p1.forward, Mathf.Lerp(prev.p1Rot, curr.p1Rot, t));

            tent.p2.Translate(Vector3.Lerp(prev.p2Vec, curr.p2Vec, t));
            tent.p2.RotateAround(tent.p2.position, tent.p2.forward, Mathf.Lerp(prev.p2Rot, curr.p2Rot, t));
        }
    }

    void InitializeInterpolations() {
        interpolations = new Interpolations[3];

        interpolations[0] = new Interpolations();
        interpolations[0].p1Vec = new Vector3(1.4f, 0f, 0f);
        interpolations[0].p1Rot = -110f;
        interpolations[0].p2Vec = new Vector3(2f, -5f, 0f);
        interpolations[0].p2Rot = -200f;

        interpolations[1] = new Interpolations();
        interpolations[1].p1Vec = new Vector3(0.2f, 0.2f, 0f);
        interpolations[1].p1Rot = 20f;
        interpolations[1].p2Vec = new Vector3(3f, 3f, 0f);
        interpolations[1].p2Rot = -10f;

        interpolations[2] = new Interpolations();
        interpolations[2].p1Vec = new Vector3(0.2f, 0.1f, 0f);
        interpolations[2].p1Rot = 10f;
        interpolations[2].p2Vec = new Vector3(2f, 3f, 0f);
        interpolations[2].p2Rot = 180f;
    }

    void ApplyInterpolation(Tentacle tent, Interpolations interp)
    {
        tent.p1.Translate(interp.p1Vec);
        tent.p1.RotateAround(tent.p1.position, tent.p1.forward, interp.p1Rot);

        tent.p2.Translate(interp.p2Vec);
        tent.p2.RotateAround(tent.p2.position, tent.p2.forward, interp.p2Rot);
    }

    void SetDefaultTentacleTransforms(int i)
    {
        Tentacle tent = tentacles[i];

        float rotation = (360f / tentacles.Length) * i;
        tent.t.RotateAround(tent.t.position, tent.t.up, rotation);

        // tent.p1.Translate(new Vector3(1.4f, 0, 0));
        // tent.p1.RotateAround(tent.p1.position, tent.p1.forward, -110);

        // tent.p2.Translate(new Vector3(2, -5, 0));
        // tent.p2.RotateAround(tent.p2.position, tent.p2.forward, -200);
    }

    Tentacle GetTentacle(int i)
    {
        string meshName = "Tentacle";
        string name = "ArmatureTentacle";
        if (i > 9)
        {
            name += $".0{i}";
            meshName += $".0{i}";
        }
        else if (i > 0)
        {
            name += $".00{i}";
            meshName += $".00{i}";
        }

        //Debug.Log(name);

        Tentacle tentacle = new Tentacle();
        tentacle.t = transform.Find(name);
        tentacle.mesh = transform.Find(meshName);

        Transform rig = tentacle.t.Find("Rig 1");
        Transform constraint = rig.Find("tentacle_constraint");

        tentacle.p0 = constraint.Find("p0");
        tentacle.p1 = constraint.Find("p1");
        tentacle.p2 = constraint.Find("p2");

        return tentacle;
    }
}
