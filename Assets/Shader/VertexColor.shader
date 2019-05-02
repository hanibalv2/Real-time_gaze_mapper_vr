// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'
Shader "Custom/VertexColor" {
     SubShader {
     Pass {
         LOD 200
          
         CGPROGRAM
         #pragma vertex vert
         #pragma fragment frag
  
         struct VertexInput {
             float4 v : POSITION;
             float4 color: COLOR;
         };
		 struct colorStruct {
			 float4 color;
		 };
		 struct WeightStruct{
			float weight;
		 };
		
		StructuredBuffer<colorStruct> _hmColors;
		StructuredBuffer<WeightStruct> _hmWeights;
        float4  _topColor;
		float4  _midColor;  
		float4	_botColor;
         struct VertexOutput {
             float4 pos : SV_POSITION;
             float4 col : COLOR;
         };

         VertexOutput vert(VertexInput v, uint id : SV_VertexID) {
             VertexOutput o;
             o.pos = UnityObjectToClipPos(v.v);
			
			//"small comptueshader color gradient"
			// o.col = _hmColors[id].color;
			
			// regular color gradient
			if(_hmWeights[id].weight < 0.5f){
					o.col=lerp(_botColor,_midColor,_hmWeights[id].weight*2);
			}else{
					o.col=lerp(_midColor,_topColor,(_hmWeights[id].weight-0.5)*2);
			}
			return o;		
         }
          
         float4 frag(VertexOutput o) : COLOR {
             return o.col;
         }
  
         ENDCG
         }
     }
}
