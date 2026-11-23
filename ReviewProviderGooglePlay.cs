#if GOOGLE_PLAY

using System;

using Google.Play.Review;
using UnityEngine;

/// <summary>
/// Review provider based on the Google Play In-App Review API.
/// IMPORTANT: Google Play does not report whether the user left a review or whether the
/// dialog was shown at all. onComplete(true) means "the flow completed without errors",
/// onComplete(false) means an API/initialization error.
/// </summary>
public class ReviewProviderGooglePlay : IReviewProvider
{
    private ReviewManager _reviewManager;
    private PlayReviewInfo _playReviewInfo;
    private Action<bool> _onComplete;
    private bool _isInitialized;
    private bool _isRequesting;

    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// Google Play provides no way to know in advance whether it will show the dialog.
    /// So availability = "the provider is initialized and we are on an Android device".
    /// </summary>
    public bool IsAvailable
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return _isInitialized;
#else
            return false;
#endif
        }
    }

    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        _reviewManager = new ReviewManager();
        _isInitialized = true;
        Debug.Log("ReviewProviderGooglePlay.Initialize: complete");
#else
        Debug.LogWarning("ReviewProviderGooglePlay.Initialize: not supported on this platform");
#endif
    }

public bool ShowReview(Action<bool> onComplete)
{
    if (!_isInitialized)
    {
        Debug.LogError("ReviewProviderGooglePlay.ShowReview: provider is not initialized!");
        return false;
    }

    if (_isRequesting)
    {
        Debug.LogWarning("ReviewProviderGooglePlay.ShowReview: previous request is still pending");
        return false;
    }

    Debug.Log("ReviewProviderGooglePlay.ShowReview: starting request flow");

    _onComplete = onComplete;
    _isRequesting = true;

    try
    {
        var requestOperation = _reviewManager.RequestReviewFlow();
        requestOperation.Completed += OnRequestReviewFlowCompleted;
    }
    catch (Exception e)
    {
        Debug.LogError($"ReviewProviderGooglePlay.ShowReview: RequestReviewFlow threw: {e}");
        _isRequesting = false;
        return false;
    }

    return true;
}

    private void OnRequestReviewFlowCompleted(AsyncOperation<PlayReviewInfo> operation)
    {
        if (!_isInitialized)
        {
            return;
        }

        if (operation.Error != ReviewErrorCode.NoError)
        {
            Debug.LogError($"ReviewProviderGooglePlay: RequestReviewFlow failed: {operation.Error}");
            Complete(false);
            return;
        }

        _playReviewInfo = operation.GetResult();

        operation.Completed -= OnRequestReviewFlowCompleted;

        // PlayReviewInfo has a limited lifetime, so LaunchReviewFlow
        // must be called immediately, without waiting for the next frame/screen.
        try
        {
            var launchOperation = _reviewManager.LaunchReviewFlow(_playReviewInfo);
            launchOperation.Completed += OnLaunchReviewFlowCompleted;
        }
        catch (Exception e)
        {
            Debug.LogError($"ReviewProviderGooglePlay: LaunchReviewFlow threw: {e}");
            Complete(false);
        }
    }

    private void OnLaunchReviewFlowCompleted(AsyncOperation<ReviewErrorCode> operation)
    {
        if (!_isInitialized)
        {
            return;
        }

        _playReviewInfo = null;

        operation.Completed -= OnLaunchReviewFlowCompleted;

        if (operation.Error != ReviewErrorCode.NoError)
        {
            Debug.LogError($"ReviewProviderGooglePlay: LaunchReviewFlow failed: {operation.Error}");
            Complete(false);
            return;
        }

        // The platform does not report the result. Treat the flow as successful if there were no errors.
        Debug.Log("ReviewProviderGooglePlay: review flow completed (platform result unknown)");
        Complete(true);
    }

    public void Destroy()
    {
        if (!_isInitialized)
        {
            return;
        }

        _isInitialized = false;
        _isRequesting = false;
        _onComplete = null;
        _playReviewInfo = null;
        _reviewManager = null;

        Debug.Log("ReviewProviderGooglePlay.Destroy");
    }

    private void Complete(bool success)
    {
        _isRequesting = false;

        var callback = _onComplete;
        _onComplete = null;
        callback?.Invoke(success);
    }
}

#endif
