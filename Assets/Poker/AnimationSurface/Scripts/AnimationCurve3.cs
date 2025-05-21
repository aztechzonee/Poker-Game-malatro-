using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AnimationCurve3
{
    [System.Serializable]
    public class Keyframe3D
    {
        public Keyframe keyX;
        public Keyframe keyY;
        public Keyframe keyZ;

        public Keyframe3D(float time, Vector3 value)
        {
            this.keyX = new Keyframe(time, value.x);
            this.keyY = new Keyframe(time, value.y);
            this.keyZ = new Keyframe(time, value.z);
        }
    }

    [SerializeField] private AnimationCurve _curveX;
    [SerializeField] private AnimationCurve _curveY;
    [SerializeField] private AnimationCurve _curveZ;

    public float maxTime{ get; private set; }
    public List<Vector3> values{ get; private set; }

    public AnimationCurve curveX
    {
        get { return this._curveX; }
    }

    public AnimationCurve curveY
    {
        get { return this._curveY; }
    }

    public AnimationCurve curveZ
    {
        get { return this._curveZ; }
    }

    public AnimationCurve3()
    {
        _curveX = new AnimationCurve();
        _curveY = new AnimationCurve();
        _curveZ = new AnimationCurve();
        values = new List<Vector3>(0);
    }
    public void SetValues(Vector3[] values)
    {
        this.maxTime = 0.0f;
        this.values = new List<Vector3>(0);

        if (_curveX == null)
        {
            _curveX = new AnimationCurve();
        }
        else
        {
            _curveX.keys = null;
        }
        if (_curveY == null)
        {
            _curveY = new AnimationCurve();
        }
        else
        {
            _curveY.keys = null;
        }
        if (_curveZ == null)
        {
            _curveZ = new AnimationCurve();
        }
        else
        {
            _curveZ.keys = null;
        }
        float f = 0.0f;
        for (int i = 0; i < values.Length; i++)
        {
            if (i != 0)
            {
                float deltaTime = Vector3.Distance(values[i - 1], values[i]);
                this.maxTime += deltaTime;
                f += deltaTime;
            }
            _curveX.AddKey(f, values[i].x);
            _curveY.AddKey(f, values[i].y);
            _curveZ.AddKey(f, values[i].z);
            this.values.Add(values[i]);
        }
    }
    public AnimationCurve3(Vector3[] values)
    {
        SetValues(values);
    }

    public void DrawCurve(LineRenderer line)
    {
        line.positionCount = this.values.Count;
        foreach (Vector3 value in values)
        {
            line.SetPositions(this.values.ToArray());
        }
    }

    public void AddKey(float time, float valueX, float valueY, float valueZ)
    {
        if (maxTime < time)
        {
            maxTime = time;
        }
        if (this.values == null)
        {
            this.values = new List<Vector3>(0);
        }
        _curveX.AddKey(time, valueX);
        _curveY.AddKey(time, valueY);
        _curveZ.AddKey(time, valueZ);
        this.values.Add(new Vector3(valueX, valueY, valueZ));
    }

    public void AddKey(float time, Vector3 value)
    {
        if (maxTime < time)
        {
            maxTime = time;
        }
        if (this.values == null)
        {
            this.values = new List<Vector3>(0);
        }

        _curveX.AddKey(time, value.x);
        _curveY.AddKey(time, value.y);
        _curveZ.AddKey(time, value.z);
        this.values.Add(value);
    }

    public void RemoveKey(int index)
    {
        _curveX.RemoveKey(index);
        _curveY.RemoveKey(index);
        _curveZ.RemoveKey(index);
    }

    public void SmoothTangents(int index, float weight)
    {
        _curveX.SmoothTangents(index, weight);
        _curveY.SmoothTangents(index, weight);
        _curveZ.SmoothTangents(index, weight);
    }

    public void Smooth()
    {
        for (int i = 0; i < this.Length; i++)
            this.SmoothTangents(i, 0.0f);
    }

    private WrapMode _preWrapMode;
    private WrapMode _postWrapMode;

    public WrapMode preWrapMode
    {
        get { return this._preWrapMode; }
        set
        {
            this._preWrapMode = value;
            _curveX.preWrapMode = _preWrapMode;
            _curveY.preWrapMode = _preWrapMode;
            _curveZ.preWrapMode = _preWrapMode;
        }
    }

    public WrapMode postWrapMode
    {
        get { return this._postWrapMode; }
        set
        {
            this._postWrapMode = value;
            _curveX.postWrapMode = _postWrapMode;
            _curveY.postWrapMode = _postWrapMode;
            _curveZ.postWrapMode = _postWrapMode;
        }
    }
        
    public int Length
    {
        get { return this._curveX.length; }
    }
        
    public Vector3 Evaluate(float time)
    {
        return new Vector3(this._curveX.Evaluate(time), this._curveY.Evaluate(time), this._curveZ.Evaluate(time));
    }

    public Vector3 Tangent(float time)
    { 
        return Vector3.Normalize((1.0f / 0.0001f) * (Evaluate(time + 0.0001f) - Evaluate(time)));
    }

    public Vector3 Normal(float time)
    { 
        Vector3 tangent0 = Tangent(time);
        Vector3 tangent = Tangent(time + 0.01f);
        return Vector3.Normalize((1.0f / 0.01f) * (tangent - tangent0));
    }

    public Vector3 Binormal(float time)
    {
        return Vector3.Normalize(-Vector3.Cross(Tangent(time), Normal(time)));
    }
}

