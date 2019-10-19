using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.Experimental.UIElements;
using UnityEngine.UI;
using UnityEngine.Events;

public class DEUI : MonoBehaviour
{
    //The ui control for dogegg editor
    public GameObject currentScene;

    public GameObject sceneList;
    public UnityEngine.UI.Button loadButton;
    public InputField fileName;
    public UnityEngine.UI.Button confirmSave;
    public UnityEngine.UI.Button cancelSave;
    public UnityEngine.UI.Button newBase;

    public UnityEngine.UI.Button buttonPrefab;

    public GameObject confirmPanel;

    private string confirmName;

    // Use this for initialization
    void Start()
    {
        Debug.Log(Application.dataPath);

        confirmPanel.SetActive(false);
        sceneList.SetActive(true);
        ShowSceneList();
        loadButton.onClick.AddListener(ShowSceneList);
        newBase.onClick.AddListener(currentScene.GetComponent<SceneView>().CreateNewScene);
    }

    // Update is called once per frame
    void OnGUI()
    {
    }

    void ShowSceneList()
    {
        RectTransform content = sceneList.GetComponent<ScrollRect>().content;
        List<string> allScenes = DEUtility.ReadAllBase();
        for (int i = 0; i < content.childCount; i++) {
            Destroy(content.GetChild(i).gameObject);
        }

        for (int i = 0; i < allScenes.Count; i++) {
            UnityEngine.UI.Button button = GameObject.Instantiate(buttonPrefab, content, false);
            button.GetComponentInChildren<Text>().text = allScenes[i];
            button.GetComponent<RectTransform>().localPosition = new Vector3(100, -30 * i, 0);
            button.name = allScenes[i];
            button.onClick.AddListener(button.GetComponent<MyButton>().LoadBase);
        }
        content.localPosition = Vector3.zero;
        sceneList.SetActive(!sceneList.activeInHierarchy);
    }

    void ConfirmName()
    {
        confirmName = fileName.transform.GetChild(2).GetComponent<Text>().text;
        confirmPanel.SetActive(false);
        currentScene.GetComponent<SceneView>().SetSceneName(confirmName);
        currentScene.GetComponent<SceneView>().SaveBase();

        loadButton.enabled = true;
        newBase.enabled = true;
        sceneList.SetActive(false);
    }

    void CancelSave()
    {
        confirmPanel.SetActive(false);

        loadButton.enabled = true;
        newBase.enabled = true;
        sceneList.SetActive(false);
    }

    public void PopSaveConfirm()
    {
        //I should disable all other ui component just incase
        loadButton.enabled = false;
        newBase.enabled = false;
        sceneList.SetActive(false);

        confirmPanel.SetActive(true);
        fileName.transform.GetComponentsInChildren<Text>()[0].text = "File Name";
        fileName.transform.GetComponentsInChildren<Text>()[1].text = "";

        confirmSave.onClick.RemoveAllListeners();
        cancelSave.onClick.RemoveAllListeners();

        confirmSave.onClick.AddListener(ConfirmName);
        cancelSave.onClick.AddListener(CancelSave);
    }
}
