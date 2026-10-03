Shader "Pharma/MedicalPalette"
{
    Properties { _MainTex("Original Tripo texture",2D)="white"{} _Color("Medical accent",Color)=(1,0.2,0.2,1) _Glossiness("Smoothness",Range(0,1))=0.25 _Metallic("Metallic",Range(0,1))=0 _EmissionColor("Glow",Color)=(0,0,0,1) }
    SubShader {
        Tags {"RenderType"="Opaque"} LOD 150
        CGPROGRAM
        #pragma surface surf Standard
        #pragma target 3.0
        sampler2D _MainTex;fixed4 _Color,_EmissionColor;half _Glossiness,_Metallic;
        struct Input{float2 uv_MainTex;};
        void surf(Input input,inout SurfaceOutputStandard output){fixed4 tex=tex2D(_MainTex,input.uv_MainTex);float high=max(tex.r,max(tex.g,tex.b)),low=min(tex.r,min(tex.g,tex.b));float accent=smoothstep(.15,.4,(high-low)/max(.01,high));output.Albedo=lerp(tex.rgb,_Color.rgb*high,accent);output.Metallic=_Metallic;output.Smoothness=_Glossiness;output.Emission=_EmissionColor.rgb*accent;output.Alpha=1;}
        ENDCG
    }
    FallBack "Diffuse"
}
