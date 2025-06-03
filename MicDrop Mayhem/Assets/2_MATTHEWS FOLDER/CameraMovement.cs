using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Net;

public class CameraMovement : MonoBehaviour
{
    public GameObject player1;
    public GameObject player2;

    public float smoothSpeed = 5f;
    public float zoomFactor = 0.5f;
    public float minZoom = 10f;
    public float maxZoom = 25f;
    public float zoomSpeed = 5f;

    private float fixedY = 30f;
    private float baseZ = -66f;

    void Start()
    {
        fixedY = transform.position.y;
        //baseZ = transform.position.z; // e.g., -10
    }

    void Update()
    {
        CenterCamera();
    }

    void CenterCamera()
    {
        Vector3 midpoint = (player1.transform.position + player2.transform.position) / 2f;

        // Move on X only, keep Y and Z stable for now
        Vector3 horizontalTarget = new Vector3(midpoint.x, fixedY, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, horizontalTarget, smoothSpeed * Time.deltaTime);

        // Zoom with Z-position: push camera back as players move apart
        float distance = Vector3.Distance(player1.transform.position, player2.transform.position);
        float zoomZ = Mathf.Clamp(baseZ - (distance * zoomFactor), -maxZoom, -minZoom);

        Vector3 zoomTarget = new Vector3(transform.position.x, fixedY, zoomZ);
        transform.position = Vector3.Lerp(transform.position, zoomTarget, zoomSpeed * Time.deltaTime);
    }

    /*public GameObject player1;
    public GameObject player2;
    public Vector3 midPoint;
    public Camera thisCamera;
    public float smoothSpeed = 2f;
    public float fixedZ;
    public float zoomFactor;
    public float minZoom;
    public float maxZoom;
    public float zoomSpeed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        thisCamera = this.GetComponent<Camera>();
    }

    public void CenterCamera()
    {
        midPoint = (player1.transform.position + player2.transform.position) / 2f;
        thisCamera.transform.position = Vector3.Lerp(transform.position, new Vector3(midPoint.x, midPoint.y, fixedZ), smoothSpeed * Time.deltaTime);
        float distance = Vector3.Distance(player1.transform.position, player2.transform.position);
        float targetZoom = Mathf.Clamp(distance * zoomFactor, minZoom, maxZoom);
        thisCamera.orthographicSize = Mathf.Lerp(thisCamera.orthographicSize, targetZoom, zoomSpeed * Time.deltaTime);
    }

    // Update is called once per frame
    void Update()
    {
        CenterCamera();
    }*/


}
