using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class LevelBuild : MonoBehaviour
{
    public BallPlay ballPlay;

    [SerializeField]
    private List<BallPlay> ballPlayList;

    [SerializeField]
    private int maxBalls = 10;
    
    public float tileSize = 0.45f;

    [HideInInspector]
    public float startX = 0;

    [HideInInspector]
    public  float startY = 0;

    private int levelRows;
    private int levelColumns;
    private int[,] TileColor;
    public int ballCounter=1;
    public static LevelBuild Instance { get; private set; }

    public int MaxRows = 17;
    public int MaxColumns;
    public BallPlay[,] BallsArray;
    public TMPro.TextMeshProUGUI ballCounterText;
    public TMPro.TextMeshProUGUI PointsText;
    public TMPro.TextMeshProUGUI RecordText;
    private int Points = 0;

    public GameObject WinningMenu;
    public GameObject LostMenu;

    private void Awake()
    {
        LostMenu.SetActive(false);
        WinningMenu.SetActive(false);
        CreateBallPlayerList();
        ReadLevel();
        Instance = this;
        PointsText.SetText(Points.ToString());
        RecordText.SetText(PlayerPrefs.GetInt("Score").ToString());
    }

    private void Update()
    {
        int firstRowCounter = 0;
        if (maxBalls-ballCounter!=0)
            ballCounterText.SetText((maxBalls - ballCounter).ToString());
        else
            ballCounterText.gameObject.SetActive(false);
        for (int j = 0; j < levelColumns; j++)
        {
            if (BallsArray[0, j].color != ballPlay.EmptyColor)
            {
                firstRowCounter++;
            }
        }

        if (firstRowCounter < 0.3f * MaxColumns)
        {
            foreach (var ball in BallsArray)
            {
                if (ball != null)
                {
                    ball.rb.isKinematic = false;
                    ball.tj.enabled = false;
                    ball.coll.isTrigger = true;
                }
            }

            WinningMenu.SetActive(true);
            LostMenu.SetActive(false);
            foreach (var ball in ballPlayList)
            {
                ball.isPlay = false;
            }

            if (PlayerPrefs.GetInt("Score") < Points)
            {
                PlayerPrefs.SetInt("Score", Points);
                RecordText.SetText(PlayerPrefs.GetInt("Score").ToString());
            }
        }
    }

    private void CreateBallPlayerList()
        {
            int k = 0;
            ballPlayList = new List<BallPlay>();
            while (k < maxBalls)
            {
                ballPlay.color = Random.Range (0, 4);
                ballPlay.spriteRenderer.sprite = ballPlay.colors[ballPlay.color];
                if (k == 0)
                {
                    var bp = Instantiate(ballPlay, new Vector3(0f,-7f), ballPlay.transform.rotation);
                    ballPlayList.Add(bp);
                }
                else if (k == 1)
                {
                    var bp = Instantiate(ballPlay, new Vector3(-2f,-8.5f), ballPlay.transform.rotation);
                    bp.gameObject.SetActive(true);
                    bp.isPlay = false;
                    ballPlayList.Add(bp);  
                } 
                else
                {
                    var bp = Instantiate(ballPlay, new Vector3(-2f,-8.5f), ballPlay.transform.rotation);
                    bp.gameObject.SetActive(false);
                    bp.isPlay = false;
                    ballPlayList.Add(bp);  
                } 
                k++;
            }

            foreach (var ball in ballPlayList)
            {
                ball.BallCollided += NextBall;
                ball.BallDestroyed += AddDestroyingPoints;
                ball.BallFall += AddFallPoints;
            }
            
        }
        
    void ReadLevel(){
            
            TextAsset textFile = Resources.Load ("Level") as TextAsset;
            string[] lines = textFile.text.Split (new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);//split by new line, return
            string[] nums = lines[0].Split(new[] { ',' });//split by ,
            levelRows=lines.Length;//number of rows
            levelColumns=nums.Length;//number of columns
            TileColor = new int[MaxRows, levelColumns];
            for (int i = 0; i < MaxRows; i++) {
                for (int j = 0; j < levelColumns; j++) {
                    TileColor[i,j] = -1;
                }
            }
            
            for (int i = 0; i < levelRows; i++) {
                string st = lines[i];
                nums = st.Split(new[] { ',' });
                for (int j = 0; j < levelColumns; j++) {
                    int val;
                    if (int.TryParse (nums[j], out val)){
                        TileColor[i,j] = val;
                    }
                }
            }
            BuildGrid();
    }

        void BuildGrid ()
        {
            
            MaxColumns = levelColumns;
            BallsArray = new BallPlay[MaxRows,MaxColumns];
            startX = (levelColumns * tileSize) * 0.5f;
            startX -= (tileSize * 0.5f + 0.1f);
            startY -= tileSize * 0.5f;
            for (int row = 0; row < levelRows; row++) {
                for (int column = 0; column < levelColumns; column++) {

                    var item = Instantiate (ballPlay) as BallPlay;
                    var ball = item.GetComponent<BallPlay>();
                    ball.color = TileColor[row, column];
                   
                    SetBallPosition(column, row, out var pos);
                    ball.transform.localPosition = pos;
                    
                    ball.spriteRenderer.sprite = ballPlay.colors[ball.color];
                    ball.transform.parent = gameObject.transform;
                    ball.row = row;
                    ball.column = column;
                    ball.gameObject.layer = 0;
                    ball.gameObject.tag = "GridBall";
                    ball.isPlay = false;
                    ball.coll.isTrigger = false;
                    BallsArray[row, column] = ball;
                }
            }

            for (int row = levelRows; row < MaxRows; row++)
            {
                for (int column = 0; column < levelColumns; column++)
                {
                    var item = Instantiate (ballPlay) as BallPlay;
                    var ball = item.GetComponent<BallPlay>();
                    SetBallPosition(column, row, out var pos);
                    ball.transform.localPosition = pos;
                    ball.color = ball.EmptyColor;
                    ball.spriteRenderer.sprite = ballPlay.colors[ball.color];
                    ball.transform.parent = gameObject.transform;
                    ball.row = row;
                    ball.column = column;
                    ball.gameObject.layer = 0;
                    ball.gameObject.SetActive(false);
                    ball.isPlay = false;
                    BallsArray[row, column] = ball;
                }
            }

            foreach (var ball in BallsArray)
            {
                ball.BallFall += AddFallPoints;
            }
        }
        public void SetBallPosition(int column, int row, out Vector3 ballTransform)
        {

            var ballPosition = new Vector3((column * tileSize) - startX, startY + (-row * tileSize), 0);

            if (row % 2 == 1)
            {
                ballPosition.x -= tileSize * 0.5f;
            }
            ballPosition.y += tileSize * 0.1f * row;
            ballTransform = ballPosition;
        }

        public void NextBall()
        {
            if (ballCounter < maxBalls)
            {
                ballPlayList[ballCounter].gameObject.transform.position = new Vector3(0,-7);
                if (ballCounter + 1 < maxBalls)
                    ballPlayList[ballCounter + 1].gameObject.SetActive(true);
                ballPlayList[ballCounter].gameObject.SetActive(true);
                ballPlayList[ballCounter].isPlay = true;
                ballCounter++;
            }
            else
            {
                LostMenu.SetActive(true);
            }
        }

        private void AddDestroyingPoints()
        {
            Points += 100;
            PointsText.SetText(Points.ToString());
        }

        private void AddFallPoints()
        {
            Points += 150;
            PointsText.SetText(Points.ToString());
        }
}
