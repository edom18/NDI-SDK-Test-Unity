using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NDISample
{
    public class Rotater : MonoBehaviour
    {
        private void Update()
        {
            transform.Rotate(Vector3.up, Space.World);
        }
    }
}