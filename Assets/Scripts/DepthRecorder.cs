using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class DepthRecorder : MonoBehaviour
{
    public Camera camera;

    public Transform[] targets = new Transform[0];
    public bool isRecordOn;

    private int frameCount;
    private StreamWriter streamWriter;

    // Start is called before the first frame update
    void Start()
    {
        EditorApplication.playModeStateChanged += Save;

        if (isRecordOn)
        {
            var path = "Assets/Depths/depth.txt";
            streamWriter = File.CreateText(path);
        }
    }

    private void Save(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.ExitingPlayMode)
        {
            streamWriter.Close();
            isRecordOn = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!isRecordOn)
        {
            return;
        }
        var zOrderedIndexes = targets
            .Select((target, index) => (
                target,
                distance: Vector3.Distance(camera.transform.position, target.position),
                index
            ))
            .OrderBy(tuple => tuple.distance)
            .Select(tuple => tuple.index);

        // frame / index / index / index / index \n
        var text = $"{frameCount},{string.Join(",", zOrderedIndexes)}";
        streamWriter.WriteLine(text);

        frameCount++;
    }

    void Destroy()
    {
        Debug.Log("bye");
    }
}
