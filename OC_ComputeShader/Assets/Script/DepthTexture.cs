using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthTexture : MonoBehaviour
{
	public Camera m_MainCamera;
	public Material m_GetDephtMat;

	public RenderTexture Texture { get { return m_DepthTexture; } }
	public Vector3 CamPos { get
	{
			return m_MainCamera.transform.position;
		}
	}

	private RenderTexture m_ColorTexture;
	private RenderTexture m_DepthTexture;
	private RenderTexture m_FinalDepthTexture;

	private readonly int TEX_SIZE = 512;
	// Start is called before the first frame update
	void Start()
	{
		m_ColorTexture = new RenderTexture(TEX_SIZE, TEX_SIZE,24);
		m_DepthTexture = new RenderTexture(TEX_SIZE, TEX_SIZE, 24,RenderTextureFormat.Depth);
		//m_FinalDepthTexture = new RenderTexture(TEX_SIZE, TEX_SIZE, 24, RenderTextureFormat.Depth);
	}

	// Update is called once per frame
	void Update()
    {
		RenderBuffer colorBuffer = m_ColorTexture.colorBuffer;
		RenderBuffer depthBuffer = m_DepthTexture.depthBuffer;

		m_MainCamera.SetTargetBuffers(colorBuffer, depthBuffer);
		//Graphics.Blit(m_DepthTexture, m_FinalDepthTexture, m_GetDephtMat);
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		Graphics.Blit(m_ColorTexture, destination);
	}
}
