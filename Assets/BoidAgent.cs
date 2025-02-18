using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public class BoidAgent : MonoBehaviour
{
    public UnityEngine.Vector2 position;
    public UnityEngine.Vector2 velocity;
    public UnityEngine.Vector2 acceleration;
    public GameObject thisBoid;
    public bool enableVisuals = false;

    [SerializeField]List<BoidAgent> boidNeighbours = new List<BoidAgent>();

    private void Awake()
    {
        thisBoid = this.gameObject;
        position = transform.position;
        velocity = new UnityEngine.Vector2(transform.right.x, transform.right.y);
    }

    private void Update()
    {

        float visionAngle = BoidManager._visionAngle;
        float halfVisionAngle = visionAngle / 2f;
        float visionConeThreshold = Mathf.Cos(halfVisionAngle * Mathf.Deg2Rad);

        boidNeighbours.Clear();
        Collider2D[] boidCols = Physics2D.OverlapCircleAll(transform.position, BoidManager._awarenessRadius);

        UnityEngine.Vector2 forward = velocity.normalized;

        foreach (var col in boidCols)
        {
            if (col == this.GetComponent<Collider2D>())
                continue;

            BoidAgent agent = col.GetComponent<BoidAgent>();
            if (agent == null)
                continue;

            UnityEngine.Vector2 toAgent = ((UnityEngine.Vector2)col.transform.position - (UnityEngine.Vector2)transform.position).normalized;

            float dot = UnityEngine.Vector2.Dot(forward, toAgent);
            if (dot >= visionConeThreshold)
            {
                boidNeighbours.Add(agent);
                if(enableVisuals) Debug.DrawLine(transform.position, col.transform.position, Color.green);
            }
        }

        //check rules
        AvoidEdges();
        UnityEngine.Vector2 alignment = Align();
        UnityEngine.Vector2 cohesion = Cohesion();
        UnityEngine.Vector2 seperate = Seperation();

        acceleration = new UnityEngine.Vector2(0, 0);
        acceleration += alignment * BoidManager._alignment * BoidManager._maxMoveSpeed;
        acceleration += cohesion * BoidManager._cohesian * BoidManager._maxMoveSpeed;
        acceleration += seperate * BoidManager._seperation * BoidManager._maxMoveSpeed;

        UnityEngine.Vector2 randomJitter = new UnityEngine.Vector2(Random.Range(-1f, 1f),Random.Range(-1f, 1f)) * BoidManager._randomness;
        UnityEngine.Vector2 driftForce = velocity.normalized * 0.1f;

        acceleration += randomJitter;
        acceleration += driftForce;

        position += velocity * Time.deltaTime;
        velocity += acceleration * Time.deltaTime;
        velocity = UnityEngine.Vector2.ClampMagnitude(velocity, BoidManager._maxMoveSpeed);

        GetComponent<Transform>().position = position;
        transform.right = velocity;
    }
    void AvoidEdges() 
    {
        if (position.x < -BoidManager._boundSize.x)
            position.x = BoidManager._boundSize.x;
        else if (position.x > BoidManager._boundSize.x)
            position.x = -BoidManager._boundSize.x;

        if (position.y > BoidManager._boundSize.y)
            position.y = -BoidManager._boundSize.y;
        else if (position.y < -BoidManager._boundSize.y)
            position.y = BoidManager._boundSize.y;
    }

    UnityEngine.Vector2 Align()
    {
        if (boidNeighbours.Count == 0)
            return UnityEngine.Vector2.zero;

        UnityEngine.Vector2 avgVelocity = UnityEngine.Vector2.zero;

        int count = 0;
        foreach (BoidAgent boid in boidNeighbours)
        {
            if (boid != null)
            {
                avgVelocity += boid.velocity;
                count++;
            }
        }

        if (count == 0)
            return UnityEngine.Vector2.zero;

        avgVelocity /= count;

        if (avgVelocity == UnityEngine.Vector2.zero)
            return UnityEngine.Vector2.zero;

        avgVelocity = avgVelocity.normalized * BoidManager._maxMoveSpeed;
        UnityEngine.Vector2 steeringForce = avgVelocity - velocity;

        return UnityEngine.Vector2.ClampMagnitude(steeringForce, BoidManager._maxTurnEffect);
    }
    UnityEngine.Vector2 Cohesion(){
        if (boidNeighbours.Count == 0)
            return UnityEngine.Vector2.zero;

        UnityEngine.Vector2 centerOfMass = UnityEngine.Vector2.zero;
        int validBoidCount = 0;

        foreach (BoidAgent boid in boidNeighbours)
        {
            if (boid != null)
            {
                centerOfMass += boid.position;
                validBoidCount++;
            }
        }

        if (validBoidCount == 0)
            return UnityEngine.Vector2.zero;

        centerOfMass /= validBoidCount;

        UnityEngine.Vector2 desiredVelocity = (centerOfMass - position).normalized * BoidManager._maxMoveSpeed;

        UnityEngine.Vector2 steeringForce = desiredVelocity - velocity;

        return UnityEngine.Vector2.ClampMagnitude(steeringForce, BoidManager._maxTurnEffect);
    }

    UnityEngine.Vector3 Seperation()
    {

        UnityEngine.Vector2 direction = UnityEngine.Vector2.zero;
        if (boidNeighbours.Count > 0)
        {
            foreach (BoidAgent boid in boidNeighbours)
            {
                if (boid != null)
                {
                    UnityEngine.Vector2 toBoid = transform.position - boid.transform.position;
                    float distance = toBoid.magnitude;
                    if (distance < BoidManager._awarenessRadius * BoidManager._avoidanceRadius)
                    {
                        direction += toBoid.normalized / distance;
                    }
                }
            }
        }
        return direction.normalized;
    }

    void OnDrawGizmos() {
        if(enableVisuals){
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, BoidManager._awarenessRadius);
            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(transform.position, BoidManager._avoidanceRadius);

        }
    }
}
