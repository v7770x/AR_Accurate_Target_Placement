using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;
using UnityEngine.Events;

public class EventHandler : MonoBehaviour
{
    public UnityAction OnTrackingFound;
    public UnityAction OnTrackingLost;

    public UnityAction<int> OnImageTargetFound;
    public UnityAction<int> OnImageTargetLost;
    public int TARGET_NUMBER;

    private TrackableBehaviour mTrackableBehaviour = null;

    private readonly List<TrackableBehaviour.Status> mTrackingFound = new List<TrackableBehaviour.Status>()
    {
        TrackableBehaviour.Status.DETECTED,
        TrackableBehaviour.Status.TRACKED,
        TrackableBehaviour.Status.EXTENDED_TRACKED
    };

    private readonly List<TrackableBehaviour.Status> mTrackingLost = new List<TrackableBehaviour.Status>()
    {
        TrackableBehaviour.Status.TRACKED,
        TrackableBehaviour.Status.NO_POSE
    };

    public void OnTrackableStateChanged(TrackableBehaviour.StatusChangeResult statusChange)
    {
        foreach (TrackableBehaviour.Status trackedStatus in mTrackingFound)
        {
            if (statusChange.NewStatus == trackedStatus)
            {
                print("Tracking Found");

                if (statusChange.NewStatus != null)
                {
                    /*keywordRecognizer.Start();
                    MenuDisplay.SetActive(true);*/

                    //OnTrackingFound.Invoke();
                    OnImageTargetFound.Invoke(TARGET_NUMBER);
                }
                return;
            }
        }

        /*foreach (TrackableBehaviour.Status trackedStatus in mTrackingLost)
        {
            if (statusChange.NewStatus == trackedStatus)
            {
                print("Tracking Lost");
                if (statusChange.NewStatus != null)
                {
                    //OnTrackingLost.Invoke();
                    OnImageTargetLost.Invoke(TARGET_NUMBER);
                }
                return;
            }
        }*/
    }

    private void Awake()
    {
        mTrackableBehaviour = GetComponent<TrackableBehaviour>();
        mTrackableBehaviour.RegisterOnTrackableStatusChanged(OnTrackableStateChanged);
    }



    private void OnDestroy()
    {

    }
}
