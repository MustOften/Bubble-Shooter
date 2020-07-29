using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trajectory : MonoBehaviour
{
    public LineRenderer lineRendererLeft;
    public LineRenderer lineRendererRight;
    private BallPlay ballPlay;
    private void Awake()
    {
        var lineRenderers = GetComponentsInChildren<LineRenderer>();
        lineRendererLeft = lineRenderers[0];
        lineRendererRight = lineRenderers[1];
        ballPlay = GetComponent<BallPlay>();

    }
    private void Update()
    {
        if (ballPlay.isPlay)
            SetDirection();
    }

    private void SetDirection()
    {
        Ray2D ray = new Ray2D(ballPlay.gameObject.transform.position,- ballPlay.direction);
        if (ballPlay.showTrajectory&& ballPlay.distance > 0.25f * ballPlay.MaxDragDistance)
        {
            lineRendererLeft.gameObject.SetActive(true);
            lineRendererRight.gameObject.SetActive(true);
             lineRendererLeft.SetPosition(0,ray.origin);
             lineRendererRight.SetPosition(0,ray.origin);
             int layerMask = 1 << 9;
             layerMask = ~layerMask;
             RaycastHit2D[] hit = Physics2D.RaycastAll(ray.origin, ray.direction, Mathf.Infinity,layerMask);
             Vector2 LeftPoint = new Vector2(hit[0].point.x-ballPlay.distance,hit[0].point.y);
             Vector2 RightPoint = new Vector2(hit[0].point.x+ballPlay.distance,hit[0].point.y);
             lineRendererLeft.SetPosition(1, LeftPoint);
             lineRendererRight.SetPosition(1, RightPoint);
             if (hit[0].collider.CompareTag("Wall"))
             {
                 lineRendererLeft.positionCount = 3;
                 lineRendererRight.positionCount = 3;
                 RicochetLine(hit[0], -ballPlay.direction);
             }
             else
             {
                 lineRendererLeft.positionCount = 2;
                 lineRendererRight.positionCount = 2;
             }
        }
        else
        {
            lineRendererLeft.gameObject.SetActive(false);
            lineRendererRight.gameObject.SetActive(false);
        }
    }

    private void RicochetLine(RaycastHit2D lastHit, Vector3 directionIn)
    {
        var reflection = new Vector2 (-directionIn.x, directionIn.y);
        RaycastHit2D[] hit2 = Physics2D.RaycastAll(lastHit.point, reflection, Mathf.Infinity);
        Vector2 LeftPointRicochet = new Vector2(hit2[0].point.x-ballPlay.distance,hit2[0].point.y);
        Vector2 RightPointRicochet = new Vector2(hit2[0].point.x+ballPlay.distance,hit2[0].point.y);
        lineRendererLeft.SetPosition(2,LeftPointRicochet);
        lineRendererRight.SetPosition(2,RightPointRicochet);
    }
}
