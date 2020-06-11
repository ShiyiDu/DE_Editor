using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

//this class can be accessed by any class, be careful
using System.IO;
using System.Text;

public static class DEUtility
{
    public static void SaveBaseFile(string fullPath, List<DEPosition> baseData)
    {
        string path = fullPath;

        //using stringBuilder is light years faster than using string++!!!
        StringBuilder stringBuilder = new StringBuilder(baseData.Count * 6);
        string sceneData = "";

        //Stopwatch stopWatch = new Stopwatch();
        //stopWatch.Start();
        for (int i = 0; i < baseData.Count; i++) {
            //sceneData += (allUnits[i].ToString() + ',');
            stringBuilder.Append(baseData[i].ToString() + ',');
        }

        sceneData = stringBuilder.ToString();
        //UnityEngine.Debug.Log(stopWatch.Elapsed.Seconds);

        StreamWriter sw;
        if (File.Exists(path)) {
            File.Delete(path);
            sw = File.CreateText(path);
        } else {
            sw = File.CreateText(path);
        }

        sw.Write(sceneData);

        //UnityEngine.Debug.Log(stopWatch.Elapsed.Seconds);
        sw.Close();
        sw.Dispose();
        //stopWatch.Reset();
    }

    public static List<string> ReadAllBase()
    {
        List<string> allFiles = new List<string>();
        string path = Application.dataPath + "/DEScenes";
        DirectoryInfo folder = new DirectoryInfo(path);

        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

        foreach (FileInfo file in folder.GetFiles("*.debs")) {
            Debug.Log(file.Name);
            allFiles.Add(file.Name.Remove(file.Name.Length - 5, 5));
        }
        return allFiles;
    }

    public static List<DEPosition> ReadBaseFile(string fileName)
    {
        string path = Application.dataPath + "/DEScenes" + "//" + fileName + ".debs";
        string sceneData;
        StreamReader sr;
        List<DEPosition> allUnits = new List<DEPosition>();
        if (File.Exists(path)) {
            sr = File.OpenText(path);
            sceneData = sr.ReadToEnd();
            string[] allNumStr = sceneData.Split(',');
            for (int i = 0; i < allNumStr.Length - 1; i += 3) {
                //it minus one cause the last char is ','
                //Debug.Log((allNumStr[0 + i]) + "," + (allNumStr[1 + i]) + "," + (allNumStr[2 + i]));
                allUnits.Add(new DEPosition(int.Parse(allNumStr[0 + i]), int.Parse(allNumStr[1 + i]), int.Parse(allNumStr[2 + i])));
            }

            sr.Close();
            sr.Dispose();
            allNumStr = null;
            sceneData = null;

        } else {
            Debug.LogError(fileName + " doesnot exits in the data folder");
        }
        return allUnits;
    }

    /// <summary>
    /// this is a intermedia methods that allow other class to check if a position exists in scene
    /// </summary>
    /// <returns><c>true</c>, if position was containsed, <c>false</c> otherwise.</returns>
    /// <param name="pos">Position.</param>
    public static bool ContainsPos(DEPosition pos) { return BaseControl.ContainsPos(pos); }

    /// <summary>
    /// Get the center point of the scene locally
    /// </summary>
    public static Vector3 GetLocalCenter()
    {
        List<DEPosition> allUnits = BaseControl.GetAllExistUnits();
        DEPosition sum = allUnits.Aggregate((total, next) => total + next);
        Vector3 average = (Vector3)sum / allUnits.Count;
        return average;
    }

    /// <summary>
    /// Get the right forward up point and left back down point
    /// </summary>
    /// <returns>The and lbd.</returns>
    /// <param name="beginPos">Begin position.</param>
    /// <param name="endPos">End position.</param>
    public static DEPosition[] RfuAndLbd(DEPosition beginPos, DEDirection beginDir, DEPosition endPos, DEDirection endDir)
    {
        DEPosition actualBegin = new DEPosition();
        actualBegin = beginPos;
        DEPosition actualEnd = new DEPosition();
        actualEnd = endPos;

        //get the actual selection based on the direction of selection
        if (WithinDirection(beginPos, beginDir, endPos) && WithinDirection(endPos, endDir, beginPos)) {
            actualBegin = beginPos.GetDirection(beginDir);
            actualEnd = endPos.GetDirection(endDir);
        } else if (WithinDirection(beginPos, beginDir, endPos)) {
            actualBegin = beginPos.GetDirection(beginDir);
        } else if (WithinDirection(endPos, endDir, beginPos)) {
            actualEnd = endPos.GetDirection(endDir);
        }

        DEPosition rfu = new DEPosition(actualBegin.x > actualEnd.x ? actualBegin.x : actualEnd.x, actualBegin.y > actualEnd.y ? actualBegin.y : actualEnd.y, actualBegin.z > actualEnd.z ? actualBegin.z : actualEnd.z);
        DEPosition lbd = new DEPosition(actualBegin.x < actualEnd.x ? actualBegin.x : actualEnd.x, actualBegin.y < actualEnd.y ? actualBegin.y : actualEnd.y, actualBegin.z < actualEnd.z ? actualBegin.z : actualEnd.z);
        return new DEPosition[2] { rfu, lbd };
    }

    public static List<DEPosition> GetSelectGroup(DEPosition beginPos, DEDirection beginDir, DEPosition endPos, DEDirection endDir)
    {
        DEPosition[] rfuAndLbd = RfuAndLbd(beginPos, beginDir, endPos, endDir);
        DEPosition rfu = rfuAndLbd[0];
        DEPosition lbd = rfuAndLbd[1];
        List<DEPosition> result = new List<DEPosition>();
        for (int x = lbd.x; x <= rfu.x; x++) {
            for (int y = lbd.y; y <= rfu.y; y++) {
                for (int z = lbd.z; z <= rfu.z; z++) {
                    result.Add(new DEPosition(x, y, z));
                }
            }
        }
        return result;
    }

    private static bool WithinDirection(DEPosition origin, DEDirection direction, DEPosition target)
    {
        if (origin == null || target == null) return false;
        if (((Vector3)(origin.GetDirection(direction) - target)).magnitude < ((Vector3)(origin - target)).magnitude) return true;
        return false;
    }

    public static DEDirection HitSide(Vector2 mousePosition, DEPosition position, GameObject scene)
    {
        //if (upOffset == Vector3.zero) {
        //	Debug.LogError("must calculate offset first");
        //	return DEDirection.up;
        //}

        //it should only excute if last position is different from current position
        Vector3 camPos = Camera.main.transform.position;

        Matrix4x4 l2W = scene.transform.localToWorldMatrix;
        //the center point of each side
        Vector3 rightSide = l2W.MultiplyPoint3x4((Vector3)position + new Vector3(0.5f, 0, 0));
        Vector3 leftSide = l2W.MultiplyPoint3x4((Vector3)position + new Vector3(-0.5f, 0, 0));
        Vector3 forwardSide = l2W.MultiplyPoint3x4((Vector3)position + new Vector3(0, 0.5f, 0));
        Vector3 backSide = l2W.MultiplyPoint3x4((Vector3)position + new Vector3(0, -0.5f, 0));
        Vector3 upSide = l2W.MultiplyPoint3x4((Vector3)position + new Vector3(0, 0, 0.5f));
        Vector3 downSide = l2W.MultiplyPoint3x4((Vector3)position + new Vector3(0, 0, -0.5f));

        //the distance from a side to the viewport
        float up = (upSide - camPos).magnitude;
        float down = (downSide - camPos).magnitude;
        float right = (rightSide - camPos).magnitude;
        float left = (leftSide - camPos).magnitude;
        float forward = (forwardSide - camPos).magnitude;
        float back = (backSide - camPos).magnitude;

        //get the closest three to the viewport
        string[] closest = new string[3];
        closest[0] = right > left ? "left" : "right";
        closest[1] = forward > back ? "back" : "forward";
        closest[2] = up > down ? "down" : "up";

        //the list of the differences between the four triangle area formed by mouse and the according side area
        //because the number can be unequal due to the differences in precision, therefore I need to calculate
        //which side has the best shot of being hit right now
        List<float> differences = new List<float> { -1, -1, -1, -1, -1, -1 };

        foreach (string str in closest) {
            switch (str) {
                case "right":
                    differences[0] = Differences(mousePosition, DEPosition.GetQuadWorldVertexs(position, DEDirection.right, l2W));
                    break;
                case "left":
                    differences[1] = Differences(mousePosition, DEPosition.GetQuadWorldVertexs(position, DEDirection.left, l2W));
                    break;
                case "forward":
                    differences[2] = Differences(mousePosition, DEPosition.GetQuadWorldVertexs(position, DEDirection.front, l2W));
                    break;
                case "back":
                    differences[3] = Differences(mousePosition, DEPosition.GetQuadWorldVertexs(position, DEDirection.back, l2W));
                    break;
                case "up":
                    differences[4] = Differences(mousePosition, DEPosition.GetQuadWorldVertexs(position, DEDirection.up, l2W));
                    break;
                case "down":
                    differences[5] = Differences(mousePosition, DEPosition.GetQuadWorldVertexs(position, DEDirection.down, l2W));
                    break;
            }
        }

        List<float> orig = new List<float>();
        foreach (float f in differences) { orig.Add(f); }

        differences.Sort();
        int result = 0;

        foreach (float i in differences) {
            if (i >= 0) {
                result = orig.IndexOf(i);
                break;
            }
        }

        switch (result) {
            case 0:
                return DEDirection.right;
            case 1:
                return DEDirection.left;
            case 2:
                return DEDirection.front;
            case 3:
                return DEDirection.back;
            case 4:
                return DEDirection.up;
            case 5:
                return DEDirection.down;
        }

        return DEDirection.down;
    }

    private static float Differences(Vector2 point, Vector3[] vertexs)
    {
        Vector2[] curSide = new Vector2[4];
        for (int i = 0; i < 4; i++) {
            curSide[i] = Camera.main.WorldToScreenPoint(vertexs[i]);
            //Debug.Log(curSide[i] + ":" + point);
        }

        float quadArea = (float)CalculateTriangleArea(curSide[0], curSide[1], curSide[2]) + (float)CalculateTriangleArea(curSide[2], curSide[3], curSide[0]);
        float firstTriangle = (float)CalculateTriangleArea(point, curSide[0], curSide[1]);
        float secondTriangle = (float)CalculateTriangleArea(point, curSide[1], curSide[2]);
        float thirdTriangle = (float)CalculateTriangleArea(point, curSide[2], curSide[3]);
        float forthTriangle = (float)CalculateTriangleArea(point, curSide[3], curSide[0]);

        return (Mathf.Abs(quadArea - (firstTriangle + secondTriangle + thirdTriangle + forthTriangle)));

    }
    // returns the area of a triangle defined by three vector2
    private static double CalculateTriangleArea(Vector2 a, Vector2 b, Vector2 c)
    {
        return 0.5 * Math.Abs(a.x * b.y + b.x * c.y + c.x * a.y - a.x * c.y - b.x * a.y - c.x * b.y);
    }

}
