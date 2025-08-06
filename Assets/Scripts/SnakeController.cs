using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.Rendering.HableCurve;

public class SnakeController : MonoBehaviour
{
    public GameObject segmentPrefab;
    public GameObject foodPrefab;

    public Sprite headUp, headDown, headLeft, headRight;
    public Sprite bodyVertical, bodyHorizontal;
    public Sprite turnUL, turnUR, turnDL, turnDR;
    public Sprite tailUp, tailDown, tailLeft, tailRight;

    public int gridWidth = 20;
    public int gridHeight = 18;
    public float moveInterval = 0.2f;


    private List<Transform> segments = new List<Transform>();

    private Vector2Int direction = Vector2Int.right;
    private Vector2Int lastDirection = Vector2Int.right;

    private float moveTimer;

    private Vector2Int gridPosition = new Vector2Int(10, 10);
    private Dictionary<Vector2Int, Vector2Int> turnPoints = new Dictionary<Vector2Int, Vector2Int>();
    private Transform currentFood;
    private List<Vector3> previousPositions = new List<Vector3>();
    private bool isGameOver = false;



    public GameObject gameOverTextUI;
    public GameObject restartButtonUI;


    // UI
    public Text scoreTextUI;

    // Очки
    private int score = 0;

    public int uiOffset = 1;



    private GameObject topBoundary;
    private BoxCollider2D topCollider;



    private void Start()
    {

        topBoundary = GameObject.FindGameObjectWithTag("TopBoundary");
        if (topBoundary != null)
        {
            topCollider = topBoundary.GetComponent<BoxCollider2D>();
        }
        else
        {
            Debug.LogWarning("TopBoundary не найден в Start!");
        }

        gridPosition = new Vector2Int(gridWidth / 2, 2);

        if (gameOverTextUI != null)
            gameOverTextUI.SetActive(false); // Скрыть текст

        if (restartButtonUI != null)
            restartButtonUI.SetActive(false);

        // Создание головы
        Transform segment = Instantiate(segmentPrefab, GridToWorld(gridPosition), Quaternion.identity).transform;
        segment.GetComponent<SpriteRenderer>().sprite = headRight;
        segment.tag = "Snake";  // Устанавливаем тег для головы
        segments.Add(segment);

        UpdateSegmentTags();
        SpawnFood();

        score = 0;
        UpdateScoreUI();
    }

    private void Update()
    {

        // Позволяем перезапуск при Game Over
        if (isGameOver)
        {
           
            return; // Остальной код не выполняется при Game Over
        }

        if (segments.Count == 0) return;

        HandleInput();

        moveTimer += Time.deltaTime;
        if (moveTimer >= moveInterval)
        {
            Move();
            moveTimer = 0f;
        }


    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.W) && lastDirection != Vector2Int.down)
            direction = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.S) && lastDirection != Vector2Int.up)
            direction = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.A) && lastDirection != Vector2Int.right)
            direction = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.D) && lastDirection != Vector2Int.left)
            direction = Vector2Int.right;
    }

    void Move()
    {
        



        Vector2Int prevPosition = gridPosition;
        gridPosition += direction;

        Vector3 worldHeadPos = GridToWorld(gridPosition);


        if (topCollider != null)
        {
            Bounds topBounds = topCollider.bounds;

            // Получим мировые координаты предполагаемой новой позиции головы
            Vector3 nextWorldPosition = GridToWorld(gridPosition);

            // Если позиция головы в пределах TopBoundary по X и Y — Game Over
            if (topBounds.Contains(nextWorldPosition))
            {
                Debug.Log("Попытка пересечения TopBoundary! Game Over.");
                GameOver();
                return;
            }
        }



        CollisionObstacle();

        // === Проверка выхода за границы камеры ===
        Vector3 camMin = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 camMax = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, 0));

        if (worldHeadPos.x < camMin.x || worldHeadPos.x > camMax.x ||
            worldHeadPos.y < camMin.y || worldHeadPos.y > camMax.y )
        {
            Debug.Log("Змея вышла за границы камеры!");
            GameOver();
            return;
        }


        // === Проверка на еду ===
        if (currentFood != null && Vector2.Distance(worldHeadPos, currentFood.position) < 0.1f)
        {
            Destroy(currentFood.gameObject);
            Grow();
            AddScore(50); 
            SpawnFood();
        }

        if (direction != lastDirection)
            turnPoints[prevPosition] = direction;

        previousPositions.Insert(0, segments[0].position);

        // Перемещение тела
        for (int i = segments.Count - 1; i > 0; i--)
        {
            segments[i].position = segments[i - 1].position;
        }

        segments[0].position = worldHeadPos;

        CheckSelfCollision();
        UpdateSprites();
        lastDirection = direction;

        
    }

    void Grow()
    {
        // Создание нового сегмента на месте последнего
        Transform lastSegment = segments[segments.Count - 1];
        Transform newSegment = Instantiate(segmentPrefab, lastSegment.position, Quaternion.identity).transform;
        newSegment.tag = "Snake"; // Устанавливаем тег для нового сегмента

        // Добавляем компоненты Collider и Rigidbody
        // AddColliders(newSegment);

        segments.Add(newSegment);

        UpdateSegmentTags();
    }

    void SpawnFood()
    {
        BoxCollider2D foodArea = GameObject.Find("FoodArea")?.GetComponent<BoxCollider2D>();

        if (foodArea == null)
        {
            Debug.LogError("Не найден FoodArea с BoxCollider2D!");
            return;
        }

        Bounds bounds = foodArea.bounds;

        int minX = Mathf.CeilToInt(bounds.min.x);
        int maxX = Mathf.FloorToInt(bounds.max.x);
        int minY = Mathf.CeilToInt(bounds.min.y);
        int maxY = Mathf.FloorToInt(bounds.max.y);

        Vector2Int foodPos;

        int attempts = 100; // защита от бесконечного цикла
        do
        {
            foodPos = new Vector2Int(
                Random.Range(minX, maxX + 1), // Добавляем +1, чтобы границы включали maxX
                Random.Range(minY, maxY + 1)  // То же самое для maxY
            );
            attempts--;
        }
        while (IsPositionOccupied(foodPos) && attempts > 0);

        currentFood = Instantiate(foodPrefab, GridToWorld(foodPos), Quaternion.identity).transform;
    }

    bool IsPositionOccupied(Vector2Int pos)
    {
        foreach (Transform segment in segments)
        {
            if (GridToWorld(pos) == segment.position)
                return true;
        }
        return false;
    }

    void AddScore(int amount)
    {
        score += amount;
        UpdateScoreUI();
    }

    void UpdateSprites()
    {
        for (int i = 0; i < segments.Count; i++)
        {
            SpriteRenderer sr = segments[i].GetComponent<SpriteRenderer>();

            if (i == 0)
            {
                sr.sprite = direction switch
                {
                    var d when d == Vector2Int.up => headUp,
                    var d when d == Vector2Int.down => headDown,
                    var d when d == Vector2Int.left => headLeft,
                    var d when d == Vector2Int.right => headRight,
                    _ => sr.sprite
                };
            }
            else if (i == segments.Count - 1)
            {
                Vector2 tailDir = segments[i - 1].position - segments[i].position;
                sr.sprite = GetTailSprite(tailDir);
            }
            else
            {
                Vector2 dirFrom = segments[i + 1].position - segments[i].position;
                Vector2 dirTo = segments[i].position - segments[i - 1].position;
                sr.sprite = GetBodySprite(dirFrom, dirTo);
            }
        }
    }

    Sprite GetTailSprite(Vector2 dir)
    {
        if (dir == Vector2.up) return tailUp;
        if (dir == Vector2.down) return tailDown;
        if (dir == Vector2.left) return tailLeft;
        if (dir == Vector2.right) return tailRight;
        return null;
    }

    Sprite GetBodySprite(Vector2 from, Vector2 to)
    {
        if ((from == Vector2.up && to == Vector2.up) || (from == Vector2.down && to == Vector2.down))
            return bodyVertical;
        if ((from == Vector2.left && to == Vector2.left) || (from == Vector2.right && to == Vector2.right))
            return bodyHorizontal;

        if ((from == Vector2.up && to == Vector2.right) || (from == Vector2.left && to == Vector2.down))
            return turnUL;
        if ((from == Vector2.up && to == Vector2.left) || (from == Vector2.right && to == Vector2.down))
            return turnUR;
        if ((from == Vector2.down && to == Vector2.right) || (from == Vector2.left && to == Vector2.up))
            return turnDL;
        if ((from == Vector2.down && to == Vector2.left) || (from == Vector2.right && to == Vector2.up))
            return turnDR;

        return bodyHorizontal;
    }


    Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x, gridPos.y, 0);
    }


    void UpdateSegmentTags()
    {
        for (int i = 0; i < segments.Count; i++)
        {
            if (i == 0)
            {
                segments[i].tag = "Head";


                Rigidbody2D rb = segments[i].gameObject.GetComponent<Rigidbody2D>();
                if (rb == null)
                {
                    rb = segments[i].gameObject.AddComponent<Rigidbody2D>();
                }

                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.simulated = true;

                BoxCollider2D collider = segments[i].gameObject.GetComponent<BoxCollider2D>();
                if (collider == null)
                {
                    collider = segments[i].gameObject.AddComponent<BoxCollider2D>();
                }

                collider.isTrigger = true;

            }
            else
            {
                segments[i].tag = "Tail";


            }
        }
    }

    void CheckSelfCollision()
    {
        if (segments.Count <= 1) return;

        Vector3 headPos = segments[0].position;

        for (int i = 1; i < segments.Count; i++)
        {
            if (Vector3.Distance(headPos, segments[i].position) < 0.1f) // Порог на погрешность координат
            {

                GameOver();
                break;



            }
        }
    }

    void UpdateScoreUI()
    {
        if (scoreTextUI != null)
            scoreTextUI.text = "Score: " + score.ToString() + " Coins: 0";
    }

    void GameOver()
    {
        isGameOver = true;
        Debug.Log("Game Over!"); // Можно заменить на UI сообщение
                                 // Если хочешь остановить игру полностью:
                                 // Time.timeScale = 0;

        if (gameOverTextUI != null)
            gameOverTextUI.SetActive(true); // Показать текст

        if (restartButtonUI != null)
            restartButtonUI.SetActive(true);

        Debug.Log("Game Over! Нажми R для перезапуска.");

    }

    public void RestartGame()
    {
        if (gameOverTextUI != null)
            gameOverTextUI.SetActive(false); // Скрыть UI текст

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }


    private void CollisionObstacle()
    {
        foreach (GameObject obstacle in GameObject.FindGameObjectsWithTag("Obstacle"))
        {
            if (Vector3.Distance(segments[0].position, obstacle.transform.position) < 1f)
            {
                Debug.Log("Столкновение с препятствием");
                GameOver();
                break;
            }
        }

    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("TopBoundary"))
        {
            Debug.Log("Столкновение с TopBoundary через OnTriggerEnter2D!");
            GameOver();
        }
    }

}