using UnityEngine;
using Unity.Sentis;

public class FireAi : MonoBehaviour
{
    public Camera targetCamera;       // 主攝影機
    public ModelAsset modelAsset;     // ONNX 模型
    private Model runtimeModel;
    private Worker worker;

    private RenderTexture renderTexture;
    private Texture2D captureTexture;
    void Start()
    {
        runtimeModel = ModelLoader.Load(modelAsset);
        worker = new Worker(runtimeModel, BackendType.GPUCompute);

        renderTexture = new RenderTexture(640, 640, 24);
        targetCamera.targetTexture = renderTexture;

        captureTexture = new Texture2D(640, 640, TextureFormat.RGB24, false);
    }

    void Update()
    {
        RenderTexture.active = renderTexture;
        captureTexture.ReadPixels(new Rect(0, 0, 640, 640), 0, 0);
        captureTexture.Apply();
        RenderTexture.active = null;

        Tensor<float> inputTensor = TextureConverter.ToTensor(captureTexture, 640, 640);

        worker.Schedule(inputTensor);
        Tensor<float> outputGPU = worker.PeekOutput() as Tensor<float>;
        Tensor<float> output = outputGPU.ReadbackAndClone();

        // 5. 判斷火焰
        bool fireDetected = false;
        int numBoxes = output.shape[2];
        for (int i = 0; i < numBoxes; i++)
        {
            float conf = output[0, 4, i];
            if (conf > 0.52f)
            {
                fireDetected = true;
                Debug.Log($"{conf}");
                break;
            }
        }

        if (fireDetected)
            Debug.Log("發現火焰，請勿靠近！");

        inputTensor.Dispose();
        output.Dispose();
    }

    private void OnDestroy()
    {
        worker.Dispose();
    }
}