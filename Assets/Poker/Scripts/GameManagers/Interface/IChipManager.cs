using System.Collections;
using UnityEngine;

public abstract class IChipManager : MonoBehaviour
{
    public abstract int ID { get; }

    public abstract void Init(int id, Transform chipsSkin, Transform shadowLight, Transform shadowPlane);

    public abstract void MoveToPivot(Vector3 endPosition);

    public abstract IEnumerator MoveToPivot(float moveTime, Vector3 endPosition, AnimationCurve chipsMoveCurve, AnimationCurve chipsDeltaYCurve);
}
