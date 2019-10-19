//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

////a look up map in order to fast locate DEObjects
//public static class CurrentScene
//{
//	private static Dictionary<DEPosition, List<GameObject>> currentScene = new Dictionary<DEPosition, List<GameObject>>;

//	//only add it in the look up dictionary not instantiate it
//	public static void AddObject(DEPosition position, GameObject target)
//	{
//		List<GameObject> objList;
//		if (currentScene.TryGetValue(position, out objList)) {
//			objList.Add(target);
//		} else {
//			objList = new List<GameObject>();
//			objList.Add(target);
//			currentScene.Add(position, objList);
//		}
//	}

//	public static void RemoveObjects(DEPosition position)
//	{
//		currentScene.Remove(position);
//	}

//	//only remove it in the look up dictionary, not destroy it
//	public static void RemoveObject(DEPosition position, int instanceID)
//	{
//		List<GameObject> objs = currentScene[position];
//		foreach (GameObject obj in objs) {
//			if (obj.GetInstanceID() == instanceID) {
//				objs.Remove(obj);
//				objs.Sort();
//			}
//		}
//	}

//	//public static List<GameObject> FindGameobjects(string sceneName, DEPosition position)
//	//{
//	//	return sceneDic[sceneName][position];
//	//}

//	//public static Dictionary<DEPosition, List<GameObject>> FindScene(string sceneName)
//	//{
//	//	return sceneDic[sceneName];
//	//}

//	//Generate the map of the all the scenes to look up, the base unit must have tag "baseunit", so do descene
//	public static void CreateMap()
//	{
//		GameObject[] allBaseUnits = GameObject.FindGameObjectsWithTag("DEBase");
//		GameObject[] allScenes = GameObject.FindGameObjectsWithTag("DEScene");
//		foreach (GameObject scene in allScenes) {
//			int totalObjects = scene.transform.childCount;
//			Dictionary<DEPosition, List<GameObject>> currentScene = new Dictionary<DEPosition, List<GameObject>>();
//			for (int i = 0; i < totalObjects; i++) {
//				List<GameObject> currentPosition;
//				GameObject curObj = scene.transform.GetChild(i).gameObject;
//				DEPosition objPos = DEPosition.NameToPosition(curObj.name);
//				if (currentScene.TryGetValue(objPos, out currentPosition)) {
//					currentPosition.Add(curObj);
//					currentScene[objPos] = currentPosition;
//				} else {
//					currentPosition = new List<GameObject>();
//					currentPosition.Add(curObj);
//					currentScene.Add(objPos, currentPosition);
//				}
//			}
//			//sceneDic.Add(scene.name, currentScene);
//		}
//	}

//	public static void ClearMap()
//	{
//		//sceneDic.Clear();
//	}
//}
