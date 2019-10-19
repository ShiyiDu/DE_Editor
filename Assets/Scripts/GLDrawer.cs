using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GLDrawer : MonoBehaviour
{
	//this class is only to make the scene view class be able to call GL draws
	private GameObject scene;
	private SceneView sceneView;

	public void SetCurrentScene(GameObject scene)
	{
		//check if there are multiple descene in the game right now
		if (this.scene == scene) Debug.LogError("there are multiple DEScene in the scene right now");
		this.scene = scene;
		sceneView = scene.GetComponent<SceneView>();
	}

	void OnPostRender()
	{
		if (sceneView != null) sceneView.MyPostRenderer();
	}

}
