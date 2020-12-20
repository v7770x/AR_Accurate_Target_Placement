using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;
using UnityEngine.Events;
using UnityEngine.Windows.Speech;
using System;
using System.Linq;

public class SpeechHandler : MonoBehaviour
{
    public GameObject MenuDisplay;
    public GameObject HomeTarget;
    public GameObject GloveTarget;
    public GameObject ConfirmTargetFoundText;

    //variables for voice recognition
    private KeywordRecognizer keywordRecognizer;
    private Dictionary<string, Action> actions = new Dictionary<string, Action>();

    //the event handler instance for the imagetarget event handler
    private EventHandler homeEventHandler;
    private EventHandler gloveTargetEventHandler;


    //to manage the different stages of the application
    public bool showGuideViewOptions = true;
    public int stage = 0;
    private int numTargetsFound = 0;

    //action to push to when new guide view is recognized
    public UnityAction<string> OnGuideViewSelected;
    public UnityAction OnGloveTargetConfirmed;

    private void KeywordSelected()
    {

    }

    private void RecognizedSpeech(PhraseRecognizedEventArgs speech)
    {
        Debug.Log(speech.text);
        if (stage == 0 && (speech.text == "one" || speech.text == "two" || speech.text == "three"))
        {
            OnGuideViewSelected.Invoke(speech.text);
            //keywordRecognizer.Stop();
            showGuideViewOptions = false;
            stage += 1;
        }
        else if (stage == 1 && (speech.text == "yes"))
        {
            OnGloveTargetConfirmed.Invoke();
            //keywordRecognizer.Stop();
            numTargetsFound += 1;
            if (numTargetsFound < 3)
            {
                stage = 0;
            }
            else
            {
                stage = 2;
            }
        }


    }

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

    private void Awake()
    {

        SetupVoiceCommands();

        //get the event handlers for the targets
        homeEventHandler = HomeTarget.GetComponent<EventHandler>();
        homeEventHandler.OnTrackingFound += HomeTargetFound;

        gloveTargetEventHandler = GloveTarget.GetComponent<EventHandler>();
        gloveTargetEventHandler.OnTrackingFound += GloveTargetFound;

    }

    private void OnDestroy()
    {
        homeEventHandler.OnTrackingFound -= HomeTargetFound;
        gloveTargetEventHandler.OnTrackingFound -= GloveTargetFound;
    }

    private void SetupVoiceCommands()
    {
        //words for selecting target
        actions.Add("one", KeywordSelected);
        actions.Add("two", KeywordSelected);
        actions.Add("three", KeywordSelected);

        //words for confirming target found
        actions.Add("yes", KeywordSelected);
        actions.Add("no", KeywordSelected);

        keywordRecognizer = new KeywordRecognizer(actions.Keys.ToArray());
        keywordRecognizer.OnPhraseRecognized += RecognizedSpeech;

        keywordRecognizer.Start();

        print("voice set up");
    }



    private void HomeTargetFound()
    {
        if (stage == 0)
        {
            MenuDisplay.SetActive(true);
            //keywordRecognizer.Start();
        }
    }

    private void GloveTargetFound()
    {
        if (stage == 1)
        {
            //make the user confirm that the target found is in the correct position
            ConfirmTargetFoundText.SetActive(true);
            //keywordRecognizer.Start();
        }
    }
}
