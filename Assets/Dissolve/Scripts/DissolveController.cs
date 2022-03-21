using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NotReaper.UI;

public class DissolveController : MonoBehaviour
{
    public float dissolveAmount;
    public float dissolveSpeed;
    public bool isDissolving = false;
    [ColorUsage(true,true)]
    public Color inColor;
    [ColorUsage(true, true)]
    public Color outColor;
    [ColorUsage(true, true)]

    private Material mat;

    void Start()
    {
        mat = GetComponent<Image>().material;
    }

    // Update is called once per frame
    void Update()
    {
        if (isDissolving)
        {
            DissolveDiff(false);
        }

        if (!isDissolving)
        {
            DissolveDiff(true);
        }

        mat.SetFloat("_DissolveAmount", dissolveAmount);


    }
    public void DissolveDiff(bool direction)
    {
        if (direction) {
            DissolveIn();
        }
        else {
            DissolveOut();
        }
    }

    public void DissolveIn()
    {
        mat.SetColor("_DissolveColor", inColor);
        if (dissolveAmount < 1) { 
            dissolveAmount += Time.deltaTime * dissolveSpeed;
        }
    }

    public void DissolveOut()
    { 
        mat.SetColor("_DissolveColor", outColor);
        if (dissolveAmount > -0.1) {
            dissolveAmount -= Time.deltaTime * dissolveSpeed;
        }
    }
}
