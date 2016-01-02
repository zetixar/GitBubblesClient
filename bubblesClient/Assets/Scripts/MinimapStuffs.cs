using UnityEngine;
using System.Collections;

public class MinimapStuffs : MonoBehaviour {
	// INSTANCE FIELDS
	public Camera minimapCamera;
	private float correctionFactor = 1.0f;
	private Rect baseRect;
	private Rect adjustedRect;

	void Start()
	{
		CorrectMinimapViewport();
	}
	// MONO METHODS
	void Update()
	{
		if(Input.GetKeyDown(KeyCode.O))
		CorrectMinimapViewport();
	}
	
	public void CorrectMinimapViewport()
	{
		baseRect = minimapCamera.rect;
		float correctionFactor = 1.7f / Camera.main.aspect;

//		float correctionFactor = 1.77778f / Camera.main.aspect;
		adjustedRect = new Rect( baseRect.x - ( ( baseRect.width * correctionFactor ) - baseRect.width ), baseRect.y , baseRect.width * correctionFactor, baseRect.height );
		minimapCamera.rect = adjustedRect;
	}
}

