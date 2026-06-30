using UnityEngine;

public class ProjectileDirection : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private Rigidbody2D rb;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Mathf.Abs(rb.linearVelocityX) > 0.05f)
        {
            float direction = Mathf.Sign(rb.linearVelocityX);

            transform.localScale = new Vector3(direction * Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
        }
    }
}
