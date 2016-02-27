using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class UIController : MonoBehaviour {

	Camera mainCam;
	public Camera miniCam;
	public Button camLockBtn;
	public Button blessingModeBtn;
	public Button pushLinkBtn;
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

	public void BlessingMode(int ZeroOffOneOn)
	{
		netClientMgr.GOspinner.blessingMode = (ZeroOffOneOn == 0) ? false : true;
		blessingModeBtn.image.color = netClientMgr.GOspinner.blessingMode ? Color.blue : Color.grey;
		
	}

	public void PushLinkMode(int ZeroOffOneOn)
	{
		netClientMgr.GOspinner.pushLinkMode = (ZeroOffOneOn == 0) ? false : true;
		pushLinkBtn.image.color = netClientMgr.GOspinner.pushLinkMode ? Color.blue : Color.grey;
	}

}
