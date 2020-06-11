using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class OcCalculate : MonoBehaviour
{
	// Start is called before the first frame update
	public ComputeShader m_OcCaluteCS;
	public Camera m_mainCamera;
	[Range(0,2)]
	public float m_DetailSize=0.1f;

	public DepthTexture m_DepthTexture;

	ComputeBuffer m_OcBundsBuffer;
	ComputeBuffer m_boundsIsVisableBuffer;
	ComputeBuffer m_TestBuffer;
	//kernel ID
	private int m_OcCaluteKernelID;

	// Shader Property ID's
	private static readonly int _InstanceDataBufferID = Shader.PropertyToID("_InstanceDataBuffer");
	private static readonly int _InputTextueID = Shader.PropertyToID("_Input");
	private static readonly int _InstanceTextureID = Shader.PropertyToID("_Result");
	private static readonly int _CamPosID = Shader.PropertyToID("_CamPosition");
	private static readonly int _IsVisableID = Shader.PropertyToID("_IsVisable");
	private static readonly int _MVPMatrixID = Shader.PropertyToID("_UNITY_MATRIX_MVP");
	private static readonly int _DetailSizeID = Shader.PropertyToID("_DetailSize");
	private static readonly int _GropuID = Shader.PropertyToID("_adfafdasdf");

	//Constants
	private const int THREAD_GROUP_SIZE = 32;
	private const int TEX_SIZE = 512;

	private RenderTexture m_OutPutTexture;
	private int[] m_IsVisableData;
	private int m_InstanceCount;
	private Matrix4x4 m_mvp;
	private bool m_isFirstFrame = true;

	private uint[] m_outputGroup;

	void Start()
	{

		m_OutPutTexture = new RenderTexture(TEX_SIZE,TEX_SIZE,24);
		m_OutPutTexture.enableRandomWrite = true;
		m_OutPutTexture.Create();
		m_outputGroup = new uint[1024*3];
		for (int i = 0; i < m_outputGroup.Length; i++)
		{
			m_outputGroup[i] = 0;
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
		if (InitEnvirmonent.m_boundsInput == null)
			return;

		m_InstanceCount = InitEnvirmonent.m_boundsInput.Count;
		Matrix4x4 viewPro = m_mainCamera.worldToCameraMatrix;
		Matrix4x4 project = m_mainCamera.projectionMatrix;
		m_mvp = project * viewPro;

		if (m_OcBundsBuffer == null)
		{
			int boundsSize = Marshal.SizeOf(typeof(InitEnvirmonent.BoundsInput));
			m_OcBundsBuffer = new ComputeBuffer(InitEnvirmonent.m_boundsInput.Count, boundsSize, ComputeBufferType.Default);
			m_boundsIsVisableBuffer = new ComputeBuffer(InitEnvirmonent.m_boundsIsVisable.Count, sizeof(uint), ComputeBufferType.Default);
			m_TestBuffer = new ComputeBuffer(m_outputGroup.Length,sizeof(uint),ComputeBufferType.Default);

		}
		if (TryGetKernels())
		{
			
			m_OcBundsBuffer.SetData(InitEnvirmonent.m_boundsInput);
			m_boundsIsVisableBuffer.SetData(InitEnvirmonent.m_boundsIsVisable);
			m_TestBuffer.SetData(m_outputGroup);

			m_OcCaluteCS.SetVector(_CamPosID,m_DepthTexture.CamPos);
			m_OcCaluteCS.SetBuffer(m_OcCaluteKernelID, _InstanceDataBufferID, m_OcBundsBuffer);
			m_OcCaluteCS.SetBuffer(m_OcCaluteKernelID,_IsVisableID,m_boundsIsVisableBuffer);
			m_OcCaluteCS.SetFloat(_DetailSizeID,m_DetailSize);
			m_OcCaluteCS.SetMatrix(_MVPMatrixID, m_mvp);
			m_OcCaluteCS.SetTexture(m_OcCaluteKernelID, _InputTextueID, m_DepthTexture.Texture);
			m_OcCaluteCS.SetTexture(m_OcCaluteKernelID,_InstanceTextureID, m_OutPutTexture);
			m_OcCaluteCS.SetBuffer(m_OcCaluteKernelID,_GropuID, m_TestBuffer);
			m_OcCaluteCS.Dispatch(m_OcCaluteKernelID, 8,8, 1);
		}
		if (m_IsVisableData == null)
		{
			m_IsVisableData = new int[m_InstanceCount];
			for (int i = 0; i < m_IsVisableData.Length; i++)
			{
				m_IsVisableData[i] = 1;
			}
		}
		m_boundsIsVisableBuffer.GetData(m_IsVisableData);
		m_TestBuffer.GetData(m_outputGroup);
		for (int i = 0; i < m_outputGroup.Length; i++)
		{
			if (m_outputGroup[i] != 0)
			{
				//Debug.Log(m_outputGroup[i]);
			}
		}


		for (int i = 0; i < m_IsVisableData.Length; i++)
		{
			if (m_IsVisableData[i] == 0)
			{
				InitEnvirmonent.m_gameObj[i].SetActive(false);
				//Debug.Log("Disable");
			}
			else
			{
				InitEnvirmonent.m_gameObj[i].SetActive(true);
				//Debug.Log("Enable");
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
			Debug.LogError(kernelName + " kernel not found in " + cs.name + "!");
			return false;
		}

		kernelID = cs.FindKernel(kernelName);
		return true;
	}


	//private void OnRenderImage(RenderTexture source, RenderTexture destination)
	//{
	//	Graphics.Blit(m_OutPutTexture, destination);
	//}
}
