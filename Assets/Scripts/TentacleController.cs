using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class Tentacle {
    public Transform t;

    public Transform mesh;
    public Transform p0;
    public Transform p1;
    public Transform p2;

    public GameObject sp1;
    public GameObject ep1;

    public GameObject sp2;
    public GameObject ep2;

    public Interpolations start;
    public Interpolations end;
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

    Interpolations resting;

    Interpolations previous;
    Interpolations current;

    private float timeToMove = 0.2f;
    private float elapsedTime = 10000f;

    private int currentInterp = 0;

    void Start()
    {
        InitializeInterpolations();

        tentacles = new Tentacle[15];
        for (int i = 0; i < tentacles.Length; i++)
        {
            tentacles[i] = GetTentacle(i);
            SetDefaultTentacleTransforms(i);
            ApplyInterpolation(tentacles[i], resting);
        }
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;

        if (elapsedTime > timeToMove)
        {
            elapsedTime = 0;
            currentInterp = (currentInterp + 1) % interpolations.Length;

            Debug.Log("interp index" + currentInterp);
            Interpolations curr = interpolations[currentInterp];
            Debug.Log($"interps {curr.p1Vec.ToString()} {curr.p2Vec.ToString()}");

            for (int i = 0; i < tentacles.Length; i++)
            {
                Tentacle tent = tentacles[i];

                /// p1
                
                // start
                tent.sp1.transform.position = tent.p1.position;
                tent.sp1.transform.rotation = tent.p1.rotation;

                // end
                tent.ep1.transform.position = tent.p1.position;
                tent.ep1.transform.rotation = tent.p1.rotation;
                tent.ep1.transform.Translate(curr.p1Vec);
                tent.ep1.transform.RotateAround(tent.ep1.transform.position, tent.ep1.transform.forward, curr.p1Rot);

                /// p2
                
                // start
                tent.sp2.transform.position = tent.p2.position;
                tent.sp2.transform.rotation = tent.p2.rotation;
                
                // end
                tent.ep2.transform.position = tent.p2.position;
                tent.ep2.transform.rotation = tent.p2.rotation;

                tent.ep2.transform.Translate(curr.p2Vec);
                tent.ep2.transform.RotateAround(tent.ep2.transform.position, tent.ep2.transform.forward, curr.p2Rot);
            }
        }

        float t = elapsedTime / timeToMove;
        //Debug.Log($"elapsedTime {elapsedTime} / {timeToMove} = {t}");

        for (int i = 0; i < tentacles.Length; i++)
        {
            Tentacle tent = tentacles[i];

            tent.p1.position = Vector3.Lerp(tent.sp1.transform.position, tent.ep1.transform.position, t);
            tent.p1.rotation = Quaternion.Lerp(tent.sp1.transform.rotation, tent.ep1.transform.rotation, t);

            tent.p2.position = Vector3.Lerp(tent.sp2.transform.position, tent.ep2.transform.position, t);
            tent.p2.rotation = Quaternion.Lerp(tent.sp2.transform.rotation, tent.ep2.transform.rotation, t);
        }
    }

    void InitializeInterpolations() {
        interpolations = new Interpolations[3];

        resting = new Interpolations();
        resting.p1Vec = new Vector3(1.4f, 0f, 0f);
        resting.p1Rot = -110f;
        resting.p2Vec = new Vector3(2f, -5f, 0f);
        resting.p2Rot = -200f;

        interpolations[0] = new Interpolations();
        interpolations[0].p1Vec = new Vector3(0.2f, 0.2f, 0f);
        interpolations[0].p1Rot = 0f;//20f;
        interpolations[0].p2Vec = Vector3.zero;
        interpolations[0].p2Rot = 0f;//-10f;

        interpolations[1] = new Interpolations();
        interpolations[1].p1Vec = new Vector3(-0.2f, -0.2f, 0f);
        interpolations[1].p1Rot = 0f;//-110f;
        interpolations[1].p2Vec = Vector3.zero;
        interpolations[1].p2Rot = 0f;//-200f;

        // interpolations[2] = new Interpolations();
        // interpolations[2].p1Vec = (interpolations[0].p1Vec + interpolations[1].p1Vec) * -1;
        // interpolations[2].p1Rot = 0f;//-110f;
        // interpolations[2].p2Vec = (interpolations[0].p2Vec + interpolations[1].p2Vec) * -1;
        // interpolations[2].p2Rot = 0f;//-200f;

        // interpolations[2] = new Interpolations();
        // interpolations[2].p1Vec = new Vector3(0.2f, 0.1f, 0f);
        // interpolations[2].p1Rot = 10f;
        // interpolations[2].p2Vec = new Vector3(1f, 1f, 0f);
        // interpolations[2].p2Rot = 180f;
    }

    void UpdateCurrentPreviousInterpolation()
    {

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
        tentacle.start = new Interpolations();
        tentacle.end = new Interpolations();
        tentacle.t = transform.Find(name);
        tentacle.mesh = transform.Find(meshName);
        tentacle.sp1 = new GameObject();
        tentacle.ep1 = new GameObject();
        tentacle.sp2 = new GameObject();
        tentacle.ep2 = new GameObject();

        Transform rig = tentacle.t.Find("Rig 1");
        Transform constraint = rig.Find("tentacle_constraint");

        tentacle.p0 = constraint.Find("p0");
        tentacle.p1 = constraint.Find("p1");
        tentacle.p2 = constraint.Find("p2");

        return tentacle;
    }
}
