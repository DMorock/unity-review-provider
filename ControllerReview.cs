using System;
using System.Threading.Tasks;

using UnityEngine;

/// <summary>
/// Application review request service.
/// </summary>
public static class ControllerReview
{
    /// <summary>How long to wait for the platform response before considering the request finished.</summary>
    public const float DefaultTimeoutSeconds = 60f;

    /// <summary>The service has been created and the provider is ready to work.</summary>
    public static bool IsInitialized => _provider != null && _provider.IsInitialized;

    /// <summary>The platform is ready to show the review dialog right now.</summary>
    public static bool IsAvailable => IsInitialized && _provider.IsAvailable;

    /// <summary>A platform response is pending.</summary>
    public static bool IsRequesting => _isRequesting;


    private static IReviewProvider _provider;
    private static bool _isRequesting;
    private static int _requestGeneration;
    private static Action<bool> _onReviewCompleted;

    /// <summary>
    /// Creates the provider for the current platform. Called once during application initialization.
    /// </summary>
    public static void Initialize()
    {
        if (IsInitialized)
        {
            return;
        }

        Debug.Log("ControllerReview.Initialize: start initialization");

        _provider = CreateProvider();
        if (_provider == null)
        {
            Debug.LogError("ControllerReview.Initialize: review provider can't be created!");
            return;
        }

        _provider.Initialize();
        if (!_provider.IsInitialized)
        {
            Debug.LogError("ControllerReview.Initialize: review provider can't be initialized!");
            _provider.Destroy();
            _provider = null;
            return;
        }

        _onReviewCompleted = null;

        Debug.Log("ControllerReview.Initialize: complete");
    }

    /// <summary>
    /// Requests the platform review dialog.
    /// </summary>
    /// <returns>true if the request was actually sent to the platform.</returns>
    public static bool RequestReview(Action<bool> onCompleted = null, float timeoutSeconds = DefaultTimeoutSeconds)
    {
        if (!IsInitialized)
        {
            Debug.LogError("ControllerReview.RequestReview: service is not initialized!");
            return false;
        }

        if (_isRequesting)
        {
            Debug.LogWarning("ControllerReview.RequestReview: previous request is still pending");
            return false;
        }

        if (!_provider.IsAvailable)
        {
            Debug.Log("ControllerReview.RequestReview: review is not available");
            return false;
        }

        Debug.Log("ControllerReview.RequestReview: showing review dialog");

        _isRequesting = true;
        int generation = ++_requestGeneration;

        _onReviewCompleted = onCompleted;

        bool started = false;
        try
        {
            started = _provider.ShowReview(success => OnReviewCompleted(generation, success));
        }
        catch (Exception e)
        {
            Debug.LogError($"ControllerReview.RequestReview: provider threw: {e}");
            OnReviewCompleted(generation, false);
            return false;
        }

        if (!started)
        {
            Debug.LogWarning($"ControllerReview.RequestReview: provider could not start");
            OnReviewCompleted(generation, false);
            return false;
        }

        StartTimeout(generation, timeoutSeconds);
        return true;
    }

    /// <summary>
    /// Releases the provider subscriptions and resets its state. Called when the application shuts down.
    /// </summary>
    public static void Shutdown()
    {
        _isRequesting = false;

        if (_provider == null)
        {
            return;
        }

        _provider.Destroy();
        _provider = null;

        _onReviewCompleted = null;

        _requestGeneration++;

        Debug.Log("ControllerReview.Shutdown: complete");
    }

    private static IReviewProvider CreateProvider()
    {
#if PLUGIN_YG_2
        return new ReviewProviderYG2();
#elif GOOGLE_PLAY
    return new ReviewProviderGooglePlay();
#else
    return new ReviewProviderDummy();
#endif
    }

    private static void OnReviewCompleted(int generation, bool success)
    {
        if (generation != _requestGeneration || !_isRequesting)
        {
            Debug.Log("ControllerReview: ignoring stale review response");
            return;
        }

        _isRequesting = false;
        _requestGeneration++;

        Debug.Log("ControllerReview: review response " + success);

        var callback = _onReviewCompleted;
        _onReviewCompleted = null;
        callback?.Invoke(success);
    }

    private static async void StartTimeout(int generation, float timeoutSeconds)
    {
        if (!(timeoutSeconds > 0f) || float.IsInfinity(timeoutSeconds)
            || timeoutSeconds > 3600f)
        {
            Debug.LogError($"ControllerReview.StartTimeout: wrong time out");
            timeoutSeconds = DefaultTimeoutSeconds;
        }

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));
            Debug.LogWarning("ControllerReview: review request timed out");
        }
        catch (Exception e)
        {
            Debug.LogError($"ControllerReview.StartTimeout: {e}");
        }

        OnReviewCompleted(generation, false);
    }
}
