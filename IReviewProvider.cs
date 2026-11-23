using System;

/// <summary>
/// Platform-specific implementation of the application review request.
/// The provider does not manage its own lifetime — that is done by <see cref="ControllerReview"/>.
/// </summary>
public interface IReviewProvider
{
    /// <summary>The provider is ready to work (subscriptions are set up).</summary>
    bool IsInitialized { get; }

    /// <summary>The platform is ready to show the review dialog right now.</summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Prepares the provider for work. Must be idempotent.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Shows the platform review dialog.
    /// <paramref name="onComplete"/> is called once with the result:
    /// true — the request was sent to the platform (the player will most likely see the review dialog and may have left a review),
    /// false — the player declined, an error occurred, or the flow was cancelled.
    /// </summary>
    /// <returns>true if the request was actually sent to the platform.</returns>
    bool ShowReview(Action<bool> onComplete);

    /// <summary>
    /// Releases all subscriptions and resets the state. Must be idempotent.
    /// </summary>
    void Destroy();
}
