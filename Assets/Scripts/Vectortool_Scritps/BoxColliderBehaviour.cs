using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;


public class BoxColliderBehaviour : MonoBehaviour
{

    
    public Material mat;
    private Renderer _r;
    private VectortoolScript VS;
    private Color _matcolor;
    private Color _fadecolor= new Color(0.103f,0.169f,0.207f);
    private void Start()
    {
        VS = GameObject.Find("Vectortoolhandler").GetComponent<VectortoolScript>();
        _r = GetComponent<MeshRenderer>();
        _r.material.CopyPropertiesFromMaterial(mat);
        VectortoolScript.hasMapColliderBoxes.Add(this, 1);

        // WARNING : start the update color function  random in 0-5 sec. can prevent lag but can also not work (update all at same time)
        InvokeRepeating("updateColor",Random.Range(0.0f,5.0f),0.5f);

        _r.enabled = false;
    }

    public void hitByRay()
    {
        VectortoolScript.hasMapColliderBoxes[this] += 1;
        VS.favcube.Add(VectortoolScript.hasMapColliderBoxes[this]);
        VS.favcube.Remove(VS.favcube.Min());
    }
    private void updateColor()
    {
        float normal = VectortoolScript.NormalizeLog(VectortoolScript.hasMapColliderBoxes[this]);
        if (VS.favcube.Contains(VectortoolScript.hasMapColliderBoxes[this]))// &&  normal>0.5f)
        {
            if (!_r.enabled)
            {
                _r.enabled = true;
            }
            //changeMatColor(VectortoolScript.NormalizeMinMax(VectortoolScript.hasMapColliderBoxes[this], 0.0f, (float)VS.getTriggerSum()));
            changeMatColor(normal);
        }
        else {
            if (_r.enabled)
            {
                _r.enabled = false;
            }
        }
    }

    private void changeMatColor(float colorGradient)
    {
        //Debug.Log(colorGradient);
        _r.material.color = Color.Lerp(Color.white, Color.yellow, colorGradient);
        _matcolor = _r.material.color;
        _matcolor.a = 0.3f;
        _r.material.color = _matcolor;
    }
}
