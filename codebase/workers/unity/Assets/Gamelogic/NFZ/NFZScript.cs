using Improbable;
using Improbable.Controller;
using UnityEngine; 
using System.Collections;

public class NFZScript : MonoBehaviour
{
    public GameObject[] nfzNodes;

    public Improbable.Controller.NoFlyZone GetNoFlyZone()
    {
        Improbable.Collections.List<Vector3f> positions = new Improbable.Collections.List<Vector3f>();

        for (int i = 0; i < nfzNodes.Length; i++)
        {
            positions.Add(nfzNodes[i].transform.position.ToSpatialVector3f());
        }

        return NFZ_Templates.CreateCustomNoFlyZone(positions);
    }
}
