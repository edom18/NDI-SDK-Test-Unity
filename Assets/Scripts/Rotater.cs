using UnityEngine;

namespace NDIPlugin
{
    public class Rotater : MonoBehaviour
    {
        private void Update()
        {
            Debug.Log("RRRRRRRRRRRRRRRRRRR");
            transform.Rotate(Vector3.up, Space.World);
        }
    }
}