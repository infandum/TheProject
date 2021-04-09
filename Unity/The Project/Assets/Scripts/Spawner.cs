using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

public class Spawner : MonoBehaviour
{

    [SerializeField]
    private GameObject _gameObjectEntity;

    [SerializeField] private GameObject _gameObjectDepre;

    static readonly ProfilerMarker s_EntityMarker = new ProfilerMarker("Spawner.Entity");

    static readonly ProfilerMarker s_ObjecMarker = new ProfilerMarker("Spawner.Obj");

    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Q))
        //{
        //    s_EntityMarker.Begin();
        //    en();
        //    s_EntityMarker.End();
        //}

        //if (Input.GetKeyDown(KeyCode.E))
        //{
        //    s_ObjecMarker.Begin();
        //    dep();
        //    s_ObjecMarker.End();

        //}
    }

    private void dep()
    {
        for (int i = 0; i < 100; i++)
        {
            Instantiate(_gameObjectDepre,
                new Vector3(UnityEngine.Random.Range(-5, 5f), UnityEngine.Random.Range(2f, 5f),
                    UnityEngine.Random.Range(-5, 5f)), Quaternion.identity);
        }
        
    }

    private void en()
    {
        for (int i = 0; i < 100; i++)
        {
            Instantiate(_gameObjectEntity,
                new Vector3(UnityEngine.Random.Range(-5, 5f), UnityEngine.Random.Range(2f, 5f),
                    UnityEngine.Random.Range(-5, 5f)), Quaternion.identity);
        }
    }
}
