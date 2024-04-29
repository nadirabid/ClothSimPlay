using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class Tentacle {
    public Transform t;
    public Transform p0;
    public Transform p1;
    public Transform p2;
}
public class TentacleController : MonoBehaviour
{
    // Start is called before the first frame update

    Tentacle[] tentacles;

    void Start()
    {
        tentacles = new Tentacle[15];

        for (int i = 0; i < tentacles.Length; i++)
        {
            tentacles[i] = GetTentacle(i);
            SetDefaultTentacleTransforms(i);
        }
    }

    void SetDefaultTentacleTransforms(int i)
    {
        Tentacle tent = tentacles[i];

        // TODO: determine position
        tent.p1.position += new Vector3(1.4f, 0, 0);
        tent.p1.RotateAround(tent.p1.position, Vector3.forward, -110);

        tent.p2.position += new Vector3(2, -5, 0);
        tent.p2.RotateAround(tent.p2.position, Vector3.forward, -200);
    }

    Tentacle GetTentacle(int i)
    {
        string name = "ArmatureTentacle";
        if (i > 9)
        {
            name += $".0{i}";
        }
        else if (i > 0)
        {
            name += $".00{i}";
        }

        //Debug.Log(name);

        Tentacle tentacle = new Tentacle();
        tentacle.t = transform.Find(name);

        Transform rig = tentacle.t.Find("Rig 1");
        Transform constraint = rig.Find("tentacle_constraint");

        tentacle.p0 = constraint.Find("p0");
        tentacle.p1 = constraint.Find("p1");
        tentacle.p2 = constraint.Find("p2");

        return tentacle;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
