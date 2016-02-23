using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class MiniCameraController : MonoBehaviour {

	Camera mainCam;
	public Camera miniCam;
	public Button camLockBtn;
	// Use this for initialization
	void Start () {
		mainCam = Camera.main;
	}

	public void MoveCamToWhereIClickOnMiniMap ()
	{
		Debug.Log("minimapclicked");
		RaycastHit hit;
		Ray ray = miniCam.ScreenPointToRay(Input.mousePosition);
		if(Physics.Raycast(ray,out hit))
		{
			mainCam.transform.position = new Vector3(hit.point.x, hit.point.y, mainCam.transform.position.z);// Vector3.Lerp(mainCam.transform.position, hit.point, 0.1f);
		}
	}

	public void CamLock()
	{
		netClientMgr.GOspinner.cameraFollowMynode = !netClientMgr.GOspinner.cameraFollowMynode;
		camLockBtn.image.color = netClientMgr.GOspinner.cameraFollowMynode ? Color.green : Color.grey;
	}

}
