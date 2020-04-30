
using UnityEngine;
using System.Collections.Generic;

namespace DebugProfiler
{
	internal static class Framework
    {
        public static void Initialize()
        {
            System.Type[] profilePoints =
            {
                /* script */
                typeof( UnityEngine.PlayerLoop.Update.ScriptRunBehaviourUpdate),
                typeof( UnityEngine.PlayerLoop.PreLateUpdate.ScriptRunBehaviourLateUpdate),
                typeof( UnityEngine.PlayerLoop.FixedUpdate.ScriptRunBehaviourFixedUpdate),
                /* script (Coroutine) */
                typeof( UnityEngine.PlayerLoop.Update.ScriptRunDelayedDynamicFrameRate),
                /* Animator */
                typeof(UnityEngine.PlayerLoop.PreLateUpdate.DirectorUpdateAnimationBegin),
                typeof(UnityEngine.PlayerLoop.PreLateUpdate.DirectorUpdateAnimationEnd),
                /* Renderer */
                typeof(UnityEngine.PlayerLoop.PostLateUpdate.UpdateAllRenderers),
                typeof(UnityEngine.PlayerLoop.PostLateUpdate.UpdateAllSkinnedMeshes),
                /* Rendering(require) */
                typeof(UnityEngine.PlayerLoop.PostLateUpdate.FinishFrameRendering),
                /* Physics */
                typeof( UnityEngine.PlayerLoop.FixedUpdate.PhysicsFixedUpdate),
            };
#if UNITY_2019_3_OR_NEWER
            var playerLoop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
#else
            var playerLoop = PlayerLoop.GetDefaultPlayerLoop();
#endif
            AppendProfilingLoopSystem( ref playerLoop, profilePoints);
            UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop( playerLoop);
        }
        public static float GetLastExecuteTime()
        {
            return prevLoopExecuteTime;
        }
        public static float GetGfxWaitForPresent()
        {
            return gfxWaitForPresentExecOnFinishRendering;
        }
        public static void OnPreCulling()
        {
            if( firstPreCullingPoint == 0.0f)
            {
                firstPreCullingPoint = Time.realtimeSinceStartup;
            }
        }
        public static float GetProfilingTime<T>()
        {
            return GetProfilingTime( typeof(T));
        }
        public static float GetProfilingTime( System.Type type)
        {
            float time = 0.0f;
            
            if( prevSubSystemExecuteTime.TryGetValue( type, out time) != false)
            {
                return time;
            }
            return 0.0f;
        }
        static void AppendProfilingLoopSystem( ref UnityEngine.LowLevel.PlayerLoopSystem playerLoop, System.Type[] profilePoints)
        {
            profilingSubSystem = new Dictionary<System.Type, ProfilingUpdate>();
            prevSubSystemExecuteTime = new Dictionary<System.Type, float>();
            
            for( int i0 = 0; i0 < profilePoints.Length; ++i0)
            {
                profilingSubSystem.Add( profilePoints[ i0], new ProfilingUpdate());
                prevSubSystemExecuteTime.Add(profilePoints[ i0], 0.0f);
            }
            System.Type finishRenderingType = typeof( UnityEngine.PlayerLoop.PostLateUpdate.FinishFrameRendering);
            if( profilingSubSystem.ContainsKey( finishRenderingType) == false)
            {
                profilingSubSystem.Add( finishRenderingType, new ProfilingUpdate());
                prevSubSystemExecuteTime.Add( finishRenderingType, 0.0f);
            }
            var newSystems = new List<UnityEngine.LowLevel.PlayerLoopSystem>();
            for( int i0 = 0; i0 < playerLoop.subSystemList.Length; ++i0)
            {
                var subSystem = playerLoop.subSystemList[ i0];
                newSystems.Clear();
                
                if( i0 == 0)
                {
                    newSystems.Add( new UnityEngine.LowLevel.PlayerLoopSystem
                    {
                        updateDelegate = Loop1stPoint
                    });
                }
                for( int i1 = 0; i1 < subSystem.subSystemList.Length; ++i1)
                {
                    var subsub = subSystem.subSystemList[ i1];
                    ProfilingUpdate updateObj;
                    
                    if( profilingSubSystem.TryGetValue( subsub.type, out updateObj) != false)
                    {
                        newSystems.Add( new UnityEngine.LowLevel.PlayerLoopSystem
                        {
                            type = typeof( ProfilingUpdate),
                            updateDelegate = updateObj.Start
                        });
                        newSystems.Add( subsub);
                        newSystems.Add( new UnityEngine.LowLevel.PlayerLoopSystem
                        {
                            type = typeof( ProfilingUpdate),
                            updateDelegate = updateObj.End
                        });
                    }
                    else
                    {
                        newSystems.Add( subsub);
                    }
                }
                if( i0 == playerLoop.subSystemList.Length - 1)
                {
                    newSystems.Add( new UnityEngine.LowLevel.PlayerLoopSystem
                    {
                        updateDelegate = LoopLastPoint
                    });
                }
                subSystem.subSystemList = newSystems.ToArray();
                playerLoop.subSystemList[ i0] = subSystem;
            }
        }
        static void Loop1stPoint()
        {
            loopStartTime = Time.realtimeSinceStartup;
        }
        static void LoopLastPoint()
        {
            var finishRenderProfiling = profilingSubSystem[typeof(UnityEngine.PlayerLoop.PostLateUpdate.FinishFrameRendering)];
            float endTime = finishRenderProfiling.GetEndTime();
            
            foreach( var kv in profilingSubSystem)
            {
                prevSubSystemExecuteTime[ kv.Key] = kv.Value.GetExecuteTime();
                kv.Value.Reset();
            }
            prevLoopExecuteTime = endTime - loopStartTime;

            if( firstPreCullingPoint != 0.0f)
            {
                float finishRenderingStart = finishRenderProfiling.GetStartTime();
                gfxWaitForPresentExecOnFinishRendering = firstPreCullingPoint - finishRenderingStart;
            }
            else
            {
                gfxWaitForPresentExecOnFinishRendering = 0.0f;
            }
            firstPreCullingPoint = 0.0f;
        }
        sealed class ProfilingUpdate
        {
            public void Start()
            {
                startTime = Time.realtimeSinceStartup;
            }
            public void End()
            {
                endTime = Time.realtimeSinceStartup;
                executeTime += endTime - startTime;
            }
            public void Reset()
            {
                executeTime = 0.0f;
            }
            public float GetExecuteTime()
            {
                return executeTime;
            }
            public float GetStartTime()
            {
                return startTime;
            }
            public float GetEndTime()
            {
                return endTime;
            }
            
            float startTime;
            float executeTime;
            float endTime;
        }
        
        static Dictionary<System.Type, ProfilingUpdate> profilingSubSystem;
        static float loopStartTime;
        static Dictionary<System.Type, float> prevSubSystemExecuteTime;
        static float prevLoopExecuteTime;
        static float gfxWaitForPresentExecOnFinishRendering;
        static float firstPreCullingPoint = 0.0f;
    }
}