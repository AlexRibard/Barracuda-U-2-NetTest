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
#else
    private Tensor m_Input;
    public Texture2D inputImage;
#endif


    public NNModel inputModel;
    public Material preprocessMaterial;
    public Material postprocessMaterial;

    public int inputResolutionY = 32;
    public int inputResolutionX = 32;
    
    void Start()
    {
        Application.targetFrameRate = 60;
		
        m_RuntimeModel = ModelLoader.Load(inputModel, false);
        m_Worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, m_RuntimeModel, false);


#if (WEBCAM)
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
#if (WEBCAM)
        var targetRT = RenderTexture.GetTemporary(inputResolutionX, inputResolutionY, 0);
        Graphics.Blit(m_WebcamTexture, targetRT, postprocessMaterial);

        Tensor input = new Tensor(targetRT, 3);
#endif
        m_Worker.Execute(input);
        Tensor result = m_Worker.PeekOutput("output");
        
        RenderTexture resultMask = new RenderTexture(inputResolutionX, inputResolutionY, 0);
        resultMask.enableRandomWrite = true;
        resultMask.Create();
        
        result.ToRenderTexture(resultMask);

        postprocessMaterial.mainTexture = resultMask;
#if (WEBCAM)
        preprocessMaterial.mainTexture = targetRT;
#endif
    }
}
