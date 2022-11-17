using DilmerGames.Core.Singletons;
using Google.XR.ARCoreExtensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;

public class UnityEventResolver : UnityEvent<Transform>{}

public class ARCloudAnchorManager : Singleton<ARCloudAnchorManager>
{

    [SerializeField]
    private Camera arCamera = null;

    [SerializeField]
    private float resolveAnchorPassedTimeout = 10.0f;

private ARAnchorManager arAnchorManager = null;

    private ARAnchor pendingHostAnchor = null;

    private ARCloudAnchor cloudAnchor = null;

    private string anchorIdToResolve;

    private bool anchorHostInProgress = false;

    private bool anchorResolveInProgress = false;

    private float safeToResolveTimePassed = 0;


    // private AnchorCreatedEvent cloudAnchorCreatedEvent = null;

    // private void Awake()
    // {
    //    cloudAnchorCreatedEvent = new AnchorCreatedEvent();
    //    cloudAnchorCreatedEvent.AddListener((t) => ARPlacementManager.Instance.ReCreatePlacement(t));
    // }

    private Pose GetCameraPose()
    {
        return new Pose(arCamera.transform.position, arCamera.transform.rotation);
    }

    #region Anchor Cycle

    public void QueueAnchor(ARAnchor arAnchor) {
        pendingHostAnchor = arAnchor;
    }

    public void HostAnchor() {
        Debug.Log("Hosting Anchor in progress");

    // Recommended upto 30 seconds of scanning before calling host anchor
        FeatureMapQuality quality = arAnchorManager.EstimateFeatureMapQualityForHosting(GetCameraPose());

        Debug.Log($"Feature Map Quality: {quality}");

        cloudAnchor = arAnchorManager.HostCloudAnchor(pendingHostAnchor, 1);

        if(cloudAnchor == null) {
            Debug.LogError("Failed to host anchor");
        } else  {
            anchorHostInProgress = true;
        }
    }

    public void Resolve() {
        Debug.Log("Resolving Anchor in progress");

        cloudAnchor = arAnchorManager.ResolveCloudAnchorId(anchorIdToResolve);

        if(cloudAnchor == null) {
            Debug.LogError("Failed to resolve anchor");
        } else {
            anchorResolveInProgress = true;
        }
    }

    private void CheckHostingProgress() {
        CloudAnchorState cloudAnchorState = cloudAnchor.cloudAnchorState;

        if(cloudAnchorState == CloudAnchorState.Success) {
            Debug.Log("Anchor Hosted Successfully");
            anchorHostInProgress = false;
            anchorIdToResolve = cloudAnchor.cloudAnchorId;
        } 
        else if(cloudAnchorState != CloudAnchorState.TaskInProgress) {
            Debug.LogError($"Anchor Hosted Failed with error: {cloudAnchorState}");
            anchorHostInProgress = false;
        }
    }

    private void CheckResolveProgress() {
        CloudAnchorState cloudAnchorState = cloudAnchor.cloudAnchorState;

        if(cloudAnchorState == CloudAnchorState.Success) {
            Debug.Log("Anchor Resolved Successfully");
            anchorResolveInProgress = false;
            ARPlacementManager.Instance.ReCreatePlacement(cloudAnchor.transform);
        } 
        else if(cloudAnchorState != CloudAnchorState.TaskInProgress) {
            Debug.LogError($"Anchor Resolve Failed with error: {cloudAnchorState}");
            anchorResolveInProgress = false;
        }
    }

    #endregion

    // Update is called once per frame
    void Update()
    {
        // Checking for hosting
        if(anchorHostInProgress) {
            CheckHostingProgress();
            return;
        }

        // Checking for resolving
        if(anchorResolveInProgress && safeToResolveTimePassed <= 0) {
           
           safeToResolveTimePassed = resolveAnchorPassedTimeout;

           if(!string.IsNullOrEmpty(anchorIdToResolve)) {
               Debug.Log($"Resolving Anchor: {anchorIdToResolve}");
               CheckResolveProgress();
           } else {
               Debug.LogError("Anchor Id to resolve is null or empty");
           }
        } else {
            safeToResolveTimePassed -= Time.deltaTime * 1.0f;
        }
        }   
    }
