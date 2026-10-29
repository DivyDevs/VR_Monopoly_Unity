using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Dice : MonoBehaviour
{
    [SerializeField] private float tolerance = 0.99f;
    [SerializeField] private float settledVelocity = 0.01f;

    private Rigidbody rb;
    private int diceIndex = -1;
    private bool hasStoppedRolling = false;
    private bool hasThrowDelayFinished = false;

    public static UnityAction<int, int> OnDiceResult;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }


    void Update()
    {
        if (!hasThrowDelayFinished) { return; }

        bool isSettled = rb.linearVelocity.sqrMagnitude <= settledVelocity * settledVelocity
            && rb.angularVelocity.sqrMagnitude <= settledVelocity * settledVelocity;

        if (!hasStoppedRolling && isSettled)
        {
            hasStoppedRolling = true;
            GetSideUp();
        }


    }

    private int GetSideUp()
    {
        Vector3[] sides = new Vector3[] {
            transform.forward.normalized,       //1
            transform.up.normalized,            //2
            -transform.right.normalized,        //3
            transform.right.normalized,         //4
            -transform.up.normalized,            //5
            -transform.forward.normalized      //6
        };

        for (int i = 0; i < sides.Length; i++)
        {
            if (Vector3.Dot(sides[i], Vector3.up) > tolerance)
            {
                OnDiceResult?.Invoke(diceIndex, i + 1);
                return i + 1;
            }

        }
        Debug.LogError("No side within tolerance");
        return 0;
    }

    internal void RollDice(float _throwForce, float _rollForce, int _diceIndex)
    {
        diceIndex = _diceIndex;
        float randomVariance = Random.Range(-1f, 1f);
        rb.AddForce(transform.forward * (_throwForce + randomVariance), ForceMode.Impulse);

        float rollX = Random.Range(0f, 1f);
        float rollY = Random.Range(0f, 1f);
        float rollZ = Random.Range(0f, 1f);

        rb.AddTorque(new Vector3(rollX, rollY, rollZ) * (_rollForce + randomVariance));

        StartCoroutine(ThrowDelay());
    }

    private IEnumerator ThrowDelay()
    {
        yield return new WaitForSeconds(1);
        hasThrowDelayFinished = true;
    }
}