# Unity In-App Review
Just on more game review request service for Unity.

A small, platform-agnostic system for asking players to rate your game in Unity.
It wraps the native review dialogs of different stores behind a single static
service and a simple provider interface, so game code never has to deal with
platform-specific APIs.

## Features

- **Single entry point.** UI screens just call `ControllerReview.RequestReview()`
  and close — the service owns the provider, handles the platform response and
  reports the result back to the caller.
- **Provider abstraction.** Each store is implemented as an `IReviewProvider`.
  A dummy provider is included for testing on platforms without native reviews.
- **Timeout handling.** Every request is guarded by a configurable timeout, so
  the caller always gets a result even if the platform never answers.
- **Stale-response protection.** A request-generation counter makes sure that
  late responses from a timed-out or shut-down request are safely ignored.
- **Idempotent lifecycle.** `Initialize()` and `Destroy()` are safe to call
  multiple times, on every provider.
- **Zero dependencies.** No external packages required; store integrations are
  enabled via Unity scripting defines.

## Included providers

| Provider | Target | Scripting define |
|---|---|---|
| `ReviewProviderGooglePlay` | Google Play In-App Review API (Google Play Billing plugin) | `GOOGLE_PLAY` |
| `ReviewProviderYG2` | [PluginYourGames (YG2)](https://github.com/YourGamesInc/UnityGamesSDK) for web games | `PLUGIN_YG_2` |
| `ReviewProviderDummy` | Editor / unsupported platforms | fallback (no define needed) |

`ControllerReview.CreateProvider()` picks the provider at compile time using
these defines. On any other configuration the dummy provider is used, which
simulates an asynchronous platform response with a configurable delay — handy
for testing the pending state, the timeout and screen closing.

## Getting started

1. Copy the `.cs` files into your Unity project.
2. Add the scripting define matching your target store (see the table above)
   in *Project Settings → Player → Scripting Define Symbols*.
3. Initialize the service once at startup:

```csharp
ControllerReview.Initialize();
```

4. Show the review dialog when it fits your UX (e.g. after a few levels):

```csharp
if (ControllerReview.IsAvailable)
{
    ControllerReview.RequestReview(
        onCompleted: success =>
        {
            // true — the flow finished without errors,
            // false — refused / error / timeout.
        });
}
```

5. Shut the service down when the application exits:

```csharp
ControllerReview.Shutdown();
```

## API overview

### ControllerReview (static service)

| Member | Description |
|---|---|
| `Initialize()` | Creates and initializes the provider for the current platform. Call once at startup. |
| `RequestReview(Action<bool> onCompleted, float timeoutSeconds = DefaultTimeoutSeconds)` | Shows the platform review dialog. Returns `true` if the request was actually sent to the platform; the `onCompleted` callback fires exactly once with the result. |
| `Shutdown()` | Releases the provider and its subscriptions. Call on application exit. |
| `IsInitialized` | The service is created and the provider is ready. |
| `IsAvailable` | The platform can show the review dialog right now. |
| `IsRequesting` | A request is currently awaiting the platform response. |
| `DefaultTimeoutSeconds` | Default platform response timeout (8 s). |

### IReviewProvider

Implement this interface to add support for another store:

| Member | Description |
|---|---|
| `IsInitialized` | The provider is ready (subscriptions are set up). |
| `IsAvailable` | The platform can show the review dialog right now. |
| `Initialize()` | Prepares the provider. Must be idempotent. |
| `ShowReview(Action<bool> onComplete)` | Shows the dialog. Returns `true` if the request was sent to the platform. `onComplete` is invoked once: `true` — the user left a rating, `false` — refused / error / cancellation. |
| `Destroy()` | Removes all subscriptions and resets the state. Must be idempotent. |

The provider never manages its own lifecycle — `ControllerReview` does that.

## Platform caveats

**Google Play** does not report whether the user actually left a rating, or
even whether the dialog was shown. In the Google Play provider,
`onComplete(true)` only means *the flow completed without API errors*, and
`onComplete(false)` means an API or initialization error. Keep this in mind
when interpreting the result.

The default 8-second timeout is tuned for this limitation: if the platform
stays silent (a realistic scenario with Google Play), the request is treated
as failed after the timeout and later responses are ignored. You can pass a
larger `timeoutSeconds` value if a longer wait suits your UX better.

## Requirements

- Unity 2021.3 or newer (uses `async/await` and C# 9-free, plain C#).
- For the Google Play provider: the [Google Play Billing / In-App Review](https://developer.android.com/guide/playcore/in-app-review/unity)
  plugin.
- For the YG2 provider: PluginYourGames SDK v2.

## License

MIT — see [LICENSE](LICENSE).
