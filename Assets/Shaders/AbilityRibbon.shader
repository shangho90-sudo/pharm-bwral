Shader "Pharma/AbilityRibbon" {
 Properties { _Color("Tint",Color)=(1,1,1,1) }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off Blend SrcAlpha One
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 fixed4 _Color;
 struct a {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 struct v {float4 position:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 v vert(a i){v o;o.position=UnityObjectToClipPos(i.vertex);o.uv=i.uv;o.color=i.color*_Color;return o;}
 fixed4 frag(v i):SV_Target{float edge=saturate(1-abs(i.uv.y*2-1));float core=pow(edge,8);return fixed4(i.color.rgb*1.2+core*.9,i.color.a*edge);}
 ENDCG }
 }
}
