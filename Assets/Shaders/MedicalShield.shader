Shader "Pharma/MedicalShield" {
 Properties { _Color("Shield",Color)=(.25,.7,1,.4) }
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent"} Blend SrcAlpha One ZWrite Off Cull Back
 Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 fixed4 _Color;
 struct v{float4 vertex:POSITION;float3 normal:NORMAL;};struct f{float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 view:TEXCOORD1;};
 f vert(v i){f o;o.pos=UnityObjectToClipPos(i.vertex);o.normal=UnityObjectToWorldNormal(i.normal);o.view=WorldSpaceViewDir(i.vertex);return o;}
 fixed4 frag(f i):SV_Target{float rim=pow(1-saturate(dot(normalize(i.normal),normalize(i.view))),3);return fixed4(_Color.rgb,_Color.a*(.03+rim));}
 ENDCG }
 }
}
