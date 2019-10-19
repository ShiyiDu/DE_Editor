using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

//The Main Controller of the current scene
//This cannot have communications with handle drawer
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Text;
public static class SceneControl
{
	//keep the information of all the objects of the scene except base unit
	//private static Dictionary<DEPosition, List<GameObject>> currentObj = new Dictionary<DEPosition, List<GameObject>>();
	////keep the info of all the base units of the scene
	//private static Dictionary<DEPosition, GameObject> currentBase = new Dictionary<DEPosition, GameObject>();

	private static Ray mouseRay = new Ray();
	private static Camera mainCam;

	private static DEPosition beginPos; //the position where selection begins
	private static DEDirection beginDir;
	private static DEPosition endPos;
	private static DEDirection endDir;

	private static DEDirection planeDirection; //this only useful if isPlane is true

	private static bool isPlane = true;
	private static bool selected = false;

	private static List<DEPosition> actualSelect = new List<DEPosition>(); //the actual selection of the units, this exclude the units that aren't there yet
	private static List<DEPosition> selection = new List<DEPosition>();

	private static Stack<Step> backwardSteps = new Stack<Step>(50);// lets store 50 steps for now
	private static Stack<Step> forwardSteps = new Stack<Step>(50);//this is used to revert forward

	//all the variables above are supposed to be updated automatically, just access them whenever needed
	//if the user actually selected something
	public static bool Selected() { return selected; }

	public static bool IsPlane() { return isPlane; }

	public static DEDirection SelectDirection() { return planeDirection; }

	public static DEPosition BeginPos() { return beginPos; }

	public static DEPosition EndPos() { return endPos; }

	public static DEDirection BeginDir() { return beginDir; }

	public static DEDirection EndDir() { return endDir; }

	//create a base that only have baseunits
	public static void InitializeBase(int x, int y, GameObject basePref, GameObject currentScene)
	{
		//Debug.Log(Application.persistentDataPath);
		//Debug.Log(Application.dataPath);

		BaseControl.Clear();

		mainCam = Camera.main;

		BaseControl.CreateBase(x, y, basePref, currentScene);
	}

	public static List<DEPosition> GetVisibleSelection()
	{
		List<DEPosition> result = new List<DEPosition>();
		foreach (DEPosition pos in actualSelect) {
			if (BaseControl.IsVisible(pos)) result.Add(pos);
		}
		return result;
	}

	public static bool ContainsPos(DEPosition pos)
	{
		return BaseControl.ContainsPos(pos);
	}

	//get the initial unit being selected
	public static void StartSelecting()
	{
		mouseRay = mainCam.ScreenPointToRay(Input.mousePosition);
		DEPosition startPos;
		DEDirection startDir;

		if (BaseControl.GetHitInfo(mouseRay, out startPos, out startDir)) {
			//Debug.Log("hit");
			if (beginPos == startPos && beginDir == startDir && actualSelect.Count == 1) {
				beginPos = DEPosition.zero;
				selected = false;
			} else {
				beginPos = startPos;
				beginDir = startDir;
				selected = true;
			}
		} else {
			selected = false;
		}
	}

	//update the group of objs/units being selected
	public static void UpdateSelecting()
	{
		mouseRay = mainCam.ScreenPointToRay(Input.mousePosition);
		DEPosition stopPos;
		DEDirection stopDir;

		if (BaseControl.GetHitInfo(mouseRay, out stopPos, out stopDir)) {
			endPos = stopPos;
			endDir = stopDir;
		}

		PlaneCheck();
		SetSelection();
	}

	//get the last unit being selected
	public static void StopSelecting()
	{
		mouseRay = mainCam.ScreenPointToRay(Input.mousePosition);
		DEPosition stopPos;
		DEDirection stopDir;

		if (BaseControl.GetHitInfo(mouseRay, out stopPos, out stopDir)) {
			endPos = stopPos;
			endDir = stopDir;
		}

		PlaneCheck();
		SetSelection();
	}

	//create unit based on selection
	public static void CreateUnits()
	{
		if (!selected) return;
		forwardSteps.Clear();
		//if plane, add on top of that
		if (isPlane) {
			DEPosition[] toBeCreate = new DEPosition[actualSelect.Count];

			for (int i = 0; i < toBeCreate.Length; i++) {
				toBeCreate[i] = actualSelect[i].GetDirection(planeDirection);
			}

			BaseControl.CreateBaseUnits(toBeCreate);
			backwardSteps.Push(new Step(StepType.create, toBeCreate, beginPos, beginDir, endPos, endDir));

			beginPos = beginPos.GetDirection(planeDirection);
			endPos = endPos.GetDirection(planeDirection);
		} else {
			BaseControl.CreateBaseUnits(selection.ToArray());
			backwardSteps.Push(new Step(StepType.create, selection.ToArray(), beginPos, beginDir, endPos, endDir));
		}

		//make sure the memory usage doesn't go too large
		if (backwardSteps.Count > 60) {
			Stack<Step> temp = new Stack<Step>(40);
			for (int i = 0; i < 40; i++) {
				temp.Push(backwardSteps.Pop());
			}
			backwardSteps.Clear();
			for (int i = 0; i < 40; i++) {
				backwardSteps.Push(temp.Pop());
			}
			UnityEngine.Debug.Log("cleaned");
		}

		SetSelection();
	}

	public static void DeleteUnits()
	{
		if (!selected) return;

		backwardSteps.Push(new Step(StepType.destroy, selection.ToArray(), beginPos, beginDir, endPos, endDir));
		forwardSteps.Clear();
		if (isPlane) {
			BaseControl.DestroyBaseUnits(selection.ToArray());

			beginPos = beginPos.GetOppositeDirection(planeDirection);
			endPos = endPos.GetOppositeDirection(planeDirection);
		} else {
			BaseControl.DestroyBaseUnits(selection.ToArray());
		}

		if (backwardSteps.Count > 80) {
			Stack<Step> temp = new Stack<Step>(60);
			for (int i = 0; i < 60; i++) {
				temp.Push(backwardSteps.Pop());
			}
			backwardSteps.Clear();
			for (int i = 0; i < 60; i++) {
				backwardSteps.Push(temp.Pop());
			}
			UnityEngine.Debug.Log("cleaned");
		}

		SetSelection();
	}

	public static void SaveFile(string fileName)
	{
		SaveBase(fileName);
	}

	public static void RevertBack()
	{
		if (backwardSteps.Count == 0) return;
		SetSelection();

		Step step = backwardSteps.Pop();
		forwardSteps.Push(step);
		if (step.GetStepType() == StepType.create) {
			BaseControl.DestroyBaseUnits(step.GetUnits());
		} else if (step.GetStepType() == StepType.destroy) {
			BaseControl.CreateBaseUnits(step.GetUnits());
		}
		beginPos = step.GetBeginPos();
		beginDir = step.GetBeginDir();
		endPos = step.GetEndPos();
		endDir = step.GetEndDir();
		if (beginDir == endDir) {
			isPlane = true;
			planeDirection = beginDir;
		}
		SetSelection();
	}

	public static void RevertForward()
	{
		if (forwardSteps.Count == 0) return;
		SetSelection();

		Step step = forwardSteps.Pop();
		backwardSteps.Push(step);
		beginPos = step.GetBeginPos();
		beginDir = step.GetBeginDir();
		endPos = step.GetEndPos();
		endDir = step.GetEndDir();

		if (step.GetStepType() == StepType.create) {
			BaseControl.CreateBaseUnits(step.GetUnits());
			if (beginDir == endDir) {
				isPlane = true;
				planeDirection = beginDir;
				beginPos = beginPos.GetDirection(beginDir);
				endPos = endPos.GetDirection(endDir);
			} else {
				isPlane = false;
			}
		} else if (step.GetStepType() == StepType.destroy) {
			BaseControl.DestroyBaseUnits(step.GetUnits());
			if (beginDir == endDir) {
				isPlane = true;
				planeDirection = beginDir;
				beginPos = beginPos.GetOppositeDirection(beginDir);
				endPos = endPos.GetOppositeDirection(endDir);
			} else {
				isPlane = false;
			}
		}

		SetSelection();
	}

	//initialize a new scene with only base units
	public static void Initialize(int xSize, int ySize)
	{

	}

	public static bool isSaving() { return Saving; }

	public static void LoadBase(string fileName)
	{
		BaseControl.Clear();
		forwardSteps.Clear();
		backwardSteps.Clear();
		List<DEPosition> allUnits = DEUtility.ReadBaseFile(fileName);
		BaseControl.CreateBaseUnits(allUnits.ToArray());
	}

	//it can take a ridiculously amount time to save the data, therefore its better to save data in another thread
	private static void SaveBase(string fileName)
	{
		if (Saving) return;
		Saving = true;

		string path = Application.dataPath + "/DEScenes" + "//" + fileName + ".debs";
		List<DEPosition> allUnits = BaseControl.GetAllExistUnits();

		Thread writeBaseData = new Thread(new ParameterizedThreadStart(WriteBaseData));

		object baseData = new object[2] { path, allUnits };
		writeBaseData.Start(baseData);
	}

	private static bool Saving = false;
	private static void WriteBaseData(object args)
	{
		object[] baseData = args as object[];
		string path = baseData[0] as string;
		List<DEPosition> allUnits = baseData[1] as List<DEPosition>;

		DEUtility.SaveBaseFile(path, allUnits);

		Saving = false;
		//stopWatch.Reset();
	}

	//check if the current selection is a plane, and set the direction of the plane if true
	private static void PlaneCheck()
	{

		if (beginDir == endDir) {
			//Debug.Log("PlaneChecked");
			planeDirection = beginDir;
			if (beginDir == DEDirection.right || beginDir == DEDirection.left) {
				isPlane = beginPos.x == endPos.x;
				//Debug.Log("x");
				return;
			}
			if (beginDir == DEDirection.forward || beginDir == DEDirection.back) {
				isPlane = beginPos.y == endPos.y;
				//Debug.Log("y");
				return;
			}
			if (beginDir == DEDirection.up || beginDir == DEDirection.down) {
				isPlane = beginPos.z == endPos.z;
				//Debug.Log("z");
				return;
			}
		}
		isPlane = false;
	}

	//set the actual selection group based on what the scene actually have
	private static void SetSelection()
	{
		if (!selected) {
			selection.Clear();
			actualSelect.Clear();
			return;
		}
		selection = DEUtility.GetSelectGroup(beginPos, beginDir, endPos, endDir).ToList();
		actualSelect.Clear();
		//to be optimized
		//maybe only calculate this when selection has changed?
		foreach (DEPosition pos in selection) {
			if (BaseControl.ContainsPos(pos)) actualSelect.Add(pos);
		}
	}

}
