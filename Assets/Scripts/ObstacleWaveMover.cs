using UnityEngine;

public class ObstacleWaveMover : MonoBehaviour
{
    public float speed = 1f;

    void Update()
    {
        transform.position += Vector3.down * speed * Time.deltaTime;

        if (transform.position.y < -2f)
        {
            Destroy(gameObject); // Удаляем всю волну целиком
        }
    }
}
