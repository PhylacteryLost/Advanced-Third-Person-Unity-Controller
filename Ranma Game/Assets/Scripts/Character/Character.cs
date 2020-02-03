﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Character : MonoBehaviour
{
    protected Rigidbody rb;
    protected CharacterAnimManager animManager;

    [HideInInspector] public GroundedCheck groundedCheck;

    [Header("Movement")]
    [Space(10)]
    [SerializeField] [Range(0, 1)] protected float moveSpeed = .6f;

    [SerializeField] [Range(0, 9000)] private int shortJumpDownForce = 1000;
    [SerializeField] [Range(0, 900)] private float jumpForce = 90f;

    /// <summary>
    /// Request move and rotate to given Vector3.
    /// </summary>
    /// <param name="moveDir"></param>
    protected void MoveAndRotate(Vector3 moveDir)
    {
        if (!groundedCheck.IsGrounded || _knockbackRequest.doingRequest) return;
        var (rotationDir, speed) = GetMoveAndRotate(moveDir);
        _speed = speed;
        ApplyRotation(rotationDir);
    }

    /// <summary>
    /// Request maneuver start.
    /// </summary>
    protected void Maneuver()
    {
        _doingJump = true;
    }

    /// <summary>
    /// Request maneuver end.
    /// </summary>
    protected void ManeuverEnd()
    {
        _doingJump = false;
    }

    private bool _doingJump = false;

    /// <summary>
    /// Request standard attack, and consequential stop of input-driven movement.
    /// </summary>
    protected void AttackStd()
    {
        if (_knockbackRequest.doingRequest) return;
        _moving = false;
        animManager.DoAttack(AttkType.standard);
    }

    /// <summary>
    /// Request heavy attack, and consequential stop of input-driven movement.
    /// </summary>
    protected void AttackHeavy()
    {
        if (_knockbackRequest.doingRequest) return;
        _moving = false;
        animManager.DoAttack(AttkType.heavy);
    }

    /// <summary>
    /// Request interaction with a gameObject.
    /// </summary>
    protected void Interact()
    {
        Debug.Log("JUST USE IT!");
    }

    /// <summary>
    /// Move character and rotate to move direction.
    /// </summary>
    /// <param name="moveDirRequest"></param>
    private (Vector3 rotationDir, float speed) GetMoveAndRotate(Vector2 moveDirRequest)
    {
        // No moving or rotation during attacks or not moving.
        if (animManager.CanDoDamage || moveDirRequest == Vector2.zero)
        {
            _speed = 0;
            _moving = false;
            return (rotationDir: transform.forward, speed: 0);
        }
        _moving = !_knockbackRequest.startNewRequest;

        // Absolute x & y for comparison.
        float absoluteX = Mathf.Abs(moveDirRequest.x);
        float absoluteY = Mathf.Abs(moveDirRequest.y);

        // Rounded x & y for direction of movement.
        int roundedX = RoundToNonZero(moveDirRequest.x);
        int roundedY = RoundToNonZero(moveDirRequest.y);

        bool moveDiagonal = absoluteX > _deadZone && absoluteY > _deadZone;

        Vector3 moveDir;
        if (moveDiagonal)
        {
            moveDir = new Vector3(roundedX, 0, roundedY);
        }
        // Only moving left or right.
        else if (absoluteX > absoluteY)
        {
            moveDir = new Vector3(roundedX, 0, 0);
        }
        // Only moving forward or back.
        else
        {
            moveDir = new Vector3(0, 0, roundedY);
        }

        return (rotationDir: moveDir, (Mathf.Abs(moveDirRequest.sqrMagnitude) * moveSpeed) * 500);
    }

    private readonly float _deadZone = .4f;
    private bool _moving = false;

    /// <summary>
    /// Update _knockback request with new request.
    /// </summary>
    /// <param name="force"></param>
    /// <param name="src"></param>
    public void UpdateKnockbackRequest(float force, Transform src)
    {
        _knockbackRequest = (true, src, force, true);
    }

    private (bool startNewRequest, Transform src, float force, bool doingRequest) _knockbackRequest = (false, null, 0, false);

    /// <summary>
    /// Handles knockback state and applies appropriate physics forces.
    /// </summary>
    private void HandleKnockback()
    {
        bool onBounceSurface = groundedCheck.BounceSurfaceCheck.isBounceSurface;
        bool knockbackActive = !_knockbackRequest.startNewRequest && _knockbackRequest.doingRequest;
        if (onBounceSurface)
        {
            _knockbackRequest = (true, groundedCheck.BounceSurfaceCheck.surfaceTransform, jumpForce, true);
        }

        if (knockbackActive)
        {
            float curVelocityToExit = new Vector2(rb.velocity.x, rb.velocity.z).magnitude;
            if (curVelocityToExit < 1) _knockbackRequest.doingRequest = false;
        }

        if (_knockbackRequest.startNewRequest)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
            rb.AddForce(GetKnockbackMoveDir(_knockbackRequest.src) * _knockbackRequest.force, ForceMode.Impulse);
            _knockbackRequest.startNewRequest = false;
        }
    }

    /// <summary>
    /// Handles jump and move states, alongside applying the relevant physics forces.
    /// </summary>
    private void HandleMovingAndJump()
    {
        bool IsGrounded = groundedCheck.IsGrounded;
        var (newKnockbackRequest, _, _, doingKnockback) = _knockbackRequest;

        // If trying to move & able to.
        if (_moving && !newKnockbackRequest && IsGrounded && !doingKnockback)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
            rb.AddForce(transform.forward * (_speed * 2.5f));
        }
        // Fall faster if not holding jump in a jump state.
        else if (!_doingJump)
        {
            rb.AddForce(-transform.up * 500);
        }

        // Apply jump force if can jump & is requested.
        if (_doingJump && IsGrounded)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        }
        // Stop jump state if falling down.
        if (_doingJump && rb.velocity.y < 0 && !IsGrounded)
        {
            _doingJump = false;
        }
        // Shorten jump by adding extra down force if not holding jump during ascent.
        else if (!_doingJump && rb.velocity.y > 0 && !IsGrounded && !doingKnockback)
        {
            rb.AddForce(-transform.up * shortJumpDownForce);
        }
    }

    private float _speed;

    /// <summary>
    /// Update physics.
    /// </summary>
    private void FixedUpdate()
    {
        HandleKnockback();

        HandleMovingAndJump();
    }

    /// <summary>
    /// Calculates direction of knockback from source.
    /// </summary>
    /// <param name="knockbackSource"></param>
    /// <returns>Direction to apply knockback forces to.</returns>
    private Vector3 GetKnockbackMoveDir(Transform knockbackSource)
    {
        return (transform.position - knockbackSource.position).normalized;
    }

    /// <summary>
    /// Rotate character to look direction.
    /// </summary>
    /// <param name="lookDir"></param>
    private void ApplyRotation(Vector3 lookDir)
    {
        transform.rotation = Quaternion.LookRotation(lookDir);
    }

    /// <summary>
    /// Returns integer next to 0 based off of whether a positive or negative input.
    /// </summary>
    /// <param name="toRound"></param>
    /// <returns>Returns "0", "-1" or "1" based off of if greater, equal or less than zero. </returns>
    private int RoundToNonZero(float toRound)
    {
        return toRound == 0 ? 0 : toRound > 0 ? 1 : -1;
    }
}