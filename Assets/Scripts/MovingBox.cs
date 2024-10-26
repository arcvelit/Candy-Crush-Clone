using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingBox : MonoBehaviour
{

    float y_threshold;
    Rigidbody2D rigidBody;
    GridManager grid;
    bool moving;
    public int i, j;

    void Start()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        moving = true;
    }

    public void SetThreshold(int i, int j)
    {
        this.y_threshold = GridManager.UPPER_GRID_Y - (GridManager.GRID_SIZE - 1 - i) * GridManager.STRIDE;
        this.i = i;
        this.j = j;
    }

    public void SetNewThreshold(int i)
    {
        SetThreshold(i, this.j);
        Unfreeze();
    }

    public void Refer(GridManager grid)
    {
        this.grid = grid;
    }

    // Update is called once per frame
    void Update()
    {
        if (moving)
        {
            if (transform.position.y < y_threshold)
            {
                transform.position = new Vector3(transform.position.x, y_threshold, 0);
                Freeze();
            }
        }
    }

    void Freeze()
    {
        rigidBody.velocity = Vector2.zero;
        rigidBody.gravityScale = 0;
        moving = false;
    }

    void Unfreeze()
    {
        rigidBody.gravityScale = 1;
        moving = true;
    }

    void OnMouseDown()
    {
        StartCoroutine(grid.CandySelect(i, j));
    }
}
