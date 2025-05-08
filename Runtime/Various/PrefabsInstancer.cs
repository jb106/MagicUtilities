using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabsInstancer : MonoBehaviour
{
    public static bool Init;

    [SerializeField] private List<GameObject> _prefabs = new List<GameObject>();

    private void Awake()
    {
        if (Init)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        foreach (GameObject p in _prefabs)
            Instantiate(p, transform);

        Init = true;
    }
}
