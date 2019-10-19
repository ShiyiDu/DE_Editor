using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyButton : MonoBehaviour
{
	GameObject currentScene;
	public void LoadBase()
	{
		string baseName = transform.name;
		currentScene = GameObject.Find("Scene");
		currentScene.GetComponent<SceneView>().LoadScene(baseName);
	}
}
