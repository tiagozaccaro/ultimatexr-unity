Shader "UltimateXR/FX/UxrWallFade Portal"
{
    Properties
    {
        _Color("Fade / Horizon Color", Color) = (0, 0, 0, 1)

        _PortalPlanePos("Portal Plane Position", Vector) = (0, 0, 0, 1)
        _PortalPlaneNormal("Portal Plane Normal", Vector) = (0, 0, 1, 0)
        _PortalSphereCenter("Portal Sphere Center", Vector) = (0, 0, 0, 1)
        _PortalSphereRadius("Portal Sphere Radius", Float) = 0.4
        _PortalEdgeSoftness("Portal Edge Softness", Float) = 0.05

        _FloorPosY("Floor Position Y", Float) = 0.0
        _FloorGridTileSize("Floor Grid Tile Size", Float) = 0.5
        _FloorGridAntiAliasing("Floor Grid Anti-Aliasing", Range(0,1)) = 0.7

        _FloorNearColor("Floor Near Color", Color) = (0.05, 0.05, 0.05, 1)
        _FloorFarColor("Floor Far Color", Color) = (0.015, 0.015, 0.015, 1)
        _FloorFarStartDistance("Floor Far Start Distance", Float) = 5.0
        _FloorFarEndDistance("Floor Far End Distance", Float) = 15.0

        _FloorGridMainColor("Floor Grid Main Color", Color) = (1, 1, 1, 1)
        _FloorGridMainLineThickness("Floor Grid Main Line Thickness", Float) = 0.004
        _FloorGridMainLineInterval("Floor Grid Main Line Interval", Float) = 5.0

        _FloorGridSecondaryColor("Floor Grid Secondary Color", Color) = (1, 1, 1, 0.5)
        _FloorGridSecondaryLineThickness("Floor Grid Secondary Line Thickness", Float) = 0.002

        _FloorGridFadeStartRadius("Floor Grid Fade Start Radius", Float) = 5.0
        _FloorGridFadeEndRadius("Floor Grid Fade End Radius", Float) = 15.0

        _CeilingHeight("Ceiling Height", Float) = 4.0
        _HorizonFadeStartDistance("Horizon Fade Start Distance", Float) = 15.0
        _HorizonFadeEndDistance("Horizon Fade End Distance", Float) = 30.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay+995"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            float _UxrRenderFirstPersonEffects;

            float4 _Color;

            float4 _PortalPlanePos;
            float4 _PortalPlaneNormal;
            float4 _PortalSphereCenter;
            float  _PortalSphereRadius;
            float  _PortalEdgeSoftness;

            float _FloorPosY;
            float _FloorGridTileSize;
            float _FloorGridAntiAliasing;

            float4 _FloorNearColor;
            float4 _FloorFarColor;
            float _FloorFarStartDistance;
            float _FloorFarEndDistance;

            float4 _FloorGridMainColor;
            float  _FloorGridMainLineThickness;
            float  _FloorGridMainLineInterval;

            float4 _FloorGridSecondaryColor;
            float  _FloorGridSecondaryLineThickness;

            float _FloorGridFadeStartRadius;
            float _FloorGridFadeEndRadius;

            float _CeilingHeight;
            float _HorizonFadeStartDistance;
            float _HorizonFadeEndDistance;

            float3 GetEyeWorldSpaceCameraPos()
            {
                #if defined(USING_STEREO_MATRICES)
                    return unity_StereoWorldSpaceCameraPos[unity_StereoEyeIndex];
                #else
                    return _WorldSpaceCameraPos;
                #endif
            }

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex   = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                return o;
            }

            float GetGridLineMask(float2 worldXZ, float tileSize, float lineThickness)
            {
                tileSize      = max(tileSize, 1e-5);
                lineThickness = max(lineThickness, 1e-5);

                float2 gridUv = worldXZ / tileSize;

                float2 cell = abs(frac(gridUv - 0.5) - 0.5);
                float  dist = min(cell.x, cell.y);

                float halfThickness = saturate(lineThickness / tileSize);

                float2 uvDerivatives = fwidth(gridUv);
                float  filterWidth   = max(max(uvDerivatives.x, uvDerivatives.y), 1e-5);

                float effectiveFilter = lerp(0.0, filterWidth, _FloorGridAntiAliasing);

                float mask = 1.0 - smoothstep(halfThickness, halfThickness + effectiveFilter, dist);

                float pixelCoverage = halfThickness / max(filterWidth, 1e-5);
                float mipFade       = lerp(1.0, saturate(pixelCoverage), _FloorGridAntiAliasing);

                return mask * mipFade;
            }

            float4 GetGridPlane(float3 worldPos, float floorFade, float horizonFade, float gridFade)
            {
                float2 worldXZ = worldPos.xz;

                float secondaryMask = GetGridLineMask(worldXZ, _FloorGridTileSize, _FloorGridSecondaryLineThickness);

                float mainInterval = max(round(_FloorGridMainLineInterval), 1.0);
                float mainTileSize = max(_FloorGridTileSize, 1e-5) * mainInterval;

                float mainMask = GetGridLineMask(worldXZ, mainTileSize, _FloorGridMainLineThickness);

                float mainAlpha      = saturate(mainMask * _FloorGridMainColor.a) * gridFade;
                float secondaryAlpha = saturate(secondaryMask * _FloorGridSecondaryColor.a) * gridFade;

                float useMain = step(1e-5, mainAlpha);

                float3 lineColor = lerp(_FloorGridSecondaryColor.rgb, _FloorGridMainColor.rgb, useMain);
                float  lineAlpha = lerp(secondaryAlpha, mainAlpha, useMain);

                float3 planeColor = lerp(_FloorNearColor.rgb, _FloorFarColor.rgb, floorFade);

                planeColor = lerp(planeColor, _Color.rgb, horizonFade);
                lineColor  = lerp(lineColor,  _Color.rgb, horizonFade);

                float3 color = lerp(planeColor, lineColor, lineAlpha);

                return float4(color, _FloorNearColor.a);
            }

            bool TryGetGridPlane(float planeY, float3 rayOrigin, float3 rayDir, out float4 planeColor)
            {
                planeColor = float4(0.0, 0.0, 0.0, 0.0);

                float denom = rayDir.y;

                if (abs(denom) <= 1e-5)
                {
                    return false;
                }

                float t = (planeY - rayOrigin.y) / denom;

                if (t <= 0.0)
                {
                    return false;
                }

                float3 planeHit = rayOrigin + rayDir * t;

                float distanceFromCamera = distance(rayOrigin, planeHit);

                float gridFadeStart = max(_FloorGridFadeStartRadius, 0.0);
                float gridFadeEnd   = max(_FloorGridFadeEndRadius, gridFadeStart + 1e-5);
                float gridFade      = 1.0 - smoothstep(gridFadeStart, gridFadeEnd, distanceFromCamera);

                float floorFarStart = max(_FloorFarStartDistance, 0.0);
                float floorFarEnd   = max(_FloorFarEndDistance, floorFarStart + 1e-5);
                float floorFade     = smoothstep(floorFarStart, floorFarEnd, distanceFromCamera);

                float horizonFadeStart = max(_HorizonFadeStartDistance, 0.0);
                float horizonFadeEnd   = max(_HorizonFadeEndDistance, horizonFadeStart + 1e-5);
                float horizonFade      = smoothstep(horizonFadeStart, horizonFadeEnd, distanceFromCamera);

                planeColor = GetGridPlane(planeHit, floorFade, horizonFade, gridFade);
                return true;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                
                clip(_UxrRenderFirstPersonEffects - 0.5);

                float3 planePos     = _PortalPlanePos.xyz;
                float3 planeNormal  = normalize(_PortalPlaneNormal.xyz);
                float3 sphereCenter = _PortalSphereCenter.xyz;
                float  sphereRadius = max(_PortalSphereRadius, 0.0);
                float  edgeSoftness = max(_PortalEdgeSoftness, 1e-5);

                float3 rayOrigin = GetEyeWorldSpaceCameraPos();
                float3 rayDir    = normalize(i.worldPos - rayOrigin);

                float portalDenom = dot(planeNormal, rayDir);
                float portalMask  = 1.0;

                if (portalDenom > 1e-5)
                {
                    float t = dot(planeNormal, planePos - rayOrigin) / portalDenom;

                    if (t > 0.0)
                    {
                        float3 portalPlaneHit = rayOrigin + rayDir * t;
                        float  distToCenter   = distance(portalPlaneHit, sphereCenter);

                        portalMask = smoothstep(sphereRadius - edgeSoftness, sphereRadius, distToCenter);
                    }
                    else
                    {
                        portalMask = 0.0;
                    }
                }

                float4 gridPlane = float4(0.0, 0.0, 0.0, 0.0);

                float floorY   = _FloorPosY;
                float ceilingY = _FloorPosY + max(_CeilingHeight, 0.0);

                bool hasFloor   = TryGetGridPlane(floorY, rayOrigin, rayDir, gridPlane);
                bool hasCeiling = false;

                if (!hasFloor)
                {
                    hasCeiling = TryGetGridPlane(ceilingY, rayOrigin, rayDir, gridPlane);
                }

                if (hasFloor || hasCeiling)
                {
                    gridPlane.a *= portalMask;
                }

                float overlayAlpha = _Color.a * portalMask;
                float3 finalRgb    = lerp(_Color.rgb, gridPlane.rgb, saturate(gridPlane.a));

                return fixed4(finalRgb, overlayAlpha);
            }
            ENDCG
        }
    }
}