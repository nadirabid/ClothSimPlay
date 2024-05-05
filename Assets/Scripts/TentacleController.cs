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

class CustomTransform
{
    public CustomTransform()
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
    }
    public Vector3 position;
    public Quaternion rotation;
}

public class TentacleController : MonoBehaviour
{
    // Start is called before the first frame update

    Tentacle[] tentacles;

    CustomTransform[] pt0;
    CustomTransform[] pt1;
    CustomTransform[] pt2;

    private float timeToMove = 1f;
    private float elapsedTime = 10000f;

    private int currentInterp = 0;

    void Start()
    {
        InitializeTransforms();

        tentacles = new Tentacle[15];
        for (int i = 0; i < tentacles.Length; i++)
        {
            tentacles[i] = GetTentacle(i);
            SetDefaultTentacleTransforms(i);
        }
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;

        if (elapsedTime > timeToMove)
        {
            elapsedTime = 0;
            currentInterp = (currentInterp + 1) % pt0.Length;

            Debug.Log("interp index" + currentInterp);
        }

        int prevInterp = (currentInterp - 1 + pt0.Length) % pt0.Length;
        // TODO: realistically we need interp to have a 

        float t = elapsedTime / timeToMove;
        //Debug.Log($"elapsedTime {elapsedTime} / {timeToMove} = {t}");

        for (int i = 0; i < tentacles.Length; i++)
        {
            Tentacle tent = tentacles[i];

            tent.p1.localPosition = Vector3.Lerp(pt1[prevInterp].position, pt1[currentInterp].position, t);
            tent.p1.localRotation = Quaternion.Lerp(pt1[prevInterp].rotation, pt1[currentInterp].rotation, t);

            tent.p2.localPosition = Vector3.Lerp(pt2[prevInterp].position, pt2[currentInterp].position, t);
            tent.p2.localRotation = Quaternion.Lerp(pt2[prevInterp].rotation, pt2[currentInterp].rotation, t);
        }
    }

    void InitializeTransforms()
    {
        int num = 4;
        pt0 = new CustomTransform[num];
        pt1 = new CustomTransform[num];
        pt2 = new CustomTransform[num];

        pt0[0] = new CustomTransform();
        pt1[0] = new CustomTransform();
        pt1[0].position = new Vector3(1.8f, 2.2f, 0f);
        pt1[0].rotation = Quaternion.AngleAxis(-100f, Vector3.forward);
        pt2[0] = new CustomTransform();
        pt2[0].position = new Vector3(2.1f, 0f, 0f);
        pt2[0].rotation =Quaternion.AngleAxis(-210f, Vector3.forward);

        pt0[1] = new CustomTransform();
        pt1[1] = new CustomTransform();
        pt1[1].position = new Vector3(2.0f, 2.1f, 0f);
        pt1[1].rotation = Quaternion.AngleAxis(-70f, Vector3.forward);
        pt2[1] = new CustomTransform();
        pt2[1].position = new Vector3(3.9f, 1.3f, 0f);
        pt2[1].rotation =Quaternion.AngleAxis(-130f, Vector3.forward);

        pt0[2] = new CustomTransform();
        pt1[2] = new CustomTransform();
        pt1[2].position = new Vector3(1.8f, 1.8f, 0f);
        pt1[2].rotation = Quaternion.AngleAxis(-80f, Vector3.forward);
        pt2[2] = new CustomTransform();
        pt2[2].position = new Vector3(4.1f, 2.0f, 0f);
        pt2[2].rotation =Quaternion.AngleAxis(-70f, Vector3.forward);

        pt0[3] = new CustomTransform();
        pt1[3] = new CustomTransform();
        pt1[3].position = new Vector3(1.4f, 2.0f, 0f);
        pt1[3].rotation = Quaternion.AngleAxis(-110f, Vector3.forward);
        pt2[3] = new CustomTransform();
        pt2[3].position = new Vector3(2f, -2f, 0f);
        pt2[3].rotation = Quaternion.AngleAxis(-200f, Vector3.forward);
    }

    void SetDefaultTentacleTransforms(int i)
    {
        Tentacle tent = tentacles[i];

        tent.p1.localPosition = pt1[3].position;
        tent.p1.localRotation = pt1[3].rotation;

        tent.p2.localPosition = pt2[3].position;
        tent.p2.localRotation = pt2[3].rotation;

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
