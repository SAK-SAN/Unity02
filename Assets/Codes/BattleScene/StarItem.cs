using UnityEngine;

public class StarItem : MonoBehaviour
{
    public float lifetime =3f;  //３秒で消える
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.name == "InhaleArea")
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();

            if(player != null && player.currentState == PlayerController.PlayerState.Inhaling)
            {
                player.Swallow();
                Destroy(gameObject);
            }
        }
    }
}
