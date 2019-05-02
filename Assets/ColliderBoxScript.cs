using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderBoxScript : MonoBehaviour
{
    [SerializeField] 
    private int _id = -1;

    public int Id
    {
        get => _id;
        set
        {
            if (_id == -1)
            {
                _id = value;
            }
        }
    }
}
