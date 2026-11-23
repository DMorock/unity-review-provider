#if PLUGIN_YG_2

using System;

using UnityEngine;

using YG;

/// <summary>
/// Review provider based on the PluginYourGames (YG2) plugin.
/// </summary>
public class ReviewProviderYG2 : IReviewProvider
{
    private Action<bool> _onComplete;
    private bool _isInitialized;

    public bool IsInitialized => _isInitialized;

    public bool IsAvailable => YG2.reviewCanShow;

    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        Debug.Log("ReviewProviderYG2.Initialize");

        // Unsubscribe first so that repeated initialization does not duplicate handlers.
        YG2.onGetSDKData -= UpdateData;
        YG2.onReviewSent -= ReviewSent;

        YG2.onGetSDKData += UpdateData;
        YG2.onReviewSent += ReviewSent;

        if (YG2.isSDKEnabled)
        {
            UpdateData();
        }

        _isInitialized = true;
    }

    public bool ShowReview(Action<bool> onComplete)
    {
        if (!_isInitialized)
        {
            Debug.LogError("ReviewProviderYG2.ShowReview: provider is not initialized!");
            return false;
        }

        if (!YG2.reviewCanShow)
        {
            Debug.LogWarning("ReviewProviderYG2.ShowReview: YG2.reviewCanShow is false");
            return false;
        }

        if (_onComplete != null)
        {
            Debug.LogWarning("ReviewProviderYG2.ShowReview: previous review request is still pending");
            return false;
        }

        Debug.Log("ReviewProviderYG2.ShowReview");

        // The callback is set BEFORE calling the platform: in the editor YG2 responds synchronously.
        _onComplete = onComplete;
        YG2.reviewCanShow = false;

        try
        {
            YG2.ReviewShow();
        }
        catch (Exception e)
        {
            Debug.LogError($"ReviewProviderYG2.ShowReview: YG2.ReviewShow threw: {e}");
            var cb = _onComplete;
            _onComplete = null;
            cb?.Invoke(false);
            return false;
        }

        return true;
    }

    public void Destroy()
    {
        if (!_isInitialized)
        {
            return;
        }

        YG2.onGetSDKData -= UpdateData;
        YG2.onReviewSent -= ReviewSent;

        _isInitialized = false;
        _onComplete = null;

        Debug.Log("ReviewProviderYG2.Destroy");
    }

    private void UpdateData()
    {
#if UNITY_EDITOR
        YG2.reviewCanShow = true;
#endif

        Debug.Log("ReviewProviderYG2.UpdateData: can show review - " + YG2.reviewCanShow.ToString());
    }

    private void ReviewSent(bool sent)
    {
        Action<bool> callback = _onComplete;
        _onComplete = null;

        if (sent)
        {
            Debug.Log("ReviewProviderYG2.ReviewSent: success");
        }
        else
        {
            Debug.Log("ReviewProviderYG2.ReviewSent: failed");
        }

        callback?.Invoke(sent);
    }
}
#endif
