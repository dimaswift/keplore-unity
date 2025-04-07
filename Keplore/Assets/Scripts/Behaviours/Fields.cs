using ConsequenceCascade.Graphics;
using UnityEngine;
using UnityEngine.Serialization;

namespace ConsequenceCascade.Behaviours
{
    public class Fields : MonoBehaviour
    {
        [Header("Field Configuration")]  
        public int width = 256;  
        public int height = 256;
        public float massScale = 0;
        public float temperatureScale = 0;
        public float spiralTightness = 1.0f;
        public float precessionOffset = 1.0f;
        public float acceleration = 100;
        public float flipFrequency = 10;
        public float speedChangeFrequency = 1;
        [Range(0.0f, 1f)] public float density = 1f;
        public float particleSize = 1;
        public int iterations = 1;
        [Range(0.0f, 0.1f)] public float damping = 0.1f;
        [Header("Simulation Parameters")]
        public SimulationConfig[] layers;

        public int[] speeds;
        [Header("Visualization")]  
        public ComputeShader computeShader;

        public Material particleMaterial;
        public Mesh particleMesh;

        [Header("Debug")]
        public bool pauseSimulation = false;  
        
        // Internal variables  
        private ComputeBuffer[] fieldBuffers;
        private ParticleDrawer[] drawers;  
        
        private int initializeKernel;  
        private int updateKernel;
        private int threadGroupsX;  
        private int threadGroupsY;  
        private bool initialized = false;
        
        private float nextFlip;
        private float nextSpeedChange;
        private Camera cam;
        private int speedThresholdIndex;
        private int direction = 1;
        struct FieldCell  
        {
            public Vector2 position;
            public Vector2 previousPosition;
            public float siderealTime;
            public float precessionalTime;
            public float mass;
            public float temperature;
            public float balance;
        }  
        
        [System.Serializable]
        public class SimulationConfig
        {
            public float baseSemiMajorAxis;
            [Range(0.9f, 1.1f)] public float baseEccentricity;
            public float precessionalSpeed;
            public float siderealSpeed;
            public int iterations;
        }
        
        void OnEnable()  
        {  
            if (!initialized)  
            {  
                InitializeSimulation();  
                PrepareVisualization();  
                initialized = true;  
            }  
        }  
        
        void Start()  
        {  
            cam = Camera.main;
            if (!initialized)  
            {  
                InitializeSimulation();  
                PrepareVisualization();  
                initialized = true;  
            }  
        }  
        
        void Update()  
        {  
            if (!initialized) return;  
            
            if (!pauseSimulation)  
            {
                for (int i = 0; i < iterations; i++)
                {
                    UpdateSimulation();  
                }
              
            }  
            UpdateVisualization();

            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetAllFields();
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                pauseSimulation = !pauseSimulation;
            }
            
            if (Input.GetKeyDown(KeyCode.Return))
            {
                direction *= -1;
                //speedThresholdIndex = speeds.Length - 1;
            }

            if (Input.GetKeyDown(KeyCode.W))
            {
                if (speedThresholdIndex < speeds.Length - 1)
                {
                    speedThresholdIndex++;
                }
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                if (speedThresholdIndex > 0)
                {
                    speedThresholdIndex--;
                }
            }
        }  
        
        void InitializeSimulation()  
        {
            if (computeShader == null)  
            {  
                Debug.LogError("Compute shader is not assigned!");  
                return;  
            }  
            
            initializeKernel = computeShader.FindKernel("InitializeField");  
            updateKernel = computeShader.FindKernel("UpdateField");
            
            threadGroupsX = Mathf.CeilToInt(width / 8f);  
            threadGroupsY = Mathf.CeilToInt(height / 8f);  
            
            fieldBuffers = new ComputeBuffer[layers.Length];  
            
            for (int i = 0; i < layers.Length; i++)  
            {
                int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(FieldCell));  
                fieldBuffers[i] = new ComputeBuffer(width * height, stride);  

                FieldCell[] initialData = new FieldCell[width * height];  
                for (int j = 0; j < width * height; j++)  
                {  
                    initialData[j] = new FieldCell();  
                }  
                fieldBuffers[i].SetData(initialData);  

                computeShader.SetInt("width", width);  
                computeShader.SetInt("height", height);  
                computeShader.SetBuffer(initializeKernel, "currentField", fieldBuffers[i]);
             
                computeShader.Dispatch(initializeKernel, threadGroupsX, threadGroupsY, 1);  
            }  
        }  
        
        void PrepareVisualization()  
        {
            drawers = new ParticleDrawer[layers.Length];  
            
            for (int i = 0; i < layers.Length; i++)  
            {
                drawers[i] = new ParticleDrawer(particleMaterial, particleMesh, fieldBuffers[i]);
            }  
        }  
        
        void UpdateSimulation()  
        {
            for (int i = 0; i < layers.Length; i++)  
            {
                SetShaderParameters(i);  
                
                computeShader.SetBuffer(updateKernel, "currentField", fieldBuffers[i]);
                computeShader.Dispatch(updateKernel, threadGroupsX, threadGroupsY, 1);
                
                
            }

            if (Time.time > nextFlip)
            {
                nextFlip = Time.time + Random.Range(flipFrequency, flipFrequency * 2);
                layers[0].precessionalSpeed *= -1;
            }
        }  
        
        void UpdateVisualization()  
        {
            for (int i = 0; i < layers.Length; i++)  
            {  
                drawers[i].SetSize(particleSize);
                drawers[i].Draw();
            }  
        }  
        
        void SetShaderParameters(int layerIndex)  
        {
            computeShader.SetFloat("baseEccentricity", layers[layerIndex].baseEccentricity);  
            computeShader.SetFloat("baseSemiMajorAxis", layers[layerIndex].baseSemiMajorAxis);
            computeShader.SetFloat("siderealSpeed", layers[layerIndex].siderealSpeed);
            //layers[layerIndex].precessionalSpeed += Time.deltaTime * acceleration;
            computeShader.SetFloat("precessionalSpeed", speeds[speedThresholdIndex] * direction);
            computeShader.SetFloat("precessionOffset", precessionOffset);
            computeShader.SetFloat("deltaTime", Time.deltaTime);
            computeShader.SetFloat("iterations", layers[layerIndex].iterations);
            computeShader.SetFloat("density", density);
            computeShader.SetFloat("damping", damping);
            computeShader.SetFloat("temperatureScale", temperatureScale);
            computeShader.SetFloat("massScale", massScale);
            computeShader.SetFloat("spiralTightness", spiralTightness);
        }  
        
        public void ResetAllFields()  
        {  
            
            for (int i = 0; i < layers.Length; i++)
            {
                layers[i].precessionalSpeed = 0;
                computeShader.SetBuffer(initializeKernel, "currentField", fieldBuffers[i]);  
                computeShader.Dispatch(initializeKernel, threadGroupsX, threadGroupsY, 1);  
            }  
        }  
      
        void OnDisable()  
        {  
            CleanupBuffers();  
            initialized = false;  
        }  
        
        void OnDestroy()  
        {  
            CleanupBuffers();  
        }  
        
        void CleanupBuffers()  
        {
            if (fieldBuffers != null)  
            {  
                foreach (var buffer in fieldBuffers)  
                {  
                    if (buffer != null) buffer.Release();  
                }  
            }
        }  
    }
}
