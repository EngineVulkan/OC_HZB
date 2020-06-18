using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

public class OcCalculate : MonoBehaviour
{
	// Start is called before the first frame update
	public ComputeShader m_OcCaluteCS;
	public DepthTexture m_depthTexture;
	public Camera m_MainCamera;
	[Range(0, 1)]
	public float m_DetailSize = 0.01f;

	// Shader Property ID's
	private static readonly int _ResutlID = Shader.PropertyToID("Result");
	private static readonly int _DepthTextureID = Shader.PropertyToID("_DepthTexture");
	private static readonly int _DepthID = Shader.PropertyToID("_Depth");
	private static readonly int _InstanceDataID = Shader.PropertyToID("_InstanceData");
	private static readonly int _MatrixVPID = Shader.PropertyToID("_UNITY_MATRIX_VP");
	private static readonly int _VisableID = Shader.PropertyToID("_IsVisable");
	private static readonly int _DetailSizeID = Shader.PropertyToID("_DetailSize");
	private static readonly int _TextureSize = Shader.PropertyToID("_HiZTextureSize");
	//
	private ComputeBuffer m_depthBuffer;
	private ComputeBuffer m_boundsBuffer;
	private ComputeBuffer m_visableBuffer;

	//kernel ID
	private int m_OcCaluteKernelID;

	//
	private float[] m_depthArr;
	private uint[] m_isVisableArr;

	private bool m_isFirstFrame = true;
	void Start()
	{
		m_depthArr = new float[16];
		for (int i = 0; i < m_depthArr.Length; i++)
		{
			m_depthArr[i] = 0.0f;
		}

	}

	// Update is called once per frame
	void Update()
	{
		if (m_isFirstFrame)
		{
			m_isFirstFrame = false;
			return;
		}

		if (m_depthTexture.Texture == null)
			return;

		Matrix4x4 view = m_MainCamera.worldToCameraMatrix;
		Matrix4x4 proj = m_MainCamera.projectionMatrix;
		Matrix4x4 vp = proj * view;

		if (m_depthBuffer == null)
		{
			int boundsSize = Marshal.SizeOf(typeof(InitEnvirmonent.BoundsInput));
			m_depthBuffer = new ComputeBuffer(m_depthArr.Length, sizeof(float), ComputeBufferType.Default);
			m_boundsBuffer = new ComputeBuffer(InitEnvirmonent.m_sBoundsInput.Count, boundsSize, ComputeBufferType.Default);
			m_visableBuffer = new ComputeBuffer(InitEnvirmonent.m_sBoundsIsVisable.Capacity, sizeof(uint), ComputeBufferType.Default);
		}

		if (TryGetKernels())
		{
			System.Diagnostics.Stopwatch beforeTime = new System.Diagnostics.Stopwatch();
			beforeTime.Start();
			m_boundsBuffer.SetData(InitEnvirmonent.m_sBoundsInput);
			m_visableBuffer.SetData(InitEnvirmonent.m_sBoundsIsVisable);
			m_OcCaluteCS.SetMatrix(_MatrixVPID, vp);
			m_OcCaluteCS.SetFloat(_DetailSizeID, m_DetailSize);
			m_OcCaluteCS.SetBuffer(m_OcCaluteKernelID, _InstanceDataID, m_boundsBuffer);
			m_OcCaluteCS.SetBuffer(m_OcCaluteKernelID, _DepthID, m_depthBuffer);
			m_OcCaluteCS.SetBuffer(m_OcCaluteKernelID, _VisableID, m_visableBuffer);
			m_OcCaluteCS.SetTexture(m_OcCaluteKernelID, _DepthTextureID, m_depthTexture.Texture);
			m_OcCaluteCS.SetVector(_DepthTextureID, m_depthTexture.TextureSize);
			m_OcCaluteCS.Dispatch(m_OcCaluteKernelID, 4, 1, 1);
			// m_OcCaluteCS.SetBuffer();
			beforeTime.Stop();
			// UnityEngine.Debug.Log(beforeTime.ElapsedMilliseconds);
		}
		if (m_isVisableArr == null)
		{
			m_isVisableArr = new uint[InitEnvirmonent.m_sBoundsInput.Count];
		}
		m_depthBuffer.GetData(m_depthArr);
		m_visableBuffer.GetData(m_isVisableArr);
		for (int i = 0; i < m_depthArr.Length; i++)
		{
			if (m_depthArr[i] != 0)
			{
				UnityEngine.Debug.Log(m_depthArr[i] + " " + i);
			}
		}

		for (int i = 0; i < m_isVisableArr.Length; i++)
		{
			if (m_isVisableArr[i] != 0)
			{
				InitEnvirmonent.m_sGameObj[i].SetActive(true);
			}
			else
			{
				InitEnvirmonent.m_sGameObj[i].SetActive(false);
			}
		}
	}

	private bool TryGetKernels()
	{
		return TryGetKernel("CSMain", ref m_OcCaluteCS, ref m_OcCaluteKernelID);
	}

	private static bool TryGetKernel(string kernelName, ref ComputeShader cs, ref int kernelID)
	{
		if (!cs.HasKernel(kernelName))
		{
			UnityEngine.Debug.LogError(kernelName + " kernel not found in " + cs.name + "!");
			return false;
		}

		kernelID = cs.FindKernel(kernelName);
		return true;
	}
}