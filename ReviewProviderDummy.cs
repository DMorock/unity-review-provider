using System;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

/// <summary>
/// Dummy review provider for platforms without native review support.
/// Simulates an asynchronous platform response with a configurable delay — handy for
/// testing the pending state, the timeout and screen closing.
/// </summary>
public class ReviewProviderDummy : IReviewProvider
{
    private readonly int _delayMilliseconds;
    private readonly bool _simulateSuccess;

    private CancellationTokenSource _cancellation;
    private bool _isInitialized;

    /// <param name="delayMilliseconds">Delay before the simulated platform "response".</param>
    /// <param name="simulateSuccess">The result the dummy will return.</param>
    public ReviewProviderDummy(int delayMilliseconds = 3000, bool simulateSuccess = true)
    {
        _delayMilliseconds = delayMilliseconds;
        _simulateSuccess = simulateSuccess;
    }

    public bool IsInitialized => _isInitialized;

    public bool IsAvailable => _isInitialized;

    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        Debug.Log("ReviewProviderDummy.Initialize");
        _isInitialized = true;
    }

    public bool ShowReview(Action<bool> onComplete)
    {
        if (!_isInitialized)
        {
            Debug.LogError("ReviewProviderDummy.ShowReview: provider is not initialized!");
            return false;
        }

        Debug.Log("ReviewProviderDummy.ShowReview: simulating platform response in "
                  + _delayMilliseconds + " ms");

        CancelPendingRequest();
        _cancellation = new CancellationTokenSource();
        _ = SimulateReviewAsync(onComplete, _cancellation.Token);

        return true;
    }

    public void Destroy()
    {
        if (!_isInitialized)
        {
            return;
        }

        CancelPendingRequest();

        _isInitialized = false;
        Debug.Log("ReviewProviderDummy.Destroy");
    }

    private void CancelPendingRequest()
    {
        if (_cancellation == null)
        {
            return;
        }

        _cancellation.Cancel();
        _cancellation.Dispose();
        _cancellation = null;
    }

    private async Task SimulateReviewAsync(Action<bool> onComplete, CancellationToken token)
    {
        try
        {
            await Task.Delay(_delayMilliseconds, token);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("ReviewProviderDummy: review request was cancelled");
            return;
        }

        if (token.IsCancellationRequested)
        {
            return;
        }

        Debug.Log("ReviewProviderDummy: review " + (_simulateSuccess ? "success" : "failed"));
        onComplete?.Invoke(_simulateSuccess);
    }
}
