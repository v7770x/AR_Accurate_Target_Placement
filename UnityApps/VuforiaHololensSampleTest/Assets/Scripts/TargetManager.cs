using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;
using System.Linq;


public class TargetManager : MonoBehaviour
{
    private List<TrackableBehaviour> mAllTargets = new List<TrackableBehaviour>();
    private string mainGloveTarget = "GTHololens";
    private List<string> trackableObjectNames = new List<string> { "GTHololens", "AceOfSpades" };
    SpeechHandler speechHandler;

    //points to save
    private List<Transform> gloveTargetPoints = new List<Transform>();

    public GameObject speechManager;


    private void Awake()
    {
        VuforiaARController.Instance.RegisterVuforiaStartedCallback(OnVuforiaStarted);

        //get the voice manager object, and add an event listener to the guide view selected action
        speechHandler = speechManager.GetComponent<SpeechHandler>();
        speechHandler.OnGuideViewSelected += HandleGuideViewSelected;
        speechHandler.OnGloveTargetConfirmed += HandleGloveTargetConfirmed;
    }

    private void OnDestroy()
    {
        VuforiaARController.Instance.UnregisterVuforiaStartedCallback(OnVuforiaStarted);
        speechHandler.OnGuideViewSelected -= HandleGuideViewSelected;
        speechHandler.OnGloveTargetConfirmed -= HandleGloveTargetConfirmed;
    }

    private void HandleGuideViewSelected(string guideViewString)
    {
        print("target manager received view :" + guideViewString);

        //map the guideview string to the guideview index
        Dictionary<string, int> guideViewMapping = new Dictionary<string, int>();
        guideViewMapping.Add("one", 1);
        guideViewMapping.Add("two", 2);
        guideViewMapping.Add("three", 3);

        //activate glove target tracking with the given guide view index
        ActivateGloveTargetTracking(guideViewMapping[guideViewString]);

        //take the options menu out of view
        speechHandler.MenuDisplay.SetActive(false);



    }

    private void HandleGloveTargetConfirmed()
    {
        print("Glove target confirmed");

        //stop showing target confirmed text
        speechHandler.ConfirmTargetFoundText.SetActive(false);

        //save the position of the target
        Transform gloveTargetPosition = speechHandler.GloveTarget.transform;
        gloveTargetPoints.Add(gloveTargetPosition);

        print(gloveTargetPosition.position);

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        sphere.transform.position = gloveTargetPosition.position;
        MeshRenderer meshRenderer = sphere.GetComponent<MeshRenderer>();
        meshRenderer.material = Resources.Load<Material>("red");

        //deactivate tracking
        DeactivateGloveTargetTracking();

        if (gloveTargetPoints.Count() == 3)
        {
            print("Finished finding 3 targets");
            /*DrawLine(gloveTargetPoints[0].position, gloveTargetPoints[1].position, Color.red);
            DrawLine(gloveTargetPoints[1].position, gloveTargetPoints[2].position, Color.red);*/
            // DrawLine(gloveTargetPoints[2].position, gloveTargetPoints[0].position, Color.red);
        }


    }

    void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 1.0f)
    {
        GameObject myLine = new GameObject();
        myLine.transform.position = start;
        myLine.AddComponent<LineRenderer>();
        LineRenderer lr = myLine.GetComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        GameObject.Destroy(myLine, duration);
    }

    private void OnVuforiaStarted()
    {
        DeactivateGloveTargetTracking();
        mAllTargets = GetTargets();
        SetupTargets(mAllTargets);


    }

    private void ActivateGloveTargetTracking(int guideViewIndex = 0)
    {
        foreach (TrackableBehaviour trackableBehaviour in mAllTargets)
        {
            print(trackableBehaviour.TrackableName);
        }
        //get the object tracker instance
        ObjectTracker objectTracker = TrackerManager.Instance.GetTracker<ObjectTracker>();
        if (objectTracker == null)
        {
            return;
        }
        objectTracker.Stop();

        //load dataset
        DataSet gloveTargetDataSet = objectTracker.CreateDataSet();
        gloveTargetDataSet.Load(mainGloveTarget);
        foreach (Trackable trackable in gloveTargetDataSet.GetTrackables())
        {
            if (trackable is ModelTarget)
            {
                ModelTarget mTrackable = trackable as ModelTarget;
                mTrackable.SetActiveGuideViewIndex(guideViewIndex);
            }
        }

        objectTracker.ActivateDataSet(gloveTargetDataSet);

        //restart tracking
        objectTracker.Start();

        foreach (TrackableBehaviour trackableBehaviour in GetTargets())
        {
            print(trackableBehaviour.TrackableName);
        }
    }


    private void DeactivateGloveTargetTracking()
    {
        //get the object tracker instance
        ObjectTracker objectTracker = TrackerManager.Instance.GetTracker<ObjectTracker>();
        if (objectTracker == null)
        {
            return;
        }

        //get all available datasets and momentarily stop tracking
        List<DataSet> dataSets = objectTracker.GetActiveDataSets().ToList();
        objectTracker.Stop();

        //find the mainGloveTarget, and deactivate it
        foreach (DataSet dataSet in dataSets)
        {
            if (dataSet.Path.IndexOf(mainGloveTarget) != -1)
            {
                objectTracker.DeactivateDataSet(dataSet);
            }
        }


        objectTracker.Start();
    }

    private List<TrackableBehaviour> GetTargets()
    {
        List<TrackableBehaviour> allTrackables = new List<TrackableBehaviour>();
        allTrackables = TrackerManager.Instance.GetStateManager().GetTrackableBehaviours().ToList();
        return allTrackables;
    }

    private void SetupTargets(List<TrackableBehaviour> allTargets)
    {
        foreach (TrackableBehaviour target in allTargets)
        {
            //Parent
            //target.gameObject.transform.parent = transform;

            //Rename
            target.gameObject.name = target.TrackableName;
            if (trackableObjectNames.IndexOf(target.TrackableName) == -1)
            {
                target.enabled = false;
                target.gameObject.SetActive(false);
            }

            //Add functionality
            // target.gameObject.AddComponent<PlaneCreator>();

        }


    }
}
