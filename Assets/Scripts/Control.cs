using UnityEngine;
using System.Collections;

public class Control : MonoBehaviour
{
    public Material Material1;
    public Material Material2;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 1000000))
            {
                var mesh = hit.transform.gameObject.GetComponent<MeshFilter>().mesh;
                var material = hit.transform.gameObject.GetComponent<MeshRenderer>().material;

                var slicedMesh = MeshSlicer.Slice(mesh, transform.up, hit.point - hit.transform.position);

                var go1 = new GameObject();
                go1.transform.position = hit.transform.position;
                go1.AddComponent<MeshFilter>().mesh = slicedMesh.Mesh1;
                go1.AddComponent<MeshRenderer>().material = Material1;

                var go2 = new GameObject();
                go2.transform.position = hit.transform.position;
                go2.AddComponent<MeshFilter>().mesh = slicedMesh.Mesh2;
                go2.AddComponent<MeshRenderer>().material = Material2;

                Destroy(hit.transform.gameObject);
            }
        }
    }
}
