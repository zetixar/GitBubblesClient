using UnityEngine;
using System.Collections;

public class MiniCameraController : MonoBehaviour {

	Camera mainCam;
	public Camera miniCam;
	// Use this for initialization
	void Start () {
		mainCam = Camera.main;
	}
	// Update is called once per frame
	void Update () {
		if(Input.GetMouseButtonDown(0)) // && mainCam.pixelRect.Contains(Input.mousePosition))
		{
			RaycastHit hit;
			Ray ray = miniCam.ScreenPointToRay(Input.mousePosition);
			if(Physics.Raycast(ray,out hit))
				{
				mainCam.transform.position = new Vector3(hit.point.x, hit.point.y, mainCam.transform.position.z);// Vector3.Lerp(mainCam.transform.position, hit.point, 0.1f);
				}
		}
	}

}
