using UnityEngine;

namespace NDIPlugin
{
    public class Rotater : MonoBehaviour
    {
        private void Update()
        {
            transform.Rotate(Vector3.up, Space.World);
        }
    }
}