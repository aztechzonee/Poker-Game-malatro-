using System.Collections;
using UnityEngine;

public abstract class ICardManager : MonoBehaviour
{
    public string Key { get; protected set; }

    public Vector3 Position { get; protected set; }
    public Quaternion Rotation { get; protected set; }

    public float CurrentAmplitudeX { get; protected set; }
    public float CurrentAmplitudeY { get; protected set; }

    public abstract void Init(Material cardMaterial, AnimationCurve xDeform, AnimationCurve yDeform);

    public abstract void CreateShadow(Transform shadowLight, Transform shadowPlane);

    public abstract void SetRect(Rect rect, Rect backRect, string key);

    public abstract void SetSprite(Sprite sprite, string key);

    public abstract void UpdateCard();

    public abstract void RotateLocal(float angle, Vector3 localAxis);

    public abstract void MoveCard(float amplitudeX, Vector3 position, Quaternion rotation);

    public abstract void MoveCard(float amplitudeX, float amplitudeY, Vector3 position, Quaternion rotation);

    public abstract void OpenCard(float openValue);

    public abstract void ForceShowCard();

    public abstract IEnumerator OpenClose(float openTime, float openValue, bool moveForward, AnimationCurve openCloseCurve);

    public abstract void Open(float openValue);

    public abstract IEnumerator Open(float openTime, float openValue, AnimationCurve openCloseCurve);
}
