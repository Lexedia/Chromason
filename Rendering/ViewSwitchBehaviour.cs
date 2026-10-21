// Parts adapted from ImBlindedByTheLights by Kinsi (MIT, see LICENCE), https://github.com/kinsi55/BeatSaber_ImBlindedByTheLights
// Based on LightSwitchHandler

using UnityEngine;

namespace Chromason.Rendering;

internal class ViewSwitchBehaviour : MonoBehaviour
{
    private Camera _camera = null!;

    private bool _loggedView;

    protected void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    protected void OnPreCull()
    {
        var view = _camera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Mono ? ViewKind.Desktop : ViewKind.Headset;

        if (!_loggedView)
        {
            _loggedView = true;
            Plugin.Log.Debug($"Camera {name}: {view} (eye {_camera.stereoActiveEye}, stereo {_camera.stereoEnabled}, target {(_camera.targetTexture != null ? _camera.targetTexture.name : "screen")})");
        }

        ViewReverterRegistry.Current?.ApplyFor(_camera, view);
    }

    protected void OnPostRender()
    {
        if (_camera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left)
        {
            return;
        }

        ViewReverterRegistry.Current?.RestoreFor(_camera);
    }
}
