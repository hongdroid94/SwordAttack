using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{    
    [SerializeField] float speed;    

    int rightValue;


    public void Init(int rightValue)
    {
        this.rightValue = rightValue;
        GetComponent<SpriteRenderer>().flipX = rightValue == -1;
    }

    void Start()
    {        
        Destroy(gameObject, 1f);
    }
   
    void Update()
    {
        transform.Translate(Vector3.right * rightValue * speed * Time.deltaTime);
    }
}
