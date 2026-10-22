---
name: fix-luna-shader
description: 修复Luna Shader/Luna兼容/Shader转Luna - 分析并修复shader的Luna(WebGL)兼容性问题
user-invocable: true
---

# Fix Luna Shader

分析 Unity shader 文件的 Luna（WebGL/HTML5 可玩广告）兼容性问题，给出诊断报告并自动修复。

**执行流程**：
1. 用户指定 shader 文件（或当前打开的 shader）
2. 读取 shader 源码，逐条检查兼容性规则
3. 输出诊断报告（问题 + 严重等级 + 修改建议）
4. 询问用户是否自动修复
5. 重写为 Luna 兼容版本

## 触发方式

```
fix-luna-shader
修复Luna Shader
Luna兼容检查
Shader转Luna
```

---

## Luna Shader 兼容性规则（完整）

### 规则 1：管线 — 必须使用 Built-in 管线

**严重等级**：🔴 致命

Luna 对 Built-in（Legacy）管线支持最好。URP/HDRP shader 转 WebGL 问题极多。

**检测特征**：
- Tags 中包含 `"RenderPipeline"="UniversalPipeline"` 或 `"RenderPipeline"="HDRenderPipeline"`
- 使用 `HLSLPROGRAM` / `ENDHLSL`（URP/HDRP 风格）
- include 路径包含 `com.unity.render-pipelines.universal` 或 `com.unity.render-pipelines.core`

**修改方案**：
- `HLSLPROGRAM` / `ENDHLSL` → `CGPROGRAM` / `ENDCG`
- 删除 Tags 中的 `"RenderPipeline"="UniversalPipeline"`
- 所有 URP include 替换为 Built-in 等价物：

| URP Include | Built-in 替代 |
|-------------|--------------|
| `Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl` | `"UnityCG.cginc"` |
| `Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl` | `"Lighting.cginc"` / `"AutoLight.cginc"` |
| `Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl` | 内联需要的函数或删除 |
| `Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl` | 删除（不需要） |
| `Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl` | 删除 |
| `Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl` | 删除 |
| `Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl` | 删除 |
| `Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl` | 删除 |
| `Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl` | 删除 |
| `Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl` | 删除 |
| `Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl` | 删除 |
| `Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl` | 删除 |

- URP 函数替换为 Built-in 等价物：

| URP 函数 | Built-in 替代 |
|----------|--------------|
| `TransformObjectToHClip(pos)` | `UnityObjectToClipPos(pos)` |
| `TransformObjectToWorld(pos)` | `mul(unity_ObjectToWorld, float4(pos, 1)).xyz` |
| `TransformWorldToHClip(pos)` | `mul(UNITY_MATRIX_VP, float4(pos, 1))` |
| `TransformObjectToWorldNormal(n)` | `UnityObjectToWorldNormal(n)` |
| `GetVertexPositionInputs(pos)` | 手动计算 `UnityObjectToClipPos` + `mul(unity_ObjectToWorld, ...)` |
| `GetShadowCoord(input)` | 删除或用 `TRANSFER_SHADOW` 宏 |
| `ComputeFogFactor(z)` | `UNITY_CALC_FOG_FACTOR(z)` |
| `MixFog(color, fog)` | `UNITY_APPLY_FOG(fog, color)` |
| `SafeNormalize(v)` | `normalize(v)` |
| `UNITY_ACCESS_INSTANCED_PROP(bufName, propName)` | 写法相同，但 buffer 名需匹配 |

---

### 规则 2：Editor Include — 禁止引用 Editor 目录

**严重等级**：🔴 致命

Editor 目录下的文件在构建时不存在，Luna 导出必定失败。

**检测特征**：
- `#include` 路径包含 `/Editor/`

**常见违规**：
```hlsl
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
```

**修改方案**：直接删除这些 include。ShaderPass.hlsl 只定义了 `SHADERPASS_xxx` 宏，如果代码中用到了，手动 `#define` 即可：
```hlsl
#define SHADERPASS_UNLIT 0
#define SHADERPASS_FORWARD 1
#define SHADERPASS_DEPTHONLY 2
#define SHADERPASS_DEPTHNORMALSONLY 3
```

---

### 规则 3：Tessellation — WebGL 不支持

**严重等级**：🔴 致命

WebGL 没有 Tessellation 阶段（Hull Shader / Domain Shader）。

**检测特征**：
- `[domain("tri")]`、`[partitioning(...)]`、`[outputtopology(...)]`、`[patchconstantfunc(...)]`
- `SV_TessFactor`、`SV_InsideTessFactor`、`SV_DomainLocation`
- `INTERNALTESSPOS` 语义
- `InputPatch<>`、`OutputPatch<>`
- `#ifdef ASE_TESSELLATION` 包裹的代码块
- `HLSLINCLUDE` 中的 tessellation 辅助函数（`FixedTess`、`CalcDistanceTessFactor` 等）

**修改方案**：
- 删除所有 tessellation 相关代码（`#if defined(ASE_TESSELLATION)` ... `#else` ... `#endif` 中保留 `#else` 分支）
- 删除 `HLSLINCLUDE` 中的 `ASE_TESS_FUNCS` 块
- 删除 CBUFFER 中的 `_TessPhongStrength`、`_TessValue`、`_TessMin`、`_TessMax`、`_TessEdgeLength`、`_TessMaxDisp`
- 删除 Properties 中被注释的 Tess 属性

---

### 规则 4：Texture2DArray — WebGL 1.0 不支持

**严重等级**：🟡 警告（通常是 lightmap 声明，不影响运行）

**检测特征**：
- Properties 中声明 `2DArray` 类型
- 代码中使用 `TEXTURE2D_ARRAY`、`SAMPLE_TEXTURE2D_ARRAY`

**常见违规**：
```hlsl
[HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
[HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
[HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
```

**修改方案**：删除这些 Property 声明。Built-in 管线不需要它们。

---

### 规则 5：Shader Target — 不超过 3.0

**严重等级**：🟡 警告

WebGL 对应 OpenGL ES 3.0 / GLSL ES 3.00，`#pragma target` 超过 3.0 的特性可能不可用。

**检测特征**：
- `#pragma target 3.5` / `4.0` / `4.5` / `5.0`
- `SHADER_TARGET >= 45` 条件编译

**修改方案**：
- 改为 `#pragma target 3.0` 或删除（默认 2.5）
- 删除 `#pragma prefer_hlslcc gles`（Built-in 管线不需要）

---

### 规则 6：多余的 Pass — 精简

**严重等级**：🟢 优化

Luna 只需要实际渲染用的 Pass。SceneSelectionPass、ScenePickingPass、DepthNormals 等编辑器专用 Pass 在 Luna 中无意义。

**检测特征**：
- `"LightMode"="SceneSelectionPass"`
- `"LightMode"="Picking"`
- `"LightMode"="DepthNormalsOnly"`
- `"LightMode"="DepthOnly"`（透明物体通常不需要）
- `"LightMode"="Meta"`（lightmap 烘焙用）

**修改方案**：只保留 Forward 渲染 Pass（`"LightMode"="UniversalForwardOnly"` 或无 LightMode tag）。如果需要投射阴影，保留 ShadowCaster Pass。

---

### 规则 7：不支持的 Shader 特性

**严重等级**：🔴 致命

| 特性 | 说明 |
|------|------|
| Geometry Shader | WebGL 无此阶段 |
| Compute Shader | WebGL 不支持 |
| Grab Pass | 不支持或极不稳定 |
| `_CameraDepthTexture` | Luna 不支持 |
| `_CameraOpaqueTexture` | Luna 不支持 |
| Stencil Buffer | 有限支持，避免复杂 stencil 操作 |
| `tex2Dlod` in vertex | 部分设备不支持 vertex texture fetch |

**检测特征**：
- `[maxvertexcount]`（Geometry Shader）
- `#pragma geometry`
- `GrabPass`
- `_CameraDepthTexture`、`_CameraOpaqueTexture`
- `tex2Dlod`（在 vertex shader 中）

**修改方案**：根据具体情况重写逻辑或删除相关功能。

---

### 规则 8：multi_compile 变体爆炸

**严重等级**：🟡 警告

过多的 `multi_compile` / `shader_feature` 会导致变体数量指数增长，Luna 处理大量变体会很慢甚至失败。

**检测特征**：
- 超过 3 个 `#pragma multi_compile` 或 `#pragma shader_feature`
- 包含 Luna 无意义的变体关键字：`DOTS_INSTANCING_ON`、`LOD_FADE_CROSSFADE`、`_DBUFFER_MRT1/2/3`、`_WRITE_RENDERING_LAYERS`、`_SCREEN_SPACE_OCCLUSION`、`DIRLIGHTMAP_COMBINED`、`LIGHTMAP_ON`、`DYNAMICLIGHTMAP_ON`、`DEBUG_DISPLAY`、`_GBUFFER_NORMALS_OCT`

**修改方案**：
- 删除所有 URP 专用的 multi_compile
- 只保留必要的：`multi_compile_instancing`（如果用了 GPU Instancing）、`multi_compile_fog`（如果需要雾效）
- 删除 `#pragma shader_feature_local _RECEIVE_SHADOWS_OFF`
- 删除 `#pragma instancing_options renderinglayer`

---

### 规则 9：纹理采样方式

**严重等级**：🟡 警告

**推荐写法**：
```hlsl
// ✅ Luna 兼容
sampler2D _MainTex;
fixed4 col = tex2D(_MainTex, uv);
```

**避免写法**：
```hlsl
// ❌ Texture2D + SamplerState 分离（Luna 解析器可能不支持）
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
```

**修改方案**：将 `TEXTURE2D` + `SAMPLER` + `SAMPLE_TEXTURE2D` 替换为 `sampler2D` + `tex2D`。

---

### 规则 10：CBUFFER 规范

**严重等级**：🟢 优化

**注意事项**：
- 空的 CBUFFER 或只有 `#ifdef` 包裹内容的 CBUFFER 可能被 Luna 解析器误判
- `CBUFFER_START(UnityPerMaterial)` / `CBUFFER_END` 在 Built-in 管线中不是必须的，可以直接声明 uniform

**修改方案**：
- 如果 CBUFFER 内容全在 `#ifdef` 中（如 `#ifdef ASE_TESSELLATION`），删除整个 CBUFFER 块
- 或者将实际使用的 uniform 移出 CBUFFER，直接声明

---

### 规则 11：Instancing Buffer 命名

**严重等级**：🟢 注意

GPU Instancing 在 Luna 中有限支持。简单的 `UNITY_INSTANCING_BUFFER` 可以工作。

**注意事项**：
- `UNITY_INSTANCING_BUFFER_START(Name)` 中的 Name 在所有 Pass 中必须一致
- 每个 Pass 中声明的 instanced property 顺序和类型必须一致
- 如果不同 Pass 中 buffer 名不同，Luna 可能报错

---

### 规则 12：Fallback Shader

**严重等级**：🟢 优化

**推荐**：
```hlsl
FallBack "Particles/Additive"          // 加法混合特效
FallBack "Mobile/Particles/Additive"   // 更轻量
FallBack "Transparent/Diffuse"         // 透明物体
FallBack "Mobile/Diffuse"              // 不透明物体
FallBack Off                           // 不需要 fallback
```

**避免**：
```hlsl
FallBack "Hidden/Shader Graph/FallbackError"  // URP 专用，Luna 找不到
```

---

## 自动修复流程

当用户确认自动修复时，按以下步骤执行：

### 步骤 1：提取核心逻辑

从原始 shader 中提取：
- Properties（去掉 URP 专用的 HideInInspector 属性和 2DArray）
- 顶点着色器逻辑（输入结构体、变换、传递数据）
- 片段着色器逻辑（纹理采样、颜色计算、alpha 计算）
- 混合模式（Blend 指令）
- 渲染状态（ZWrite、ZTest、Cull）

### 步骤 2：重写为 Built-in 管线

模板结构：
```hlsl
Shader "ShaderName"
{
    Properties
    {
        // 只保留实际使用的属性
    }

    SubShader
    {
        Tags { "RenderType"="..." "Queue"="..." "IgnoreProjector"="True" }
        LOD 100

        Pass
        {
            Name "Forward"
            Blend ... // 保持原混合模式
            ZWrite ...
            ZTest ...
            Cull ...

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing  // 如果需要
            #include "UnityCG.cginc"

            struct appdata { ... };
            struct v2f { ... };

            // uniform 声明
            // instancing buffer（如果需要）

            v2f vert(appdata v) { ... }
            half4 frag(v2f i) : SV_Target { ... }
            ENDCG
        }
    }

    FallBack "..."  // 合适的 Built-in fallback
}
```

### 步骤 3：验证

- 确认所有 Properties 名称不变（不破坏现有材质引用）
- 确认混合模式与原 shader 一致
- 确认核心视觉效果逻辑完整保留

---

## 常见 ASE Shader 修复模式

Amplify Shader Editor 生成的 URP shader 有固定的代码结构，修复模式如下：

1. **删除** `HLSLINCLUDE` 中的整个 tessellation 函数块
2. **删除** 除 Forward Pass 外的所有 Pass（DepthOnly、SceneSelection、ScenePicking、DepthNormals）
3. **重写** Forward Pass：`HLSLPROGRAM` → `CGPROGRAM`，替换所有 URP include 和函数
4. **清理** Properties 中的 2DArray、Tess 参数、QueueOffset、QueueControl
5. **替换** `CustomEditor "UnityEditor.ShaderGraphUnlitGUI"` → 删除或改为默认
6. **替换** FallBack
7. **保留** ASE 元数据注释块（`/*ASEBEGIN ... ASEEND*/`），不影响运行但保留 ASE 可编辑性

---

## 诊断报告格式

```
=== Luna Shader 兼容性诊断 ===

文件：Assets/FX/Common/Shader/xxx.shader

🔴 致命问题（必须修复）：
  1. [规则1] 使用 URP 管线 — 第39行 Tags 包含 RenderPipeline=UniversalPipeline
  2. [规则2] Editor Include — 第223行 引用 Editor/ShaderGraph/Includes/ShaderPass.hlsl
  3. [规则3] Tessellation 代码 — 第54-154行 HLSLINCLUDE 中包含 tessellation 函数

🟡 警告（建议修复）：
  4. [规则4] 2DArray 纹理 — 第26-28行 unity_Lightmaps 等声明
  5. [规则8] 变体爆炸 — 共12个 multi_compile，大部分 Luna 无意义

🟢 优化（可选）：
  6. [规则6] 多余 Pass — DepthOnly/SceneSelection/ScenePicking/DepthNormals 共4个无用 Pass
  7. [规则12] Fallback — 使用了 URP 专用 FallbackError

是否自动修复？(Y/N)
```

---

## 错误处理

- 如果 shader 使用了 Geometry Shader 或 Compute Shader，提示用户这些功能无法在 Luna 中实现，需要替代方案
- 如果 shader 使用了 `_CameraDepthTexture`（深度效果），提示需要删除或用其他方式模拟
- 如果 shader 逻辑过于复杂（超过 500 行核心逻辑），建议用户确认哪些效果可以简化

---

**更新日期**: 2026-04-22
