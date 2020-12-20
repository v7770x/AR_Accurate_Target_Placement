using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ImageTargetHandler : MonoBehaviour
{
    //declare number of targets
    public const int NUM_TARGETS = 3;

    //needed lists to store targets and locations
    public GameObject[] targets = new GameObject[NUM_TARGETS];
    private GameObject[] targetObjects = new GameObject[NUM_TARGETS];
    private EventHandler[] targetEventHandlers = new EventHandler[NUM_TARGETS];
    private Transform[] targetTransforms = new Transform[NUM_TARGETS];

    //keep track of whether or not the target is currently being tracked and how many targets were found
    private bool[] targetTracked = new bool[NUM_TARGETS];
    private bool[] targetFound = new bool[NUM_TARGETS];
    private int numTargetsFound = 0;

    //display points, distances
    public GameObject [] point_texts = new GameObject[NUM_TARGETS];
    public GameObject [] distance_texts = new GameObject[NUM_TARGETS];
    public GameObject[] model_texts = new GameObject[NUM_TARGETS];
    public GameObject ip_text;
    public GameObject optimize_text;

    //hold projection objects
    public GameObject ProjectionObject;
    public GameObject objHolder;

    double[,] ModelPoints;

    //buttons
    public GameObject IPButton; 
    public GameObject ResetButton;
    public GameObject OptimizeButton;

    //keyboard input
    TouchScreenKeyboard keyboard;
    public static string ipAddressText = "192.168.0.119";



    private void Update()
    {
        if (TouchScreenKeyboard.visible == false && keyboard != null)
        {
            ip_text.GetComponent<Text>().text = "IP Input: " + keyboard.text + "";
            if (keyboard.status == TouchScreenKeyboard.Status.Done)
            {
                if (keyboard.text != "")
                {
                    ipAddressText = keyboard.text;
                }
                ip_text.GetComponent<Text>().text = "IP set to: " + ipAddressText;
                keyboard = null;
                UpdateModel();
            }
        }


    }


    /// <summary>
    /// Awake function to set intial params
    /// </summary>
    public void Awake()
    {
        for (int i = 0; i < NUM_TARGETS; i++)
        {
            // set the event handlers and callback functions
            EventHandler eventHandler = targets[i].GetComponent<EventHandler>();
            int targetNumber = eventHandler.TARGET_NUMBER;
            targetObjects[targetNumber] = targets[i];
            targetEventHandlers[targetNumber] = eventHandler;
            targetEventHandlers[targetNumber].OnImageTargetFound += HandleTargetFound;

            //indicate target not yet tracked
            targetTracked[i] = false;
        }

        IPButton.GetComponent<Interactable>().OnClick.AddListener(HandleIPButtonClick);
        ResetButton.GetComponent<Interactable>().OnClick.AddListener(HandleResetButtonClick);
        OptimizeButton.GetComponent<Interactable>().OnClick.AddListener(HandleOptimizeButtonClick);
        //printMatrix(ApiController.GetModelPoints().points, "space points");

        //double[,] pointsFromAPI = new double[NUM_TARGETS, 3] { { 0.25, 0.25, 0 }, { 0.25, -0.25, 0 }, { -0.25, 0.25, 0 } };
        //DisplayDistances(pointsFromAPI);
        //print("image target handler awake");
        //ApplyTransform(pointsFromAPI);
        HandleIPButtonClick();
        //StartCoroutine(ApiController.GetModelPointsAsync(HandleModelPoints));
        UpdateModel();
        //HandleOptimizeButtonClick();


    }



    /// <summary>
    /// When destroyed, remove the callback functions from the event handlers
    /// </summary>
    public void OnDestroy()
    {
        foreach (EventHandler handler in targetEventHandlers)
        {
            handler.OnImageTargetFound -= HandleTargetFound;
        }
        IPButton.GetComponent<Interactable>().OnClick.RemoveListener(HandleIPButtonClick);
        ResetButton.GetComponent<Interactable>().OnClick.RemoveListener(HandleResetButtonClick);
        OptimizeButton.GetComponent<Interactable>().OnClick.RemoveListener(HandleOptimizeButtonClick);
    }

    public void HandleResetButtonClick()
    {
        print("reset button clicked");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void HandleIPButtonClick()
    {
        print("IP button Clicked");
        // display ip address keyboard
        keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default, false, false, false, false, "Enter ip address of server (i.e. 192.168.0.23)");
        IPButton.SetActive(false);
    }

    public void OptimizationOnSuccess(List<double [,]> possibleSpacePoints)
    {
        optimize_text.GetComponent<Text>().text = "Possible Optimization Points Count: " + possibleSpacePoints.Count;
        for (int i = 0; i<possibleSpacePoints.Count; i++)
        {
            printMatrix(possibleSpacePoints[i], "possible matrix " + i);
        }
    }

    public void HandleOptimizeButtonClick()
    {
        double[,] spacePoints;
        spacePoints = ExtractPointsFromTransforms(targetTransforms);
        //spacePoints = new double[NUM_TARGETS, 3] { { -0.6659, -0.5421, -0.7766 }, { -0.2835, -0.5464, -0.5027 }, { -0.3628, -0.5376, -1.1833 } };
        StartCoroutine(ApiController.RunOptimizationAsync(OptimizationOnSuccess, spacePoints));
        print("Optimize button clicked");
        optimize_text.GetComponent<Text>().text = "Optimizing...";
        if (numTargetsFound!= NUM_TARGETS)
        {
            print("targets not found");
        }
        else
        {

        }
    }

    public void DisplayDistances(double [,] points)
    {
        for(int i = 0; i<points.GetLength(0); i++)
        {
            int next_ind = (i + 1) % points.GetLength(0);
            double distance = Point.Distance(new Point(points[i, 0], points[i, 1], points[i, 2]), new Point(points[next_ind, 0], points[next_ind, 1], points[next_ind, 2]));
            distance_texts[i].GetComponent<Text>().text = "Distance point " + (i+1) + " to point " + (next_ind+1) + " = " + distance;
            print("Distance point " + (i + 1) + " to point " + (next_ind + 1) + " = " + distance);
        }
    }

    /// <summary>
    /// event handler function to add position of target to list when a target is found
    /// </summary>
    /// <param name="targetNumber"></param>
    private void HandleTargetFound(int targetNumber)
    {
        print(targetNumber);
        // set target found if not already set
        if (!targetFound[targetNumber])
        {
            print("target " + targetNumber + " found");
            targetFound[targetNumber] = true;
            numTargetsFound += 1;

            //get target's transform object and save it
            Transform targetTransform = targetObjects[targetNumber].transform;
            UpdateTargetPosition(targetNumber, targetTransform);
            print("Target " + targetNumber + ", Position = " + targetTransforms[targetNumber].position);


            //draw sphere at (0,0,0) of target
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            sphere.transform.position = targetTransform.position;
            MeshRenderer meshRenderer = sphere.GetComponent<MeshRenderer>();
            meshRenderer.material = Resources.Load<Material>("red");

            if (numTargetsFound == NUM_TARGETS)
            {
                //ApiController.PointsData modelPoints =  ApiController.GetModelPoints();
                //double[,] pointsFromAPI = new double[NUM_TARGETS, 3] { { 0.25, 0.25, 0 }, { 0.25, -0.25, 0 }, { -0.25, 0.25, 0 } };
                ApplyTransform(ModelPoints);

            }
        }

       

    }


    /// <summary>
    /// Calculate the transform and extract the quaternion as well as the translation
    /// Then apply the transformation to the 3D model
    /// </summary>
    /// <param name="modelPoints"></param>
    private void ApplyTransform(double [,] modelPoints) 
    {
        //extract the points in real space from the transform components saved
        double[,] spacePoints;
        spacePoints = ExtractPointsFromTransforms(targetTransforms);
        //spacePoints = ExtractPointsFromGameObjects(targetObjects);
        DisplayDistances(spacePoints);

        //spacePoints = new double[NUM_TARGETS, 3] { { -0.6659, -0.5421, -0.7766 }, { -0.2835, -0.5464, -0.5027 }, { -0.3628, -0.5376, -1.1833 } };
        //spacePoints = new double[NUM_TARGETS, 3] { { 0.25, 0.25, 0 }, { 0.25, -0.25, 0 }, { -0.25, 0.25, 0 } };

        //use svd method to find transformation of points from model to space
        Transform3DBestFit transformation = new Transform3DBestFit(modelPoints, spacePoints);
        transformation.CalcTransform(transformation.actualsMatrix, transformation.nominalsMatrix);

        //FitPoints3D fitPoints = FitPoints3D.Fit(new List<Point> { }, new List<Point);

        printMatrix(transformation.TransformMatrix, "Transform");
        printMatrix(transformation.RotationMatrix, "Rotation");
        printMatrix(transformation.TranslationMatrix, "Translation");

        //find euler angles/ quaternion
        Quaternion quaternion = QuaternionFromMatrix(transformation.TransformMatrix);
        print("euler angles: " +quaternion.eulerAngles);

        Quaternion quaternion2 = ExtractRotation(transformation.TransformMatrix);
        print("euler angles 2: " + quaternion2.eulerAngles);

        //find translation
        Vector3 translation = TranslationFromMatrix(transformation.TransformMatrix);
        print("translation" + translation);

        //apply transformation
        ProjectionObject.SetActive(true);
        ProjectionObject.transform.position = translation;
        ProjectionObject.transform.rotation = quaternion;

    }

    private double[,] ExtractPointsFromTransforms(Transform [] transformArray)
    {
        double[,] spacePoints = new double[transformArray.Length, 3];
        for (int i = 0; i < transformArray.Length; i++)
        {
            spacePoints[i, 0] = transformArray[i].position.x;
            spacePoints[i, 1] = transformArray[i].position.y;
            spacePoints[i, 2] = transformArray[i].position.z;
        }
        return spacePoints;
    }

    private double[,] ExtractPointsFromGameObjects(GameObject[] objectArray)
    {
        double[,] spacePoints = new double[objectArray.Length, 3];
        for (int i = 0; i < objectArray.Length; i++)
        {
            Transform currObjTransform = objectArray[i].GetComponent<Transform>();
            spacePoints[i, 0] = currObjTransform.position.x;
            spacePoints[i, 1] = currObjTransform.position.y;
            spacePoints[i, 2] = currObjTransform.position.z;
        }
        return spacePoints;
    }

    private void printMatrix(double[,] matrix, string matrixName = "matrix")
    {
        print("Matrix: " + matrixName);        
        for (int i = 0; i < matrix.GetLength(0); i++)
        {
            string output = "";
            for (int j = 0; j < matrix.GetLength(1); j++)
            {
                output += matrix[i, j] + ", ";
            }
            print(output);
        }
    }

    /// <summary>
    /// extract quaternion from 4x4 rotation matrix/ transform matrix
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    private Quaternion QuaternionFromMatrix(double[,] m)
    {
        // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
        Quaternion q = new Quaternion();
        q.w = Mathf.Sqrt(Mathf.Max(0, (float)(1 + m[0, 0] + m[1, 1] + m[2, 2])) ) / 2;
        q.x = Mathf.Sqrt(Mathf.Max(0, (float)(1 + m[0, 0] - m[1, 1] - m[2, 2])) ) / 2;
        q.y = Mathf.Sqrt(Mathf.Max(0, (float) (1 - m[0, 0] + m[1, 1] - m[2, 2])) ) / 2;
        q.z = Mathf.Sqrt(Mathf.Max(0, (float)(1 - m[0, 0] - m[1, 1] + m[2, 2]))) / 2;
        q.x *= Mathf.Sign( q.x * (float)(m[2, 1] - m[1, 2]));
        q.y *= Mathf.Sign(q.y * (float) (m[0, 2] - m[2, 0]));
        q.z *= Mathf.Sign(q.z * (float)(m[1, 0] - m[0, 1]));
        return q;
    }

    public static Quaternion ExtractRotation(double[,] m)
    {
        Vector3 forward;
        forward.x = (float)m[0,2];
        forward.y = (float)m[1,2];
        forward.z = (float)m[2,2];

        Vector3 upwards;
        upwards.x = (float)m[0, 1];
        upwards.y = (float)m[1, 1];
        upwards.z = (float)m[2, 1];

        return Quaternion.LookRotation(forward, upwards);
    }


    /// <summary>
    /// given an affine transform 4x4 matrix, returns a vector 3 with just the translation
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    private Vector3 TranslationFromMatrix(double [,] m)
    {
        return new Vector3((float)m[0, 3], (float)m[1, 3], (float)m[2, 3]);
    }

    /// <summary>
    /// Helper function to update given target's position 
    /// </summary>
    /// <param name="targetNumber"></param>
    /// <param name="targetTransform"></param>
    private void UpdateTargetPosition(int targetNumber, Transform targetTransform)
    {
        print("updating target " + targetNumber + " position");
        targetTransforms[targetNumber] = targetTransform;
        Text UIText = point_texts[targetNumber].GetComponent<Text>();
        UIText.text = "Target " + targetNumber + ", Position = " + targetTransform.position * 1000;
    }

    public void HandleModelPoints(double[,] modelPoints)
    {
        print("Model points received");
        ModelPoints = modelPoints;
        printMatrix(ModelPoints);
        for (int i = 0; i < ModelPoints.GetLength(0); i++)
        {
            model_texts[i].GetComponent<Text>().text = "Model point " + (i + 1) + " = " + "(" + ModelPoints[i, 0] + ", " + ModelPoints[i, 1] + ", " + ModelPoints[i, 2] + ")";
        }
    }

    private void UpdateModel()
    {
        StartCoroutine(ApiController.SetModelObjectAsync(objHolder));
        StartCoroutine(ApiController.GetModelPointsAsync(HandleModelPoints));

    }


}
