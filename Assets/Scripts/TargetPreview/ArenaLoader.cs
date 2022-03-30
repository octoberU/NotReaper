using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class ArenaLoader : MonoBehaviour
{
    private string path = $"D:/Games/Steam/steamapps/common/Audica/Mods/Arenas/mistyforest.arena";

    [SerializeField] private Material swapMaterial;
    [SerializeField] private GameObject fakeSkybox;

    private void Awake()
    {
        fakeSkybox.SetActive(false);
        
    }
    private void Start()
    {
        //LoadArena(path);
    }


    public void LoadArena(string path)
    {
        StartCoroutine(DoLoadArena(path));
    }

    private IEnumerator DoLoadArena(string path)
    {
        var request = UnityWebRequestAssetBundle.GetAssetBundle(path);
        yield return request.SendWebRequest();
        if(request.result != UnityWebRequest.Result.Success)
        {
            Debug.Log($"Something went wrong while loading arena at path {path}");
            yield break;
        }
        AssetBundle asset = DownloadHandlerAssetBundle.GetContent(request);
        var scenes = asset.GetAllScenePaths();
        if(scenes != null && scenes.Length > 0)
        {
            var arenaPath = scenes[0];
            yield return SceneManager.LoadSceneAsync(arenaPath, LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByPath(arenaPath);
            var roots = scene.GetRootGameObjects();
            foreach(var go in roots)
            {
                foreach(var renderer in go.GetComponentsInChildren<Renderer>())
                {
                    if (renderer.GetComponent<ParticleSystem>() != null) continue;
                    renderer.material.shader = swapMaterial.shader;
                }
                if(go.GetComponentsInChildren<ReflectionProbe>() != null)
                {
                    fakeSkybox.SetActive(true);
                }
            }
        }

    }
}
