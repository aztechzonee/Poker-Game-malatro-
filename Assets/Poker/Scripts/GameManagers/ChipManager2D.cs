using System.Collections;
using UnityEngine;

public class ChipManager2D : IChipManager
{
    private int id;
    public override int ID => id;

    public override void Init(int id, Transform chipsSkin, Transform shadowLight, Transform shadowPlane)
    {
        this.id = id;
        Instantiate(chipsSkin, transform.position, transform.rotation, transform);
    }

    public override void MoveToPivot(Vector3 endPosition)
    {
        transform.position = endPosition;
    }

    public override IEnumerator MoveToPivot(float moveTime, Vector3 endPosition, AnimationCurve chipsMoveCurve, AnimationCurve chipsDeltaYCurve)
    {
        Vector3 startPosition = transform.position;
        float time = 0f;
        WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();

        while (time < moveTime)
        {
            float t = time / moveTime;
            Vector3 deltaY = new Vector3(0f, 0.3f * chipsDeltaYCurve.Evaluate(t), 0f);
            transform.position = deltaY + Vector3.Lerp(startPosition, endPosition, chipsMoveCurve.Evaluate(t));
            time += Time.deltaTime;
            yield return waitForEndOfFrame;
        }
        transform.position = endPosition;
    }
}
