using UnityEngine;
using System;

// Conditional compilation for IronPython imports
#if UNITY_EDITOR || UNITY_STANDALONE
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
#endif

namespace L5RGame
{
    public class IronPythonNuGetTest : MonoBehaviour
    {
        void Start()
        {
            TestIronPythonInstallation();
        }
        
        void TestIronPythonInstallation()
        {
            Debug.Log("🔍 Testing IronPython installation...");
            
#if UNITY_EDITOR || UNITY_STANDALONE
            try
            {
                // Try to create Python engine
                var engine = Python.CreateEngine();
                
                // Simple test script
                string testCode = @"
def hello_from_python():
    return 'Hello from IronPython via NuGet!'

result = hello_from_python()
";
                
                // Execute the test
                var scope = engine.CreateScope();
                engine.Execute(testCode, scope);
                
                // Get the result
                dynamic result = scope.GetVariable("result");
                
                Debug.Log($"✅ SUCCESS: {result}");
                Debug.Log("✅ IronPython is working correctly via NuGet!");
                
                // Test some basic Python functionality
                string mathTest = "result = 2 + 2";
                engine.Execute(mathTest, scope);
                dynamic mathResult = scope.GetVariable("result");
                Debug.Log($"✅ Python math test: 2 + 2 = {mathResult}");
                
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ IronPython test FAILED: {e.Message}");
                Debug.LogError($"Full error: {e}");
                
                // Give helpful advice
                Debug.LogError("💡 Try these solutions:");
                Debug.LogError("1. Check if NuGet installed correctly (NuGet menu visible?)");
                Debug.LogError("2. Verify IronPython package was installed");
                Debug.LogError("3. Check Project Settings → Player → Api Compatibility Level = .NET Standard 2.1");
                Debug.LogError("4. Try restarting Unity");
            }
#else
            Debug.LogWarning("⚠️ IronPython only works in Editor/Standalone builds, not on mobile");
#endif
        }
    }
}