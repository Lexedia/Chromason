using System.Collections.Generic;
using UnityEngine;

namespace Chromason.Environment;

internal readonly struct TransformState
{
    internal readonly Vector3 LocalPosition;
    internal readonly Quaternion LocalRotation;
    internal readonly Vector3 LocalScale;

    private TransformState(Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
    {
        LocalPosition = localPosition;
        LocalRotation = localRotation;
        LocalScale = localScale;
    }

    internal static TransformState Capture(Transform transform) =>
        new(transform.localPosition, transform.localRotation, transform.localScale);
}

internal readonly struct ObjectState
{
    internal readonly bool ActiveSelf;
    internal readonly TransformState Transform;

    internal ObjectState(GameObject gameObject)
    {
        ActiveSelf = gameObject.activeSelf;
        Transform = TransformState.Capture(gameObject.transform);
    }
}

internal readonly struct FogState
{
    private static readonly int AttenuationId = Shader.PropertyToID("_CustomFogAttenuation");
    private static readonly int OffsetId = Shader.PropertyToID("_CustomFogOffset");
    private static readonly int HeightFogStartYId = Shader.PropertyToID("_CustomFogHeightFogStartY");
    private static readonly int HeightFogHeightId = Shader.PropertyToID("_CustomFogHeightFogHeight");

    private readonly float _attenuation;
    private readonly float _offset;
    private readonly float _heightFogStartY;
    private readonly float _heightFogHeight;

    internal FogState(BloomFogEnvironmentParams fogParams)
        : this(fogParams.attenuation, fogParams.offset, fogParams.heightFogStartY, fogParams.heightFogHeight)
    {
    }

    private FogState(float attenuation, float offset, float heightFogStartY, float heightFogHeight)
    {
        _attenuation = attenuation;
        _offset = offset;
        _heightFogStartY = heightFogStartY;
        _heightFogHeight = heightFogHeight;
    }

    internal static FogState CaptureGlobal() =>
        new(
            Shader.GetGlobalFloat(AttenuationId),
            Shader.GetGlobalFloat(OffsetId),
            Shader.GetGlobalFloat(HeightFogStartYId),
            Shader.GetGlobalFloat(HeightFogHeightId));

    internal void Apply()
    {
        Shader.SetGlobalFloat(AttenuationId, _attenuation);
        Shader.SetGlobalFloat(OffsetId, _offset);
        Shader.SetGlobalFloat(HeightFogStartYId, _heightFogStartY);
        Shader.SetGlobalFloat(HeightFogHeightId, _heightFogHeight);
    }
}

internal class EnvironmentSnapshot
{
    internal Dictionary<GameObject, ObjectState> Objects { get; } = [];

    internal FogState? Fog { get; private set; }

    internal static EnvironmentSnapshot Take()
    {
        var snapshot = new EnvironmentSnapshot();

        foreach (var gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (IsEnvironmentObject(gameObject))
            {
                snapshot.Add(gameObject.transform);
            }
        }

        foreach (var fogEnvironment in Resources.FindObjectsOfTypeAll<BloomFogEnvironment>())
        {
            if (fogEnvironment.isActiveAndEnabled && fogEnvironment._fogParams != null)
            {
                snapshot.Fog = new FogState(fogEnvironment._fogParams);
                break;
            }
        }

        return snapshot;
    }

    private void Add(Transform transform)
    {
        if (Objects.ContainsKey(transform.gameObject))
        {
            return;
        }

        Objects[transform.gameObject] = new ObjectState(transform.gameObject);

        foreach (Transform child in transform)
        {
            Add(child);
        }
    }

    private static bool IsEnvironmentObject(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return false;
        }

        var sceneName = gameObject.scene.name;
        if (sceneName == null)
        {
            return false;
        }

        return (sceneName.Contains("Environment") && !sceneName.Contains("Menu")) ||
               gameObject.GetComponent<TrackLaneRing>() != null;
    }
}
