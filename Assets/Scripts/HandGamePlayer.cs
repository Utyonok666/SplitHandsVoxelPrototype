using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
// ============================================================
// HandGamePlayer
// Main player interaction/controller logic for hands, items, breaking, placing, and gameplay input. (Этот скрипт отвечает за: main player interaction/controller logic for hands, items, breaking, placing, and gameplay input.)
// ============================================================
public class HandGamePlayer : MonoBehaviour
{
// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Components")]
    public CharacterController controller;
    public Transform playerCamera;
    public PlayerAudio playerAudio;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Movement")]
    public float walkSpeed = 6f;
    public float mouseSensitivity = 2f;
    public float gravity = -18f;
    public float jumpHeight = 1.6f;
    public float groundCheckRadius = 0.45f;
    public float groundCheckOffset = 0.05f;
    public float fallClampSpeed = -20f;
    public LayerMask groundLayer;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Noclip")]
    public float noclipSpeed = 10f;
    public float noclipSprintMultiplier = 2f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Hands")]
    public Transform leftHand;
    public Transform rightHand;
    public float handForwardDistance = 1.05f;
    public float handVerticalPitchAmount = 0.45f;
    public float handPitchRotationAmount = 25f;
    public float handMoveSpeed = 12f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Idle Hand Follow")]
    public float idleVerticalPitchAmount = 0.12f;
    public float idlePitchRotationAmount = 8f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Interaction")]
    public float grabDistance = 4f;
    public float blockDamagePerSecond = 1f;
    public float objectDamageMultiplier = 12f;
    public KeyCode pickupKey = KeyCode.F;
    public LayerMask lookHitMask = ~0;
    public float blockCaptureInset = 0.08f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Placement")]
    public float placeDistance = 4f;
    public LayerMask placementMask = ~0;
    public Vector3 placementOverlapHalfExtents = new Vector3(0.45f, 0.45f, 0.45f);
    public float placementSurfaceOffset = 0.5f;
    public float minPlaceDistanceFromPlayer = 0.9f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Enemy Hit")]
    public float enemyHitDistance = 3f;
    public float enemyDamageMultiplier = 1f;
    public float enemyKnockbackForce = 7f;
    public float enemyUpKnockbackForce = 1.75f;
    public float pressAttackCooldown = 0.18f;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Highlight")]
    public Material outlineMaterial;
    public GameObject lockedTargetOutlinePrefab;
    public Vector3 outlineOffset = Vector3.zero;
    public Vector3 outlineScale = Vector3.one;

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Debug Text")]
    public bool showHitDebugText = true;
    public Vector2 debugTextOffset = new Vector2(0f, 70f);

    private ToolSettings lastHighlighted;
    private GameObject currentLockedOutline;

    private Vector3 leftOrigin;
    private Vector3 rightOrigin;
    private Quaternion leftOriginRot;
    private Quaternion rightOriginRot;

    private ToolSettings leftHeld;
    private ToolSettings rightHeld;

    private Transform leftHolder;
    private Transform rightHolder;

    private float xRotation;
    private Vector3 velocity;
    private Vector3 lastHorizontalMoveVelocity;
    private bool isGrounded;

    private bool noclipEnabled = false;
    private bool chatInputBlocked = false;

    private HeldHitTarget leftLockedTarget;
    private HeldHitTarget rightLockedTarget;

    private string currentBreakText = "";
    private float currentBreakPercent = 0f;
    private bool isCurrentlyBreaking = false;
    private GUIStyle debugStyle;

    private float nextPressAttackTime = 0f;

    private class HeldHitTarget
    {
        public Chunk chunk;
        public InteractableObject interactable;
        public LootChest chest;
        public Vector3 worldBlockPos;
        public Vector3 blockCenterWorld;

        public bool IsValid => chunk != null || interactable != null || chest != null;
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        controller.enabled = true;

        if (playerAudio == null)
            playerAudio = GetComponent<PlayerAudio>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (leftHand != null)
        {
            leftOrigin = leftHand.localPosition;
            leftOriginRot = leftHand.localRotation;
            leftHolder = CreateHandHolder("LeftItemHolder", leftHand);
        }

        if (rightHand != null)
        {
            rightOrigin = rightHand.localPosition;
            rightOriginRot = rightHand.localRotation;
            rightHolder = CreateHandHolder("RightItemHolder", rightHand);
        }

        debugStyle = new GUIStyle();
        debugStyle.alignment = TextAnchor.UpperCenter;
        debugStyle.fontSize = 22;
        debugStyle.fontStyle = FontStyle.Bold;
        debugStyle.normal.textColor = Color.white;
    }

    // Get Camera Pitch. (Get Camera Pitch)
    public float GetCameraPitch()
    {
        return xRotation;
    }

    // Apply Loaded Rotation. (Apply Loaded Rotation)
    public void ApplyLoadedRotation(float yaw, float pitch)
    {
        xRotation = Mathf.Clamp(pitch, -90f, 90f);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (playerCamera != null)
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    // Toggle No Clip. (Toggle No Clip)
    public void ToggleNoClip()
    {
        noclipEnabled = !noclipEnabled;

        if (controller != null)
            controller.enabled = !noclipEnabled;

        velocity = Vector3.zero;
        lastHorizontalMoveVelocity = Vector3.zero;
    }

    // Is No Clip Enabled. (Is No Clip Enabled)
    public bool IsNoClipEnabled()
    {
        return noclipEnabled;
    }

    // Set Chat Input Blocked. (Set Chat Input Blocked)
    public void SetChatInputBlocked(bool blocked)
    {
        chatInputBlocked = blocked;
    }

    Transform CreateHandHolder(string holderName, Transform hand)
    {
        GameObject go = new GameObject(holderName);
        Transform holder = go.transform;
        holder.SetParent(hand, false);
        holder.localPosition = Vector3.zero;
        holder.localRotation = Quaternion.identity;
        holder.localScale = new Vector3(5.75f, 9f, 1f);
        return holder;
    }

    void Update()
    {
        if (chatInputBlocked)
        {
            if (noclipEnabled)
                return;

            UpdateGroundedState();

            if (controller != null && controller.enabled)
            {
                if (isGrounded && velocity.y < 0f)
                    velocity.y = -2f;

                velocity.y += gravity * Time.deltaTime;

                if (velocity.y < fallClampSpeed)
                    velocity.y = fallClampSpeed;

                lastHorizontalMoveVelocity = Vector3.zero;
                controller.Move(velocity * Time.deltaTime);
            }

            if (playerAudio != null)
                playerAudio.Tick(lastHorizontalMoveVelocity, isGrounded, noclipEnabled);

            return;
        }

        RotateCamera();

        if (noclipEnabled)
        {
            MoveNoClip();
            HandleHighlight();
            UpdateLookDebugText();

            if (playerAudio != null)
                playerAudio.Tick(Vector3.zero, false, true);

            return;
        }

        UpdateGroundedState();
        MovePlayer();

        if (playerAudio != null)
            playerAudio.Tick(lastHorizontalMoveVelocity, isGrounded, noclipEnabled);

        HandleHighlight();

        bool leftHeldKey = Input.GetKey(KeyCode.Q);
        bool rightHeldKey = Input.GetKey(KeyCode.E);

        isCurrentlyBreaking = false;

        bool leftPressed = Input.GetKeyDown(KeyCode.Q);
        bool rightPressed = Input.GetKeyDown(KeyCode.E);

        bool leftReleased = Input.GetKeyUp(KeyCode.Q);
        bool rightReleased = Input.GetKeyUp(KeyCode.E);

        if (leftPressed)
        {
            bool hitEnemy = TryAttackEnemyWithHand(leftHeld);
            if (!hitEnemy)
                leftLockedTarget = CaptureCurrentTarget();
        }

        if (rightPressed)
        {
            bool hitEnemy = TryAttackEnemyWithHand(rightHeld);
            if (!hitEnemy)
                rightLockedTarget = CaptureCurrentTarget();
        }

        if (leftReleased)
        {
            leftLockedTarget = null;
            currentBreakPercent = 0f;
            isCurrentlyBreaking = false;
        }

        if (rightReleased)
        {
            rightLockedTarget = null;
            currentBreakPercent = 0f;
            isCurrentlyBreaking = false;
        }

        UpdateLockedOutline(leftHeldKey, rightHeldKey);

        if (leftHand != null)
            UpdateHandPose(leftHand, leftOrigin, leftOriginRot, leftHeldKey);

        if (rightHand != null)
            UpdateHandPose(rightHand, rightOrigin, rightOriginRot, rightHeldKey);

        if (leftHeldKey)
            UseLockedTarget(leftLockedTarget, leftHeld, KeyCode.Q);

        if (rightHeldKey)
            UseLockedTarget(rightLockedTarget, rightHeld, KeyCode.E);

        if (Input.GetMouseButtonDown(0) && (leftHeldKey || rightHeldKey))
            TryPickUpToolToHand(leftHeldKey, rightHeldKey);

        if (Input.GetMouseButtonDown(0) && !leftHeldKey && !rightHeldKey)
            TryPlaceHeldItem();

        if (Input.GetKeyDown(pickupKey))
            TryPickupLootToInventory();

        if (Input.GetMouseButtonDown(1) && (leftHeldKey || rightHeldKey))
            TryDropFromActiveHand(leftHeldKey, rightHeldKey);

        UpdateLookDebugText();
    }

    void OnGUI()
    {
        if (!showHitDebugText || string.IsNullOrEmpty(currentBreakText))
            return;

        Rect rect = new Rect(
            (Screen.width * 0.5f) - 250f + debugTextOffset.x,
            25f + debugTextOffset.y,
            500f,
            40f
        );

        GUI.Label(rect, currentBreakText, debugStyle);
    }

    void UpdateLookDebugText()
    {
        if (isCurrentlyBreaking)
            return;

        currentBreakPercent = 0f;
        currentBreakText = "";

        if (TryGetEnemyHit(out RaycastHit enemyHit))
        {
            EnemyHitReceiver enemy = enemyHit.collider.GetComponentInParent<EnemyHitReceiver>();
            if (enemy != null)
            {
                EnemyHealth hp = enemy.GetHealth();

                if (hp != null)
                    currentBreakText = $"Target: Skeleton {Mathf.RoundToInt(hp.CurrentHealth)}/{Mathf.RoundToInt(hp.MaxHealth)} HP";
                else
                    currentBreakText = "Target: Skeleton";

                return;
            }
        }

        if (TryGetLookHit(out RaycastHit hit))
        {
            LootChest chest = hit.collider.GetComponentInParent<LootChest>();
            if (chest != null)
            {
                if (chest.IsOpening())
                {
                    currentBreakText = $"Opening: Chest ({Mathf.RoundToInt(chest.GetOpenPercent())}%)";
                    isCurrentlyBreaking = true;
                }
                else
                {
                    currentBreakText = "Target: Chest";
                }
                return;
            }
        }

        if (TryConfirmBlockHit(out Chunk hitChunk, out Vector3 hitPos, out Vector3 _, out int blockType))
        {
            string blockName = GetBlockTypeName(blockType);
            ResourceType toolType = GetActiveToolType();

            if (hitChunk.CanBreakBlockAtWorld(hitPos, toolType))
                currentBreakText = $"Target: {blockName}";
            else
                currentBreakText = $"Unbreaking: {blockName}";

            return;
        }

        if (TryGetLookHit(out RaycastHit hit2))
        {
            InteractableObject interactable = hit2.collider.GetComponentInParent<InteractableObject>();
            if (interactable != null)
            {
                string objectName = GetInteractableDisplayName(interactable.gameObject.name);
                ResourceType toolType = GetActiveToolType();

                if (interactable.CanBeDamagedBy(toolType))
                    currentBreakText = $"Target: {objectName}";
                else
                    currentBreakText = $"Unbreaking: {objectName}";
            }
        }
    }

    ResourceType GetActiveToolType()
    {
        if (leftHeld != null)
            return leftHeld.toolType;

        if (rightHeld != null)
            return rightHeld.toolType;

        return ResourceType.Flesh;
    }

    string GetBlockTypeName(int blockType)
    {
        if (blockType == 1) return "Grass";
        if (blockType == 2) return "Dirt";
        if (blockType == 3) return "Stone";
        return "Unknown";
    }

    string GetInteractableDisplayName(string objectName)
    {
        string lower = objectName.ToLower();

        if (lower.Contains("tree"))
            return "Tree";

        if (lower.Contains("rock") || lower.Contains("stone"))
            return "Rock";

        if (lower.Contains("iron"))
            return "Iron Ore";

        return "Object";
    }

    void UpdateGroundedState()
    {
        Vector3 spherePos = transform.position + Vector3.up * groundCheckOffset;

        bool sphereGrounded = Physics.CheckSphere(
            spherePos,
            groundCheckRadius,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        isGrounded = sphereGrounded || controller.isGrounded;

        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;
    }

    void MovePlayer()
    {
        if (controller == null || !controller.enabled)
            return;

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 move = (transform.right * x + transform.forward * z).normalized;
        lastHorizontalMoveVelocity = move * walkSpeed;
        controller.Move(lastHorizontalMoveVelocity * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            if (playerAudio != null)
                playerAudio.PlayJump();
        }

        velocity.y += gravity * Time.deltaTime;

        if (velocity.y < fallClampSpeed)
            velocity.y = fallClampSpeed;

        controller.Move(Vector3.up * velocity.y * Time.deltaTime);
    }

    void MoveNoClip()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        float vertical = 0f;
        if (Input.GetKey(KeyCode.Space))
            vertical += 1f;
        if (Input.GetKey(KeyCode.LeftControl))
            vertical -= 1f;

        float speed = noclipSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
            speed *= noclipSprintMultiplier;

        Vector3 move =
            transform.right * x +
            transform.forward * z +
            Vector3.up * vertical;

        if (move.sqrMagnitude > 1f)
            move.Normalize();

        lastHorizontalMoveVelocity = Vector3.zero;
        transform.position += move * speed * Time.deltaTime;
    }

    void RotateCamera()
    {
        float mX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (playerCamera != null)
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mX);
    }

    void UpdateHandPose(Transform hand, Vector3 originLocalPos, Quaternion originLocalRot, bool isExtended)
    {
        float pitchT = Mathf.Clamp(xRotation / 90f, -1f, 1f);

        Vector3 idleOffset = Vector3.up * (-pitchT * idleVerticalPitchAmount);
        Quaternion idleRot = originLocalRot * Quaternion.Euler(-pitchT * idlePitchRotationAmount, 0f, 0f);

        Vector3 extendedOffset = Vector3.zero;
        Quaternion extendedRot = Quaternion.identity;

        if (isExtended)
        {
            extendedOffset =
                Vector3.forward * handForwardDistance +
                Vector3.up * (-pitchT * handVerticalPitchAmount);

            extendedRot = Quaternion.Euler(-pitchT * handPitchRotationAmount, 0f, 0f);
        }

        Vector3 targetLocalPos = originLocalPos + idleOffset + extendedOffset;
        Quaternion targetLocalRot = idleRot * extendedRot;

        hand.localPosition = Vector3.Lerp(hand.localPosition, targetLocalPos, Time.deltaTime * handMoveSpeed);
        hand.localRotation = Quaternion.Slerp(hand.localRotation, targetLocalRot, Time.deltaTime * handMoveSpeed);
    }

    Ray GetCenterScreenRay()
    {
        Camera cam = playerCamera != null ? playerCamera.GetComponent<Camera>() : null;

        if (cam != null)
            return cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        return new Ray(playerCamera.position, playerCamera.forward);
    }

    bool TryGetLookHit(out RaycastHit hit)
    {
        hit = default;

        if (playerCamera == null)
            return false;

        Ray ray = GetCenterScreenRay();

        return Physics.Raycast(
            ray.origin,
            ray.direction,
            out hit,
            grabDistance,
            lookHitMask,
            QueryTriggerInteraction.Ignore
        );
    }

    bool TryGetPlacementHit(out RaycastHit hit)
    {
        hit = default;

        if (playerCamera == null)
            return false;

        Ray ray = GetCenterScreenRay();

        return Physics.Raycast(
            ray.origin,
            ray.direction,
            out hit,
            placeDistance,
            placementMask,
            QueryTriggerInteraction.Ignore
        );
    }

    bool TryGetEnemyHit(out RaycastHit hit)
    {
        hit = default;

        if (playerCamera == null)
            return false;

        Ray ray = GetCenterScreenRay();

        if (!Physics.Raycast(
            ray.origin,
            ray.direction,
            out hit,
            enemyHitDistance,
            lookHitMask,
            QueryTriggerInteraction.Ignore))
            return false;

        EnemyHitReceiver enemy = hit.collider.GetComponentInParent<EnemyHitReceiver>();
        return enemy != null;
    }

    bool TryConfirmBlockHit(out Chunk chunk, out Vector3 blockWorldPos, out Vector3 blockCenter, out int blockType)
    {
        chunk = null;
        blockWorldPos = Vector3.zero;
        blockCenter = Vector3.zero;
        blockType = 0;

        if (playerCamera == null)
            return false;

        Ray ray = GetCenterScreenRay();

        if (!Physics.Raycast(ray, out RaycastHit hit, grabDistance, lookHitMask, QueryTriggerInteraction.Ignore))
            return false;

        Chunk hitChunk = hit.collider.GetComponent<Chunk>();
        if (hitChunk == null)
            hitChunk = hit.collider.GetComponentInParent<Chunk>();

        if (hitChunk == null)
            return false;

        Vector3 samplePoint = hit.point + ray.direction * blockCaptureInset;

        if (!hitChunk.TryGetBlockAtWorld(samplePoint, out blockType, out Vector3 center))
            return false;

        if (blockType == 0)
            return false;

        chunk = hitChunk;
        blockWorldPos = samplePoint;
        blockCenter = center;
        return true;
    }

    HeldHitTarget CaptureCurrentTarget()
    {
        if (TryConfirmBlockHit(out Chunk voxelChunk, out Vector3 blockPos, out Vector3 center, out int _))
        {
            return new HeldHitTarget
            {
                chunk = voxelChunk,
                worldBlockPos = blockPos,
                blockCenterWorld = center
            };
        }

        if (TryGetLookHit(out RaycastHit hit))
        {
            LootChest chest = hit.collider.GetComponentInParent<LootChest>();
            if (chest != null)
            {
                return new HeldHitTarget
                {
                    chest = chest
                };
            }

            InteractableObject interactable = hit.collider.GetComponentInParent<InteractableObject>();
            if (interactable != null)
            {
                return new HeldHitTarget
                {
                    interactable = interactable
                };
            }
        }

        return null;
    }

    void UpdateLockedOutline(bool leftHeldKey, bool rightHeldKey)
    {
        HeldHitTarget activeTarget = null;

        if (leftHeldKey && leftLockedTarget != null && leftLockedTarget.chunk != null)
            activeTarget = leftLockedTarget;
        else if (rightHeldKey && rightLockedTarget != null && rightLockedTarget.chunk != null)
            activeTarget = rightLockedTarget;

        if (activeTarget == null)
        {
            HideLockedOutline();
            return;
        }

        if (lockedTargetOutlinePrefab == null)
            return;

        if (currentLockedOutline == null)
            currentLockedOutline = Instantiate(lockedTargetOutlinePrefab);

        currentLockedOutline.SetActive(true);
        currentLockedOutline.transform.position = activeTarget.blockCenterWorld + outlineOffset;
        currentLockedOutline.transform.rotation = Quaternion.identity;
        currentLockedOutline.transform.localScale = outlineScale;
    }

    void HideLockedOutline()
    {
        if (currentLockedOutline != null)
            currentLockedOutline.SetActive(false);
    }

    void UseLockedTarget(HeldHitTarget target, ToolSettings heldTool, KeyCode holdKey)
    {
        if (target == null || !target.IsValid)
            return;

        if (target.chest != null)
        {
            target.chest.HoldInteract(Time.deltaTime, holdKey);
            isCurrentlyBreaking = true;
            currentBreakText = $"Opening: Chest ({Mathf.RoundToInt(target.chest.GetOpenPercent())}%)";
            return;
        }

        ResourceType toolType = GetHeldToolType(heldTool);
        float damage = GetHeldDamage(heldTool);

        if (target.chunk != null)
        {
            if (!TryConfirmBlockHit(out Chunk hitChunk, out Vector3 hitPos, out Vector3 hitCenter, out int blockType))
                return;

            if (hitChunk != target.chunk)
            {
                target.chunk = hitChunk;
                target.worldBlockPos = hitPos;
                target.blockCenterWorld = hitCenter;
            }

            if (hitChunk.CanBreakBlockAtWorld(hitPos, toolType))
            {
                float multiplier = hitChunk.GetBreakMultiplierForBlock(hitPos, toolType);
                float finalDamage = blockDamagePerSecond * damage * Mathf.Max(0.01f, multiplier);

                float breakTime = blockType == 3 ? hitChunk.stoneBreakTime : hitChunk.groundBreakTime;
                float progressPerSecond = finalDamage / Mathf.Max(0.01f, breakTime);

                currentBreakPercent += progressPerSecond * Time.deltaTime * 100f;
                currentBreakPercent = Mathf.Clamp(currentBreakPercent, 0f, 100f);

                hitChunk.BreakBlockAtWorld(hitPos, finalDamage);

                isCurrentlyBreaking = true;
                currentBreakText = $"Breaking: {GetBlockTypeName(blockType)} {Mathf.RoundToInt(currentBreakPercent)}%";

                if (currentBreakPercent >= 100f)
                    currentBreakPercent = 0f;
            }
            else
            {
                isCurrentlyBreaking = false;
                currentBreakPercent = 0f;
                currentBreakText = $"Unbreaking: {GetBlockTypeName(blockType)}";
            }

            return;
        }

        if (target.interactable != null)
        {
            if (!TryGetLookHit(out RaycastHit hit))
                return;

            InteractableObject interactable = hit.collider.GetComponentInParent<InteractableObject>();
            if (interactable == null || interactable != target.interactable)
                return;

            string objectName = GetInteractableDisplayName(interactable.gameObject.name);

            if (!interactable.CanBeDamagedBy(toolType))
            {
                isCurrentlyBreaking = false;
                currentBreakPercent = 0f;
                currentBreakText = $"Unbreaking: {objectName}";
                return;
            }

            float finalDamage = damage * objectDamageMultiplier * Time.deltaTime;
            interactable.TakeHit(finalDamage, toolType);

            isCurrentlyBreaking = true;
            currentBreakPercent = interactable.GetBreakProgressPercent();
            currentBreakText = $"Breaking: {objectName} {Mathf.RoundToInt(currentBreakPercent)}%";
        }
    }

    bool TryAttackEnemyWithHand(ToolSettings heldTool)
    {
        if (Time.time < nextPressAttackTime)
            return false;

        if (heldTool == null)
            return false;

        if (!TryGetEnemyHit(out RaycastHit hit))
            return false;

        EnemyHitReceiver enemy = hit.collider.GetComponentInParent<EnemyHitReceiver>();
        if (enemy == null)
            return false;

        float damage = Mathf.Max(0.01f, heldTool.damage) * enemyDamageMultiplier;
        enemy.ApplyDamage(damage);

        if (playerAudio != null)
            playerAudio.PlayAttack();

        EnemyHealth hp = enemy.GetHealth();

        Rigidbody enemyRb = hit.collider.GetComponentInParent<Rigidbody>();
        if (enemyRb != null)
        {
            Vector3 pushDir = enemyRb.position - transform.position;
            pushDir.y = 0f;

            if (pushDir.sqrMagnitude < 0.001f)
                pushDir = transform.forward;
            else
                pushDir.Normalize();

            Vector3 force = pushDir * enemyKnockbackForce + Vector3.up * enemyUpKnockbackForce;
            enemyRb.AddForce(force, ForceMode.Impulse);
        }

        nextPressAttackTime = Time.time + pressAttackCooldown;

        if (hp != null)
            currentBreakText = $"Hit: Skeleton {Mathf.RoundToInt(hp.CurrentHealth)}/{Mathf.RoundToInt(hp.MaxHealth)} HP";
        else
            currentBreakText = "Hit: Skeleton";

        currentBreakPercent = 0f;
        isCurrentlyBreaking = false;

        return true;
    }

    ResourceType GetHeldToolType(ToolSettings held)
    {
        if (held == null)
            return ResourceType.Flesh;

        return held.toolType;
    }

    float GetHeldDamage(ToolSettings held)
    {
        if (held == null)
            return 1f;

        return Mathf.Max(0.01f, held.damage);
    }

    void TryPickUpToolToHand(bool leftActive, bool rightActive)
    {
        if (!TryGetLookHit(out RaycastHit hit))
            return;

        ToolSettings t = hit.collider.GetComponentInParent<ToolSettings>();
        if (t == null || t.isEquipped)
            return;

        bool picked = false;

        if (leftActive && !rightActive)
        {
            if (leftHeld == null)
            {
                Equip(t, leftHolder, ref leftHeld);
                picked = true;
            }

            if (picked && playerAudio != null)
                playerAudio.PlayPickup();

            return;
        }

        if (rightActive && !leftActive)
        {
            if (rightHeld == null)
            {
                Equip(t, rightHolder, ref rightHeld);
                picked = true;
            }

            if (picked && playerAudio != null)
                playerAudio.PlayPickup();

            return;
        }

        if (leftActive && rightActive)
        {
            if (leftHeld == null)
            {
                Equip(t, leftHolder, ref leftHeld);
                picked = true;
            }
            else if (rightHeld == null)
            {
                Equip(t, rightHolder, ref rightHeld);
                picked = true;
            }
        }

        if (picked && playerAudio != null)
            playerAudio.PlayPickup();
    }

    void TryPickupLootToInventory()
    {
        if (!TryGetLookHit(out RaycastHit hit))
            return;

        LootChest chest = hit.collider.GetComponentInParent<LootChest>();
        if (chest != null)
        {
            chest.StartManualOpen();
            return;
        }

        ItemDrop itemDrop = hit.collider.GetComponentInParent<ItemDrop>();
        if (itemDrop != null)
        {
            itemDrop.Pickup();

            if (playerAudio != null)
                playerAudio.PlayPickup();
        }
    }

    void TryDropFromActiveHand(bool leftActive, bool rightActive)
    {
        bool dropped = false;

        if (leftActive && !rightActive)
        {
            if (leftHeld != null)
            {
                Drop(ref leftHeld);
                dropped = true;
            }

            if (dropped && playerAudio != null)
                playerAudio.PlayDrop();

            return;
        }

        if (rightActive && !leftActive)
        {
            if (rightHeld != null)
            {
                Drop(ref rightHeld);
                dropped = true;
            }

            if (dropped && playerAudio != null)
                playerAudio.PlayDrop();

            return;
        }

        if (leftActive && rightActive)
        {
            if (leftHeld != null)
            {
                Drop(ref leftHeld);
                dropped = true;
            }
            else if (rightHeld != null)
            {
                Drop(ref rightHeld);
                dropped = true;
            }
        }

        if (dropped && playerAudio != null)
            playerAudio.PlayDrop();
    }

    void TryPlaceHeldItem()
    {
        ToolSettings held = null;
        bool useLeftHand = false;

        if (leftHeld != null && leftHeld.isPlaceable)
        {
            held = leftHeld;
            useLeftHand = true;
        }
        else if (rightHeld != null && rightHeld.isPlaceable)
        {
            held = rightHeld;
            useLeftHand = false;
        }

        if (held == null || held.placedPrefab == null)
            return;

        if (!TryGetPlacementHit(out RaycastHit hit))
            return;

        Quaternion placeRotation = Quaternion.Euler(-90f, 0f, 0f);

        Vector3 basePosition;
        if (hit.normal.y > 0.5f)
        {
            basePosition = new Vector3(
                Mathf.Round(hit.point.x),
                hit.point.y,
                Mathf.Round(hit.point.z)
            );
        }
        else
        {
            Vector3 offset = hit.normal.normalized;
            basePosition = new Vector3(
                Mathf.Round(hit.point.x + offset.x * placementSurfaceOffset),
                hit.point.y,
                Mathf.Round(hit.point.z + offset.z * placementSurfaceOffset)
            );
        }

        GameObject placed = Instantiate(held.placedPrefab, basePosition, placeRotation);
        Bounds placedBounds = GetHierarchyBounds(placed);

        if (placedBounds.size == Vector3.zero)
        {
            Destroy(placed);
            return;
        }

        float yOffset = hit.point.y - placedBounds.min.y + 0.01f;
        placed.transform.position += Vector3.up * yOffset;

        placedBounds = GetHierarchyBounds(placed);

        Vector3 playerCheckPos = new Vector3(placedBounds.center.x, transform.position.y, placedBounds.center.z);
        if (Vector3.Distance(playerCheckPos, transform.position) < minPlaceDistanceFromPlayer)
        {
            Destroy(placed);
            return;
        }

        Collider[] overlaps = Physics.OverlapBox(
            placedBounds.center,
            placedBounds.extents * 0.95f,
            placed.transform.rotation,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider c = overlaps[i];

            if (c == null)
                continue;

            if (c.transform.IsChildOf(placed.transform))
                continue;

            if (controller != null && c == controller)
                continue;

            if (c.transform.IsChildOf(transform))
                continue;

            if (c == hit.collider || c.transform.IsChildOf(hit.collider.transform))
                continue;

            Destroy(placed);
            return;
        }

        if (playerAudio != null)
            playerAudio.PlayDrop();

        if (useLeftHand)
            RemoveHeldItem(ref leftHeld);
        else
            RemoveHeldItem(ref rightHeld);
    }
        Bounds GetHierarchyBounds(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>();
        bool hasBounds = false;
        Bounds result = new Bounds(root.transform.position, Vector3.zero);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider c = colliders[i];
            if (c == null || !c.enabled)
                continue;

            if (!hasBounds)
            {
                result = c.bounds;
                hasBounds = true;
            }
            else
            {
                result.Encapsulate(c.bounds);
            }
        }

        if (hasBounds)
            return result;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || !r.enabled)
                continue;

            if (!hasBounds)
            {
                result = r.bounds;
                hasBounds = true;
            }
            else
            {
                result.Encapsulate(r.bounds);
            }
        }

        return result;
    }
    void RemoveHeldItem(ref ToolSettings held)
    {
        if (held == null)
            return;

        Destroy(held.gameObject);
        held = null;
    }

    void Equip(ToolSettings t, Transform holder, ref ToolSettings held)
    {
        if (t == null || holder == null)
            return;

        SaveableWorldObject saveable = t.GetComponent<SaveableWorldObject>();
        if (saveable != null)
            saveable.MarkRemoved();

        held = t;
        t.OnEquipped(holder);

        ItemPhysics physicsHelper = t.GetComponent<ItemPhysics>();
        if (physicsHelper != null)
            physicsHelper.PrepareForPickup();

        ResetHL();
    }

    void Drop(ref ToolSettings held)
    {
        if (held == null)
            return;

        Rigidbody rb = held.GetComponent<Rigidbody>();

        held.OnDropped();

        if (rb != null && playerCamera != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(playerCamera.forward * 4f, ForceMode.Impulse);
        }

        held = null;
    }

    void HandleHighlight()
    {
        if (!TryGetLookHit(out RaycastHit hit))
        {
            ResetHL();
            return;
        }

        ToolSettings t = hit.collider.GetComponentInParent<ToolSettings>();
        if (t != null && !t.isEquipped)
        {
            if (lastHighlighted != t)
            {
                ResetHL();
                lastHighlighted = t;
                SetHL(t, true);
            }
            return;
        }

        ResetHL();
    }

    void ResetHL()
    {
        if (lastHighlighted != null)
        {
            SetHL(lastHighlighted, false);
            lastHighlighted = null;
        }
    }

    void SetHL(ToolSettings t, bool state)
    {
        if (outlineMaterial == null || t == null)
            return;

        foreach (MeshRenderer r in t.GetComponentsInChildren<MeshRenderer>(true))
        {
            List<Material> mats = new List<Material>(r.sharedMaterials);

            if (state)
            {
                if (!mats.Contains(outlineMaterial))
                    mats.Add(outlineMaterial);
            }
            else
            {
                mats.RemoveAll(m =>
                    m == outlineMaterial ||
                    (m != null && outlineMaterial != null && m.name.Contains(outlineMaterial.name)));
            }

            r.materials = mats.ToArray();
        }
    }

    // Capture Left Hand Save. (Capture Left Hand Save)
    public HeldItemSaveRecord CaptureLeftHandSave()
    {
        if (leftHeld == null)
            return null;

        return new HeldItemSaveRecord
        {
            resourceName = leftHeld.gameObject.name.Replace("(Clone)", "").Trim()
        };
    }

    // Capture Right Hand Save. (Capture Right Hand Save)
    public HeldItemSaveRecord CaptureRightHandSave()
    {
        if (rightHeld == null)
            return null;

        return new HeldItemSaveRecord
        {
            resourceName = rightHeld.gameObject.name.Replace("(Clone)", "").Trim()
        };
    }

    // Load Held Items. (Load Held Items)
    public void LoadHeldItems(WorldSaveData data)
    {
        if (data == null)
            return;

        if (data.leftHand != null && !string.IsNullOrEmpty(data.leftHand.resourceName))
            TrySpawnHeldItem(data.leftHand.resourceName, true);

        if (data.rightHand != null && !string.IsNullOrEmpty(data.rightHand.resourceName))
            TrySpawnHeldItem(data.rightHand.resourceName, false);
    }

    void TrySpawnHeldItem(string resourceName, bool leftSide)
    {
        GameObject prefab = Resources.Load<GameObject>(resourceName);
        if (prefab == null)
        {
            Debug.LogWarning("[HandGamePlayer] Could not load held item from Resources: " + resourceName);
            return;
        }

        GameObject obj = Instantiate(
            prefab,
            transform.position + transform.forward,
            Quaternion.identity
        );

        ToolSettings tool = obj.GetComponent<ToolSettings>();
        if (tool == null)
        {
            Debug.LogWarning("[HandGamePlayer] Spawned held item has no ToolSettings: " + resourceName);
            Destroy(obj);
            return;
        }

        if (leftSide)
        {
            if (leftHeld != null)
            {
                Destroy(obj);
                return;
            }

            Equip(tool, leftHolder, ref leftHeld);
        }
        else
        {
            if (rightHeld != null)
            {
                Destroy(obj);
                return;
            }

            Equip(tool, rightHolder, ref rightHeld);
        }
    }
}
