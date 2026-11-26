using UnityEngine;

public sealed class RockSpawnPoint : MonoBehaviour
{
    [SerializeField] private GameObject rockPrefab;
    [SerializeField] private MineableRock currentRock;
    [SerializeField] private float respawnSeconds = 90f;

    private float timer;

    private void Update()
    {
        if (currentRock != null)
        {
            timer = 0f;
            return;
        }

        timer += Time.deltaTime;
        if (timer < respawnSeconds || rockPrefab == null)
        {
            return;
        }

        timer = 0f;
        GameObject created = Instantiate(rockPrefab, transform.position, transform.rotation);
        currentRock = created.GetComponent<MineableRock>();
    }
}
