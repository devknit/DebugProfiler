
using UnityEngine;

namespace DebugProfiler
{
	public class Meter : MonoBehaviour
	{
		void Awake()
		{
	        parameters = new float[ meters.Length];
	    }
	    void Update()
	    {
	        float sum = 0.0f;
	        
	        for( int i0 = 0; i0 < meters.Length; ++i0)
	        {
	            meters[ i0].sizeDelta = new Vector2( parameters[i0] * 200.0f , meters[ i0].sizeDelta.y);
	            meters[ i0].anchoredPosition = new Vector2( sum * 200.0f, meters[ i0].anchoredPosition.y);
	            sum += parameters[ i0];
	        }
	    }
	    public void SetParameter( int index , float param)
	    {
	        this.parameters[ index] = param;
	    }
	    
	    [SerializeField]
	    RectTransform[] meters = default;
	    
	    float[] parameters;
	}
}
