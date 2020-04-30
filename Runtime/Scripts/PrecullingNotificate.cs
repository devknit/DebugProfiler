
using UnityEngine;

namespace DebugProfiler
{
    public class PrecullingNotificate : MonoBehaviour
    {
        void OnPreCull()
        {
            Framework.OnPreCulling();
        }
    }
}
