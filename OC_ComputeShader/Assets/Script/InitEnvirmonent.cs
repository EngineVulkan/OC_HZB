using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitEnvirmonent : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject m_parent;
    public InstanceCount m_instanceCount;
    public GameObject m_testObj;

    public static List<BoundsInput> m_sBoundsInput;
    public static List<GameObject> m_sGameObj;
    public static List<int> m_sBoundsIsVisable;
    public struct BoundsInput
    {
        public Vector3 boundsCenter;
        public Vector3 boundsExtents;
    }
    public enum InstanceCount
    {
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096
    }
    void Start()
    {
        m_sBoundsInput = new List<BoundsInput>();
        m_sGameObj = new List<GameObject>();
        m_sBoundsIsVisable = new List<int>();

        TestInit();
        InitInstace();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void TestInit()
    {
        m_sGameObj.Add(m_testObj);
        Bounds b = CalculateBounds(m_testObj);
        m_sBoundsInput.Add(new BoundsInput
        {
            boundsCenter = b.center,
            boundsExtents = b.extents
        });
        m_sBoundsIsVisable.Add(1);
    }

    private void InitInstace()
    {
        GameObject sourceObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sourceObject.transform.parent = m_parent.transform;

        sourceObject.transform.position = new Vector3(0, 0, 0);

        Vector3 pos = new Vector3(0, 0, 0);
        int length = (int)Mathf.Sqrt((int)m_instanceCount);

        for (int x = 0; x < length; x++)
        {
            for (int y = 0; y < length; y++)
            {
                GameObject cloneObject = GameObject.Instantiate(sourceObject);
                pos.x = x * 2;
                pos.z = y * 2;
                cloneObject.transform.position = pos;
                cloneObject.transform.parent = m_parent.transform;

                Bounds boun = CalculateBounds(cloneObject);
                m_sGameObj.Add(cloneObject);
                m_sBoundsInput.Add(new BoundsInput
                {
                    boundsCenter = boun.center,
                    boundsExtents = boun.extents
                });
                m_sBoundsIsVisable.Add(1);
            }

        }

    }

    private Bounds CalculateBounds(GameObject _prefab)
    {
        GameObject obj = _prefab;

        Renderer[] rends = obj.GetComponentsInChildren<Renderer>();
        Bounds b = new Bounds();
        if (rends.Length > 0)
        {
            b = new Bounds(rends[0].bounds.center, rends[0].bounds.size);
            for (int r = 1; r < rends.Length; r++)
            {
                b.Encapsulate(rends[r].bounds);
            }
        }
        //b.center = Vector3.zero;
        //DestroyImmediate(obj);

        return b;
    }
}