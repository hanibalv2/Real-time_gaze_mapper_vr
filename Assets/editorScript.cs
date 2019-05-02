using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 Small cleanup messy unity editor script.
*/

public class editorScript : MonoBehaviour {
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Assets/Unload Assets")]
    static void UnloadAssets()
    {
        Resources.UnloadUnusedAssets();
    }
#endif
}
