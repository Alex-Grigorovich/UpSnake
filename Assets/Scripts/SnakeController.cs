using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SnakeController : MonoBehaviour
{
    public GameObject segmentPrefab;
    public GameObject foodPrefab;

    public Sprite headUp, headDown, headLeft, headRight;
    public Sprite bodyVertical, bodyHorizontal;
    public Sprite turnUL, turnUR, turnDL, turnDR;
    public Sprite tailUp, tailDown, tailLeft, tailRight;

    private List<Transform> segments = new List<Transform>();
    private Vector2Int direction = Vector2Int.right;
    private Vector2Int lastDirection = Vector2Int.right;

    public float moveInterval = 0.2f;
    private float moveTimer;

    private Vector2Int gridPosition = new Vector2Int(10, 10);

    private Dictionary<Vector2Int, Vector2Int> turnPoints = new Dictionary<Vector2Int, Vector2Int>();

    private Transform currentFood;

    private List<Vector3> previousPositions = new List<Vector3>();

    private bool isGameOver = false;

    private void Start()
    {
        // Создание головы
        Transform segment = Instantiate(segmentPrefab, GridToWorld(gridPosition), Quaternion.identity).transform;
        segment.GetComponent<SpriteRenderer>().sprite = headRight;
        segment.tag = "Snake";  // Устанавливаем тег для головы
        segments.Add(segment);

        SpawnFood();
    }

    private void Update()
    {

        // Позволяем перезапуск при Game Over
        if (isGameOver)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

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

        // Проверка на еду
        if (currentFood != null && Vector2.Distance(GridToWorld(gridPosition), currentFood.position) < 0.1f)
        {
            Destroy(currentFood.gameObject);
            Grow();
            SpawnFood();
        }

        // Сохраняем поворот
        if (direction != lastDirection)
            turnPoints[prevPosition] = direction;

        previousPositions.Insert(0, segments[0].position);

        // Перемещение тела
        for (int i = segments.Count - 1; i > 0; i--)
        {
            segments[i].position = segments[i - 1].position;
        }

        segments[0].position = GridToWorld(gridPosition);

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
        Vector2Int foodPos = new Vector2Int(Random.Range(0, 20), Random.Range(0, 20));
        currentFood = Instantiate(foodPrefab, GridToWorld(foodPos), Quaternion.identity).transform;
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

    void GameOver()
    {
        isGameOver = true;
        Debug.Log("Game Over!"); // Можно заменить на UI сообщение
                                 // Если хочешь остановить игру полностью:
                                 // Time.timeScale = 0;

        Debug.Log("Game Over! Нажми R для перезапуска.");

    }

}
