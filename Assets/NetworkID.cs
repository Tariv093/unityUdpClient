using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkID : MonoBehaviour
{
    [SerializeField]
    public string id;
    public Color color = new Color(0, 0, 0);
    public Vector3 pos = new Vector3(0, 0, 0);
    public bool myCube;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Renderer renderer = GetComponent<Renderer>();
        renderer.material.color = color;
        //if your cube, do not update pos
        if (myCube == false)
        {
            transform.position = pos;
        }
            //if not your cube, update pos
    }

  public bool getBool()
    {
        return myCube;
    }

  public void setBool(bool b)
    {
        myCube = b;
    }

}
