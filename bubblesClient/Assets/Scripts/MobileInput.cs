using UnityEngine;

using System.Collections;
using UnityEngine.UI;
//&& !EventSystem.current.IsPointerOverGameObject())

public class MobileInput : MonoBehaviour {

	Camera mainCamera;
	public GameObject[] cubes = new GameObject[5];
	public Button[] button = new Button[3];
	private bool buttonpressed = false;

	// Use this for initialization
	void Start () {

		mainCamera = Camera.main;

	}

	public void buttonpressedd()
	{
		buttonpressed = true;
		Debug.Log("buttn true");
	}
	public void buttonunpressedd()
	{
		buttonpressed = false;
		Debug.Log("buttn false");
	}
	// Update is called once per frame
	void Update () {
		if (Input.touchCount == 1 && Input.touchCount < 2)
		{
//			Color colorr = 
			if(Input.GetTouch(0).phase == TouchPhase.Ended)
					cubes[1].GetComponent<Renderer>().material.color =  new Color(Random.Range(0.0f,1.0f),Random.Range(0.0f,1.0f),Random.Range(0.0f,1.0f));
		
//			if(Input.GetTouch(0).phase == TouchPhase.Moved)
//				cubes[1].GetComponent<Renderer>().material.color =  new Color(Random.Range(0.0f,1.0f),Random.Range(0.0f,1.0f),Random.Range(0.0f,1.0f));
		}


		if (Input.touchCount == 2)
		{
			if(buttonpressed && Input.GetTouch(1).phase == TouchPhase.Began)
			{
//				cubes[1].GetComponent<Renderer>().material.color =  Color.blue;
				cubes[2].GetComponent<Renderer>().material.color =  new Color(Random.Range(0.0f,1.0f),Random.Range(0.0f,1.0f),Random.Range(0.0f,1.0f));


			}
			//			Color colorr = 
//			if(Input.GetTouch(0).phase == TouchPhase.Began)
//				cubes[2].GetComponent<Renderer>().material.color =  new Color(Random.Range(0.0f,1.0f),Random.Range(0.0f,1.0f),Random.Range(0.0f,1.0f));
//
//			if(Input.GetTouch(1).phase == TouchPhase.Moved)
//				cubes[2].GetComponent<Renderer>().material.color =  new Color(Random.Range(0.0f,1.0f),Random.Range(0.0f,1.0f),Random.Range(0.0f,1.0f));
//
		}


//		if (Input.touchCount == 2)
//		{
//			cubes[1].GetComponent<Renderer>().material.color = Color.red;
//		}
//		if (Input.touchCount == 1)
//		{
//			cubes[1].GetComponent<Renderer>().material.color = Color.red;
//		}
//		if (Input.touchCount == 1)
//		{
//			cubes[1].GetComponent<Renderer>().material.color = Color.red;
//		}
		if (Input.touchCount == 2 && !buttonpressed) 
			{
				Vector2 cameraViewSize = new Vector2 (mainCamera.pixelWidth, mainCamera.pixelHeight);
				Touch touchZero = Input.GetTouch(0);
				Touch touchOne = Input.GetTouch(1);

				// Find the position in the previous frame of each touch.
				Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
				Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

				// Find the magnitude of the vector (the distance) between the touches in each frame.
				float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
				float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

				// Find the difference in the distances between each frame.
				float deltaMagnitudeDiff = (prevTouchDeltaMag - touchDeltaMag) / 6.0f;
				Debug.Log("pinch for mobile zooming" + deltaMagnitudeDiff);
			mainCamera.orthographicSize += deltaMagnitudeDiff;


//
//				mainCamera.transform.position += mainCamera.transform.TransformDirection((touchZeroPrevPos + touchOnePrevPos - cameraViewSize) * mainCamera.orthographicSize / cameraViewSize.y);
//				if (deltaMagnitudeDiff > 0.0f)
//				{
//					mainCamera.orthographicSize += deltaMagnitudeDiff;
//				}
//				if (deltaMagnitudeDiff < 0.0f)
//				{
//				mainCamera.orthographicSize -= deltaMagnitudeDiff * -1;
//				}
//				mainCamera.transform.position -= mainCamera.transform.TransformDirection((touchZero.position + touchOne.position - cameraViewSize) * mainCamera.orthographicSize / cameraViewSize.y);
			}
	}
}
