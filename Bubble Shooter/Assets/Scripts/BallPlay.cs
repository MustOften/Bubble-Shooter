using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.WSA;

[Serializable]

public class BallPlay : MonoBehaviour
{
    [SerializeField]
    private Sprite[] _colors;

    public Sprite[] colors
    {
        get { return _colors; }
        set { _colors = value; }
    }

    public Collider2D coll;
    public Rigidbody2D rb;
    public SpriteRenderer spriteRenderer;

    public TargetJoint2D tj;
    
    public float distance;
    public float MaxDragDistance;
    private bool isPressed = false;
    public Vector2 direction;
    [HideInInspector]
    public bool showTrajectory = false;
    
    //[HideInInspector]
    public bool readyForShoot=false;
    public float minDistance;
    
    public int row;
    public int column;
    public int color;

    public bool isPlay = true;
    public GameObject Pop;
    public int EmptyColor = 4;
    
    
    private void Awake()
    {
        coll = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        tj = GetComponent<TargetJoint2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        coll.isTrigger = false;
    }

    void FixedUpdate()
    {
        if (isPlay)
        {
            if (isPressed)
            {
                DragBall();
            }

            if (readyForShoot)
            {
                transform.position += new Vector3(4 * -direction.x * distance * Time.deltaTime,
                    -4 * direction.y * distance * Time.deltaTime - 9.8f * Mathf.Pow(Time.deltaTime, 2f) / 2f);
            }
        }
        CheckArray();
    }

    private void DragBall()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        mousePosition.y = Mathf.Clamp(mousePosition.y, tj.target.y - MaxDragDistance, tj.target.y - 0.2f);
        mousePosition.x= Mathf.Clamp(mousePosition.x, tj.target.x -MaxDragDistance,  tj.target.x + MaxDragDistance);

        if ((mousePosition - tj.target).magnitude <= MaxDragDistance)
        {
            rb.position = mousePosition;
        }
        else
        {
            rb.position = tj.target + direction * MaxDragDistance;
        }
        distance = Vector2.Distance(rb.position, tj.target);
        direction = (mousePosition - tj.target).normalized;
    }

    private void OnMouseDown()
    {
        isPressed = true;
        rb.isKinematic = true;
        showTrajectory = true;
    }

    private void OnMouseUp()
    {
        rb.isKinematic = false;
        isPressed = false;
        showTrajectory = false;
        if (distance > 0.25f * MaxDragDistance)
        {
            rb.isKinematic = true;
            tj.enabled = false;
            readyForShoot = true;
        }
    }

    public event System.Action BallCollided;
    public event System.Action BallDestroyed;
    public event System.Action BallFall;
    private void OnCollisionEnter2D(Collision2D other)
    {
        List<BallPlay> HandleBalls = new List<BallPlay>();
        if (isPlay)
        {
            if (other.collider.CompareTag("Wall"))
            {
                direction = new Vector2(-direction.x, direction.y);
            }

            if (other.collider.CompareTag("GridBall"))
            {
                List<BallPlay> SameColorBalls = new List<BallPlay>();
                minDistance = Vector2.Distance(transform.position, other.transform.position);

                BallPlay Left = null;
                BallPlay Right = null;
                BallPlay LeftUp = null;
                BallPlay LeftDown = null;
                BallPlay RightUp = null;
                BallPlay RightDown = null;
                DetectNeighbours(other.gameObject.GetComponent<BallPlay>(), ref Left, ref Right, ref LeftUp,
                    ref LeftDown, ref RightUp, ref RightDown);

                if (Math.Abs(distance - MaxDragDistance) < 0.05f)
                {
                    transform.position = other.gameObject.transform.position;
                    row = other.gameObject.GetComponent<BallPlay>().row;
                    column = other.gameObject.GetComponent<BallPlay>().column;
                    GameObject pop = Instantiate(Pop);
                    pop.transform.position = other.gameObject.transform.position;
                    Destroy(other.gameObject);
                    LevelBuild.Instance.BallsArray[row, column] = this;
                }
                else
                {
                    if (Left) FindingPlace(Left);
                    if (Right) FindingPlace(Right);
                    if (LeftUp) FindingPlace(LeftUp);
                    if (RightDown) FindingPlace(RightDown);
                    if (LeftDown) FindingPlace(LeftDown);
                    if (RightUp) FindingPlace(RightUp);
                }
                isPlay = false;
                readyForShoot = false;
                rb.isKinematic = false;
                this.tag = "GridBall";
                this.gameObject.layer = 0;
                tj.enabled = true;
               
                ClusterFinder(this, SameColorBalls);

                if (SameColorBalls.Count >= 3)
                {
                    foreach (var colorBall in SameColorBalls)
                    {
                        GameObject pop = Instantiate(Pop);
                        pop.transform.position = colorBall.transform.position;
                        colorBall.gameObject.SetActive(false);
                        colorBall.gameObject.layer = 9;
                        colorBall.color = EmptyColor;
                        colorBall.spriteRenderer.sprite = LevelBuild.Instance.ballPlay.colors[ colorBall.color];
                        BallDestroyed?.Invoke();
                    }
                } 
                BallCollided?.Invoke();
            }
            for (int column = 0; column < LevelBuild.Instance.MaxColumns; column++)
            {
                if (LevelBuild.Instance.BallsArray[0, column].color != EmptyColor)
                {
                    CheckFreeBalls(LevelBuild.Instance.BallsArray[0, column], HandleBalls);
                }
            }

            foreach (var ball in LevelBuild.Instance.BallsArray)
            {
                if (!HandleBalls.Contains(ball)&& ball.color!=EmptyColor)
                {
                    ball.gameObject.tag = "Untagged";
                    ball.rb.isKinematic = false;
                    ball.tj.enabled = false;
                    ball.coll.isTrigger = true;
                }
            }
            
            if (other.collider.CompareTag("Roof"))
            {
                Destroy(this.gameObject);
                BallCollided?.Invoke();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Floor"))
        {
            GameObject pop = Instantiate(Pop);
            pop.transform.position = this.gameObject.transform.position;
            BallFall?.Invoke();
            Destroy(this);
            Destroy(this.gameObject);
        } 
    }
    private void DetectNeighbours(BallPlay CenterBall, ref BallPlay Left, ref BallPlay Right, ref BallPlay LeftUp,ref BallPlay LeftDown,ref BallPlay RightUp,ref BallPlay RightDown)
    {
        if (CenterBall.row % 2 == 0)
        {
            LeftDown = LevelBuild.Instance.BallsArray[CenterBall.row + 1, CenterBall.column];
            if (CenterBall.row == 0)
            {
                RightUp = null;
                LeftUp = null;
            }

            if (CenterBall.column == 0)
            {
                Left = null;
            }
            else 
            { 
                Left = LevelBuild.Instance.BallsArray[CenterBall.row, CenterBall.column - 1];
                LeftDown = LevelBuild.Instance.BallsArray[CenterBall.row + 1, CenterBall.column];
                if (CenterBall.row != 0) 
                    LeftUp = LevelBuild.Instance.BallsArray[CenterBall.row - 1, CenterBall.column];
            }

            if (CenterBall.column == LevelBuild.Instance.MaxColumns - 1)
            {
                Right = null;
                RightUp = null;
                RightDown = null;
            }
            else
            {
                Right = LevelBuild.Instance.BallsArray[CenterBall.row, CenterBall.column + 1];
                RightDown = LevelBuild.Instance.BallsArray[CenterBall.row + 1, CenterBall.column + 1];
                if (CenterBall.row != 0) 
                    RightUp = LevelBuild.Instance.BallsArray[CenterBall.row - 1, CenterBall.column + 1];
            }
        }
        else if (CenterBall.row % 2 == 1)
        {
            
            RightUp = LevelBuild.Instance.BallsArray[CenterBall.row - 1, CenterBall.column];
            
            RightDown = LevelBuild.Instance.BallsArray[CenterBall.row + 1, CenterBall.column];
         
            if (CenterBall.column == 0)
            {
                Left = null;
                LeftUp = null;
                LeftDown = null;
            }
            else
            {
                Left = LevelBuild.Instance.BallsArray[CenterBall.row, CenterBall.column - 1];
                LeftUp = LevelBuild.Instance.BallsArray[CenterBall.row - 1, CenterBall.column - 1];
                LeftDown = LevelBuild.Instance.BallsArray[CenterBall.row + 1, CenterBall.column - 1];
               
            }
            if (CenterBall.column == LevelBuild.Instance.MaxColumns-1)
            {
                Right = null;
            }
            else
            {
                Right = LevelBuild.Instance.BallsArray[CenterBall.row, CenterBall.column + 1];
            }
        }
    }

    private void FindingPlace(BallPlay Neighbour)
    {
        if (Vector2.Distance(transform.position, Neighbour.transform.position) < minDistance)
        {
            if (Neighbour.color == EmptyColor)
            {
                minDistance = Mathf.Abs(Vector2.Distance(transform.position, Neighbour.transform.position));
                row = Neighbour.row;
                column = Neighbour.column;
                transform.position = Neighbour.transform.position;
                Destroy(Neighbour.gameObject);
                LevelBuild.Instance.BallsArray[row, column] = this;
            }
        }
    }

    private void ClusterFinder(BallPlay ballPlay, List<BallPlay> SameColorBalls)
    {
        Dictionary<string,BallPlay> Neighbours = new Dictionary<string, BallPlay>();
        SameColorBalls.Add(ballPlay);
        BallPlay Left = null;
        BallPlay Right= null;
        BallPlay LeftUp= null;
        BallPlay LeftDown= null;
        BallPlay RightUp= null;
        BallPlay RightDown= null;
        DetectNeighbours(ballPlay, ref Left, ref Right, ref LeftUp,ref LeftDown,ref RightUp,ref RightDown);
        if (Left) Neighbours.Add("Left", Left);
        if (Right) Neighbours.Add("Right", Right);
        if (LeftUp) Neighbours.Add("LeftUp", LeftUp);
        if (LeftDown) Neighbours.Add("LeftDown", LeftDown);
        if (RightUp) Neighbours.Add("RightUp", RightUp);
        if (RightDown) Neighbours.Add("RightDown", RightDown);
        foreach (var neighbour in Neighbours.Values)
        {
            if (neighbour != null)
            {
                if (ballPlay.color == neighbour.color && !SameColorBalls.Contains(neighbour)) 
                {
                    ClusterFinder(neighbour, SameColorBalls);
                }
            }
        }
    }

    private void CheckArray()
    {
        for (int row = 0; row < LevelBuild.Instance.MaxRows; row++)
        {
            for (int column = 0; column < LevelBuild.Instance.MaxColumns; column++)
            {
                if (LevelBuild.Instance.BallsArray[row, column] == null) 
                {
                    var item = Instantiate (LevelBuild.Instance.ballPlay) as BallPlay;
                    var ball = item.GetComponent<BallPlay>();
                    ball.color = EmptyColor;
                    ball.spriteRenderer.sprite = ball.colors[ball.color];
                    ball.row = row;
                    ball.column = column;
                    ball.gameObject.layer = 0;
                    ball.isPlay = false;
                    LevelBuild.Instance.BallsArray[row, column] = ball;
                    ball.gameObject.SetActive(false);
                }
            }
        }
    }

    private void CheckFreeBalls(BallPlay ballPlay, List<BallPlay> HandleBalls)
    {
        Dictionary<string, BallPlay> Neighbours = new Dictionary<string, BallPlay>();
        HandleBalls.Add(ballPlay);
        BallPlay Left = null;
        BallPlay Right = null;
        BallPlay LeftUp = null;
        BallPlay LeftDown = null;
        BallPlay RightUp = null;
        BallPlay RightDown = null;
        DetectNeighbours(ballPlay, ref Left, ref Right, ref LeftUp, ref LeftDown, ref RightUp, ref RightDown);
        if (Left) Neighbours.Add("Left", Left);
        if (Right) Neighbours.Add("Right", Right);
        if (LeftUp) Neighbours.Add("LeftUp", LeftUp);
        if (LeftDown) Neighbours.Add("LeftDown", LeftDown);
        if (RightUp) Neighbours.Add("RightUp", RightUp);
        if (RightDown) Neighbours.Add("RightDown", RightDown);
        foreach (var neighbour in Neighbours.Values)
        {
            if (neighbour != null && !HandleBalls.Contains(neighbour))
            {
                if (neighbour.color!=EmptyColor)
                    CheckFreeBalls(neighbour, HandleBalls);
            }
        }
    }
}
