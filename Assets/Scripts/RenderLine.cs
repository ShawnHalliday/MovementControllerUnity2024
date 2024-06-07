using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderLine : MonoBehaviour
{
    private LineRenderer lineRenderer;
    public GameObject player;
    private PlayerController playerController;
    // Start is called before the first frame update
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        playerController = player.GetComponent<PlayerController>();
        lineRenderer.positionCount = 2;
        lineRenderer.startColor = Color.white;
        lineRenderer.endColor = Color.black;

    }

    // Update is called once per frame
    void Update()
    {
        if (playerController.isGrappled)
        {
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, player.transform.position);
            lineRenderer.SetPosition(1, playerController.grapplePos);

        }
        else
        {
            lineRenderer.enabled = false;
        }
    }
}
