using UnityEngine;
using UnityEngine.Events;

public class PhysicalDice : MonoBehaviour
{
    public static UnityAction<int> OnPhysicalDiceResult;

    private Rigidbody rb;
    public int currentFaceValue = 1;
    public bool isSettled = true;

    // Map local directions to standard dice faces (opposite sides add up to 7)
    private Vector3[] faceDirections = { Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
    private int[] faceValues = { 1, 6, 4, 3, 5, 2 };

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // If the dice is moving faster than a tiny threshold, it's rolling
        if (rb.linearVelocity.sqrMagnitude > 0.01f || rb.angularVelocity.sqrMagnitude > 0.01f)
        {
            isSettled = false;
        }
        else if (!isSettled)
        {
            isSettled = true;
            DetermineFace();
        }
    }

    void DetermineFace()
    {
        float highestDot = -1f;
        int topFaceIndex = 0;

        for (int i = 0; i < faceDirections.Length; i++)
        {
            // Convert the local direction (e.g., "up" relative to the tilted dice) into world space
            Vector3 worldDir = transform.TransformDirection(faceDirections[i]);
            float dot = Vector3.Dot(worldDir, Vector3.up);

            if (dot > highestDot)
            {
                highestDot = dot;
                topFaceIndex = i;
            }
        }

        currentFaceValue = faceValues[topFaceIndex];
        Debug.Log("Physical Roll Result: " + currentFaceValue);
        OnPhysicalDiceResult?.Invoke(currentFaceValue);
    }
}