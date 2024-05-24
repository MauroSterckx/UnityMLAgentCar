using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class AgentML : Agent
{
    public float speed = 10f;
    public float rotationSpeed = 100f;
    public Transform[] waypoints;
    private Rigidbody rb;

    private int currentWaypointIndex = 0;
    private bool allWaypointsReached = false;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("No Rigidbody component found on this GameObject");
        }
    }

    public override void OnEpisodeBegin()
    {
        // Reset de positie en rotatie van de agent
        transform.localPosition = new Vector3(0, 0.5f, 0);
        transform.localRotation = Quaternion.identity;

        // Reset de huidige waypoint index
        currentWaypointIndex = 0;
        allWaypointsReached = false;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Voeg de afstand tot elk waypoint toe als observatie
        foreach (Transform waypoint in waypoints)
        {
            float distanceToWaypoint = Vector3.Distance(transform.localPosition, waypoint.position);
            sensor.AddObservation(distanceToWaypoint);
        }

        // Voeg de huidige rotatie van de agent toe als observatie
        sensor.AddObservation(transform.rotation);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Acties, grootte = 1 (rotatie)
        float rotationAction = actionBuffers.ContinuousActions[0];

        // Roateer de agent
        transform.Rotate(Vector3.up * rotationAction * rotationSpeed * Time.deltaTime);

        // Beweeg de agent vooruit
        rb.velocity = transform.forward * speed * Time.deltaTime;

        // Beloningen
        float reward = 0f;

        // Controleer of de agent een muur raakt
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 1.0f);
        foreach (Collider col in hitColliders)
        {
            if (col.CompareTag("WL"))
            {
                reward -= 0.5f; // Straf de agent als hij een muur raakt
                //Debug.Log("Muur geraakt");
                SetReward(-5f);
                EndEpisode();
            }
            if (col.CompareTag("Finish"))
            {
                //SetReward(+2f);
                EndEpisode();
            }
        }

        // Controleer of de agent een waypoint heeft bereikt
        if (Vector3.Distance(transform.localPosition, waypoints[currentWaypointIndex].position) < 3.0f)
        {
            reward += 1.0f; // Beloon de agent als hij een waypoint bereikt
            SetReward(+20f);
            //Debug.Log("Waypoint bereikt: " + currentWaypointIndex);
            if (currentWaypointIndex != 0)
            {
                Debug.Log(currentWaypointIndex);
            }

            // Ga naar het volgende waypoint
            currentWaypointIndex++;
            //Debug.Log("Nu naar " + currentWaypointIndex);

            // Controleer of alle waypoints zijn bereikt
            if (currentWaypointIndex >= waypoints.Length)
            {
                allWaypointsReached = true;
                //reward += 10.0f; // Grote beloning voor het bereiken van alle waypoints
                SetReward(+10.0f);
                EndEpisode();
                Debug.Log("Hit alle waypoints");
            }

            if (currentWaypointIndex >= 5)
            {
                allWaypointsReached = true;
                // Grote beloning voor het behalen van waypoints
                SetReward(+10.0f);
                Debug.Log("Hit 5");
            }

            if (currentWaypointIndex >= 4)
            {
                allWaypointsReached = true;
                // Grote beloning voor het behalen van 2 waypoints
                SetReward(+5.0f);
                Debug.Log("Hit 4");
            }
        }

        // Geef een negatieve beloning voor inefficiënt gedrag (bijvoorbeeld terugkeren naar een eerder waypoint)
        for (int i = 0; i < currentWaypointIndex; i++)
        {
            if (Vector3.Distance(transform.localPosition, waypoints[i].position) < 1.0f)
            {
                SetReward(-1.0f);
                break;
            }
        }
        // Geef de beloning aan de agent
        //SetReward(reward);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
    }
}
