using UnityEngine;

public class Movement : MonoBehaviour
{
    public Transform player;

    private void Update()
    {
        transform.position = player.transform.position;
    }
}