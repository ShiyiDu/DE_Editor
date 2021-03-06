﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

//Draw the handles for the descene, giving visual feedbacks of the editor
//this can only be accessed by sceneview maybe? this cannot have communications with SceneControl
using System.Linq;
using System.Threading;
using System.Collections.Concurrent;
using System;

public static class HandleDrawer
{
    //private static bool selecting = false;
    private static DEPosition pointA;
    private static DEPosition pointB;

    private static Material faceMat;
    private static Material lineMat;
    private static Material dottedLineMat;
    //private static Material vertMat;

    private static GameObject scene;

    private static List<Vector3> quadVerts = new List<Vector3>();
    //verts of selection cube or plane, only first 4 is used if a plane is being drawn
    private static Vector3[] p = new Vector3[8];
    private static Vector3[] planeVerts = new Vector3[4];
    //private static bool isPlane = true;
    //private static DEDirection planeDirection;


    //todo: this is not working
    //use a already calculated dictionary to accelerate the process of calculating verts
    //private static Dictionary<DEPosition, Vector3[]> calculatedVerts = new Dictionary<DEPosition, Vector3[]>();
    //private static Vector3 originScenePos = new Vector3();
    //private static Quaternion originSceneRot = new Quaternion();

    public static void SetCurrentScene(GameObject scene)
    {
        HandleDrawer.scene = scene;
        //originScenePos = scene.transform.position;
        //originSceneRot = scene.transform.parent.rotation;
    }


    private static Stopwatch sw = new Stopwatch();

    //this update the selection info,
    //this class is created so that when selection is unchanged, the vertex doesnot need to be recalculated
    public static void UpdateSelection(DEPosition rfu, DEPosition lbd, DEPosition[] visibleSelection, bool isPlane, DEDirection planeDirection)
    {
        if (updatingSelection) return;
        //else UnityEngine.Debug.Log("last update took: " + sw.ElapsedMilliseconds + "(ms)");
        updatingSelection = true;
        HandleDrawer.isPlane = isPlane;
        HandleDrawer.planeDirection = planeDirection;
        Thread updater = new Thread(new ParameterizedThreadStart(UpdateSelectionThread));


        //UnityEngine.Debug.Log("visible selection size:" + visibleSelection.Length);

        l2W = scene.transform.localToWorldMatrix;
        object updateData = new object[2] { new DEPosition[2] { rfu, lbd }, visibleSelection };
        //UpdateSelectionThread(updateData);

        updater.Start(updateData);
        updater.Join();
        //UnityEngine.Debug.Log("Time used for calculate side verts:" + sw.ElapsedMilliseconds);
    }

    private static Matrix4x4 l2W; //local to world
    private static bool updatingSelection = false;
    private static bool isPlane = true;
    private static DEDirection planeDirection;
    //use another thread to update selection
    private static void UpdateSelectionThread(object args)
    {
        //todo: maybe instead of recalculate the verts everysingle time, I just calculate the changes?
        //todo: maybe instead of recalculate the verts everytime the scene moves, I just add the offsets on top of every verts?
        //todo: maybe use another thread to dynamicly recalculate verts?
        //todo: or!!!! maybe I pre-calculate all the verts of the scene, decide which to draw, and add offsets on top of them whenever the scene moves!!!
        //Stopwatch sw = new Stopwatch();
        //sw.Reset();
        //sw.Start();

        List<Vector3> quadVerts = new List<Vector3>();
        //verts of selection cube or plane, only first 4 is used if a plane is being drawn
        Vector3[] p = new Vector3[8];
        Vector3[] planeVerts = new Vector3[4];
        //HandleDrawer.isPlane = isPlane;
        //HandleDrawer.planeDirection = planeDirection;

        object[] updateInfo = args as object[];
        DEPosition[] rfuAndLbd = updateInfo[0] as DEPosition[];
        DEPosition rfu = rfuAndLbd[0]; //right front up
        DEPosition lbd = rfuAndLbd[1]; //left back down
        DEPosition[] visibleSelection = updateInfo[1] as DEPosition[];

        //get all 8 points
        Vector3 lP3 = new Vector3(rfu.x + 0.5f, rfu.y + 0.5f, rfu.z + 0.5f);
        Vector3 lP6 = new Vector3(lbd.x - 0.5f, lbd.y - 0.5f, lbd.z - 0.5f);
        p[0] = l2W.MultiplyPoint3x4(new Vector3(lP6.x, lP3.y, lP3.z));
        p[1] = l2W.MultiplyPoint3x4(new Vector3(lP6.x, lP6.y, lP3.z));
        p[2] = l2W.MultiplyPoint3x4(new Vector3(lP3.x, lP6.y, lP3.z));
        p[3] = l2W.MultiplyPoint3x4(lP3);
        p[4] = l2W.MultiplyPoint3x4(new Vector3(lP3.x, lP3.y, lP6.z));
        p[5] = l2W.MultiplyPoint3x4(new Vector3(lP3.x, lP6.y, lP6.z));
        p[6] = l2W.MultiplyPoint3x4(lP6);
        p[7] = l2W.MultiplyPoint3x4(new Vector3(lP6.x, lP3.y, lP6.z));


        //create a hash set for faster lookups later
        HashSet<DEPosition> visibleHashSet = new HashSet<DEPosition>(visibleSelection);
        //UnityEngine.Debug.Log("Time used for creating hashset:" + sw.ElapsedMilliseconds);
        if (!isPlane) {
            //todo: this is not working for now!
            //Vector3 offSet = Vector3.zero;
            //if (scene.transform.parent.rotation != originSceneRot) {
            //	calculatedVerts.Clear();
            //	originScenePos = scene.transform.position;
            //	originSceneRot = scene.transform.parent.rotation;
            //} else if (scene.transform.position != originScenePos) {
            //	offSet = scene.transform.position - originScenePos;
            //}

            List<DEPosition> r2d = new List<DEPosition>();
            List<DEPosition> l2d = new List<DEPosition>();
            List<DEPosition> f2d = new List<DEPosition>();
            List<DEPosition> b2d = new List<DEPosition>();
            List<DEPosition> u2d = new List<DEPosition>();
            List<DEPosition> d2d = new List<DEPosition>();

            for (int x = lbd.x; x <= rfu.x; x++) {
                bool xInRange = x == lbd.x || x == rfu.x;
                for (int y = lbd.y; y <= rfu.y; y++) {
                    bool yInRange = y == lbd.y || y == rfu.y;
                    for (int z = lbd.z; z <= rfu.z; z++) {
                        bool zInRange = z == lbd.z || z == rfu.z;
                        if (!xInRange && !yInRange && !zInRange) continue;
                        //make sure only the most outside is drawn

                        //if (!(x == lbd.x || x == rfu.x || y == lbd.y || y == rfu.y || z == lbd.z || z == rfu.z)) continue;

                        //make sure it doesn't intersect with visible selections
                        DEPosition pos = new DEPosition(x, y, z);
                        if (visibleHashSet.Contains(pos)) continue;

                        if (pos.x == rfu.x && DEUtility.ContainsPos(pos.GetDirection(DEDirection.right))) r2d.Add(pos);
                        if (pos.x == lbd.x && DEUtility.ContainsPos(pos.GetDirection(DEDirection.left))) l2d.Add(pos);
                        if (pos.y == rfu.y && DEUtility.ContainsPos(pos.GetDirection(DEDirection.front))) f2d.Add(pos);
                        if (pos.y == lbd.y && DEUtility.ContainsPos(pos.GetDirection(DEDirection.back))) b2d.Add(pos);
                        if (pos.z == rfu.z && DEUtility.ContainsPos(pos.GetDirection(DEDirection.up))) u2d.Add(pos);
                        if (pos.z == lbd.z && DEUtility.ContainsPos(pos.GetDirection(DEDirection.down))) d2d.Add(pos);
                    }
                }
            }

            //get verts on the sides
            foreach (DEPosition pos in r2d) quadVerts.AddRange(DEPosition.GetQuadWorldVertexs(pos, DEDirection.right, l2W));
            foreach (DEPosition pos in l2d) quadVerts.AddRange(DEPosition.GetQuadWorldVertexs(pos, DEDirection.left, l2W));
            foreach (DEPosition pos in f2d) quadVerts.AddRange(DEPosition.GetQuadWorldVertexs(pos, DEDirection.front, l2W));
            foreach (DEPosition pos in b2d) quadVerts.AddRange(DEPosition.GetQuadWorldVertexs(pos, DEDirection.back, l2W));
            foreach (DEPosition pos in u2d) quadVerts.AddRange(DEPosition.GetQuadWorldVertexs(pos, DEDirection.up, l2W));
            foreach (DEPosition pos in d2d) quadVerts.AddRange(DEPosition.GetQuadWorldVertexs(pos, DEDirection.down, l2W));

            //UnityEngine.Debug.Log("Time used for finding side units:" + sw.ElapsedMilliseconds);

            //free up memory
            //r2d = null;
            //l2d = null;
            //f2d = null;
            //b2d = null;
            //u2d = null;
            //d2d = null;

            //get verts within selection
            //List<Vector3[]> vertexToCalculate = new List<Vector3[]>();
            Stopwatch sw = new Stopwatch();
            sw.Reset();
            sw.Start();

            ///Turns out its the hashing that takes up the most amount of cpu time
            //foreach (DEPosition pos in visibleSelection) {
            //    if (!DEUtility.ContainsPos(pos.right)) ;// vertexToCalculate.Add(DEPosition.GetQuadLocalVertexs(pos, DEDirection.right));
            //    if (!DEUtility.ContainsPos(pos.left)) ;// vertexToCalculate.Add(DEPosition.GetQuadLocalVertexs(pos, DEDirection.left));
            //    if (!DEUtility.ContainsPos(pos.forward)) ;// vertexToCalculate.Add(DEPosition.GetQuadLocalVertexs(pos, DEDirection.forward));
            //    if (!DEUtility.ContainsPos(pos.back)) ;// vertexToCalculate.Add(DEPosition.GetQuadLocalVertexs(pos, DEDirection.back));
            //    if (!DEUtility.ContainsPos(pos.up)) ;// vertexToCalculate.Add(DEPosition.GetQuadLocalVertexs(pos, DEDirection.up));
            //    if (!DEUtility.ContainsPos(pos.down)) ;// vertexToCalculate.Add(DEPosition.GetQuadLocalVertexs(pos, DEDirection.down));
            //}
            //UnityEngine.Debug.Log("Time used for updating selection:" + sw.ElapsedMilliseconds);
            ////////////////////////////////////////

            //multithreading speed up the drawing by around 60 %
            int threadCount = 6;
            ConcurrentQueue<Vector3[]> sels = new ConcurrentQueue<Vector3[]>();

            void worker(object workerArgs)
            {
                int[] threadArg = (int[])workerArgs;
                int count = threadArg[0];
                int index = threadArg[1];
                int block = visibleSelection.Length / count;
                int start = index * (block);
                int end = (index + 1) * block;
                if (index + 1 == count) end = visibleSelection.Length;

                for (int i = start; i < end; i++) {
                    DEPosition pos = visibleSelection[i];
                    if (!DEUtility.ContainsPos(pos.right)) sels.Enqueue(DEPosition.GetQuadWorldVertexs(pos, DEDirection.right, l2W));
                    if (!DEUtility.ContainsPos(pos.left)) sels.Enqueue(DEPosition.GetQuadWorldVertexs(pos, DEDirection.left, l2W));
                    if (!DEUtility.ContainsPos(pos.front)) sels.Enqueue(DEPosition.GetQuadWorldVertexs(pos, DEDirection.front, l2W));
                    if (!DEUtility.ContainsPos(pos.back)) sels.Enqueue(DEPosition.GetQuadWorldVertexs(pos, DEDirection.back, l2W));
                    if (!DEUtility.ContainsPos(pos.up)) sels.Enqueue(DEPosition.GetQuadWorldVertexs(pos, DEDirection.up, l2W));
                    if (!DEUtility.ContainsPos(pos.down)) sels.Enqueue(DEPosition.GetQuadWorldVertexs(pos, DEDirection.down, l2W));
                }
            }

            Thread[] workers = new Thread[threadCount - 1];
            for (int i = 0; i < threadCount - 1; i++) {
                workers[i] = new Thread(new ParameterizedThreadStart(worker));
                workers[i].Start(new int[] { threadCount, i });
            }

            worker(new int[] { threadCount, threadCount - 1 });

            foreach (Thread w in workers) {
                w.Join();
            }

            foreach (Vector3[] verts in sels) quadVerts.AddRange(verts);
            UnityEngine.Debug.Log("Time used for updating selection:" + sw.ElapsedMilliseconds);
            ////////////////////////////////

            //split this thread into 6 child thread to do the following calculation:
            //List<Vector3[]> rightSel = new List<Vector3[]>();
            //List<Vector3[]> leftSel = new List<Vector3[]>();
            //List<Vector3[]> backSel = new List<Vector3[]>();
            //List<Vector3[]> frontSel = new List<Vector3[]>();
            //List<Vector3[]> upSel = new List<Vector3[]>();
            //List<Vector3[]> downSel = new List<Vector3[]>();

            ////Duplicated code is to avoid branching, so it runs more efficiently

            //void w1()
            //{
            //    foreach (DEPosition pos in visibleSelection) {
            //        if (!DEUtility.ContainsPos(pos.right)) rightSel.Add(DEPosition.GetQuadWorldVertexs(pos, DEDirection.right, l2W));
            //    }
            //}

            //void w2()
            //{
            //    foreach (DEPosition pos in visibleSelection) {
            //        if (!DEUtility.ContainsPos(pos.left)) leftSel.Add(DEPosition.GetQuadWorldVertexs(pos, DEDirection.left, l2W));
            //    }
            //}

            //void w3()
            //{
            //    foreach (DEPosition pos in visibleSelection) {
            //        if (!DEUtility.ContainsPos(pos.back)) backSel.Add(DEPosition.GetQuadWorldVertexs(pos, DEDirection.back, l2W));
            //    }
            //}

            //void w4()
            //{
            //    foreach (DEPosition pos in visibleSelection) {
            //        if (!DEUtility.ContainsPos(pos.forward)) frontSel.Add(DEPosition.GetQuadWorldVertexs(pos, DEDirection.forward, l2W));
            //    }
            //}

            //void w5()
            //{
            //    foreach (DEPosition pos in visibleSelection) {
            //        if (!DEUtility.ContainsPos(pos.up)) upSel.Add(DEPosition.GetQuadWorldVertexs(pos, DEDirection.up, l2W));
            //    }
            //}

            //void w6()
            //{
            //    foreach (DEPosition pos in visibleSelection) {
            //        if (!DEUtility.ContainsPos(pos.down)) downSel.Add(DEPosition.GetQuadWorldVertexs(pos, DEDirection.down, l2W));
            //    }
            //}

            //Thread worker1 = new Thread(new ThreadStart(w1));
            //Thread worker2 = new Thread(new ThreadStart(w2));
            //Thread worker3 = new Thread(new ThreadStart(w3));
            //Thread worker4 = new Thread(new ThreadStart(w4));
            //Thread worker5 = new Thread(new ThreadStart(w5));
            //Thread worker6 = new Thread(new ThreadStart(w6));

            //worker1.Start();
            //worker2.Start();
            //worker3.Start();
            //worker4.Start();
            //worker5.Start();
            //worker6.Start();

            //worker1.Join();
            //worker2.Join();
            //worker3.Join();
            //worker4.Join();
            //worker5.Join();
            //worker6.Join();

            //foreach (Vector3[] verts in leftSel) { quadVerts.AddRange(verts); }
            //foreach (Vector3[] verts in rightSel) { quadVerts.AddRange(verts); }
            //foreach (Vector3[] verts in upSel) { quadVerts.AddRange(verts); }
            //foreach (Vector3[] verts in downSel) { quadVerts.AddRange(verts); }
            //foreach (Vector3[] verts in frontSel) { quadVerts.AddRange(verts); }
            //foreach (Vector3[] verts in backSel) { quadVerts.AddRange(verts); }


            ////////////////////////////////// //////////////////////////////////

            //for (int i = 0; i < visibleSelection.Length; i++) {
            //    DEPosition pos = visibleSelection[i];
            //    if (!DEUtility.ContainsPos(pos.right)) quadVerts.AddRange(DEPosition.GetQuadWorldVertexs(pos, DEDirection.right, l2W));
            //    if (!DEUtility.ContainsPos(pos.left)) quadVerts.AddRange(DEPosition.GetQuadWorldVertexs(pos, DEDirection.left, l2W));
            //    if (!DEUtility.ContainsPos(pos.forward)) quadVerts.AddRange(DEPosition.GetQuadWorldVertexs(pos, DEDirection.forward, l2W));
            //    if (!DEUtility.ContainsPos(pos.back)) quadVerts.AddRange(DEPosition.GetQuadWorldVertexs(pos, DEDirection.back, l2W));
            //    if (!DEUtility.ContainsPos(pos.up)) quadVerts.AddRange(DEPosition.GetQuadWorldVertexs(pos, DEDirection.up, l2W));
            //    if (!DEUtility.ContainsPos(pos.down)) quadVerts.AddRange(DEPosition.GetQuadWorldVertexs(pos, DEDirection.down, l2W));
            //}

            //foreach (DEPosition pos in visibleSelection) { 
            //DEPosition right = pos.right;
            //DEPosition left = pos.left;
            //DEPosition front = pos.forward;
            //DEPosition back = pos.back;
            //DEPosition up = pos.up;
            //DEPosition down = pos.down;

            //if (!DEUtility.ContainsPos(pos.right)) quadVerts.AddRange(DEPosition.GetQuadWorldVertexs(pos, DEDirection.right, l2W));
            //if (!DEUtility.ContainsPos(pos.left)) quadVerts.AddRange(DEPosition.GetQuadWorldVertexs(pos, DEDirection.left, l2W));
            //if (!DEUtility.ContainsPos(pos.forward)) quadVerts.AddRange(DEPosition.GetQuadWorldVertexs(pos, DEDirection.forward, l2W));
            //if (!DEUtility.ContainsPos(pos.back)) quadVerts.AddRange(DEPosition.GetQuadWorldVertexs(pos, DEDirection.back, l2W));
            //if (!DEUtility.ContainsPos(pos.up)) quadVerts.AddRange(DEPosition.GetQuadWorldVertexs(pos, DEDirection.up, l2W));
            //if (!DEUtility.ContainsPos(pos.down)) quadVerts.AddRange(DEPosition.GetQuadWorldVertexs(pos, DEDirection.down, l2W));
            //}

            //HashSet<DEPosition> searched = new HashSet<DEPosition>();

            //foreach (DEPosition pos in visibleSelection) {
            //    DEPosition right = pos.right;
            //    DEPosition left = pos.left;
            //    DEPosition front = pos.forward;
            //    DEPosition back = pos.back;
            //    DEPosition up = pos.up;
            //    DEPosition down = pos.down;

            //    if (!searched.Contains(right)) {
            //        searched.Add(right);
            //        if (!DEUtility.ContainsPos(pos.right)) quadVerts.AddRange(DEPosition.GetQuadWorldVertexs(pos, DEDirection.right, l2W));
            //    }
            //    if (!searched.Contains(left)) {
            //        searched.Add(left);
            //        if (!DEUtility.ContainsPos(pos.left)) quadVerts.AddRange(DEPosition.GetQuadWorldVertexs(pos, DEDirection.left, l2W));
            //    }
            //    if (!searched.Contains(front)) {
            //        searched.Add(front);
            //        if (!DEUtility.ContainsPos(pos.forward)) quadVerts.AddRange(DEPosition.GetQuadWorldVertexs(pos, DEDirection.forward, l2W));
            //    }
            //    if (!searched.Contains(back)) {
            //        searched.Add(back);
            //        if (!DEUtility.ContainsPos(pos.back)) quadVerts.AddRange(DEPosition.GetQuadWorldVertexs(pos, DEDirection.back, l2W));
            //    }
            //    if (!searched.Contains(up)) {
            //        searched.Add(up);
            //        if (!DEUtility.ContainsPos(pos.up)) quadVerts.AddRange(DEPosition.GetQuadWorldVertexs(pos, DEDirection.up, l2W));
            //    }
            //    if (!searched.Contains(down)) {
            //        searched.Add(down);
            //        if (!DEUtility.ContainsPos(pos.down)) quadVerts.AddRange(DEPosition.GetQuadWorldVertexs(pos, DEDirection.down, l2W));
            //    }
            //}
        } else {
            foreach (DEPosition pos in visibleSelection) {
                quadVerts.AddRange(DEPosition.GetQuadWorldVertexs(pos, planeDirection, l2W));
            }
            switch (planeDirection) {
                case DEDirection.right:
                    planeVerts = new Vector3[] { p[5], p[4], p[3], p[2] };
                    break;
                case DEDirection.left:
                    planeVerts = new Vector3[] { p[1], p[0], p[7], p[6] };
                    break;
                case DEDirection.front:
                    planeVerts = new Vector3[] { p[0], p[3], p[4], p[7] };
                    break;
                case DEDirection.back:
                    planeVerts = new Vector3[] { p[6], p[5], p[2], p[1] };
                    break;
                case DEDirection.up:
                    planeVerts = new Vector3[] { p[0], p[1], p[2], p[3] };
                    break;
                case DEDirection.down:
                    planeVerts = new Vector3[] { p[4], p[5], p[6], p[7] };
                    break;
            }
        }

        HandleDrawer.quadVerts = quadVerts;
        HandleDrawer.planeVerts = planeVerts;
        HandleDrawer.p = p;

        //UnityEngine.Debug.Log("Time used for updating selection:" + sw.ElapsedMilliseconds);
        updatingSelection = false;
        //sw.Stop();
    }

    public static void DrawSelectionCube()
    {
        GL.Begin(GL.QUADS);
        faceMat.SetPass(0);
        //GL.Color(Color.white);
        for (int i = 0; i < quadVerts.Count; i += 4) {
            DrawQuad(new Vector3[] { quadVerts[i], quadVerts[i + 1], quadVerts[i + 2], quadVerts[i + 3] }, faceMat);
        }
        GL.End();
        //now lets draw 12 sides
        GL.Begin(GL.LINES);
        DrawSelectionLine(p[0], p[1]);
        DrawSelectionLine(p[1], p[2]);
        DrawSelectionLine(p[2], p[3]);
        DrawSelectionLine(p[3], p[0]);
        DrawSelectionLine(p[0], p[7]);
        DrawSelectionLine(p[1], p[6]);
        DrawSelectionLine(p[2], p[5]);
        DrawSelectionLine(p[3], p[4]);
        DrawSelectionLine(p[4], p[5]);
        DrawSelectionLine(p[5], p[6]);
        DrawSelectionLine(p[6], p[7]);
        DrawSelectionLine(p[7], p[4]);
        GL.End();
        //To do: draw all 8 vertex for stronger visual feed back
    }

    public static void DrawSelectionPlane()
    {
        GL.Begin(GL.QUADS);
        faceMat.SetPass(0);
        //GL.Color(Color.white);
        for (int i = 0; i < quadVerts.Count; i += 4) {
            DrawQuad(new Vector3[] { quadVerts[i], quadVerts[i + 1], quadVerts[i + 2], quadVerts[i + 3] }, faceMat);
        }
        GL.End();

        GL.Begin(GL.LINES);
        DrawSelectionLine(planeVerts[0], planeVerts[1]);
        DrawSelectionLine(planeVerts[1], planeVerts[2]);
        DrawSelectionLine(planeVerts[2], planeVerts[3]);
        DrawSelectionLine(planeVerts[3], planeVerts[0]);
        GL.End();
    }

    //public static void BeginSelection()
    //{
    //	selecting = true;
    //}

    //public static void SetSelection(DEPosition rfu, DEPosition lbd)
    //{
    //	pointA = rfu;
    //	pointB = lbd;
    //}

    //public static void EndSelection()
    //{
    //	selecting = false;
    //}

    //set the material for drawing handles
    public static void SetMaterial(Material face, Material outline, Material dottedLine, Material verts)
    {
        faceMat = face;
        lineMat = outline;
        dottedLineMat = dottedLine;
        //vertMat = verts;
    }

    //todo: this is not working, maybe modify it later
    //private static List<Vector3> GetQuadWorldVertexs(DEPosition position, DEDirection direction, Matrix4x4 l2w, Vector3 offset)
    //{
    //	Vector3[] verts;
    //	List<Vector3> result;
    //	if (calculatedVerts.TryGetValue(position, out verts)) {
    //		result = new List<Vector3>();
    //		switch (direction) {
    //			case DEDirection.right:
    //				result.AddRange(new Vector3[] { verts[5], verts[4], verts[3], verts[2] });
    //				break;
    //			case DEDirection.left:
    //				result.AddRange(new Vector3[] { verts[1], verts[0], verts[7], verts[6] });
    //				break;
    //			case DEDirection.forward:
    //				result.AddRange(new Vector3[] { verts[0], verts[3], verts[4], verts[7] });
    //				break;
    //			case DEDirection.back:
    //				result.AddRange(new Vector3[] { verts[6], verts[5], verts[2], verts[1] });
    //				break;
    //			case DEDirection.up:
    //				result.AddRange(new Vector3[] { verts[0], verts[1], verts[2], verts[3] });
    //				break;
    //			case DEDirection.down:
    //				result.AddRange(new Vector3[] { verts[4], verts[5], verts[6], verts[7] });
    //				break;
    //		}
    //	} else {
    //		verts = new Vector3[8];
    //		verts.ToList().AddRange(DEPosition.GetQuadWorldVertexs(position, DEDirection.up, l2w));
    //		verts.ToList().AddRange(DEPosition.GetQuadWorldVertexs(position, DEDirection.down, l2w));

    //		for (int i = 0; i < verts.Length; i++) {
    //			verts[i] = verts[i] - offset;
    //		}
    //		result = new List<Vector3>();
    //		switch (direction) {
    //			case DEDirection.right:
    //				result.AddRange(new Vector3[] { verts[5], verts[4], verts[3], verts[2] });
    //				break;
    //			case DEDirection.left:
    //				result.AddRange(new Vector3[] { verts[1], verts[0], verts[7], verts[6] });
    //				break;
    //			case DEDirection.forward:
    //				result.AddRange(new Vector3[] { verts[0], verts[3], verts[4], verts[7] });
    //				break;
    //			case DEDirection.back:
    //				result.AddRange(new Vector3[] { verts[6], verts[5], verts[2], verts[1] });
    //				break;
    //			case DEDirection.up:
    //				result.AddRange(new Vector3[] { verts[0], verts[1], verts[2], verts[3] });
    //				break;
    //			case DEDirection.down:
    //				result.AddRange(new Vector3[] { verts[4], verts[5], verts[6], verts[7] });
    //				break;
    //		}

    //		calculatedVerts.Add(position, verts);
    //	}


    //	for (int i = 0; i < result.Count; i++) {
    //		result[i] = result[i] + offset;
    //	}
    //	return result;
    //}

    private static void DrawSelectionLine(Vector3 beginPos, Vector3 endPos)
    {
        //this is to make sure the line is rendered on top of scene
        beginPos.Set(beginPos.x, beginPos.y, beginPos.z - 0.02f); //any number lower than 0.02 seems to make the line thinner
        endPos.Set(endPos.x, endPos.y, endPos.z - 0.02f);
        DrawLine(beginPos, endPos, lineMat);

        //make sure the dotted lines are on top of everything
        beginPos.Set(beginPos.x, beginPos.y, -9.5f); //set it to -9.5f to make sure it will always on top of everything
        endPos.Set(endPos.x, endPos.y, -9.5f);
        DrawDottedLine(beginPos, endPos, dottedLineMat);
    }

    //draw a line
    private static void DrawLine(Vector3 beginPos, Vector3 endPos, Material mat)
    {
        //GL.Begin(GL.LINES);
        //mat.SetPass(0);
        GL.Color(Color.white);
        GL.Vertex(beginPos);
        GL.Vertex(endPos);
        //GL.End();
    }

    //draw a dotted line
    private static void DrawDottedLine(Vector3 beginPos, Vector3 endPos, Material mat)
    {
        //10 segements for each one unity unit
        int segNum = Mathf.RoundToInt((beginPos - endPos).magnitude * 4);
        int vertNum = segNum * 2;

        //GL.Begin(GL.LINES);
        mat.SetPass(0);
        GL.Color(Color.white);

        Vector3 offSet = (endPos - beginPos) / vertNum;

        for (int i = 0; i < vertNum; i++) {
            GL.Vertex(beginPos + i * offSet);
        }

        //GL.End();
    }

    //draw a plane that can be hidden behind objects
    private static void DrawQuad(Vector3[] verts, Material mat)
    {
        //GL.Begin(GL.QUADS);
        //mat.SetPass(0);

        for (int i = 0; i < verts.Length; i++) {
            GL.Vertex3(verts[i].x, verts[i].y, verts[i].z - 0.001f); //0.00008 seems to be a promising number
        }
        //GL.End();
    }
}
