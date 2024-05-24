using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors.Reflection;

public class RayAgent : Agent
{
    public float speed = 10f;
    public float rotationSpeed = 100f;
    private Rigidbody rb;
    private RayPerceptionSensorComponent3D raySensor;

    public float wallCollisionThreshold = 0.1f;
    private int wallCollisionCount = 0;

    private int lastWaypointIndex = -1;
    private int sameWaypointCount = 0;
    public int maxSameWaypointCount = 3; // Maximum aantal keren dat de agent dezelfde waypoint mag raken voordat hij strafpunten krijgt

    public GameObject[] waypoints;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("No Rigidbody component found on this GameObject");
        }

        raySensor = GetComponent<RayPerceptionSensorComponent3D>();
        if (raySensor == null)
        {
            Debug.LogError("No Ray Perception Sensor 3D component found on this GameObject");
        }
    }

    public override void OnEpisodeBegin()
    {
        // Reset de positie en rotatie van de agent
        transform.localPosition = new Vector3(0, 0.5f, 0);
        transform.localRotation = Quaternion.identity;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        wallCollisionCount = 0;
        lastWaypointIndex = -1;
        sameWaypointCount = 0;

        ReactivateWaypoints();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Voeg de observaties van de Ray Perception Sensor toe
        RayPerceptionInput rayInput = raySensor.GetRayPerceptionInput();
        var rayOutput = RayPerceptionSensor.Perceive(rayInput);
        foreach (var rayOutputResult in rayOutput.RayOutputs)
        {
            sensor.AddObservation(rayOutputResult.HitFraction); // Afstand tot het object
            sensor.AddObservation(rayOutputResult.HasHit ? 1.0f : 0.0f); // Of er een hit was
            sensor.AddObservation(rayOutputResult.HasHit ? rayOutputResult.HitTagIndex : -1); // De tag van het object
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Acties, grootte = 2 (vooruitbeweging, rotatie)
        float moveAction = actionBuffers.ContinuousActions[0];
        float rotationAction = actionBuffers.ContinuousActions[1];

        // Controleer of de agent achteruit rijdt
        if (moveAction < 0)
        {
            AddReward(-0.1f); // Geef strafpunten voor achteruit rijden
        }

        // Beweeg de agent vooruit
        rb.velocity = transform.forward * moveAction * speed * Time.deltaTime;

        // Roateer de agent
        transform.Rotate(Vector3.up * rotationAction * rotationSpeed * Time.deltaTime);

        // Controleer of de agent een muur raakt
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 1.0f);
        foreach (Collider col in hitColliders)
        {
            if (col.CompareTag("WL"))
            {
                Debug.Log("Muur geraakt");
                SetReward(-5f);
                EndEpisode();
            }
            if (col.CompareTag("WP"))
            {
                Debug.Log("WP geraakt");
                SetReward(+5f);
                col.gameObject.SetActive(false); // Deactiveer de waypoint
            }
            if (col.CompareTag("Finish"))
            {
                //SetReward(+2f);
                EndEpisode();
            }
        }

    }


    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Vertical"); // Vooruit en achteruit
        continuousActionsOut[1] = Input.GetAxis("Horizontal"); // Rotatie
    }

    private void ReactivateWaypoints()
    {
        Debug.Log("Reactivate");
        //GameObject[] waypoints = GameObject.FindGameObjectsWithTag("WP");
        foreach (GameObject wp in waypoints)
        {
            wp.SetActive(true);
        }
    }
}
