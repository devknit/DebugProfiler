
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Profiling;

namespace DebugProfiler
{
    public class Profiler : MonoBehaviour
    {
        void Awake()
        {
            if( Application.isPlaying != false)
            {
                Framework.Initialize();
            }
        }
        void Start()
        {
            SetCameraForPreCulling();
        }
        void SetCameraForPreCulling()
        {
		#if UNITY_ANDROID  && !UNITY_EDITOR
            if( SystemInfo.graphicsMultiThreaded == false)
            {
                return;
            }
            var cameras = Camera.allCameras;
            foreach( var camera in cameras)
            {
                if( camera.gameObject.GetComponent<PrecullingNotificate>() == false)
                {
                    camera.gameObject.AddComponent<PrecullingNotificate>();
                }
            }
		#endif 
        }
        void GotoNextSeconds( int seconds)
        {
            sumExecuteTime = 0.0f;
            minExecuteTime = float.MaxValue;
            maxExecuteTime = 0.0f; 
            sumCount = 0;
            currentStartSec = seconds;
        }
        void AppendExecuteTime( float time)
        {
            sumExecuteTime += time;
            minExecuteTime = Mathf.Min( time, minExecuteTime);
            maxExecuteTime = Mathf.Max( time, maxExecuteTime);
            ++sumCount;
        }
        void Update()
        {
            UpdateExpectedExecuteTime();
            UpdateMainThreadMeter();
		#if DEVELOPMENT_BUILD || UNITY_EDITOR
            UpdateRenderThreadMeter();
		#endif
            int seconds = (int)Time.realtimeSinceStartup;

            float totalTime = Framework.GetLastExecuteTime();
		#if UNITY_ANDROID && !UNITY_EDITOR
            if( SystemInfo.graphicsMultiThreaded != false)
            {
                totalTime -= Framework.GetGfxWaitForPresent();
            }
		#endif
            AppendExecuteTime( totalTime);
            
            if( currentStartSec != seconds)
            {
                stringBuilderBuffer.Length = 0;
                stringBuilderBuffer.Append( "FPS:").Append( sumCount);
                stringBuilderBuffer.Append( " (Avg:")
                    .AddMsecFromSec( sumExecuteTime / (float)sumCount)
                    .Append( "ms)\n");
                stringBuilderBuffer.Append( "min-max:")
                    .AddMsecFromSec( minExecuteTime)
                    .Append( "ms");
                stringBuilderBuffer.Append( " - ")
                    .AddMsecFromSec( maxExecuteTime)
                    .Append( "ms");
                frameRateText.text = stringBuilderBuffer.ToString();
                GotoNextSeconds( seconds);
            }
        }
        void UpdateExpectedExecuteTime()
        {
            float targetFrame = (float)Application.targetFrameRate;
            if( targetFrame <= 0.0f)
            {
                targetFrame = 60.0f;
            }
            expectedExecuteTime = 1.0f / targetFrame;
        }
        void UpdateMainThreadMeter()
        {
            float totalTime = Framework.GetLastExecuteTime();
            float scriptUpdateTime = Framework.GetProfilingTime<UnityEngine.PlayerLoop.Update.ScriptRunBehaviourUpdate>() +
                Framework.GetProfilingTime<UnityEngine.PlayerLoop.PreLateUpdate.ScriptRunBehaviourLateUpdate>() +
                Framework.GetProfilingTime<UnityEngine.PlayerLoop.FixedUpdate.ScriptRunBehaviourFixedUpdate>() +
                Framework.GetProfilingTime<UnityEngine.PlayerLoop.Update.ScriptRunDelayedDynamicFrameRate>();
            float animatorTime = Framework.GetProfilingTime<UnityEngine.PlayerLoop.PreLateUpdate.DirectorUpdateAnimationBegin>() +
                Framework.GetProfilingTime<UnityEngine.PlayerLoop.PreLateUpdate.DirectorUpdateAnimationEnd>();
            float renderTime = Framework.GetProfilingTime<UnityEngine.PlayerLoop.PostLateUpdate.FinishFrameRendering>();
            float physicsTime = Framework.GetProfilingTime<UnityEngine.PlayerLoop.FixedUpdate.PhysicsFixedUpdate>();
		#if UNITY_ANDROID && !UNITY_EDITOR
            if( SystemInfo.graphicsMultiThreaded != false)
            {
                float waitForGfxPresent = Framework.GetGfxWaitForPresent();
                renderTime -= waitForGfxPresent;
                totalTime -= waitForGfxPresent;
            }
		#endif
            float otherTime = totalTime - scriptUpdateTime - animatorTime - renderTime - physicsTime;

            mainThreadMeter.SetParameter( kMeterIndexScript, scriptUpdateTime / expectedExecuteTime);
            mainThreadMeter.SetParameter( kMeterIndexAnimator, animatorTime / expectedExecuteTime);
            mainThreadMeter.SetParameter( kMeterIndexRendeing, renderTime / expectedExecuteTime);
            mainThreadMeter.SetParameter( kMeterIndexPhysics, physicsTime / expectedExecuteTime);
            mainThreadMeter.SetParameter( kMeterIndexOther, otherTime / expectedExecuteTime);
        }
	#if DEVELOPMENT_BUILD || UNITY_EDITOR
        void UpdateRenderThreadMeter()
        {
            if( SystemInfo.graphicsMultiThreaded != false)
            {
	            if( recordCamerRender == null)
	            {
	                recordCamerRender = Recorder.Get( "Camera.Render");
	            }
	            float cameraRenderTime = recordCamerRender.elapsedNanoseconds * 0.000000001f;
	            float mainThreadTime = Framework.GetProfilingTime<UnityEngine.PlayerLoop.PostLateUpdate.FinishFrameRendering>();
			#if UNITY_ANDROID && !UNITY_EDITOR
	            mainThreadTime -= Framework.GetGfxWaitForPresent();
			#endif
	            float renderThreadTime = cameraRenderTime - mainThreadTime;
	            renderThreadMeter.SetParameter( 0, renderThreadTime / expectedExecuteTime);
	        }
        }
	#endif
	#if UNITY_EDITOR
		[UnityEditor.MenuItem("GameObject/UI/Debug Profiler/Screen Space - Overlay", false, 10000)]
		static void CreateWithOverlay()
		{
			Create( "bd87ac908f5b6654c81d1150ca3ed7e8");
		}
		[UnityEditor.MenuItem("GameObject/UI/Debug Profiler/Screen Space - Camera", false, 10000)]
		static void CreateWithCamera()
		{
			Create( "f6f8b34bcc23c6e4683884310e17b8a7");
		}
		static void Create( string guid)
		{
			string path = UnityEditor.AssetDatabase.GUIDToAssetPath( guid);
			if( string.IsNullOrEmpty( path) == false)
			{
				if( UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>( path) is GameObject prefab)
				{
					GameObject newGameObject = GameObject.Instantiate( prefab);
					newGameObject.name = "DebugProfiler";
					
					if( newGameObject.GetComponent<Profiler>() is Profiler profiler)
					{
						Font font = Resources.GetBuiltinResource<Font>( "Arial.ttf");
						profiler.frameRateText.font = font;
					}
				}
			}
		}
	#endif
		
		#pragma warning disable 0414
		const int kMeterIndexScript = 0;
        const int kMeterIndexPhysics = 1;
        const int kMeterIndexAnimator = 2;
        const int kMeterIndexRendeing = 3;
        const int kMeterIndexOther = 4;

		[SerializeField]
        Text frameRateText = default;
        [SerializeField]
        Meter mainThreadMeter = default;
        [SerializeField]
        Meter renderThreadMeter = default;

        StringBuilder stringBuilderBuffer = new StringBuilder();
        Recorder recordCamerRender;
        float expectedExecuteTime;
        float sumExecuteTime = 0.0f;
        float minExecuteTime = float.MaxValue;
        float maxExecuteTime = 0.0f;
        int sumCount = 0;
        int currentStartSec = 0;
    }
    public static class StringBuilderExtention
    {
        public static StringBuilder AddMsecFromSec( this StringBuilder builder, float time)
        {
            int div = 10000;
            int output = (int)(time * 1000.0f * div);

            builder.Append( output / div);
            builder.Append( ".");
            
            for( int i0 = 1; i0 < 4; ++i0)
            {
				div /= 10;
                builder.Append( output / div % 10);
            }
            return builder;
        }
    }
}
