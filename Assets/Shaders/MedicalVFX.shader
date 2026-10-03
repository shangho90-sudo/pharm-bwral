Shader "Pharma/MedicalVFX" {
 Properties { _MainTex("Atlas",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _DstBlend("Destination blend",Float)=1 }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off Blend SrcAlpha [_DstBlend]
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;float4 _MainTex_ST,_Color;
 struct v {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 struct f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 f vert(v i){f o;o.pos=UnityObjectToClipPos(i.vertex);o.uv=TRANSFORM_TEX(i.uv,_MainTex);o.color=i.color*_Color;return o;}
 fixed4 frag(f i):SV_Target{fixed4 c=tex2D(_MainTex,i.uv)*i.color;c.a=saturate(c.a*4);return c;}
 ENDCG }
 }
}
