Shader "SuperSystems/Wireframe-Transparent-Highlight"
{
    Properties
    {
        _WireThickness("Wire Thickness", RANGE(0, 800)) = 100
        _WireSmoothness("Wire Smoothness", RANGE(0, 20)) = 3
        _WireColor("Wire Color", Color) = (0.0, 1.0, 0.0, 1.0)
        _BaseColor("Base Color", Color) = (0.0, 0.0, 0.0, 0.0)
        _MaxTriSize("Max Tri Size", RANGE(0, 200)) = 25

[Header(Front color simple)]_ColorFront("_ColorFront",Color)=(1.0,0.0,0.0,1.0)
[Header(Front colors)] _Color0("_Color0",Color)=(1.0,0.0,0.0,1.0)
_Color1("_Color1",Color)=(1.0,0.0,0.0,1.0)
_Color2("_Color2",Color)=(1.0,0.0,0.0,1.0)
_Color3("_Color3",Color)=(1.0,0.0,0.0,1.0)
_Color4("_Color4",Color)=(1.0,0.0,0.0,1.0)
_Color5("_Color5",Color)=(1.0,0.0,0.0,1.0)
_Color6("_Color6",Color)=(1.0,0.0,0.0,1.0)
_Color7("_Color7",Color)=(1.0,0.0,0.0,1.0)
//_Color8("_Color8",Color)=(1.0,0.0,0.0,1.0)
// z
[Header(Back color simple)] _HColorBack("_HColorBack",Color)=(1.0,0.0,0.0,1.0)
[Header(Back colors)]_HColor0("_HColor0",Color)=(1.0,0.0,0.0,1.0)
_HColor1("_HColor1",Color)=(1.0,0.0,0.0,1.0)
_HColor2("_HColor2",Color)=(1.0,0.0,0.0,1.0)
_HColor3("_HColor3",Color)=(1.0,0.0,0.0,1.0)
_HColor4("_HColor4",Color)=(1.0,0.0,0.0,1.0)
_HColor5("_HColor5",Color)=(1.0,0.0,0.0,1.0)
_HColor6("_HColor6",Color)=(1.0,0.0,0.0,1.0)
_HColor7("_HColor7",Color)=(1.0,0.0,0.0,1.0)
//_HColor8("_HColor8",Color)=(1.0,0.0,0.0,1.0)

[Space][Enum(Simple,1,Colormaps,2)] _colorMode ("Color mode", int) = 1
[Header(Set Colormap Distance)]_colmapDis("Colormap distance", RANGE(0, 1)) = 1
[Header(Set Colormap Distance between Mapcolors)]_colmapDisCols("Distance between Colors", RANGE(0, 10)) = 1
    }

        SubShader
    {
        Tags {
            "IgnoreProjector" = "True"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }
// Behinde Wire
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite OFF
            Cull OFF
            ZTest OFF
			// Wireframe shader based on the the following
			// http://developer.download.nvidia.com/SDK/10/direct3d/Source/SolidWireframe/Doc/SolidWireframe.pdf

			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag

			#include "UnityCG.cginc"

			//#include "WireframeHighlight.cginc"
			uniform float _WireThickness = 100;
			uniform float _WireSmoothness = 3;
			uniform float4 _WireColor = float4(0.0, 1.0, 0.0, 1.0);
			uniform float4 _BaseColor = float4(0.0, 0.0, 0.0, 0.0);
			uniform float _MaxTriSize = 25.0;

			struct appdata
			{
				float4 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			struct v2g
			{
				float4 projectionSpaceVertex : SV_POSITION;
				float4 worldSpacePosition : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};
			struct g2f
			{
				float4 projectionSpaceVertex : SV_POSITION;
				float4 worldSpacePosition : TEXCOORD0;
				float4 dist : TEXCOORD1;
				float4 area : TEXCOORD2;
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2g vert(appdata v)
			{
				v2g o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.projectionSpaceVertex = UnityObjectToClipPos(v.vertex);
				o.worldSpacePosition = mul(unity_ObjectToWorld, v.vertex);
				return o;
			}
			[maxvertexcount(3)]
			void geom(triangle v2g i[3], inout TriangleStream<g2f> triangleStream)
			{
				float2 p0 = i[0].projectionSpaceVertex.xy / i[0].projectionSpaceVertex.w;
				float2 p1 = i[1].projectionSpaceVertex.xy / i[1].projectionSpaceVertex.w;
				float2 p2 = i[2].projectionSpaceVertex.xy / i[2].projectionSpaceVertex.w;

				float2 edge0 = p2 - p1;
				float2 edge1 = p2 - p0;
				float2 edge2 = p1 - p0;

				float4 worldEdge0 = i[0].worldSpacePosition - i[1].worldSpacePosition;
				float4 worldEdge1 = i[1].worldSpacePosition - i[2].worldSpacePosition;
				float4 worldEdge2 = i[0].worldSpacePosition - i[2].worldSpacePosition;

				// To find the distance to the opposite edge, we take the
				// formula for finding the area of a triangle Area = Base/2 * Height, 
				// and solve for the Height = (Area * 2)/Base.
				// We can get the area of a triangle by taking its cross product
				// divided by 2.  However we can avoid dividing our area/base by 2
				// since our cross product will already be double our area.
				float area = abs(edge1.x * edge2.y - edge1.y * edge2.x);
				float wireThickness = 800 - _WireThickness;

				g2f o;

				o.area = float4(0, 0, 0, 0);
				o.area.x = max(length(worldEdge0), max(length(worldEdge1), length(worldEdge2)));

				o.worldSpacePosition = i[0].worldSpacePosition;
				o.projectionSpaceVertex = i[0].projectionSpaceVertex;
				o.dist.xyz = float3((area / length(edge0)), 0.0, 0.0) * o.projectionSpaceVertex.w * wireThickness;
				o.dist.w = 1.0 / o.projectionSpaceVertex.w;
				UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(i[0], o);
				triangleStream.Append(o);

				o.worldSpacePosition = i[1].worldSpacePosition;
				o.projectionSpaceVertex = i[1].projectionSpaceVertex;
				o.dist.xyz = float3(0.0, (area / length(edge1)), 0.0) * o.projectionSpaceVertex.w * wireThickness;
				o.dist.w = 1.0 / o.projectionSpaceVertex.w;
				UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(i[1], o);
				triangleStream.Append(o);

				o.worldSpacePosition = i[2].worldSpacePosition;
				o.projectionSpaceVertex = i[2].projectionSpaceVertex;
				o.dist.xyz = float3(0.0, 0.0, (area / length(edge2))) * o.projectionSpaceVertex.w * wireThickness;
				o.dist.w = 1.0 / o.projectionSpaceVertex.w;
				UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(i[2], o);
				triangleStream.Append(o);
			}

float  _colorMode;
float4 _HColorBack;
fixed4 _HColor0;
fixed4 _HColor1;
fixed4 _HColor2;
fixed4 _HColor3;
fixed4 _HColor4;
fixed4 _HColor5;
fixed4 _HColor6;
fixed4 _HColor7;
float _colmapDis;
float _colmapDisCols;
			fixed4 frag(g2f i) : SV_Target
			{
				float minDistanceToEdge = min(i.dist[0], min(i.dist[1], i.dist[2])) * i.dist[3];

				// Early out if we know we are not on a line segment.
				if (minDistanceToEdge > 0.9 || i.area.x > _MaxTriSize)
				{
					return fixed4(_BaseColor.rgb,0);
				}

				// Smooth our line out
				float t = exp2(_WireSmoothness * -1.0 * minDistanceToEdge * minDistanceToEdge);
				//fixed4 finalColor = lerp(_BaseColor, _WireColor, t);
				const fixed4 colors[8] = {
										_HColor0,
										_HColor1,
										_HColor2,
										_HColor3,
										_HColor4,
										_HColor5,
										_HColor6,
										_HColor7
										};

				float cameraToVertexDistance = length(_WorldSpaceCameraPos - i.worldSpacePosition);
				cameraToVertexDistance = cameraToVertexDistance*_colmapDis;
				cameraToVertexDistance = cameraToVertexDistance / _colmapDisCols;


				int index = clamp(floor(cameraToVertexDistance), 0, 7);
				fixed4 wireColor = colors[index];

				fixed4 finalColor = lerp(float4(0,0,0,1), wireColor, t);

				if(_colorMode == 1)
				{
					finalColor = _HColorBack;
				}
				finalColor.a=wireColor.a;// = (t+finalColor.a)/2;//t;
				return finalColor;
			}
                            ENDCG
        }
        
// Front Wire
        Pass	
		{
                    Blend SrcAlpha OneMinusSrcAlpha

					CGPROGRAM
					#pragma vertex vert
					#pragma geometry geom
					#pragma fragment frag

					#include "UnityCG.cginc"

					//#include "WireframeHighlight.cginc"
					uniform float _WireThickness = 100;
					uniform float _WireSmoothness = 3;
					uniform float4 _WireColor = float4(0.0, 1.0, 0.0, 1.0);
					uniform float4 _BaseColor = float4(0.0, 0.0, 0.0, 0.0);
					uniform float _MaxTriSize = 25.0;

			struct appdata
			{
				float4 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			struct v2g
			{
				float4 projectionSpaceVertex : SV_POSITION;
				float4 worldSpacePosition : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};
			struct g2f
			{
				float4 projectionSpaceVertex : SV_POSITION;
				float4 worldSpacePosition : TEXCOORD0;
				float4 dist : TEXCOORD1;
				float4 area : TEXCOORD2;
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2g vert(appdata v)
			{
				v2g o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.projectionSpaceVertex = UnityObjectToClipPos(v.vertex);
				o.worldSpacePosition = mul(unity_ObjectToWorld, v.vertex);
				return o;
			}
			[maxvertexcount(3)]
			void geom(triangle v2g i[3], inout TriangleStream<g2f> triangleStream)
			{
				float2 p0 = i[0].projectionSpaceVertex.xy / i[0].projectionSpaceVertex.w;
				float2 p1 = i[1].projectionSpaceVertex.xy / i[1].projectionSpaceVertex.w;
				float2 p2 = i[2].projectionSpaceVertex.xy / i[2].projectionSpaceVertex.w;

				float2 edge0 = p2 - p1;
				float2 edge1 = p2 - p0;
				float2 edge2 = p1 - p0;

				float4 worldEdge0 = i[0].worldSpacePosition - i[1].worldSpacePosition;
				float4 worldEdge1 = i[1].worldSpacePosition - i[2].worldSpacePosition;
				float4 worldEdge2 = i[0].worldSpacePosition - i[2].worldSpacePosition;

				// To find the distance to the opposite edge, we take the
				// formula for finding the area of a triangle Area = Base/2 * Height, 
				// and solve for the Height = (Area * 2)/Base.
				// We can get the area of a triangle by taking its cross product
				// divided by 2.  However we can avoid dividing our area/base by 2
				// since our cross product will already be double our area.
				float area = abs(edge1.x * edge2.y - edge1.y * edge2.x);
				float wireThickness = 800 - _WireThickness;

				g2f o;

				o.area = float4(0, 0, 0, 0);
				o.area.x = max(length(worldEdge0), max(length(worldEdge1), length(worldEdge2)));

				o.worldSpacePosition = i[0].worldSpacePosition;
				o.projectionSpaceVertex = i[0].projectionSpaceVertex;
				o.dist.xyz = float3((area / length(edge0)), 0.0, 0.0) * o.projectionSpaceVertex.w * wireThickness;
				o.dist.w = 1.0 / o.projectionSpaceVertex.w;
				UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(i[0], o);
				triangleStream.Append(o);

				o.worldSpacePosition = i[1].worldSpacePosition;
				o.projectionSpaceVertex = i[1].projectionSpaceVertex;
				o.dist.xyz = float3(0.0, (area / length(edge1)), 0.0) * o.projectionSpaceVertex.w * wireThickness;
				o.dist.w = 1.0 / o.projectionSpaceVertex.w;
				UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(i[1], o);
				triangleStream.Append(o);

				o.worldSpacePosition = i[2].worldSpacePosition;
				o.projectionSpaceVertex = i[2].projectionSpaceVertex;
				o.dist.xyz = float3(0.0, 0.0, (area / length(edge2))) * o.projectionSpaceVertex.w * wireThickness;
				o.dist.w = 1.0 / o.projectionSpaceVertex.w;
				UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(i[2], o);
				triangleStream.Append(o);
			}

float  _colorMode;
float4 _ColorFront;
fixed4 _Color0;
fixed4 _Color1;
fixed4 _Color2;
fixed4 _Color3;
fixed4 _Color4;
fixed4 _Color5;
fixed4 _Color6;
fixed4 _Color7;
float _colmapDis;
float _colmapDisCols;
			fixed4 frag(g2f i) : SV_Target
			{
				float minDistanceToEdge = min(i.dist[0], min(i.dist[1], i.dist[2])) * i.dist[3];

			// Early out if we know we are not on a line segment.
			if (minDistanceToEdge > 0.9 || i.area.x > _MaxTriSize)
			{
				return fixed4(_BaseColor.rgb,0);
			}

			// Smooth our line out
			float t = exp2(_WireSmoothness * -1.0 * minDistanceToEdge * minDistanceToEdge);
			//fixed4 finalColor = lerp(_BaseColor, _WireColor, t);
			const fixed4 colors[8] = {
									_Color0,
									_Color1,
									_Color2,
									_Color3,
									_Color4,
									_Color5,
									_Color6,
									_Color7
									};

				float cameraToVertexDistance = length(_WorldSpaceCameraPos - i.worldSpacePosition);
				cameraToVertexDistance = cameraToVertexDistance*_colmapDis;
				cameraToVertexDistance = cameraToVertexDistance / _colmapDisCols;
				int index = clamp(floor(cameraToVertexDistance), 0, 7);
				//int index = floor(lerp(0,7,lerp(0,_colmapDis,cameraToVertexDistance)));
				fixed4 wireColor = colors[index];

				fixed4 finalColor = lerp(float4(0,0,0,1), wireColor, t);
				finalColor.a = t;
				if(_colorMode == 1)
				{
					finalColor = _ColorFront;
				}
				return finalColor;
			}
										ENDCG
		}
    }
}
