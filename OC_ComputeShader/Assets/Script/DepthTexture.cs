using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class DepthTexture : MonoBehaviour
{
    public Camera m_MainCamera;
    public Shader m_GenerateBufferShader = null;
    public RenderTexture Texture { get { return m_MipmapTexture; } }
	public Vector2 TextureSize { get { return m_textureSize; } }
    public Vector3 CamPos
    {
        get
        {
            return m_MainCamera.transform.position;
        }
    }

    private RenderTexture m_ColorTexture;
    private RenderTexture m_DepthTexture;
	private RenderTexture m_MipmapTexture;

    private CommandBuffer m_CommandBuffer;
    private int[] m_Temporaries;
    private Material m_generateBufferMaterial;
    private RenderTexture m_ShadowmapCopy;
    private CameraEvent m_CameraEvent = CameraEvent.AfterForwardOpaque;
	private CameraEvent m_lastCameraEvent = CameraEvent.AfterForwardOpaque;
	private Vector2 m_textureSize;

	private const int MAXIMUM_BUFFER_SIZE = 1024;
	private enum Pass
    {
        Blit,
        Reduce
    }

    private readonly int TEX_SIZE = 512;
    private int m_LODCount;
	// Start is called before the first frame update
	private void Awake()
	{
		m_generateBufferMaterial = new Material(m_GenerateBufferShader);
	}

	void Start()
    {
		int size = (int)Mathf.Max((float)m_MainCamera.pixelWidth, (float)m_MainCamera.pixelHeight);
		size = (int)Mathf.Min((float)Mathf.NextPowerOfTwo(size), (float)MAXIMUM_BUFFER_SIZE);
		m_textureSize.x = size;
		m_textureSize.y = size;

		
        m_ColorTexture = new RenderTexture(size, size, 24);
        m_DepthTexture = new RenderTexture(size, size, 24, RenderTextureFormat.Depth);
        m_DepthTexture.useMipMap = true;
        m_DepthTexture.autoGenerateMips = false;
        m_DepthTexture.filterMode = FilterMode.Point;
        m_DepthTexture.hideFlags = HideFlags.HideAndDontSave;
        //m_FinalDepthTexture = new RenderTexture(TEX_SIZE, TEX_SIZE, 24, RenderTextureFormat.Depth);
    }
	// Update is called once per frame
	void Update()
    {
        RenderBuffer colorBuffer = m_ColorTexture.colorBuffer;
        RenderBuffer depthBuffer = m_DepthTexture.depthBuffer;

        m_MainCamera.SetTargetBuffers(colorBuffer, depthBuffer);
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		Graphics.Blit(m_ColorTexture, destination);
	}

	private void OnDisable()
	{
		if (m_MainCamera != null)
		{
			if (m_CommandBuffer != null)
			{
				m_MainCamera.RemoveCommandBuffer(m_CameraEvent, m_CommandBuffer);
				m_CommandBuffer = null;
			}
		}
	}

	public void InitializeTexture()
	{
		if (m_MipmapTexture != null)
		{
			m_MipmapTexture.Release();
		}

		int size = (int)Mathf.Max((float)m_MainCamera.pixelWidth, (float)m_MainCamera.pixelHeight);
		size = (int)Mathf.Min((float)Mathf.NextPowerOfTwo(size), (float)MAXIMUM_BUFFER_SIZE);
		m_textureSize.x = size;
		m_textureSize.y = size;

		m_MipmapTexture = new RenderTexture(size, size, 0, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
		m_MipmapTexture.filterMode = FilterMode.Point;
		m_MipmapTexture.useMipMap = true;
		m_MipmapTexture.autoGenerateMips = false;
		m_MipmapTexture.Create();
		m_MipmapTexture.hideFlags = HideFlags.HideAndDontSave;
	}

	private void OnPreRender()
	{
		int size = (int)Mathf.Max((float)m_MainCamera.pixelWidth, (float)m_MainCamera.pixelHeight);
		size = (int)Mathf.Min((float)Mathf.NextPowerOfTwo(size), (float)MAXIMUM_BUFFER_SIZE);
		m_textureSize.x = size;
		m_textureSize.y = size;
		m_LODCount = (int)Mathf.Floor(Mathf.Log(size, 2f));

		if (m_LODCount == 0)
		{
			return;
		}

		bool isCommandBufferInvalid = false;
		if (m_MipmapTexture == null
			|| (m_MipmapTexture.width != size
			|| m_MipmapTexture.height != size)
			|| m_lastCameraEvent != m_CameraEvent
			)
		{
			InitializeTexture();

			m_lastCameraEvent = m_CameraEvent;
			isCommandBufferInvalid = true;
		}
		isCommandBufferInvalid = true;
		if (m_CommandBuffer == null || isCommandBufferInvalid == true)
		{
			m_Temporaries = new int[m_LODCount];

			if (m_CommandBuffer != null)
			{
				m_MainCamera.RemoveCommandBuffer(m_CameraEvent, m_CommandBuffer);
			}

			m_CommandBuffer = new CommandBuffer();
			m_CommandBuffer.name = "Hi-Z Buffer";
			//因为m_DepthTexture 跟生成的RT的TextureFormat不一样，在下面CopyTexture 的时候会出现错误
		    //
			RenderTargetIdentifier id = new RenderTargetIdentifier(m_MipmapTexture);
			m_CommandBuffer.SetGlobalTexture("_LightTexture", m_ShadowmapCopy);
			m_CommandBuffer.SetGlobalTexture("_DepthTexture", m_DepthTexture);
			m_CommandBuffer.Blit(null, id, m_generateBufferMaterial, (int)Pass.Blit);

			for (int i = 0; i < m_LODCount; ++i)
			{
				m_Temporaries[i] = Shader.PropertyToID("_09659d57_Temporaries" + i.ToString());
				size >>= 1;
				size = Mathf.Max(size, 1);

				m_CommandBuffer.GetTemporaryRT(m_Temporaries[i], size, size, 0, FilterMode.Point, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);

				if (i == 0)
				{
					m_CommandBuffer.Blit(id, m_Temporaries[0], m_generateBufferMaterial, (int)Pass.Reduce);
				}
				else
				{
					m_CommandBuffer.Blit(m_Temporaries[i - 1], m_Temporaries[i], m_generateBufferMaterial, (int)Pass.Reduce);
				}

				m_CommandBuffer.CopyTexture(m_Temporaries[i], 0, 0, id, 0, i + 1);

				if (i >= 1)
				{
					m_CommandBuffer.ReleaseTemporaryRT(m_Temporaries[i - 1]);
				}
			}

			m_CommandBuffer.ReleaseTemporaryRT(m_Temporaries[m_LODCount - 1]);
			m_MainCamera.AddCommandBuffer(m_CameraEvent, m_CommandBuffer);
		}
	}
}