using UnityEngine;
using UnityEngine.SceneManagement;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject obstaclePrefab; // ������ �����������
    public float spawnInterval = 2.0f; // �������� ��������� �����
    public float fallSpeed = 2.0f;     // �������� �������
    public float spawnStep = 2.0f;     // ��� ����� ������������� �� X

    private float timer = 0f;
    private SnakeController snakeController;
    private bool isGameOverHandled = false;
    private bool isGameOver = false; // ���� ��������� ����

    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded; // �������������� ��� ������������ �����
        FindSnakeController();
    }

    void Update()
    {
        if (snakeController == null)
        {
            FindSnakeController();
            return;
        }

        // ���������, ���� ���� ��������
        if (snakeController.IsGameOver)
        {
            if (!isGameOverHandled)
            {
                isGameOverHandled = true;
                isGameOver = true; // ��������, ��� ���� ���������
                StopFallingObstacles(); // ������������� ��� �����������
                Debug.Log("������� ���������� (���� ��������).");
            }
            return; // �����, ���� ���� ��������
        }

        isGameOverHandled = false;

        // ���� ���� �� ��������, ������� �����������
        if (!isGameOver)
        {
            timer += Time.deltaTime;
            if (timer >= spawnInterval)
            {
                SpawnHorizontalObstacles();
                timer = 0f;
            }
        }
    }

    void SpawnHorizontalObstacles()
    {
        Vector3 screenLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 screenRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, 0));
        Vector3 screenTop = Camera.main.ViewportToWorldPoint(new Vector3(0, 1, 0));

        float ySpawn = Mathf.Ceil(screenTop.y) + 1f;

        float startX = Mathf.Floor(screenLeft.x) - 1f;
        float endX = Mathf.Ceil(screenRight.x) + 1f;

        for (float x = startX; x <= endX; x += spawnStep)
        {
            Vector3 spawnPos = new Vector3(x, ySpawn, 0f); // Z = 0
            GameObject obstacle = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity);

            if (!obstacle.TryGetComponent<FallingObstacle>(out _))
            {
                var mover = obstacle.AddComponent<FallingObstacle>();
                mover.fallSpeed = fallSpeed;
            }
        }
    }

    // ����� ��� ��������� ���� �������� �����������
    void StopFallingObstacles()
    {
        foreach (var fallingObstacle in FindObjectsOfType<FallingObstacle>())
        {
            fallingObstacle.StopFalling();
        }
    }

    void FindSnakeController()
    {
        snakeController = FindObjectOfType<SnakeController>();
        if (snakeController == null)
        {
            Debug.LogWarning("SnakeController �� ������.");
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ������� ���� � ����� ����� SnakeController
        timer = 0f;
        isGameOverHandled = false;
        isGameOver = false; // ���������� ���� ��������� ����
        FindSnakeController();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // ������ ��������
    }
}
