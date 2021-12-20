using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Beffio.Dithering;

public class StylizerRenderFeature : ScriptableRendererFeature
{
    CustomRenderPass m_ScriptablePass;
    Stylizer stylizer;

    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass();
    }
 
    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {   
        stylizer = renderingData.cameraData.camera.gameObject.GetComponent<Stylizer>(); //Don't like this
        if(stylizer!=null && stylizer.enabled){
            RenderPassEvent renderPassEvent = RenderPassEvent.AfterRendering;
            var src = renderer.cameraColorTarget;
            stylizer = renderingData.cameraData.camera.gameObject.GetComponent<Stylizer>();
            if(stylizer.Grain_New && stylizer.Dither && !stylizer.Grain_Old && stylizer.Grain){
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            }
            m_ScriptablePass.Setup(renderer.cameraColorTarget,RenderTargetHandle.CameraTarget,stylizer,renderPassEvent);
            renderer.EnqueuePass(m_ScriptablePass);
        }

    }

    class CustomRenderPass : ScriptableRenderPass
    {

        Stylizer stylizer;
        Camera camera;
        RenderTargetIdentifier currentTarget;

        private static readonly int DitherID = Shader.PropertyToID("_DitherID");
        private static readonly int GrainID = Shader.PropertyToID("_GrainID");

        public void Setup(RenderTargetIdentifier source, RenderTargetHandle destination, Stylizer stylizer, RenderPassEvent renderPassEvent){
            this.currentTarget = source;
            this.stylizer = stylizer;
            this.renderPassEvent = renderPassEvent;
        }
 
        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {

            if(stylizer!=null){
                
                camera = renderingData.cameraData.camera;

                CommandBuffer cmd = CommandBufferPool.Get();
                RenderTargetIdentifier destination = BuiltinRenderTextureType.CurrentActive;
                RenderTextureDescriptor opaqueDescriptor = renderingData.cameraData.cameraTargetDescriptor;

                if((stylizer.Dither && !stylizer.Grain_Old && !stylizer.Grain_New)||(stylizer.Dither && !stylizer.Grain)) {
                    DitherRender(currentTarget,destination,cmd);			
                }

                if(stylizer.Grain_New && !stylizer.Dither && !stylizer.Grain_Old && stylizer.Grain){
                    NewGrainRender(currentTarget,destination, cmd, camera);
                    #if UNITY_EDITOR
                        if(!Application.isPlaying){
                            NewGrainRender(currentTarget, destination, cmd, camera);
                        }
                    #endif
                }

                if(stylizer.Grain_New && stylizer.Dither && !stylizer.Grain_Old && stylizer.Grain){
                    NewCombine(destination, cmd, camera, opaqueDescriptor);	
                    #if UNITY_EDITOR
                        if(!Application.isPlaying){
                            NewCombine(destination, cmd, camera, opaqueDescriptor);
                        }
                    #endif	
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

            }

        }

        //Stylizer functions
        private void DitherRender(RenderTargetIdentifier source, RenderTargetIdentifier destination, CommandBuffer cmd){

                Palette palette = stylizer.Palette;
                Pattern pattern = stylizer.Pattern;
                Texture2D patternTexture = stylizer.PatternTexture;
                Material material = stylizer.Material;
                
				if(palette == null || (pattern == null && patternTexture == null)) {
					return;
				}

				if(!palette.HasTexture || (patternTexture == null && !pattern.HasTexture)) {
					return;
				}

				Texture2D patTex = (pattern == null ? patternTexture : pattern.Texture);

				material.SetFloat("_PaletteColorCount", palette.MixedColorCount);
				material.SetFloat("_PaletteHeight", palette.Texture.height);
				material.SetTexture("_PaletteTex", palette.Texture);
				material.SetFloat("_PatternSize", patTex.width);
				material.SetTexture("_PatternTex", patTex);

                //Material mat = new Material(Shader.Find("Beffio/Image Effects/ColorFilter"));
                //mat.SetColor("_FilterColor",stylizer.Palette.Colors[4]);

				cmd.Blit(source, destination, material);
                
		}
        private void NewGrainRender(RenderTargetIdentifier source, RenderTargetIdentifier destination, CommandBuffer cmd, Camera cam){

            PostProf profile = stylizer.profile;
            PostCont m_Context = stylizer.m_Context;

			if(profile.grain.enabled==false){
				profile.grain.enabled=true;
			}
			
			var context = m_Context.Reset();
            context.profile = profile;
            context.renderTextureFactory = stylizer.m_RenderTextureFactory;
            context.materialFactory = stylizer.m_MaterialFactory;
            context.camera = cam;
#if UNITY_EDITOR
			var uberMaterial = stylizer.m_MaterialFactory.Get("Hidden/Post FX/Uber Shader_Grain");
#else
			var uberMaterial = ub;
#endif
            uberMaterial.shaderKeywords = null;

			Texture autoExposure = GU.whiteTexture;
			uberMaterial.SetTexture("_AutoExposure", autoExposure);


			stylizer.m_Grain.Init(context,profile.grain);

			TryPrepareUberImageEffect(stylizer.m_Grain, uberMaterial);

			cmd.Blit(source, destination, uberMaterial, 0);

		 	stylizer.m_RenderTextureFactory.ReleaseAll();

		
		}
        private void NewCombine(RenderTargetIdentifier destination, CommandBuffer cmd, Camera cam, RenderTextureDescriptor opaqueDescriptor){

            PostProf profile = stylizer.profile;
			
			if(profile.grain.enabled==false){
				profile.grain.enabled=true;
			}
			
			var context = stylizer.m_Context.Reset();
            context.profile = profile;
            context.renderTextureFactory = stylizer.m_RenderTextureFactory;
            context.materialFactory = stylizer.m_MaterialFactory;
            context.camera = cam;
#if UNITY_EDITOR
			var uberMaterial = stylizer.m_MaterialFactory.Get("Hidden/Post FX/Uber Shader_Grain");
#else
			var uberMaterial = ub;
#endif
            uberMaterial.shaderKeywords = null;

			Texture autoExposure = GU.whiteTexture;
			uberMaterial.SetTexture("_AutoExposure", autoExposure);

			stylizer.m_Grain.Init(context,profile.grain);

			TryPrepareUberImageEffect(stylizer.m_Grain, uberMaterial);

            Palette palette = stylizer.Palette;
            Pattern pattern = stylizer.Pattern;
            Texture2D patternTexture = stylizer.PatternTexture;

			if(palette == null || (pattern == null && patternTexture == null)) {
				return;
			}

			if(!palette.HasTexture || (patternTexture == null && !pattern.HasTexture)) {
				return;
			}

			Texture2D patTex = (pattern == null ? patternTexture : pattern.Texture);

            Material material = stylizer.Material;

			material.SetFloat("_PaletteColorCount", palette.MixedColorCount);
			material.SetFloat("_PaletteHeight", palette.Texture.height);
			material.SetTexture("_PaletteTex", palette.Texture);
			material.SetFloat("_PatternSize", patTex.width);
			material.SetTexture("_PatternTex", patTex);
         
            //Blitting
            int ditherTemp = DitherID;
            int grainTemp = GrainID;
            RenderTargetIdentifier source = currentTarget;

            cmd.GetTemporaryRT(ditherTemp, cam.pixelWidth, cam.pixelHeight, 0, FilterMode.Point, RenderTextureFormat.Default);
            cmd.GetTemporaryRT(grainTemp, cam.pixelWidth, cam.pixelHeight, 0, FilterMode.Point, RenderTextureFormat.Default);

            //Dither
            cmd.Blit(source, ditherTemp, uberMaterial, 0);
            
            //Grain
            cmd.Blit(ditherTemp, grainTemp, material);
            
            //Output to Screen
            cmd.Blit(grainTemp, source);
            
            cmd.ReleaseTemporaryRT(ditherTemp);
            cmd.ReleaseTemporaryRT(grainTemp);

			stylizer.m_RenderTextureFactory.ReleaseAll();
		}

        #region New grain functions
		T AddComponent<T>(T component)
            where T : PostComp
        {
            stylizer.m_Components.Add(component);
            return component;
        }

		bool TryPrepareUberImageEffect<T>(PostProcessingComponentRenderTexture<T> component, Material material)
            where T : PostMod
        {
            if (!component.active)
                return false;

            component.Prepare(material, stylizer.gm);
            return true;
        }

		#endregion
       
    }
}