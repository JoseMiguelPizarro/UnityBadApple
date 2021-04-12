using UnityEngine;
using UnityEngine.Video;

public class BadApple : MonoBehaviour
{
	VideoPlayer videoPlayer;
	public ComputeShader badAppleCompute;
	private RenderTexture rt;
	public int xResolution;
	public int yResolution;

	private CellData[] data;
	private ComputeBuffer dataBuffer;

	private int kernelHandler;
	private uint threadGroupX;
	private uint threadGroupY;

	public void OnEnable()
	{
		videoPlayer = GetComponent<VideoPlayer>();

		videoPlayer.prepareCompleted += Prepared;
		videoPlayer.sendFrameReadyEvents = true;
		videoPlayer.frameReady += FrameReady;
		videoPlayer.Prepare();

		kernelHandler = badAppleCompute.FindKernel("BadApple");
		badAppleCompute.GetKernelThreadGroupSizes(kernelHandler, out threadGroupX, out threadGroupY, out _);
	}

	private void FrameReady(VideoPlayer source, long frameIdx)
	{
		Graphics.Blit(source.texture, rt);

		Dispatch();
	}

	private void Prepared(VideoPlayer source)
	{
		rt = new RenderTexture(source.texture.width, source.texture.height, 0);

		xResolution = Mathf.CeilToInt((float)rt.width / threadGroupX);
		yResolution = Mathf.CeilToInt(((float)rt.height / threadGroupY));

		dataBuffer = new ComputeBuffer(xResolution * yResolution, sizeof(float) * 6);
		badAppleCompute.SetBuffer(kernelHandler, "result", dataBuffer);
		badAppleCompute.SetInt("widthCount", xResolution);
		badAppleCompute.SetTexture(kernelHandler, "source", rt);
		videoPlayer.Play();

		data = new CellData[xResolution * yResolution];
	}

	private void Dispatch()
	{
		badAppleCompute.Dispatch(kernelHandler, xResolution, yResolution, 1);
		dataBuffer.GetData(data);
	}


	private void OnDisable()
	{
		rt.Release();
		dataBuffer.Release();
	}

	private void OnDrawGizmos()
	{
		if (rt != null)
		{
			var offset = new Vector2(xResolution * 0.5f, yResolution * 0.5f);
			Gizmos.matrix = transform.localToWorldMatrix;
			foreach (var d in data)
			{
				Gizmos.color = d.color;
				Gizmos.DrawCube((Vector3)(d.position - offset), Vector3.one);
			}
		}
	}

	public struct CellData
	{
		public Vector2 position;
		public Color color;
	}
}
