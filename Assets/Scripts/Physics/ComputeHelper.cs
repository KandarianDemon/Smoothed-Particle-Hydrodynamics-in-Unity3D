using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEditor;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;
using Unity.VisualScripting;
using System;
using System.Linq;
namespace PhysicsComputeShaderUtility
{

    public class ComputeShaderData<T>{

        public readonly string[] bufferNames;
        public readonly string[] kernelNames;
        public readonly Dictionary<string, ComputeBuffer> buffers;
        public readonly Dictionary<string, string[]> kernelBufferMap;

        public ComputeShaderData(ComputeShader cs, T data)
        {
            buffers = ComputeHelper.LoopOverStructMembers<T>(data);
            bufferNames = buffers.Keys.ToArray();
            kernelNames = ComputeHelper.GetKernelNamesAsArray(cs).ToArray();
            kernelBufferMap = ComputeHelper.GenerateBufferKernelMap(cs);

        }

        public void LinkBuffersToKernels(ComputeShader cs)
        {

            // loop over dictionary.
            if(kernelBufferMap == null)
            {
                throw new System.Exception("kernelBufferMap has not been set!");
            }

            foreach(var entry in kernelBufferMap)
            {

                Debug.Log($"entry {entry}");
                foreach(var e in entry.Value){ Debug.Log($"value: {e}"); }
                int kernelID = cs.FindKernel(entry.Key);
                string[] kernelBuffers = entry.Value;
                Debug.Log($"Current Kernel {entry.Key}");
                for(int i = 0; i<kernelBuffers.Length;i++)
                {

                    try
                    {


                        cs.SetBuffer(kernelID, kernelBuffers[i], buffers[kernelBuffers[i]]);
                        Debug.Log($"----------- {kernelBuffers[i]} has been set");
                    }
                    catch(KeyNotFoundException){
                        
                    }
                }
            }


        }
    }
    public static class ComputeHelper
    {
        // ComputeHelper contains functions that help turn data into a compute shader

        // look into generics and classes
        // ParticleData -> buffers.

        // Need to return kernel Ids
        // void LinkKernels(List<string> kernelNames{})

        // void CreateComputeBuffers()

        //void Dispatch

       



        // ************ Get Kernel Names and kernel Indices **********************************************

         private static readonly Regex KernelBufferRegex = new Regex(
        @"^#pragma\s+kernel\s+(\w+)\s*//\s*<::(.+?)::>",
        RegexOptions.Multiline | RegexOptions.Compiled
         );

         public static Dictionary<string, string[]> ParseComputeShader(string shaderSource)
    {
        var result = new Dictionary<string, string[]>();

        foreach (Match match in KernelBufferRegex.Matches(shaderSource))
        {
            string kernelName = match.Groups[1].Value;
            string[] buffers = match.Groups[2].Value.Split(new[] { "::" }, StringSplitOptions.RemoveEmptyEntries);

            result[kernelName] = buffers;
        }

        return result;
    }

    public static void PrintKernelBuffers(Dictionary<string, string[]> kernelBuffers)
    {

            Debug.Log($"kernelBuffers Length: {kernelBuffers.Count}");
        foreach (var kernel in kernelBuffers)
        {
            Debug.Log($"Kernel: {kernel.Key}");
            Debug.Log("Buffers:");
            foreach (var buffer in kernel.Value)
            {
                Debug.Log($"  - {buffer}");
            }
            Debug.Log("");
        }
    }   

    public static Dictionary<string,string[]> GenerateBufferKernelMap(ComputeShader cs)
    {

            string assetPath = AssetDatabase.GetAssetPath(cs);
            string shaderSource = File.ReadAllText(assetPath);
            var map = ParseComputeShader(shaderSource);
            PrintKernelBuffers(map);

            return map;
    }




        
        public static List<string> GetKernelNames(ComputeShader cs)
        {
          List<string> kernelNames = new List<string>();

        // Get the asset path of the compute shader
        string assetPath = AssetDatabase.GetAssetPath(cs);

        // Read the contents of the HLSL file
        string shaderSource = File.ReadAllText(assetPath);

        // Use regex to find all kernel function declarations
        Regex kernelRegex = new Regex(@"#pragma\s+kernel\s+(\w+)");
        MatchCollection matches = kernelRegex.Matches(shaderSource);

        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                kernelNames.Add(match.Groups[1].Value);
            }
        }

        return kernelNames;
        }

        public static List<string> GetKernelBufferMap(ComputeShader cs, string pattern)
        {
          List<string> kernelNames = new List<string>();

        // Get the asset path of the compute shader
        string assetPath = AssetDatabase.GetAssetPath(cs);

        // Read the contents of the HLSL file
        string shaderSource = File.ReadAllText(assetPath);

        // Use regex to find all kernel function declarations
        //Regex kernelRegex = new Regex(@"#pragma\s+kernel\s+(\w+)");
        MatchCollection matches = Regex.Matches(shaderSource,pattern);
        Debug.Log($" Matches Length: {matches.Count}");

        foreach (Match match in matches)
        {
                Debug.Log($" Match: {match}");
           
                kernelNames.Add(match.Groups[1].Value);
            
        }

        return kernelNames;
        }

        public static string[] GetKernelNamesAsArray(ComputeShader cs)
        {

            return GetKernelNames(cs).ToArray();

        }

        public static string[] GetBufferKernelMapAsArray(ComputeShader cs, string pattern)
        {

            return GetKernelBufferMap(cs,pattern).ToArray();
        }
        public static Dictionary<string,int> KernelIndices(ComputeShader cs, List<string> kernelNames){

            Dictionary<string, int> indices = new Dictionary<string, int>();
            foreach(var i in kernelNames)
            {

                indices.Add(i,cs.FindKernel(i));

            }

            return indices;


        }

        public static Dictionary<string,int> KernelIndices(ComputeShader cs, string[] kernelNames){

            Dictionary<string, int> indices = new Dictionary<string, int>();
            foreach(var i in kernelNames)
            {

                indices.Add(i,cs.FindKernel(i));

            }

            return indices;


        }



        // ********* Buffer Creation ***************************************

       

        

          public static ComputeBuffer CreateStructuredBufferFromArray(Array data)
          {

            int count = data.Length;
            int stride = System.Runtime.InteropServices.Marshal.SizeOf(data.GetType().GetElementType());

            return new ComputeBuffer(count, stride);
          }

          public static Vector3Int GetThreadGroupSize(ComputeShader cs, int kernelIndex = 0)
          {

            uint x, y, z;
            cs.GetKernelThreadGroupSizes(kernelIndex, out x, out y, out z);

            return new Vector3Int((int)x, (int)y, (int)z);

          }

        public static Dictionary<string,ComputeBuffer> LoopOverStructMembers<T>(T data)
          {

            
            FieldInfo[] fi = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
           
            Dictionary<string,ComputeBuffer> buffers = new Dictionary<string, ComputeBuffer>();
            foreach( FieldInfo info in fi)
            {
               

                Array value = (Array)info.GetValue(data);
                
                if (value == null) continue;

                if(value is Array array)
                {
                    
                    buffers.Add(info.Name.ToUpper(),CreateStructuredBufferFromArray(array));
                }
               

              
            }

            return buffers;
          }


         // buffer creation in the case of particle Data I want to pass the particle data struct and turn the arrays into buffers.

         // arrayName -> "_" + "arrayName" + 

         // take in particle data, loop over the fields if possible. Maybe serialization before hand. Call buffer creation function
        public static void LinkBuffersToKernels(ComputeShader cs)
        {

        }
        
        // ************** DISPATCH ***************************
         public static void DispatchKernels(ComputeShader cs, string[] kernelHandles)
        {

            if(cs == null || kernelHandles == null)
            {
            throw new System.Exception("Compute shader is not set up properly. Kernels cannot be dispatched.");
            }

            for(int i = 0; i<kernelHandles.Length; i++)
            {

                int kernelHandle = cs.FindKernel(kernelHandles[i]);
                Vector3Int groupSize = GetThreadGroupSize(cs, kernelHandle);
                cs.Dispatch(kernelHandle, groupSize.x, groupSize.y, groupSize.z);

            }



        


        }
    
    
    }

        

       
          
}
