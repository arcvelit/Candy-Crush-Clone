using UnityEngine;

public class Activator : MonoBehaviour
{
    //SpriteRenderer spriteRenderer = other.GetComponent<SpriteRenderer>();
    //spriteRenderer.enabled = false;     

    void OnTriggerEnter2D(Collider2D other)
    {
        Transform childTransform = other.transform.GetChild(0); // Get the first child (index 0)
        SpriteRenderer childSpriteRenderer = childTransform.GetComponent<SpriteRenderer>();
        childSpriteRenderer.enabled = true;  
    }

}