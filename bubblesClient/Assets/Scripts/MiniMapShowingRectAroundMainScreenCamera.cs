using UnityEngine;
using System.Collections;

public class MiniMapShowingRectAroundMainScreenCamera : MonoBehaviour {
	float height, width;
	Camera mainCam;
	// Use this for initialization
	void Start () {
		mainCam = Camera.main;
	}
	
	// Update is called once per frame
	void Update () {
		height = 2f * mainCam.orthographicSize;
		width = height * mainCam.aspect;
		this.gameObject.transform.localScale = new Vector2 (height, width / 2.0f);
//		this.gameObject.transform.position = new Vector2(mainCam.transform.position.x,mainCam.transform.position.x) ;
	}

}
