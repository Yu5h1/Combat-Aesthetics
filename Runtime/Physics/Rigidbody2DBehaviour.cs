using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yu5h1Lib;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class Rigidbody2DBehaviour : BaseMonoBehaviour
{
    [SerializeField]
    private Rigidbody2D _rigidbody;
#pragma warning disable 0109
    public new Rigidbody2D rigidbody => _rigidbody;
#pragma warning restore 0109
    public virtual Vector2 velocity { get => rigidbody.linearVelocity; protected set => rigidbody.linearVelocity = value; }
    public Vector2 up => (Vector2)transform.up;
    public Vector2 down => -up;
    public Vector2 right => transform.right;
    public Vector2 left => -right;

    protected override void OnInitializing()
    {        
        this.GetComponent(ref _rigidbody);
    }
    protected virtual void Reset() => Init(true); 
}
