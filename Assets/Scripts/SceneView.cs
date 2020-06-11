using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//the main input controller of the editor
using System.Threading;
using System;
using UnityEngine.UI;
public class SceneView : MonoBehaviour
{
    public GameObject currentUI;
    public Text currentSceneIndicator;

    //the viewing angles for the scene
    [SerializeField]
    private KeyCode firstDir = KeyCode.F1;
    [SerializeField]
    private KeyCode secondDir = KeyCode.F2;
    [SerializeField]
    private KeyCode thirdDir = KeyCode.F3;
    [SerializeField]
    private KeyCode forthDir = KeyCode.F4;

    [SerializeField]
    private KeyCode createUnit = KeyCode.W;
    [SerializeField]
    private KeyCode deleteUnit = KeyCode.Q;

    [SerializeField]
    private KeyCode[] saveFile = { KeyCode.LeftControl, KeyCode.S };
    [SerializeField]
    private KeyCode[] revertBack = { KeyCode.LeftControl, KeyCode.Z };
    [SerializeField]
    private KeyCode[] revertForward = { KeyCode.LeftControl, KeyCode.LeftShift, KeyCode.Z };
    [SerializeField]
    private int selectUnit = 0; //the mouse button for selecting unit
    [SerializeField]
    private int moveScene = 1;
    [SerializeField]
    private int rotateScene = 2;
    [SerializeField]
    private int resetScene = 2; //double click this to reset the scene
    [SerializeField]
    private KeyCode[] moveScene_1 = { KeyCode.Space, KeyCode.Mouse0 };

    [SerializeField]
    private Material selectFace; //the face color of selection
    [SerializeField]
    private Material selectLine; //the line color of selection
    [SerializeField]
    private Material selectDottedLine; //the doted line material
    [SerializeField]
    private Material selectVert; //the verts color of selection
    [SerializeField]
    private Material baseMaterial;
    [SerializeField]
    private GameObject baseUnit;
    [SerializeField]
    private Vector2 baseSize;
    [SerializeField]
    private GameObject savingIndicator;

    private bool selected = false;
    private bool isPlane = false;
    private bool movingScene = false;
    private bool repeatingAction = false;
    private bool updaterRunning = false; //the coroutine updater is running

    private DEDirection selectDirection;

    private DEPosition[] rfuAndLbd;
    private DEPosition[] visibleSelection;
    private DEPosition beginPos = new DEPosition();
    private DEPosition endPos = new DEPosition();
    private DEDirection beginDir;
    private DEDirection endDir;

    private Camera mainCam;

    private Coroutine updater;
    private Coroutine actionRepeater;
    //temp variables
    private Vector3 offset = new Vector3();

    private float cameraSize;
    private Vector3 scenePosition;
    private Quaternion sceneRotation;

    private string fileName = "NewBase";

    public void CreateNewScene()
    {
        if (!SaveBase()) return;
        fileName = "NewBase";
        SceneControl.InitializeBase(32, 32, baseUnit, gameObject);
    }

    public void SetSceneName(string newName)
    {
        fileName = newName;
        Debug.Log("file name: " + fileName);
    }

    public void LoadScene(string sceneName)
    {
        if (!SaveBase()) return;
        fileName = sceneName;
        transform.parent.rotation = sceneRotation;
        transform.position = scenePosition;
        mainCam.orthographicSize = cameraSize;
        SceneControl.LoadBase(fileName);
    }

    public bool SaveBase()
    {
        List<string> allBase = DEUtility.ReadAllBase();

        if (fileName == "NewBase") {
            currentUI.GetComponent<DEUI>().PopSaveConfirm();
            return false;
        }
        SceneControl.SaveFile(fileName);
        return true;
    }

    //private void StartUpdateSelection()
    //{
    //    if (!updaterRunning) {
    //        updater = StartCoroutine(UpdateScene());
    //        updaterRunning = true;
    //    }
    //}

    //private void StopUpdateSelection()
    //{
    //    if (updaterRunning) {
    //        StopCoroutine(updater);
    //        updaterRunning = false;
    //    }
    //}

    private void UpdateScene()
    {
        //while (true) {
        currentSceneIndicator.text = fileName;

        if (SceneControl.isSaving()) {
            savingIndicator.SetActive(true);
        } else {
            savingIndicator.SetActive(false);
        }
        bool selChange = SceneControl.SelectionChanged();
        if (Input.GetMouseButton(selectUnit) && !Input.GetKey(KeyCode.Space) && selChange)
            SceneControl.UpdateSelecting();

        if (selChange) visibleSelection = SceneControl.GetVisibleSelection().ToArray();
        beginPos = SceneControl.BeginPos();
        endPos = SceneControl.EndPos();
        beginDir = SceneControl.BeginDir();
        endDir = SceneControl.EndDir();

        rfuAndLbd = DEUtility.RfuAndLbd(beginPos, beginDir, endPos, endDir);
        isPlane = SceneControl.IsPlane();
        selectDirection = SceneControl.SelectDirection();

        HandleDrawer.UpdateSelection(rfuAndLbd[0], rfuAndLbd[1], visibleSelection, isPlane, selectDirection);

        //    yield return new WaitForSeconds(Time.deltaTime);
        //}
    }

    private IEnumerator RepeatAction(Action action)
    {
        while (true) {
            action.Invoke();
            yield return new WaitForSeconds(0.05f);
        }
    }

    float clickInterval = 0.2f;
    private float lastClickTime = -1;
    private void ResetTransform()
    {
        if (Input.GetKeyDown(firstDir)) {
            transform.parent.rotation = Quaternion.Euler(225, 0, 0);
        } else if (Input.GetKeyDown(secondDir)) {
            transform.parent.rotation = Quaternion.Euler(225, 0, 90);
        } else if (Input.GetKeyDown(thirdDir)) {
            transform.parent.rotation = Quaternion.Euler(225, 0, 180);
        } else if (Input.GetKeyDown(forthDir)) {
            transform.parent.rotation = Quaternion.Euler(225, 0, 270);
        }

        if (Input.GetMouseButtonUp(resetScene)) {
            if (lastClickTime == -1) {
                lastClickTime = Time.time;
                return;
            }
            if (Time.time - lastClickTime <= clickInterval) {

                transform.parent.rotation = sceneRotation;

                transform.position = scenePosition;
                mainCam.orthographicSize = cameraSize;
            }
            lastClickTime = Time.time;
        }
    }

    private float lastMouseX;
    private float lastMouseY;
    private void RotateScene()
    {
        if (!updatingLocalCenter) {
            Thread updateLocalCenter = new Thread(new ThreadStart(UpdateLocalCenter));
            updateLocalCenter.Start();
        }

        if (Input.GetMouseButtonDown(rotateScene)) {
            lastMouseX = Input.mousePosition.x;
            lastMouseY = Input.mousePosition.y;

            Matrix4x4 l2w = transform.localToWorldMatrix;
            Vector3 centerInWorld = l2w.MultiplyPoint3x4(centerInLocal);
            Vector3 originPos = transform.position;

            transform.parent.position = centerInWorld;
            transform.position = originPos;

            return;
            //float xRotation = transform.parent.rotation.eulerAngles.x;
            //// a really stupid way of doing this, but it works!
            //if (transform.parent.eulerAngles.y > 179) xRotation = xRotation - (xRotation - 270) * 2;
            //transform.position = new Vector3(transform.position.x, transform.position.y, Mathf.Tan(Mathf.Deg2Rad * (xRotation - 180)) * transform.position.y + 100);
            //Debug.Log(transform.parent.localEulerAngles.x + ": " + xRotation + ": ");
        }

        if (Input.GetMouseButton(rotateScene)) {
            transform.parent.Rotate(0, 0, -(Input.mousePosition.x - lastMouseX) / 5, Space.Self);
            transform.parent.Rotate((Input.mousePosition.y - lastMouseY) / 5, 0, 0, Space.World);

            lastMouseX = Input.mousePosition.x;
            lastMouseY = Input.mousePosition.y;
            return;
        }

        if (Input.GetMouseButtonUp(rotateScene)) {
            Vector3 originPos = transform.position;
            transform.parent.position = new Vector3(0, 0, 100);
            transform.position = originPos;
        }
    }

    private Vector3 centerInLocal;
    private bool updatingLocalCenter = false;
    private void UpdateLocalCenter()
    {
        updatingLocalCenter = true;
        centerInLocal = DEUtility.GetLocalCenter();
        Thread.Sleep(5000);
        updatingLocalCenter = false;
    }

    private void ZoomScene()
    {
        float mult = 1.1f;

        if (Input.mouseScrollDelta.y != 0) {
            offset = mainCam.ScreenToWorldPoint(Input.mousePosition) - gameObject.transform.position;
            mainCam.orthographicSize = mainCam.orthographicSize * Mathf.Pow(mult, -Input.mouseScrollDelta.y);
            if (mainCam.orthographicSize > 1 && mainCam.orthographicSize < 20) gameObject.transform.position = mainCam.ScreenToWorldPoint(Input.mousePosition) - offset;
        }
        mainCam.orthographicSize = Mathf.Max(1, mainCam.orthographicSize);
        mainCam.orthographicSize = Mathf.Min(40, mainCam.orthographicSize);
    }

    private void MoveScene()
    {

        if (Input.GetMouseButtonDown(moveScene) || GetKeysDown(moveScene_1)) {
            Ray mouseRay = mainCam.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(mouseRay, 200f, LayerMask.GetMask("DEBase"))) return;

            offset = mainCam.ScreenToWorldPoint(Input.mousePosition) - gameObject.transform.position;
            //StartUpdateSelection();
            movingScene = true;
        }

        if ((Input.GetMouseButton(moveScene) || GetKeys(moveScene_1)) && movingScene) {
            gameObject.transform.position = mainCam.ScreenToWorldPoint(Input.mousePosition) - offset;
            return;
        }
        if (movingScene) {
            movingScene = false;
            //StopUpdateSelection();
            updaterRunning = false;
        }
    }

    //this function will be called by GLdrawer in camara
    public void MyPostRenderer()
    {
        selected = SceneControl.Selected();
        if (selected) {
            //now I've got all the data I need, let's draw the selection
            if (isPlane) {
                HandleDrawer.DrawSelectionPlane();
            } else {
                HandleDrawer.DrawSelectionCube();
            }
        }
    }

    void Start()
    {

        mainCam = Camera.main;
        mainCam.GetComponent<GLDrawer>().SetCurrentScene(gameObject);
        //SceneControl.SetCurrentScene(gameObject);
        //SceneControl.SetBasePref(baseUnit);

        HandleDrawer.SetCurrentScene(gameObject);
        HandleDrawer.SetMaterial(selectFace, selectLine, selectDottedLine, selectVert);

        SceneControl.InitializeBase(Mathf.RoundToInt(baseSize.x), Mathf.RoundToInt(baseSize.y), baseUnit, gameObject);

        //StartUpdateSelection();
        cameraSize = mainCam.orthographicSize;
        scenePosition = transform.parent.position;
        sceneRotation = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !SceneControl.isSaving()) {
            SceneControl.SaveFile(fileName);
            Application.Quit();
        }

        if (Input.GetMouseButtonDown(selectUnit) && !Input.GetKey(KeyCode.Space)) {
            SceneControl.StartSelecting();
            //StartUpdateSelection();
        }
        //if (Input.GetMouseButton(selectUnit)) SceneControl.UpdateSelecting();
        if (Input.GetMouseButtonUp(selectUnit) && !Input.GetKey(KeyCode.Space)) {
            //StopCoroutine(updater);
            //StopUpdateSelection();
        }

        if (GetLongPress(createUnit)) {
            if (repeatingAction) return;
            actionRepeater = StartCoroutine(RepeatAction(new Action(SceneControl.CreateUnits)));
            repeatingAction = true;
            return;
        } else if (Input.GetKeyDown(createUnit)) {
            if (repeatingAction) {
                StopCoroutine(actionRepeater);
                repeatingAction = false;
            }
            SceneControl.CreateUnits();
            return;
        } else {
            if (Input.GetKeyUp(createUnit) && repeatingAction) {
                StopCoroutine(actionRepeater);
                repeatingAction = false;
                return;
            }
        }

        if (GetLongPress(deleteUnit)) {
            if (repeatingAction) return;
            actionRepeater = StartCoroutine(RepeatAction(new Action(SceneControl.DeleteUnits)));
            repeatingAction = true;
            return;
        } else if (Input.GetKeyDown(deleteUnit)) {
            if (repeatingAction) {
                StopCoroutine(actionRepeater);
                repeatingAction = false;
            }
            SceneControl.DeleteUnits();
            return;
        } else {
            if (Input.GetKeyUp(deleteUnit) && repeatingAction) {
                StopCoroutine(actionRepeater);
                repeatingAction = false;
                return;
            }
        }

        if (GetLongPress(revertForward)) {
            if (!repeatingAction) {
                actionRepeater = StartCoroutine(RepeatAction(SceneControl.RevertForward));
                repeatingAction = true;
            }
        } else if (GetLongPress(revertBack)) {
            if (!repeatingAction) {
                actionRepeater = StartCoroutine(RepeatAction(SceneControl.RevertBack));
                repeatingAction = true;
            }
        } else if (GetKeysUp(revertForward)) {
            if (repeatingAction) {
                StopCoroutine(actionRepeater);
                repeatingAction = false;
            }
        }

        if (GetKeysDown(revertForward)) {
            SceneControl.RevertForward();
        } else if (GetKeysDown(revertBack)) {
            SceneControl.RevertBack();
        } else if (GetKeysDown(saveFile)) {
            SaveBase();
        }

        this.MoveScene();
        this.ZoomScene();
        this.RotateScene();
        this.ResetTransform();

        UpdateScene();
    }

    bool GetKeys(KeyCode[] keys)
    {
        for (int i = 0; i < keys.Length; i++) {
            if (!Input.GetKey(keys[i])) return false;
        }
        return true;
    }

    bool GetKeysDown(KeyCode[] keys)
    {
        for (int i = 0; i < keys.Length - 1; i++) {
            if (!Input.GetKey(keys[i])) return false;
        }
        return Input.GetKeyDown(keys[keys.Length - 1]);
    }

    bool GetKeysUp(KeyCode[] keys)
    {
        foreach (KeyCode key in keys) {
            if (Input.GetKeyUp(key)) return true;
        }
        return false;
    }
    //bool GetLongPress(KeyCode[] keys)
    //{
    //	foreach (KeyCode key in keys) {
    //		if (!Input.GetKey(key)) {
    //			pressTimer = 0;
    //			return false;
    //		}
    //	}
    //	pressTimer += Time.deltaTime;
    //	return pressTimer >= effectTime;
    //}

    Dictionary<KeyCode, float> timer = new Dictionary<KeyCode, float>();
    float effectTime = 0.5f;
    bool GetLongPress(KeyCode key)
    {
        float pressTimer;
        if (!timer.TryGetValue(key, out pressTimer)) {
            timer.Add(key, pressTimer);
        }

        if (!Input.GetKey(key)) {
            pressTimer = 0;
            timer[key] = pressTimer;
            return false;
        }
        pressTimer += Time.deltaTime;
        timer[key] = pressTimer;
        return pressTimer >= effectTime;
    }

    Dictionary<KeyCode[], float> keysTimer = new Dictionary<KeyCode[], float>();
    bool GetLongPress(KeyCode[] keys)
    {
        float pressTimer;
        if (!keysTimer.TryGetValue(keys, out pressTimer)) {
            keysTimer.Add(keys, pressTimer);
        }

        foreach (KeyCode key in keys) {
            if (!Input.GetKey(key)) {
                pressTimer = 0;
                keysTimer[keys] = pressTimer;
                return false;
            }
        }

        pressTimer += Time.deltaTime;
        keysTimer[keys] = pressTimer;
        return pressTimer >= effectTime;
    }
}
