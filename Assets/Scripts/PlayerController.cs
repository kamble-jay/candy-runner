using System;
using System.Collections;
using Spine.Unity;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Spine")]
    private SkeletonAnimation _skeletonAnimation;
    public  Spine.AnimationState spineAnimationState;
    public  Spine.Skeleton skeleton;

    [Header("Jump")]
    public float jumpForce = 12f;
    public bool  allowDoubleJump = false;

    [Header("Power-Up Durations")]
    public float shieldDuration = 18f;
    public float boostDuration = 15f;

    public bool IsShielded { get; private set; }
    public bool IsBoosted  { get; private set; }

    public Action OnDeath;
    public Action<bool> OnShieldChanged;
    public Action<bool> OnBoostChanged;

    private Rigidbody2D _rb;
    private bool _grounded;
    private bool _hasAirJump;
    private Coroutine _shieldRoutine;
    private Coroutine _boostRoutine;

    private float _baseSpeed;
    
    public GameObject shieldIndicator;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        _skeletonAnimation = GetComponent<SkeletonAnimation>();
        skeleton = _skeletonAnimation.skeleton;
        spineAnimationState = _skeletonAnimation.AnimationState;
        _skeletonAnimation.AnimationState.SetAnimation(0, "run", true);
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TryJump();
#endif
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            TryJump();
    }

    private void TryJump()
    {
        if (_grounded)
        {
            ApplyJump();
            _grounded    = false;
            _hasAirJump  = allowDoubleJump;
        }
        else if (_hasAirJump)
        {
            ApplyJump();
            _hasAirJump = false;
        }
    }

    private void ApplyJump()
    {
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Ground"))
        {
            _grounded   = true;
            _hasAirJump = false;
        }
        else if (col.gameObject.CompareTag("Hudle"))
        {
            Die();
        }
        else if (col.gameObject.CompareTag("Booster"))
        {
            ActivateBoost(col.gameObject);
        }
        else if (col.gameObject.CompareTag("Shield"))
        {
            ActivateShield();
            col.gameObject.SetActive(false); 
        }
        else if (col.gameObject.CompareTag("Candy"))
        {
            GameManager.Instance.AddScore(1);
            col.gameObject.SetActive(false);
        }
    }

    private void OnCollisionExit2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Ground"))
            _grounded = false;
    }


    public void ActivateShield()
    {
        if (_shieldRoutine != null) StopCoroutine(_shieldRoutine);
        IsShielded = true;
        shieldIndicator.SetActive(true);
        OnShieldChanged?.Invoke(true);
        _shieldRoutine = StartCoroutine(ShieldTimer());
    }
    public void ConsumeShield()
    {
        if (_shieldRoutine != null) StopCoroutine(_shieldRoutine);
        IsShielded = false;
        shieldIndicator.SetActive(false);
        OnShieldChanged?.Invoke(false);
    }

    private IEnumerator ShieldTimer()
    {
        yield return new WaitForSeconds(shieldDuration);
        IsShielded = false;
        shieldIndicator.SetActive(false);
        OnShieldChanged?.Invoke(false);
    }

    public void ActivateBoost(GameObject pickup)
    {
        pickup.SetActive(false);
        if (!IsBoosted)
        {
            _baseSpeed = ObjectPoolManager.instancePool.moveSpeed;
            ObjectPoolManager.instancePool.SetMoveSpeed(0.3f);
        }

        if (_boostRoutine != null) StopCoroutine(_boostRoutine);
        IsBoosted = true;
        OnBoostChanged?.Invoke(true);
        _boostRoutine = StartCoroutine(BoostTimer());
    }

    private IEnumerator BoostTimer()
    {
        yield return new WaitForSeconds(boostDuration);
        ObjectPoolManager.instancePool.RestoreSpeed(_baseSpeed);
        IsBoosted = false;
        OnBoostChanged?.Invoke(false);
    }

    public void Die()
    {
        if (IsShielded) { ConsumeShield(); return; }

        ProfileManager.Instance?.SetLeaderBoard();
        StopAllPowerUps();
        if (IsBoosted) ObjectPoolManager.instancePool.RestoreSpeed(_baseSpeed);
        IsShielded = false;
        IsBoosted  = false;
        shieldIndicator.SetActive(false);
        OnDeath?.Invoke();
        GameManager.Instance.GameOver();
    }

    public void ResetState()
    {
        StopAllPowerUps();
        if (IsBoosted) ObjectPoolManager.instancePool.RestoreSpeed(_baseSpeed);
        IsShielded = false;
        IsBoosted  = false;
        shieldIndicator.SetActive(false);
    }

    private void StopAllPowerUps()
    {
        if (_shieldRoutine != null) { StopCoroutine(_shieldRoutine); _shieldRoutine = null; }
        if (_boostRoutine  != null) { StopCoroutine(_boostRoutine);  _boostRoutine  = null; }
    }
}
