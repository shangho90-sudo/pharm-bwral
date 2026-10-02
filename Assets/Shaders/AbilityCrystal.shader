Shader "Pharma/AbilityCrystal" {
 Properties { _Color("Energy tint",Color)=(0.2,0.8,1,1) }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Back ZWrite Off Blend SrcAlpha One
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 fixed4 _Color;
 struct a {float4 vertex:POSITION;float3 normal:NORMAL;};
 struct v {float4 position:SV_POSITION;float3 normal:TEXCOORD0;float3 view:TEXCOORD1;};
 v vert(a i){v o;o.position=UnityObjectToClipPos(i.vertex);float3 world=mul(unity_ObjectToWorld,i.vertex).xyz;o.normal=UnityObjectToWorldNormal(i.normal);o.view=_WorldSpaceCameraPos-world;return o;}
 fixed4 frag(v i):SV_Target{float3 n=normalize(i.normal);float rim=pow(1-saturate(abs(dot(n,normalize(i.view)))),2);float facet=.25+.75*saturate(dot(n,normalize(float3(-.4,.85,-.3))));return fixed4(_Color.rgb*(.55+facet)+rim*.65,_Color.a);}
 ENDCG }
 }
}
