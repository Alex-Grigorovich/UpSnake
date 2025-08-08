using UnityEngine;
using UnityEngine.SceneManagement;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject obstaclePrefab; // Префаб препятствия
    public float spawnInterval = 2.0f; // Интервал появления рядов
    public float fallSpeed = 2.0f;     // Скорость падения
    public float spawnStep = 2.0f;     // Шаг между препятствиями по X

    private float timer = 0f;
    private SnakeController snakeController;
    private bool isGameOverHandled = false;
    private bool isGameOver = false; // Флаг окончания игры

    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded; // Автоперезапуск при перезагрузке сцены
        FindSnakeController();
    }

    void Update()
    {
        if (snakeController == null)
        {
            FindSnakeController();
            return;
        }

        // Проверяем, если игра окончена
        if (snakeController.IsGameOver)
        {
            if (!isGameOverHandled)
            {
                isGameOverHandled = true;
                isGameOver = true; // Отмечаем, что игра закончена
                StopFallingObstacles(); // Останавливаем все препятствия
                Debug.Log("Спавнер остановлен (игра окончена).");
            }
            return; // Выход, если игра окончена
        }

        isGameOverHandled = false;

        // Если игра не окончена, спавним препятствия
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

    // Метод для остановки всех падающих препятствий
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
            Debug.LogWarning("SnakeController не найден.");
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Сбросим флаг и снова найдём SnakeController
        timer = 0f;
        isGameOverHandled = false;
        isGameOver = false; // Сбрасываем флаг окончания игры
        FindSnakeController();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // Чистим подписку
    }
}
