using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Diagnostics;
public static class BaseControl
{
    //The controller to manipulate the base, should only be accessed by scene control
    //private static Stopwatch stopWatch = new Stopwatch();

    //private static GameObject currentBase;
    //private static MeshCollider currentMeshCollider;
    //private static Mesh baseMesh;
    private static Dictionary<Vector3, BaseChunk> baseData = new Dictionary<Vector3, BaseChunk>(); //the information about the base
    private static Dictionary<Vector3, GameObject> currentChunks = new Dictionary<Vector3, GameObject>(); //the dictionary for faster lookups

    private static GameObject currentScene;
    private static GameObject basePrefab;

    /// <summary>
    /// clear the current scene
    /// </summary>
    public static void Clear()
    {
        baseData.Clear();
        foreach (GameObject obj in currentChunks.Values) {
            GameObject.Destroy(obj);
        }
        currentChunks.Clear();
    }

    public static List<DEPosition> GetAllExistUnits()
    {
        List<DEPosition> allUnits = new List<DEPosition>();
        foreach (BaseChunk chunk in baseData.Values) {
            allUnits.AddRange(chunk.GetAllUnits());
        }
        return allUnits;
    }

    //Generate all base unit in the scene
    public static void CreateBase(int x, int y, GameObject basePrefab, GameObject scene)
    {
        currentScene = scene;
        BaseControl.basePrefab = basePrefab;
        //generate base data info
        List<DEPosition> toBeCreate = new List<DEPosition>();
        for (int i = 0; i < x; i++) {
            for (int j = 0; j < y; j++) {
                toBeCreate.Add(new DEPosition(i, j, 0));
            }
        }
        CreateBaseUnits(toBeCreate.ToArray());
    }

    //get the hit unit position and hit direction of the unit based on the ray info
    public static bool GetHitInfo(Ray ray, out DEPosition position, out DEDirection direction)
    {
        RaycastHit hit;
        int baseMask = LayerMask.GetMask("DEBase");
        if (Physics.Raycast(ray, out hit, 200f, baseMask)) {
            Matrix4x4 w2l = currentScene.transform.worldToLocalMatrix;
            Matrix4x4 l2w = currentScene.transform.localToWorldMatrix;

            Vector3 localHitPos = w2l.MultiplyPoint3x4(hit.point);

            float xOffset = Mathf.Abs(0.5f - Mathf.Abs(localHitPos.x % 1));
            float yOffset = Mathf.Abs(0.5f - Mathf.Abs(localHitPos.y % 1));
            float zOffset = Mathf.Abs(0.5f - Mathf.Abs(localHitPos.z % 1));

            float min = Mathf.Min(xOffset, yOffset, zOffset);

            if (min == xOffset) {
                Vector3 left = new Vector3(localHitPos.x - 0.5f, localHitPos.y, localHitPos.z);
                Vector3 rayOrigin = ray.origin;
                float rightDis = (l2w.MultiplyPoint3x4(localHitPos) - rayOrigin).magnitude;
                float leftDis = (l2w.MultiplyPoint3x4(left) - rayOrigin).magnitude;
                direction = rightDis > leftDis ? DEDirection.left : DEDirection.right;
            } else if (min == yOffset) {
                Vector3 back = new Vector3(localHitPos.x, localHitPos.y - 0.5f, localHitPos.z);
                Vector3 rayOrigin = ray.origin;
                float forwardDis = (l2w.MultiplyPoint3x4(localHitPos) - rayOrigin).magnitude;
                float backDis = (l2w.MultiplyPoint3x4(back) - rayOrigin).magnitude;
                direction = forwardDis > backDis ? DEDirection.back : DEDirection.forward;
            } else {
                Vector3 down = new Vector3(localHitPos.x, localHitPos.y, localHitPos.z - 0.5f);
                Vector3 rayOrigin = ray.origin;
                float upDis = (l2w.MultiplyPoint3x4(localHitPos) - rayOrigin).magnitude;
                float downDis = (l2w.MultiplyPoint3x4(down) - rayOrigin).magnitude;
                direction = upDis > downDis ? DEDirection.down : DEDirection.up;
            }

            float x = localHitPos.x;
            float y = localHitPos.y;
            float z = localHitPos.z;
            switch (direction) {
                case DEDirection.right:
                    x -= 0.5f;
                    break;
                case DEDirection.left:
                    x += 0.5f;
                    break;
                case DEDirection.forward:
                    y -= 0.5f;
                    break;
                case DEDirection.back:
                    y += 0.5f;
                    break;
                case DEDirection.up:
                    z -= 0.5f;
                    break;
                case DEDirection.down:
                    z += 0.5f;
                    break;
            }

            position = (DEPosition)new Vector3(x, y, z);
            return true;
        } else {
            position = DEPosition.zero;
            direction = DEDirection.up;
            return false;
        }
    }

    public static void CreateBaseUnits(DEPosition[] positions)
    {
        //first put units into the data, get relative chunks
        //than get the visible units of the relative chunks
        List<Vector3> relateChunks = GetRelativeChunks(positions);

        foreach (Vector3 chunkPos in relateChunks) {
            AddUnits(positions.ToList(), chunkPos);
        }

        positions = null; //collect memories

        //now lets modify the meshes
        foreach (Vector3 chunkPos in relateChunks) {
            GameObject myChunk;
            Mesh newMesh = CalculateMesh(baseData[chunkPos].GetLocalVisibleUnits()); //we can get mesh now because the chunk data is created already

            if (currentChunks.TryGetValue(chunkPos, out myChunk)) {
                myChunk.GetComponent<MeshFilter>().mesh = newMesh;
                myChunk.GetComponent<MeshCollider>().sharedMesh = newMesh;
            } else {
                myChunk = GameObject.Instantiate(basePrefab, currentScene.transform, false);
                myChunk.transform.localPosition = GetChunkLocalPos(chunkPos);
                myChunk.name = chunkPos.ToString();

                myChunk.GetComponent<MeshFilter>().mesh = newMesh;
                myChunk.GetComponent<MeshCollider>().sharedMesh = newMesh;
                currentChunks.Add(chunkPos, myChunk);
            }
        }
        relateChunks = null;
    }

    public static void DestroyBaseUnits(DEPosition[] positions)
    {
        //first put units into the data, get relative chunks
        //than get the visible units of the relative chunks
        List<Vector3> relateChunks = GetRelativeChunks(positions);
        foreach (Vector3 chunkPos in relateChunks) {
            RemoveUnits(positions.ToList(), chunkPos);
        }

        positions = null;

        //now lets modify the meshes
        foreach (Vector3 chunkPos in relateChunks) {
            GameObject myChunk;
            if (currentChunks.TryGetValue(chunkPos, out myChunk)) {
                Mesh newMesh = CalculateMesh(baseData[chunkPos].GetLocalVisibleUnits());
                //myChunk.transform.position = GetChunkLocalPos(chunkPos);
                myChunk.GetComponent<MeshFilter>().mesh = newMesh;
                myChunk.GetComponent<MeshCollider>().sharedMesh = newMesh;

                //Debug.Log("units destroied");

            } else {
                UnityEngine.Debug.Log("the chunk gameobject doesn't exist");
            }
        }
        relateChunks = null;
    }

    public static bool ContainsPos(DEPosition position)
    {
        Vector3 chunkPos = GetChunkPos(position);
        BaseChunk chunkData;
        if (baseData.TryGetValue(chunkPos, out chunkData)) {
            return chunkData.HasUnit(position);
        } else {
            return false;
        }
    }

    public static bool IsVisible(DEPosition position)
    {
        Vector3 chunkPos = GetChunkPos(position);
        BaseChunk myChunk;
        if (baseData.TryGetValue(chunkPos, out myChunk)) {
            return myChunk.IsVisible(position);
        } else {
            return false;
        }
    }

    //returns visible unit of specified chunk position
    private static List<DEPosition> GetVisibleUnits(List<Vector3> chunksPos)
    {
        List<DEPosition> result = new List<DEPosition>();
        foreach (Vector3 chunkPos in chunksPos) {
            BaseChunk chunk;
            if (baseData.TryGetValue(chunkPos, out chunk)) {
                //not sure if this does what I think it does
                result.AddRange(chunk.GetVisibleUnits());
            } else {
                UnityEngine.Debug.LogError("this is not supposed to happen");
            }
        }

        //this is commended out because the destroy of units might influence other chunks that are not being calculated for visibility
        //for (int i = 0; i < result.Count; i++) {
        //	if (!IsVisible(result[i])) result.RemoveAt(i);
        //}
        return result;
    }

    //get all the relative chunk positions
    private static List<Vector3> GetRelativeChunks(DEPosition[] positions)
    {
        List<Vector3> result = new List<Vector3>();
        foreach (DEPosition pos in positions) {
            Vector3 chunkPos = GetChunkPos(pos);
            if (result.Contains(chunkPos)) continue;
            result.Add(chunkPos);
        }
        return result;
    }

    //check if a unit is visible
    //private static bool IsVisible(DEPosition pos)
    //{
    //	return !(ContainsPos(pos.right) && ContainsPos(pos.left) && ContainsPos(pos.forward) && ContainsPos(pos.back) && ContainsPos(pos.up) && ContainsPos(pos.down));
    //}

    private static void RemoveUnits(List<DEPosition> positions, Vector3 chunkPos)
    {
        BaseChunk myChunk;
        if (baseData.TryGetValue(chunkPos, out myChunk)) {
            foreach (DEPosition pos in positions) {
                if (GetChunkPos(pos) != chunkPos) continue;
                myChunk.RemoveUnit(pos);
            }
            positions = null;
            return;
        } else {
            UnityEngine.Debug.Log("this is not supposed to happen");
        }
    }

    //add a bunch of units if the chunk position is already known, might be slightly faster than using AddUnit();
    private static void AddUnits(List<DEPosition> positions, Vector3 chunkPos)
    {
        //Debug.Log("unit: " + positions[0]);
        BaseChunk myChunk;
        if (baseData.TryGetValue(chunkPos, out myChunk)) {
            foreach (DEPosition pos in positions) {
                if (GetChunkPos(pos) != chunkPos) continue;
                myChunk.AddUnit(pos);
            }
        } else {
            myChunk = new BaseChunk(chunkPos);
            //Debug.Log("new chunk add" + chunkPos);
            foreach (DEPosition pos in positions) {
                if (GetChunkPos(pos) != chunkPos) continue;
                myChunk.AddUnit(pos);
            }
            baseData.Add(chunkPos, myChunk);
        }
        positions = null;
    }

    //get the position of the chunk relative to the scene
    private static Vector3 GetChunkLocalPos(Vector3 chunkPos)
    {
        //O(1)
        return new Vector3(chunkPos.x * 16, chunkPos.y * 16, chunkPos.z * 8);
    }

    private static Vector3 GetChunkPos(DEPosition position)
    {
        //this takes O(1) time
        int chunkX;
        int chunkY;
        int chunkZ;
        if (position.x < 0) {
            chunkX = (position.x + 1) / 16;
            chunkX -= 1;
        } else {
            chunkX = position.x / 16;
        }

        if (position.y < 0) {
            chunkY = (position.y + 1) / 16;
            chunkY -= 1;
        } else {
            chunkY = position.y / 16;
        }

        if (position.z < 0) {
            chunkZ = (position.z + 1) / 8;
            chunkZ -= 1;
        } else {
            chunkZ = position.z / 8;
        }

        Vector3 chunkPos = new Vector3(chunkX, chunkY, chunkZ);
        return chunkPos;
    }

    //this returns the mesh generated according to the unit list
    private static Mesh CalculateMesh(List<DEPosition> toBeCreate)
    {

        //must make sure the position is withing range(0,0,0)-(15,15,7) in order to generate right mesh vertexs
        //Debug.Log("new mesh: " + toBeCreate[0]);
        //seems like a really stupid way to change mesh, maybe optimize it later
        Mesh myMesh = new Mesh();

        List<Vector3> targetVertices = new List<Vector3>();
        List<int> targetTriangles = new List<int>();
        List<Vector2> targetUVs = new List<Vector2>();

        foreach (DEPosition unitPos in toBeCreate) {
            foreach (Vector3 vert in DEPosition.GetQuadLocalVertexs(unitPos, DEDirection.right))
                targetVertices.Add(vert);
            foreach (Vector3 vert in DEPosition.GetQuadLocalVertexs(unitPos, DEDirection.left))
                targetVertices.Add(vert);
            foreach (Vector3 vert in DEPosition.GetQuadLocalVertexs(unitPos, DEDirection.forward))
                targetVertices.Add(vert);
            foreach (Vector3 vert in DEPosition.GetQuadLocalVertexs(unitPos, DEDirection.back))
                targetVertices.Add(vert);
            foreach (Vector3 vert in DEPosition.GetQuadLocalVertexs(unitPos, DEDirection.up))
                targetVertices.Add(vert);
            foreach (Vector3 vert in DEPosition.GetQuadLocalVertexs(unitPos, DEDirection.down))
                targetVertices.Add(vert);
        }

        for (int i = 0; i < targetVertices.Count / 4; i++) {
            int counter = i * 4;
            //ups
            targetTriangles.Add(0 + counter);
            targetTriangles.Add(1 + counter);
            targetTriangles.Add(2 + counter);

            targetTriangles.Add(2 + counter);
            targetTriangles.Add(3 + counter);
            targetTriangles.Add(0 + counter);

            targetUVs.Add(new Vector2(0, 0));
            targetUVs.Add(new Vector2(0, 1));
            targetUVs.Add(new Vector2(1, 1));
            targetUVs.Add(new Vector2(1, 0));
        }

        myMesh.Clear();
        myMesh.vertices = targetVertices.ToArray();
        myMesh.triangles = targetTriangles.ToArray();
        myMesh.uv = targetUVs.ToArray();
        myMesh.RecalculateNormals();

        targetVertices = null;
        targetTriangles = null;
        targetUVs = null;

        return myMesh;
    }

}
