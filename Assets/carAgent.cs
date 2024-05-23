using System.Collections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class carAgent : Agent
{
    public Transform[] waypoints;
    public float speed = 10f;
    public float turnSpeed = 200f;
    private Rigidbody rb;
    private int currentWaypointIndex;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentWaypointIndex = 0;
    }

    public override void OnEpisodeBegin()
    {
        // Reset agent and environment at the beginning of an episode
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.localPosition = new Vector3(0, 0.5f, 0);  // Assuming starting position
        currentWaypointIndex = 0;

        // Optionally reset waypoints or other environment elements here
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Add the agent's velocity
        sensor.AddObservation(rb.velocity);

        // Add the direction to the next waypoint
        Vector3 directionToWaypoint = waypoints[currentWaypointIndex].position - transform.position;
        sensor.AddObservation(directionToWaypoint.normalized);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Actions, size = 2
        float forwardAmount = actions.ContinuousActions[0];
        float turnAmount = actions.ContinuousActions[1];

        // Move the agent
        rb.AddForce(transform.forward * forwardAmount * speed);
        transform.Rotate(transform.up * turnAmount * turnSpeed * Time.deltaTime);

        // Reward the agent for moving towards the waypoint
        Vector3 directionToWaypoint = waypoints[currentWaypointIndex].position - transform.position;
        if (directionToWaypoint.magnitude < 1.0f)
        {
            SetReward(1.0f);
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("WL"))
        {
            SetReward(-1.0f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // For manual testing with keyboard controls
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Vertical");
        continuousActionsOut[1] = Input.GetAxis("Horizontal");
    }
}
