using UnityEngine;

public class FallingObstacle : MonoBehaviour
{
    public float fallSpeed = 2.0f; // �������� �������
    private bool isFalling = true; // ����, ����������� �� ��, ��������� �� �����������

    void Update()
    {
        if (isFalling)
        {
            transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

            // ���� ����������� ������� �� ������� ������, ���������� ���
            if (transform.position.y < -Camera.main.orthographicSize)
            {
                Destroy(gameObject);
            }
        }
    }

    // ����� ��� ��������� �������
    public void StopFalling()
    {
        isFalling = false;
    }
}
