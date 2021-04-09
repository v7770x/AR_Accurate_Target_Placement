using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using TMPro;
//using MathNet.Numerics.LinearAlgebra.Complex;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using Microsoft.MixedReality.Toolkit.Physics;
using System.Data.Odbc;

enum ProjectionMethod
{
    METHOD_NONE,
    PCA,
    NORMAL,
    CROSS
}

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
    public GameObject[] opt_texts = new GameObject[NUM_TARGETS];
    public GameObject ip_text;
    public GameObject optimize_text;
    public GameObject[] optimizationMarkers = new GameObject[NUM_TARGETS];

    //hold projection objects
    public GameObject ProjectionObject;
    public GameObject objHolder;

    double[,] ModelPoints;
    double[,] ModelVectors;

    //buttons
    public GameObject IPButton; 
    public GameObject ResetButton;
    public GameObject OptimizeButton;
    public GameObject ChangeIPButton;
    public GameObject ShowVectorsButton;
    public GameObject UpdatePointsButton;
    public GameObject UpdateModelButton;
    public GameObject RotateZButton;
    public GameObject RotateYButton;
    public GameObject RotateXButton;
    public GameObject ProjectModelPCAButton;
    public GameObject ProjectModelNormalButton;
    public GameObject ProjectModelCrossButton;

    //store chosen projection method temporarily
    private ProjectionMethod chosenProjectionMethod = ProjectionMethod.METHOD_NONE;

    //keyboard input
    TouchScreenKeyboard keyboard;
    public static string ipAddressText = "192.168.0.22";

    //data in persistent storage
    [System.Serializable]
    public class StorageData
    {
        public string ipAddress;
    }

    public static StorageData storageData = new StorageData();
    string STORAGE_DATA_PATH;

    void updateIpAddressText(string ipAddress)
    {
        print("updating ip address text to: " + ipAddress);
        ip_text.GetComponent<TextMeshPro>().text = "IP address: " + ipAddress;
    }

    private void Update()
    {
        if (TouchScreenKeyboard.visible == false && keyboard != null)
        {
            updateIpAddressText(keyboard.text);
            if (keyboard.status == TouchScreenKeyboard.Status.Done)
            {
                if (keyboard.text != "")
                {
                    storageData.ipAddress = keyboard.text;
                    updateIpAddressText(storageData.ipAddress);
                }
                
                keyboard = null;
                updateStoredData();
                CheckUpdateModelOnStart();
            }
        }


    }

    void updateStoredData()
    {
        print(STORAGE_DATA_PATH);
        string jsonText = JsonUtility.ToJson(storageData);
        using (StreamWriter sw = new StreamWriter(STORAGE_DATA_PATH, false))
        {
            sw.WriteLine(jsonText);
        }
    }

    bool loadStoredData()
    {
        try
        {
            using (StreamReader sr = new StreamReader(STORAGE_DATA_PATH))
            {
                string jsonText = "";

                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    jsonText += line;
                }
                storageData = JsonUtility.FromJson<StorageData>(jsonText);
            }
            print(storageData);
            return true;
        }catch{
            print("no stored data found");
            return false;
        }
        
    }


    /// <summary>
    /// Awake function to set intial params
    /// </summary>
    public void Awake()
    {
        STORAGE_DATA_PATH = Application.persistentDataPath + "/storageData.json";
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
        ChangeIPButton.GetComponent<Interactable>().OnClick.AddListener(HandleIPButtonClick);
        ShowVectorsButton.GetComponent<Interactable>().OnClick.AddListener(HandleShowVectorsClick);
        UpdateModelButton.GetComponent<Interactable>().OnClick.AddListener(HandleUpdateModelClick);
        UpdatePointsButton.GetComponent<Interactable>().OnClick.AddListener(UpdatePoints);
        ProjectModelPCAButton.GetComponent<Interactable>().OnClick.AddListener(HandleProjectPCAClick);
        ProjectModelNormalButton.GetComponent<Interactable>().OnClick.AddListener(HandleProjectNormalClick);
        ProjectModelCrossButton.GetComponent<Interactable>().OnClick.AddListener(HandleProjectCrossClick);
        RotateXButton.GetComponent<Interactable>().OnClick.AddListener(HandleRotateX);
        RotateYButton.GetComponent<Interactable>().OnClick.AddListener(HandleRotateY);
        RotateZButton.GetComponent<Interactable>().OnClick.AddListener(HandleRotateZ);

        //printMatrix(ApiController.GetModelPoints().points, "space points");
        ///*
        if (!loadStoredData())
        {
            HandleIPButtonClick();
        }
        else
        {
            updateIpAddressText(storageData.ipAddress);
            CheckUpdateModelOnStart();
        }
        //*/
        /*
        storageData.ipAddress = ipAddressText;
        updateStoredData();
        updateIpAddressText(ipAddressText);
        UpdateModel();
        */
        


    }

    private void CheckUpdateModelOnStart()
    {
        print(ApiController.MODEL_SAVE_PATH);
        if(File.Exists(ApiController.MODEL_SAVE_PATH))
        {
            print("Model exists, not updating");
            UpdateModel(false);
        }
        else
        {
            print("Model does not exist, updating");
            UpdateModel(true);
        }
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
        ChangeIPButton.GetComponent<Interactable>().OnClick.RemoveListener(HandleIPButtonClick);
        ShowVectorsButton.GetComponent<Interactable>().OnClick.RemoveListener(HandleShowVectorsClick);
        UpdateModelButton.GetComponent<Interactable>().OnClick.RemoveListener(HandleUpdateModelClick);
        ProjectModelPCAButton.GetComponent<Interactable>().OnClick.RemoveListener(HandleProjectPCAClick);
        ProjectModelNormalButton.GetComponent<Interactable>().OnClick.RemoveListener(HandleProjectNormalClick);
        ProjectModelCrossButton.GetComponent<Interactable>().OnClick.RemoveListener(HandleProjectCrossClick);
        RotateXButton.GetComponent<Interactable>().OnClick.RemoveListener(HandleRotateX);
        RotateYButton.GetComponent<Interactable>().OnClick.RemoveListener(HandleRotateY);
        RotateZButton.GetComponent<Interactable>().OnClick.RemoveListener(HandleRotateZ);
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

        Vector3[] vector3Points = ConvertToVector3s(possibleSpacePoints[0]);

        // check which method was called earlier and call it again with the new adjusted points
        if (possibleSpacePoints.Count == 1)
        {
            switch (chosenProjectionMethod)
            {
                case ProjectionMethod.PCA:
                    print("Projecting PCA after optimizing");
                    ApplyTransformPCA(ModelPoints, possibleSpacePoints[0]);
                    break;
                case ProjectionMethod.CROSS:
                    print("Projecting Cross after optimizing");
                    ApplyTransformCross(ModelPoints, possibleSpacePoints[0]);
                    break;
                case ProjectionMethod.NORMAL:
                    print("Projecting normal after optimizing");
                    ApplyTransformNormal(ModelVectors, ModelPoints, possibleSpacePoints[0]);
                    break;
                default:
                    break;
            }
            if (numTargetsFound != NUM_TARGETS)
            {
                return;
            }

            // change the positions of the optimization targets to the new positions from the optimization result
            for (int i = 0; i < NUM_TARGETS; i++)
            {
                // set marker
                optimizationMarkers[i].transform.position = vector3Points[i];
                // update text
                UpdateOptimizationPoints(i, vector3Points[i]);
            }
        }

    }

    public void HandleOptimizeButtonClick()
    {
        //extract points and normal vectors
        double[,] spacePoints;
        double[,] normalVectors;
        spacePoints = ExtractPointsFromTransforms(targetTransforms);
        normalVectors = ExtractVectorsFromTransforms(targetTransforms);
        //spacePoints = new double[NUM_TARGETS, 3] { { -0.6659, -0.5421, -0.7766 }, { -0.2835, -0.5464, -0.5027 }, { -0.3628, -0.5376, -1.1833 } };
        //normalVectors = new double[NUM_TARGETS, 3] { { -0.6659, -0.5421, -0.7766 }, { -0.2835, -0.5464, -0.5027 }, { -0.3628, -0.5376, -1.1833 } };


        //call api to optimize
        print("Optimize button clicked");
        optimize_text.GetComponent<Text>().text = "Optimizing...";
        StartCoroutine(ApiController.RunOptimizationAsync(OptimizationOnSuccess, spacePoints, normalVectors));
        if (numTargetsFound!= NUM_TARGETS)
        {
            print("targets not found");
        }
    }

    public void HandleShowVectorsClick()
    {
        if (numTargetsFound == NUM_TARGETS)
        {
            print("Up Vectors: ");
            for (int i = 0; i < NUM_TARGETS; i++)
            {
                print(targetTransforms[i].up * 1000);
                GameObject targetText = targetObjects[i].transform.Find("NormText").gameObject;
                targetText.GetComponent<TextMeshPro>().text = "(" + targetTransforms[i].up[0] * 1000 + ", "
                        + targetTransforms[i].up[1] * 1000 + ", " + targetTransforms[i].up[2] * 1000 + ")";
                targetText.SetActive(true);
                GameObject targetSys = targetObjects[i].transform.Find("CoordSys").gameObject;
                targetSys.SetActive(true);
            }
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

    public GameObject createSphere(Vector3 position, string color)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        sphere.transform.position = position;
        MeshRenderer meshRenderer = sphere.GetComponent<MeshRenderer>();
        meshRenderer.material = Resources.Load<Material>(color);
        return sphere;
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
            GameObject sphere = createSphere(targetTransform.position, "red");
            GameObject opSphere = createSphere(targetTransform.position, "green");
            optimizationMarkers[targetNumber] = opSphere;

            HandleProjectPCAClick();
            //HandleProjectNormalClick();

        }

       

    }

    /// <summary>
    /// Method to rotate projection object 180 degrees about plane created by points
    /// </summary>
    public void HandleRotateX()
    {
        objHolder.gameObject.transform.localRotation *= Quaternion.Euler(180, 0, 0);
    }

    public void HandleRotateY()
    {
        objHolder.gameObject.transform.localRotation *= Quaternion.Euler(0, 180, 0);
    }

    public void HandleRotateZ()
    {
        objHolder.gameObject.transform.localRotation *= Quaternion.Euler(0, 0, 180);
    }

    /// <summary>
    /// Method to rotate projection object 180 degrees about plane created by points
    /// </summary>
    public void HandleFlipProjectionObject2()
    {
        ProjectionObject.gameObject.transform.localRotation *= Quaternion.Euler(0, 0, 180);
    }

    /// <summary>
    /// event handler to trigger projection of the object using the PCA method
    /// </summary>
    public void HandleProjectPCAClick()
    {
        if (numTargetsFound == NUM_TARGETS)
        {
            ApplyTransformPCA(ModelPoints, ExtractPointsFromTransforms(targetTransforms));
            chosenProjectionMethod = ProjectionMethod.PCA;
            
        }
    }

    public void HandleProjectNormalClick()
    {
        if (numTargetsFound == NUM_TARGETS)
        {
            ApplyTransformNormal(ModelVectors, ModelPoints, ExtractPointsFromTransforms(targetTransforms));
            chosenProjectionMethod = ProjectionMethod.NORMAL;

        }
    }

    public void HandleProjectCrossClick()
    {
        if (numTargetsFound == NUM_TARGETS)
        {
            ApplyTransformCross(ModelPoints, ExtractPointsFromTransforms(targetTransforms));
            chosenProjectionMethod = ProjectionMethod.CROSS;

        }
    }

    private Vector3[] ConvertToVector3s(double [,] doubleVectors)
    {
        Vector3[] vector3s = new Vector3[NUM_TARGETS];
        for (int i = 0; i < NUM_TARGETS; i++)
        {
            vector3s[i].x = (float)doubleVectors[i, 0];
            vector3s[i].y = (float)doubleVectors[i, 1];
            vector3s[i].z = (float)doubleVectors[i, 2];
        }
        return vector3s;
    }

    private Matrix<double> FindOrthonormalMatrix(Vector3[] points)
    {
        // find 3 orthogonal vectors
        Vector3 v1 = Vector3.Normalize(points[1] - points[0]);
        Vector3 v2 = Vector3.Normalize(points[1] - points[2]);
        Vector3 norm = Vector3.Normalize(Vector3.Cross(v1, v2));
        Vector3 v2_ortho = Vector3.Normalize(Vector3.Cross(norm, v1));

        // build a matrix with the vectors
        Matrix<double> T = Matrix<double>.Build.Dense(3, 3);
        T[0, 0] = v1.x;
        T[1, 0] = v1.y;
        T[2, 0] = v1.z;
        T[0, 1] = v2_ortho.x;
        T[1, 1] = v2_ortho.y;
        T[2, 1] = v2_ortho.z;
        T[0, 2] = norm.x;
        T[1, 2] = norm.y;
        T[2, 2] = norm.z;
        
        return T;
    }


    private void ApplyTransformCross(double[,] modelPoints, double[,] spacePoints)
    {
        print("Applying Transform Cross");
        // get in vector3 format
        Vector3[] modelVector3s = ConvertToVector3s(modelPoints);
        Vector3[] spaceVector3s = ConvertToVector3s(spacePoints);
        

        // find the orthogonal matricies of each set of points
        var R1 = FindOrthonormalMatrix(modelVector3s);
        var R2 = FindOrthonormalMatrix(spaceVector3s);

        // find rotation matrix
        var R = R2.Multiply(R1.Inverse());

        // find translation
        var T = Matrix<double>.Build.DenseIdentity(4);
        Vector<double> V = Vector<double>.Build.DenseOfArray(new double[] { modelPoints[1, 0], modelPoints[1, 1], modelPoints[1, 2] });
        T.SetSubMatrix(0, 0, R);
        var P2_Rotated = R * V;
        var translation = spaceVector3s[1] - (new Vector3( (float)P2_Rotated[0], (float)P2_Rotated[1], (float)P2_Rotated[2] ));

        print(R);
        print(translation);

        // apply transformation
        ProjectionObject.SetActive(true);
        ProjectionObject.transform.rotation = QuaternionFromMatrix(R.ToArray());
        ProjectionObject.transform.transform.position = translation;

    }



    private void ApplyTransformNormal(double [,] modelVectors, double[,] modelPoints, double[,] spacePoints)
    {
        print("APPLYING Normal TRANSFORM");
        
        double[,] spaceVectors = ExtractVectorsFromTransforms(targetTransforms);

        //testing:
        modelVectors = new double[,] { { 0, 0, 1 }, { 1, 0, 1 }, { 1, 1, 1 } };
        spaceVectors = new double[,] { { 1, 0, 0 }, { 3, 0, 1 }, { 3, 4, 1 } };

        Vector3[] modelVector3s = new Vector3[NUM_TARGETS];
        Vector3[] spaceVector3s = new Vector3[NUM_TARGETS];

        printMatrix(modelVectors, "model vectors");
        //printMatrix(modelPoints, "model points");
        printMatrix(spaceVectors, "space vectors");
        //printMatrix(spacePoints, "space points");
        for (int i = 0; i<NUM_TARGETS; i++)
        {
            modelVector3s[i].x = (float)modelVectors[i, 0];
            modelVector3s[i].y = (float)modelVectors[i, 1];
            modelVector3s[i].z = (float)modelVectors[i, 2];

            spaceVector3s[i].x = (float)spaceVectors[i, 0];
            spaceVector3s[i].y = (float)spaceVectors[i, 1];
            spaceVector3s[i].z = (float)spaceVectors[i, 2];
        }
        print(modelVector3s[2]);

        //calculate the cross product of the model vectors of interest
        Vector3 modelCross1;
        int crossIndex;
        if(Vector3.Angle(modelVector3s[0], modelVector3s[1]) > 10.0f)
        {
            modelCross1 = Vector3.Cross(modelVector3s[0], modelVector3s[1]).normalized;
            crossIndex = 1;
        }
        else
        {
            modelCross1 = Vector3.Cross(modelVector3s[1], modelVector3s[2]).normalized;
            crossIndex = 2;
        }
        Vector3 modelCross2 = Vector3.Cross(modelVector3s[0], modelCross1).normalized;

        //calculate the cross product of the space vectors of interest
        Vector3 spaceCross1 = Vector3.Cross(spaceVector3s[0], spaceVector3s[crossIndex]).normalized;
        Vector3 spaceCross2 = Vector3.Cross(spaceVector3s[0], spaceCross1).normalized;
        print("space Cross 1: [" + spaceCross1.x + "," + spaceCross1.y + ", " + spaceCross1.z + "]");
        print("space Cross 2: [" + spaceCross2.x + "," + spaceCross2.y + ", " + spaceCross2.z + "]");


        //find rotation and translation
        //Quaternion quaternion = Quaternion.FromToRotation(modelCross1, spaceCross1);
        double [,] combinedModelMatrix = new double[NUM_TARGETS, 3];
        double[,] combinedSpaceMatrix = new double[NUM_TARGETS, 3];

        //model matrix
        combinedModelMatrix[0, 0] = modelVector3s[0].x;
        combinedModelMatrix[0, 1] = modelVector3s[0].y;
        combinedModelMatrix[0, 2] = modelVector3s[0].z;

        combinedModelMatrix[1, 0] = modelCross1.x;
        combinedModelMatrix[1, 1] = modelCross1.y;
        combinedModelMatrix[1, 2] = modelCross1.z;

        combinedModelMatrix[2, 0] = modelCross2.x;
        combinedModelMatrix[2, 1] = modelCross2.y;
        combinedModelMatrix[2, 2] = modelCross2.z;

        //space matrix
        combinedSpaceMatrix[0, 0] = spaceVector3s[0].x;
        combinedSpaceMatrix[0, 1] = spaceVector3s[0].y;
        combinedSpaceMatrix[0, 2] = spaceVector3s[0].z;

        combinedSpaceMatrix[1, 0] = spaceCross1.x;
        combinedSpaceMatrix[1, 1] = spaceCross1.y;
        combinedSpaceMatrix[1, 2] = spaceCross1.z;

        combinedSpaceMatrix[2, 0] = spaceCross2.x;
        combinedSpaceMatrix[2, 1] = spaceCross2.y;
        combinedSpaceMatrix[2, 2] = spaceCross2.z;

        printMatrix(combinedSpaceMatrix, "combinedModelMatrix");
        printMatrix(combinedSpaceMatrix, "combinedSpaceMatrix");
        //calculate rotation matrix
        double[,] rotationMatrix = ( Matrix<double>.Build.DenseOfArray(combinedModelMatrix).Multiply(Matrix<double>.Build.DenseOfArray(combinedSpaceMatrix).Transpose()) ).ToArray();
        printMatrix(rotationMatrix, "rotationMatrix");

        Vector3 translation = new Vector3((float)(spacePoints[0,0] - modelPoints[0,0]) , (float)(spacePoints[0, 1] - modelPoints[0, 1]),
            (float)(spacePoints[0, 2] - modelPoints[0, 2]));

        print("model cross " + modelCross1);
        print("space cross " + spaceCross1);

        //apply transformation
        ProjectionObject.SetActive(true);

        ProjectionObject.transform.rotation = QuaternionFromMatrix(rotationMatrix);
        ProjectionObject.transform.transform.position = translation;
        //ProjectionObject.transform.Translate(translation);





    }




    /// <summary>
    /// Calculate the transform and extract the quaternion as well as the translation
    /// Then apply the transformation to the 3D model
    /// </summary>
    /// <param name="modelPoints"></param>
    private void ApplyTransformPCA(double [,] modelPoints, double[,] spacePoints) 
    {
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

    private double[,] ExtractVectorsFromTransforms(Transform[] transformArray)
    {
        double[,] normalVectors = new double[transformArray.Length, 3];
        for (int i = 0; i < transformArray.Length; i++)
        {
            normalVectors[i, 0] = transformArray[i].up.x;
            normalVectors[i, 1] = transformArray[i].up.y;
            normalVectors[i, 2] = transformArray[i].up.z;
        }
        return normalVectors;
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

    private void UpdateOptimizationPoints(int targetNumber, Vector3 position)
    {
        print("Updating opt target " + targetNumber + " position to " + position);
        Text UIText = opt_texts[targetNumber].GetComponent<Text>();
        UIText.text = "Opt Target " + targetNumber + ", Position = " + position * 1000;
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
        //double [,] spacePoints = new double[NUM_TARGETS, 3] { { -0.6659, -0.5421, -0.7766 }, { -0.2835, -0.5464, -0.5027 }, { -0.3628, -0.5376, -1.1833 } };
        //ApplyTransformCross(modelPoints, spacePoints);
        //flipProjectionObject();
        //HandleOptimizeButtonClick();
    }

    public void HandleModelVectors(double[,] modelVectors)
    {
        print("Model vectors received");
        ModelVectors = modelVectors;
        printMatrix(modelVectors, "Model Vectors");
    }

    public void HandleUpdateModelClick()
    {
        UpdateModel(true);
    }

    private void UpdateModel(bool sendRequest)
    {
        StartCoroutine(ApiController.SetModelObjectAsync(objHolder, sendRequest));
        UpdatePoints();

    }

    private void UpdatePoints()
    {
        StartCoroutine(ApiController.GetModelPointsAsync(HandleModelPoints));
        StartCoroutine(ApiController.GetModelVectorsAsync(HandleModelVectors));
    }


}
