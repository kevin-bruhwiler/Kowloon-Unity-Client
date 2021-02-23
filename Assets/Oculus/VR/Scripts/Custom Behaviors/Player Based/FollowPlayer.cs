using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    Transform player;
    float f_RotSpeed = 2.0f;
    float f_MoveSpeed = 1.0f;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(player.position - transform.position), f_RotSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, player.position) > 2)
        {
            transform.position += transform.forward * f_MoveSpeed * Time.deltaTime;
        }
    }
}
