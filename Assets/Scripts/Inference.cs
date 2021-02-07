#define WEBCAM

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;


public class Inference : MonoBehaviour
{
    private Model m_RuntimeModel;
    private IWorker m_Worker;
#if (WEBCAM)
    private WebCamTexture m_WebcamTexture;
    private Tensor input;
#else
    private Tensor m_Input;
    public Texture2D inputImage;
#endif
    private Tensor result;
    private RenderTexture targetRT;

    public NNModel inputModel;
    public Material preprocessMaterial;
    public Material postprocessMaterial;

    public int inputResolutionY = 32;
    public int inputResolutionX = 32;

    public void OnDestroy()
    {
        m_Worker?.Dispose();
        result.Dispose();
        result.FlushCache();
        input.Dispose();
        input.FlushCache();
    }
    void OnDisable()
    {
        m_Worker?.Dispose();
        result.Dispose();
        result.FlushCache();
        input.Dispose();
        input.FlushCache();
    }

    void Start()
    {
        Application.targetFrameRate = 60;
        m_RuntimeModel = ModelLoader.Load(inputModel, false);
        m_Worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, m_RuntimeModel, false);


#if (WEBCAM)
#if !UNITY_EDITOR
//Print this on device
        Debug.Log("Using webcam");
#endif
        m_WebcamTexture = new WebCamTexture();
        m_WebcamTexture.Play();
#else
        var targetRT = RenderTexture.GetTemporary(inputResolutionX, inputResolutionY, 0);
        Graphics.Blit(inputImage, targetRT, postprocessMaterial);
        m_Input = new Tensor(targetRT, 3);

        m_Input = new Tensor(1, inputResolutionY, inputResolutionX, 3);
#endif
    }

    void Update()
    {
        //m_Worker.Dispose();
        // result.Dispose();
#if (WEBCAM)
        targetRT = RenderTexture.GetTemporary(inputResolutionX, inputResolutionY, 0);
        Graphics.Blit(m_WebcamTexture, targetRT, postprocessMaterial);
        input = new Tensor(targetRT, 3);
        //input.shape = targetRT;
#endif
        m_Worker.Execute(input);
        result = m_Worker.PeekOutput("output");

        RenderTexture resultMask = new RenderTexture(inputResolutionX, inputResolutionY, 0);
        resultMask.enableRandomWrite = true;
        resultMask.Create();

        result.ToRenderTexture(resultMask);


        postprocessMaterial.mainTexture = resultMask;
#if (WEBCAM)
        preprocessMaterial.mainTexture = targetRT;
#endif
        result.Dispose();
        result.FlushCache();
        input.Dispose();
        input.FlushCache();
    }
}
