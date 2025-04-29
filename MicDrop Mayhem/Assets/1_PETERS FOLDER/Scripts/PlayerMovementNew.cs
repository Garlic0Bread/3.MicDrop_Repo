using System;
using UnityEngine;

public class PlayerMovementNew : MonoBehaviour
{

    [Header("References")]
    public PlayerMovementSTats movementSTats;
    [SerializeField] private Collider2D _feetColl;
    [SerializeField] private Collider2D _bodyColl;

    private Rigidbody2D _rb;

    //movement vars
    private Vector2 _moveVelocity;
    private bool _isFacingRight;


    //collision check vars

    private RaycastHit2D _groundHit;
    private RaycastHit2D _headHit;

    private bool _isGrounded;
    private bool _bumpedHead;

    private void Awake()
    {
        _isFacingRight = true;

        _rb = GetComponent<Rigidbody2D>();
    }


    #region Movement







    #endregion




}