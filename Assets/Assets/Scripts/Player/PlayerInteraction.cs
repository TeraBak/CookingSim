using UnityEngine;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public LayerMask interactLayer;
    private Collider playerCollider;

    [Header("Pickup Settings")]
    public float interactDistance = 3f;
    public float holdDistance = 1.5f;
    public float moveSpeed = 10f;

    private GameObject heldObject;
    private Rigidbody heldRb;

    private GameObject lastHighlighted;
    private Outline lastOutline;

    [Header("Throw Settings")]
    public float minThrowForce = 2f;
    public float maxThrowForce = 10f;
    public float maxHoldTime = 2f;
    public float chargeIndicatorDelay = 0.8f;

    private float holdTime = 0f;
    private bool isHoldingThrow = false;
    private bool hasShownIndicator = false;

    [Header("UI")]
    public Image throwChargeIndicator;

    void Start()
    {
        playerCollider = GetComponent<Collider>();
    }

    void Update()
    {
        if (heldObject == null)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                TryPickupObject();
            }

            UpdateHighlight();
        }
        else
        {
            UpdateHeldObjectPosition();

            if (Input.GetKeyDown(KeyCode.G))
            {
                isHoldingThrow = true;
                holdTime = 0f;
                hasShownIndicator = false;
                ShowThrowIndicator(false);
            }

            if (isHoldingThrow)
            {
                holdTime += Time.deltaTime;
                holdTime = Mathf.Min(holdTime, maxHoldTime);

                if (!hasShownIndicator && holdTime >= chargeIndicatorDelay)
                {
                    ShowThrowIndicator(true);
                    hasShownIndicator = true;
                }

                if (hasShownIndicator)
                {
                    float normalized = (holdTime - chargeIndicatorDelay) / (maxHoldTime - chargeIndicatorDelay);
                    UpdateThrowIndicator(Mathf.Clamp01(normalized));
                }
            }

            if (Input.GetKeyUp(KeyCode.G) && isHoldingThrow)
            {
                float normalizedForce = Mathf.Clamp01(holdTime / maxHoldTime);
                float throwForce = Mathf.Lerp(minThrowForce, maxThrowForce, normalizedForce);
                ThrowObject(throwForce);
                isHoldingThrow = false;
                hasShownIndicator = false;
                ShowThrowIndicator(false);
            }
        }
    }

    void TryPickupObject()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, interactLayer))
        {
            float distanceFromPlayer = Vector3.Distance(transform.position, hit.point);
            if (distanceFromPlayer <= interactDistance)
            {
                GameObject target = hit.collider.gameObject;
                if (target.CompareTag("Draggable"))
                {
                    heldObject = target;
                    heldRb = heldObject.GetComponent<Rigidbody>();

                    heldRb.useGravity = false;
                    heldRb.constraints = RigidbodyConstraints.FreezeRotation;
                    heldRb.isKinematic = true;

                    ClearHighlight();

                    Collider objCollider = heldObject.GetComponent<Collider>();
                    if (objCollider != null && playerCollider != null)
                    {
                        Physics.IgnoreCollision(objCollider, playerCollider, true);
                    }
                }
            }
        }
    }

    void ThrowObject(float force)
    {
        heldRb.useGravity = true;
        heldRb.isKinematic = false;
        heldRb.constraints = RigidbodyConstraints.None;
        heldRb.velocity = playerCamera.transform.forward * force;

        Collider objCollider = heldObject.GetComponent<Collider>();
        if (objCollider != null && playerCollider != null)
        {
            Physics.IgnoreCollision(objCollider, playerCollider, false);
        }

        heldObject = null;
        heldRb = null;
    }

    void UpdateHeldObjectPosition()
    {
        if (heldObject != null)
        {
            Vector3 holdOffset = playerCamera.transform.forward * holdDistance
                               + playerCamera.transform.right * 0.8f
                               - playerCamera.transform.up * 0.3f;

            Vector3 targetPos = playerCamera.transform.position + holdOffset;
            heldRb.MovePosition(Vector3.Lerp(heldObject.transform.position, targetPos, moveSpeed * Time.deltaTime));
        }
    }

    void UpdateHighlight()
    {
        if (heldObject != null)
        {
            ClearHighlight();
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, interactLayer))
        {
            float distanceFromPlayer = Vector3.Distance(transform.position, hit.point);
            if (distanceFromPlayer <= interactDistance)
            {
                GameObject hitObject = hit.collider.gameObject;

                if (hitObject.CompareTag("Draggable"))
                {
                    if (hitObject != lastHighlighted)
                    {
                        ClearHighlight();

                        Outline outline = hitObject.GetComponent<Outline>();
                        if (outline != null)
                        {
                            outline.enabled = true;
                            lastHighlighted = hitObject;
                            lastOutline = outline;
                        }
                    }
                    return;
                }
            }
        }

        ClearHighlight();
    }

    void ClearHighlight()
    {
        if (lastOutline != null)
        {
            lastOutline.enabled = false;
            lastOutline = null;
            lastHighlighted = null;
        }
    }

    void ShowThrowIndicator(bool show)
    {
        if (throwChargeIndicator != null)
        {
            throwChargeIndicator.gameObject.SetActive(show);
            if (!show)
                throwChargeIndicator.fillAmount = 0f;
        }
    }

    void UpdateThrowIndicator(float normalizedValue)
    {
        if (throwChargeIndicator != null)
        {
            throwChargeIndicator.fillAmount = normalizedValue;
        }
    }
}