using UnityEngine;
using System.Collections.Generic;

public class Control : MonoBehaviour
{
    public Material Material1;
    public Material Material2;

    public static Vector3 Position;
    
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 1000000))
            {
                if (hit.transform.GetComponent<MeshRenderer>() != null)
                {
                    Position = hit.transform.position;

                    Material1 = new Material(Material1);
                    Material2 = new Material(Material2);
                    Material1.color = Random.ColorHSV();
                    Material2.color = Random.ColorHSV();

                    var mesh = hit.transform.gameObject.GetComponent<MeshFilter>().mesh;
                    var material = hit.transform.gameObject.GetComponent<MeshRenderer>().material;

                    var slicedMesh = MeshSlicer.Slice(mesh, hit.transform.InverseTransformDirection(transform.up), hit.transform.InverseTransformPoint(hit.point));

                    var go1 = new GameObject();
                    go1.transform.position = hit.transform.position;
                    go1.transform.rotation = hit.transform.rotation;
                    go1.AddComponent<MeshFilter>().mesh = slicedMesh.Mesh1;
                    go1.AddComponent<MeshCollider>().sharedMesh = slicedMesh.Mesh1;
                    go1.AddComponent<MeshRenderer>().material = Material1;
                    go1.GetComponent<MeshCollider>().convex = true;
                    //go1.AddComponent<Rigidbody>();

                    var go2 = new GameObject();
                    go2.transform.position = hit.transform.position;
                    go2.transform.rotation = hit.transform.rotation;
                    go2.AddComponent<MeshFilter>().mesh = slicedMesh.Mesh2;
                    go2.AddComponent<MeshCollider>().sharedMesh = slicedMesh.Mesh2;
                    go2.AddComponent<MeshRenderer>().material = Material2;
                    go2.GetComponent<MeshCollider>().convex = true;
                    //go2.AddComponent<Rigidbody>();

                    Destroy(hit.transform.gameObject);
                }
            }
        }
    }
}
