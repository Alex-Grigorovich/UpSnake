using UnityEngine;

public class FallingObstacle : MonoBehaviour
{
    public float fallSpeed = 2.0f; // Скорость падения
    private bool isFalling = true; // Флаг, указывающий на то, двигается ли препятствие

    void Update()
    {
        if (isFalling)
        {
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

            // Если препятствие выходит за пределы экрана, уничтожаем его
            if (transform.position.y < -Camera.main.orthographicSize)
            {
                Destroy(gameObject);
            }
        }
    }

    // Метод для остановки падения
    public void StopFalling()
    {
        isFalling = false;
    }
}
