using System.Reflection;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XROrigin))]
[RequireComponent(typeof(CharacterController))]
public class VRLocomotionExtendedWithLogs : MonoBehaviour
{
    [Header("References")]
    public XROrigin xrOrigin;
    public CharacterController cc;

    [Header("Input (New Input System)")]
    public InputActionReference crouchAction;   // 按住＝蹲
    public InputActionReference jumpAction;     // 點擊＝跳
    public InputActionReference runAction;      // ★ 按住＝奔跑（新增）

    [Header("Crouch Settings")]
    public float crouchHeight = 1.2f;
    public float viewCrouchOffset = 0.45f;
    public float viewLerpSpeed = 12f;

    [Header("Jump & Gravity")]
    public bool useGravity = true;
    public bool enableJump = true;
    public float gravity = -9.81f;
    public float jumpSpeed = 3.5f;
    public float groundedSkin = 0.05f;

    [Header("Run Settings")]
    [Tooltip("指向你的移動元件（DynamicMoveProvider / ActionBasedContinuousMoveProvider / 任何有 moveSpeed 欄位的元件）")]
    public Component moveProvider;          // ★ 指向移動提供器
    [Tooltip("奔跑倍率：最終速度 = 基礎速度 * 此倍率")]
    public float runMultiplier = 1.8f;      // ★ 奔跑倍率
    [Tooltip("蹲下時是否允許奔跑")]
    public bool allowRunWhileCrouching = false;

    [Header("Logs")]
    public bool verboseLogs = true;

    // ----- internal -----
    Transform _cameraOffset;
    float _baseOffsetY;
    float _originalCCHeight;
    float _verticalVel;
    bool _crouchHeld;
    bool _isCrouching;

    bool _runHeld;                 // ★ 是否按住奔跑
    float _baseMoveSpeed = -1f;    // ★ 紀錄原始移動速度
    FieldInfo _moveSpeedField;     // ★ 反射取得 moveSpeed 欄位
    PropertyInfo _moveSpeedProp;   // ★ 或屬性

    void Reset()
    {
        xrOrigin = GetComponent<XROrigin>();
        cc = GetComponent<CharacterController>();
    }

    void OnEnable()
    {
        if (!xrOrigin) xrOrigin = GetComponent<XROrigin>();
        if (!cc) cc = GetComponent<CharacterController>();

        // 眼睛高度模式，讓視角位移可被 Camera Offset 生效
        xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;

        _cameraOffset = xrOrigin.CameraFloorOffsetObject
            ? xrOrigin.CameraFloorOffsetObject.transform
            : xrOrigin.transform;

        _baseOffsetY = _cameraOffset.localPosition.y;
        _originalCCHeight = Mathf.Max(cc.height, 1.6f);

        // 綁定輸入
        if (crouchAction)
        {
            crouchAction.action.performed += OnCrouchPerformed;
            crouchAction.action.canceled += OnCrouchCanceled;
            crouchAction.action.Enable();
        }
        if (jumpAction)
        {
            jumpAction.action.performed += OnJumpPerformed;
            jumpAction.action.Enable();
        }
        if (runAction)
        {
            runAction.action.performed += OnRunPerformed;
            runAction.action.canceled += OnRunCanceled;
            runAction.action.Enable();
        }

        // ★ 尋找並快取 moveSpeed 欄位/屬性
        CacheMoveProvider();
        ReadBaseMoveSpeed();

        //Log($"[Init] BaseOffsetY={_baseOffsetY:F2}, CC.height={_originalCCHeight:F2}, BaseMoveSpeed={_baseMoveSpeed:F2}");
    }

    void OnDisable()
    {
        if (crouchAction)
        {
            crouchAction.action.performed -= OnCrouchPerformed;
            crouchAction.action.canceled -= OnCrouchCanceled;
            crouchAction.action.Disable();
        }
        if (jumpAction)
        {
            jumpAction.action.performed -= OnJumpPerformed;
            jumpAction.action.Disable();
        }
        if (runAction)
        {
            runAction.action.performed -= OnRunPerformed;
            runAction.action.canceled -= OnRunCanceled;
            runAction.action.Disable();
        }

        // 還原速度
        RestoreBaseMoveSpeed();
    }

    // -------------------- Input --------------------
    void OnCrouchPerformed(InputAction.CallbackContext ctx)
    {
        _crouchHeld = true;
        Log("[Crouch] 開始蹲下 (hold)");
    }
    void OnCrouchCanceled(InputAction.CallbackContext ctx)
    {
        _crouchHeld = false;
        Log("[Crouch] 結束蹲下 (release)");
    }
    void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        if (!enableJump) return;
        if (cc.isGrounded || CheckGrounded())
        {
            _verticalVel = jumpSpeed;
            Log($"[Jump] 起跳 vY={_verticalVel:F2}");
        }
    }
    void OnRunPerformed(InputAction.CallbackContext ctx)
    {
        _runHeld = true;
        ApplyRunSpeed();
        Log("[Run] 開始奔跑 (hold)");
    }
    void OnRunCanceled(InputAction.CallbackContext ctx)
    {
        _runHeld = false;
        RestoreBaseMoveSpeed();
        Log("[Run] 結束奔跑 (release)");
    }

    // -------------------- Update Loop --------------------
    void Update()
    {
        UpdateCrouchState();
        UpdateCharacterControllerSize();
        ApplyGravityAndMove();

        // 若不允許蹲下時奔跑，則強制還原速度
        if (!allowRunWhileCrouching && _isCrouching && _runHeld)
            ApplyRunSpeed(forceStop: true);
    }

    void LateUpdate()
    {
        ForceViewCrouchOffset(); // 視角壓低（最後一刀）
    }

    // -------------------- Crouch --------------------
    void UpdateCrouchState()
    {
        bool target = _crouchHeld;
        if (target != _isCrouching)
        {
            _isCrouching = target;
            Log($"[Crouch] 切換 → {(_isCrouching ? "Crouch" : "Stand")}");
        }
    }

    void UpdateCharacterControllerSize()
    {
        float targetH = _isCrouching ? crouchHeight : _originalCCHeight;
        targetH = Mathf.Clamp(targetH, 0.9f, 2.5f);

        float before = cc.height;
        cc.height = Mathf.Lerp(cc.height, targetH, Time.deltaTime * 12f);

        var c = cc.center;
        float targetCenterY = cc.height * 0.5f + cc.skinWidth;
        c.y = Mathf.Lerp(c.y, targetCenterY, Time.deltaTime * 12f);
        cc.center = c;

        if (Mathf.Abs(cc.height - before) > 0.001f) { }
            //Log($"[CC] Height {before:F2} → {cc.height:F2} (center.y {c.y:F2})");
    }

    void ForceViewCrouchOffset()
    {
        float targetY = _baseOffsetY + (_isCrouching ? -viewCrouchOffset : 0f);
        var lp = _cameraOffset.localPosition;
        float before = lp.y;
        lp.y = Mathf.Lerp(lp.y, targetY, Time.deltaTime * viewLerpSpeed);
        _cameraOffset.localPosition = lp;

        if (Mathf.Abs(lp.y - before) > 0.0005f)
            Log($"[View] CameraOffsetY {before:F2} → {lp.y:F2} (target {targetY:F2})");
    }

    // -------------------- Gravity / Move --------------------
    void ApplyGravityAndMove()
    {
        if (!useGravity) { _verticalVel = 0f; return; }

        bool grounded = cc.isGrounded || CheckGrounded();
        if (grounded && _verticalVel < 0f) _verticalVel = -1f;
        _verticalVel += gravity * Time.deltaTime;

        Vector3 motion = Vector3.up * _verticalVel * Time.deltaTime;
        if (motion.sqrMagnitude > 0f) cc.Move(motion);
    }

    bool CheckGrounded()
    {
        Vector3 start = transform.position + Vector3.up * 0.1f;
        return Physics.SphereCast(start, cc.radius * 0.95f, Vector3.down, out _, groundedSkin + 0.2f, ~0, QueryTriggerInteraction.Ignore);
    }

    // -------------------- Run (speed scaling) --------------------
    void CacheMoveProvider()
    {
        if (!moveProvider)
        {
            // 嘗試在整個場景找第一個有 moveSpeed 欄位/屬性的元件
            var providers = FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var p in providers)
            {
                if (TryBindMoveSpeed(p)) { moveProvider = p; break; }
            }
        }
        else
        {
            TryBindMoveSpeed(moveProvider);
        }

        if (!moveProvider)
            Log("[Run] 未找到可控制的 Move Provider（請在 Inspector 指定）");
    }

    bool TryBindMoveSpeed(Component comp)
    {
        if (!comp) return false;
        var type = comp.GetType();

        // 先找 public/非 public 的欄位或屬性名稱 "moveSpeed"
        _moveSpeedField = type.GetField("moveSpeed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        _moveSpeedProp = type.GetProperty("moveSpeed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        return (_moveSpeedField != null || _moveSpeedProp != null);
    }

    float GetMoveSpeed()
    {
        if (!moveProvider) return -1f;
        if (_moveSpeedField != null) return (float)_moveSpeedField.GetValue(moveProvider);
        if (_moveSpeedProp != null) return (float)_moveSpeedProp.GetValue(moveProvider);
        return -1f;
    }

    void SetMoveSpeed(float v)
    {
        if (!moveProvider) return;
        if (_moveSpeedField != null) _moveSpeedField.SetValue(moveProvider, v);
        else if (_moveSpeedProp != null) _moveSpeedProp.SetValue(moveProvider, v);
    }

    void ReadBaseMoveSpeed()
    {
        float sp = GetMoveSpeed();
        if (sp > 0f) _baseMoveSpeed = sp;
        Log($"[Run] BaseMoveSpeed = {_baseMoveSpeed:F2} (Provider={moveProvider?.GetType().Name ?? "None"})");
    }

    void ApplyRunSpeed(bool forceStop = false)
    {
        if (!moveProvider) return;

        bool running = _runHeld && (allowRunWhileCrouching || !_isCrouching);
        float target = (!running || forceStop || _baseMoveSpeed <= 0f)
            ? _baseMoveSpeed
            : _baseMoveSpeed * Mathf.Max(1f, runMultiplier);

        float before = GetMoveSpeed();
        if (before < 0f) return;

        if (Mathf.Abs(before - target) > 0.001f)
        {
            SetMoveSpeed(target);
            Log($"[Run] Speed {before:F2} → {target:F2}  (running={running}, crouch={_isCrouching})");
        }
    }

    void RestoreBaseMoveSpeed()
    {
        if (!moveProvider || _baseMoveSpeed <= 0f) return;
        float before = GetMoveSpeed();
        SetMoveSpeed(_baseMoveSpeed);
        Log($"[Run] 還原速度 {before:F2} → {_baseMoveSpeed:F2}");
    }

    // -------------------- Utils --------------------
    void Log(string msg)
    {
        if (verboseLogs) Debug.Log($"[VRLocomotion] {msg}", this);
    }
}
