using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CardManager2D : ICardManager
{
    [SerializeField] private Image cardImage;
    [SerializeField] private Image cardBackImage;

    public override void Init(Material cardMaterial, AnimationCurve xDeform, AnimationCurve yDeform)
    {
        Position = transform.position;
        Rotation = transform.rotation;
    }

    public override void CreateShadow(Transform shadowLight, Transform shadowPlane)
    {
        
    }   

    public override void MoveCard(float amplitudeX, Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;

        Position = transform.position;
        Rotation = transform.rotation;
    }

    public override void MoveCard(float amplitudeX, float amplitudeY, Vector3 position, Quaternion rotation)
    {
        CurrentAmplitudeX = amplitudeX;
        CurrentAmplitudeY = amplitudeY;
        MoveCard(position, rotation);
    }

    private void MoveCard(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;

        Position = transform.position;
        Rotation = transform.rotation;
        UpdateCard();
    }

    public override void ForceShowCard()
    {
        cardBackImage.gameObject.SetActive(false);
    }

    public override void Open(float openValue)
    {
        
    }

    public override IEnumerator Open(float openTime, float openValue, AnimationCurve openCloseCurve)
    {
        yield return null;
    }

    public override void OpenCard(float openValue)
    {
        
    }

    public override IEnumerator OpenClose(float openTime, float openValue, bool moveForward, AnimationCurve openCloseCurve)
    {
        yield return null;
    }

    public override void RotateLocal(float angle, Vector3 localAxis)
    {
        
    }

    public override void SetSprite(Sprite sprite, string key)
    {
        Key = key;
        cardImage.sprite = sprite;
    }

    public override void SetRect(Rect rect, Rect backRect, string key)
    {
        Key = key;
    }

    public override void UpdateCard()
    {
        
    }
}
