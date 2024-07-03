using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Tourmaline.Simulation;
using Tourmaline.Viewer3D.Common;
using Tourmaline.Viewer3D.Processes;
using Tourmaline.Viewer3D.TvForms;
using TOURMALINE.Common;
using TwoMGFX;
using static Tourmaline.Viewer3D.Common.Helpers;

namespace Tourmaline.Viewer3D.Materials
{
    internal class SharedTextureManager
    {
        //Almacén de texturas con memoria.
        //Sólo carga cada textura una sola vez. Si ya existía, no la volverá a cargar y la
        //pillará de la memoria.
        readonly _3dTrainViewer viewer;
        Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();

        internal SharedTextureManager(_3dTrainViewer viewer)
        {
            this.viewer = viewer;
        }

        #region GetTextures
        internal static string GetRouteTextureFile(TextureFlags textureFlags, string textureName)
        {
            string contentPath = FirstLoadProcess.Instance.ContentPath;
            return GetTextureFile(textureFlags, contentPath + @"\Textures", textureName);
        }

        internal static string GetTransferTextureFile(string textureName)
        {
            string contentPath = FirstLoadProcess.Instance.ContentPath;
            return GetTextureFile(Helpers.TextureFlags.Snow, contentPath + @"\Textures", textureName);
        }

        internal static string GetTerrainTextureFile(string textureName)
        {
            string contentPath = FirstLoadProcess.Instance.ContentPath;
            return GetTextureFile(Helpers.TextureFlags.Snow, contentPath + @"\TerrTex", textureName);
        }

        internal static string GetTextureFile(TextureFlags textureFlags, string texturePath, string textureName)
        {
            var alternativePath = @"\";
            if (alternativePath.Length > 0) return texturePath + alternativePath + textureName;
            return texturePath + @"\" + textureName;
        }
        #endregion

        internal Texture2D Get(string path, bool required = false)
        {
            return Get(path, SharedMaterialManager.MissingTexture, required);
        }
        internal Texture2D Get(string path, Texture2D defaultTexture, bool required = false) 
        { 
            if(null==path || path.Length==0) return defaultTexture;

            path = path.ToLowerInvariant();
            if(!Textures.ContainsKey(path))
            {
                try
                {
                    Texture2D texture;
                    if(Path.GetExtension(path).Equals(".dds"))
                    {
                        string aceTexture = Path.ChangeExtension(path, ".ace");
                        if(File.Exists(aceTexture))
                        {
                            texture = Formats.Msts.AceFile.Texture2DFromFile(viewer.GControl.GraphicsDevice, aceTexture);
                        }
                        else
                        {
                            texture = defaultTexture;
                        }
                        //TODO: Meter aquí las texturas DDS.
                    }
                    else if(Path.GetExtension(path).Equals(".ace"))
                    {
                        if(File.Exists(path))
                        {
                            texture = Formats.Msts.AceFile.Texture2DFromFile(viewer.GControl.GraphicsDevice, path);
                        }
                        else
                        {
                            Texture2D missing()
                            {
                                if (required)
                                    Trace.TraceWarning("Missing texture {0} replaced with default texture", path);
                                return defaultTexture;
                            }
                            Texture2D invalid()
                            {
                                if (required)
                                    Trace.TraceWarning("Invalid texture {0} replaced with default texture", path);
                                return defaultTexture;
                            }
                            //Si no se encuentra la textura, sube un nivel de directorio
                            DirectoryInfo currentDir;
                            string search;
                            try
                            {
                                currentDir = Directory.GetParent(path); //Nivel de profundidad actual.
                                search = $"{Directory.GetParent(currentDir.FullName).FullName}\\{Path.GetFileName(path)}";
                            }
                            catch
                            {
                                return missing();
                            }
                            if (File.Exists(search) && search.ToLower().Contains("texture"))   //Existe y está dentro de "texture"
                            {
                                try
                                {
                                    texture = Formats.Msts.AceFile.Texture2DFromFile(viewer.GControl.GraphicsDevice, search);
                                }
                                catch
                                {
                                    return invalid();
                                }                                
                            }
                            else
                            {
                                return missing();
                            }
                        }                        
                    }
                    else return defaultTexture;

                    Textures.Add(path, texture);
                    return texture;
                }
                catch (InvalidDataException error)
                {
                    Trace.TraceWarning("Skipped texture with error: {0} in {1}", error.Message, path);
                    return defaultTexture;
                }
                catch (Exception error)
                {
                    if (File.Exists(path))
                        Trace.WriteLine(new FileLoadException(path, error));
                    else
                        Trace.WriteLine("Ignored missing texture file {0}", path);
                    return defaultTexture;
                }
            }
            else
            {
                return Textures[path]; //Ya la teníamos de antes.
            }
        }
    
        internal static Texture2D Get(GraphicsDevice gd,string path)
        {
            if (null == path || path.Length == 0) return SharedMaterialManager.MissingTexture;
            path = path.ToLowerInvariant();
            string ext = Path.GetExtension(path);  
            if(ext.Equals(".ace"))
                return Formats.Msts.AceFile.Texture2DFromFile(gd, path);
            using (FileStream stream = File.OpenRead(path))
            {
                if (ext.Equals(".gif") || ext.Equals(".jpg") || ext.Equals(".png"))
                    return Texture2D.FromStream(gd, stream);
                else if (ext.Equals(".bmp"))
                {
                    using (System.Drawing.Image image = System.Drawing.Image.FromStream(stream))
                    {
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                            memoryStream.Seek(0, SeekOrigin.Begin);
                            return Texture2D.FromStream(gd, memoryStream);
                        }
                    }
                }
                else
                    Trace.TraceWarning("Unsupported texture format: {0}", path);
                return SharedMaterialManager.MissingTexture;
            }
        }
    }

    internal class SharedMaterialManager
    {
        IDictionary<(string, string, int, float, Effect), Material> Materials = new Dictionary<(string, string, int, float, Effect), Material>();
        internal static Texture2D MissingTexture;
        internal readonly SceneryShader SceneryShader;
        internal _3dTrainViewer Viewer;
        internal SharedMaterialManager(_3dTrainViewer viewer)
        {
            this.Viewer= viewer;
            SceneryShader = new SceneryShader(viewer.GControl.GraphicsDevice);
            MissingTexture = SharedTextureManager.Get(viewer.GControl.GraphicsDevice,Path.Combine(FirstLoadProcess.Instance.ResourcesPath, "blank.bmp"));
        }
        internal Material Load(string materialName, string textureName = null, int options = 0, float mipMapBias=0f, Effect effect = null)
        {
            if (null != textureName)
                textureName = textureName.ToLower();
            (string,string,int,float,Effect) materialKey = (materialName,textureName,options,mipMapBias, effect);
            if(!Materials.ContainsKey(materialKey))
            {
                switch(materialName)
                {
                    case "Scenery":
                        Materials[materialKey] = new SceneryMaterial(Viewer, textureName, (SceneryMaterialOptions)options, mipMapBias);
                        break;
                    default:
                        Trace.TraceInformation("Skipped unknown material type {0}", materialName);
                        Materials[materialKey] = new YellowMaterial(Viewer);
                        break;
                }
            }
            return Materials[materialKey];
        }
    }



    [Flags]
    internal enum SceneryMaterialOptions
    {
        None = 0,
        // Diffuse
        Diffuse = 0x1,
        // Alpha test
        AlphaTest = 0x2,
        // Blending
        AlphaBlendingNone = 0x0,
        AlphaBlendingBlend = 0x4,
        AlphaBlendingAdd = 0x8,
        AlphaBlendingMask = 0xC,
        // Shader
        ShaderImage = 0x00,
        ShaderDarkShade = 0x10,
        ShaderHalfBright = 0x20,
        ShaderFullBright = 0x30,
        ShaderVegetation = 0x40,
        ShaderMask = 0x70,
        // Lighting
        Specular0 = 0x000,
        Specular25 = 0x080,
        Specular750 = 0x100,
        SpecularMask = 0x180,
        // Texture address mode
        TextureAddressModeWrap = 0x000,
        TextureAddressModeMirror = 0x200,
        TextureAddressModeClamp = 0x400,
        TextureAddressModeBorder = 0x600,
        TextureAddressModeMask = 0x600,
        // Night texture
        NightTexture = 0x800,
        // Texture to be shown in tunnels and underground (used for 3D cab night textures)
        UndergroundTexture = 0x40000000,
    }

    internal class SceneryMaterial:Materials.Material
    {
        readonly SceneryMaterialOptions options;
        readonly float mipMapBias;
        protected Texture2D texture;
        private readonly string texturePath;
        protected Texture2D nightTexture;
        byte aceAlphaBits; //Número de bits en el canal alpha del ACE.
        IEnumerator<EffectPass> ShaderPassesDarkShade;
        IEnumerator<EffectPass> ShaderPassesFullBright;
        IEnumerator<EffectPass> ShaderPassesHalfBright;
        IEnumerator<EffectPass> ShaderPassesImage;
        IEnumerator<EffectPass> ShaderPassesVegetation;
        IEnumerator<EffectPass> ShaderPasses;
        public static readonly DepthStencilState DepthReadCompareLess = new DepthStencilState
        {
            DepthBufferWriteEnable = false,
            DepthBufferFunction = CompareFunction.Less,
        };
        private static readonly Dictionary<TextureAddressMode, Dictionary<float,SamplerState>> SamplerStates = new Dictionary<TextureAddressMode, Dictionary<float, SamplerState>>();
        public SceneryMaterial(_3dTrainViewer viewer, string texturePath, SceneryMaterialOptions options, float mipMapBias)
            : base(viewer, String.Format("{0}:{1:X}:{2}", texturePath, options, mipMapBias))
        {
            this.options = options;
            this.mipMapBias = mipMapBias;
            this.texturePath = texturePath;
            this.texture = Viewer.TextureManager.Get(texturePath, true);

            // Record the number of bits in the alpha channel of the original ace file
            Texture2D auxTexture = SharedMaterialManager.MissingTexture;
            if (this.texture != SharedMaterialManager.MissingTexture && this.texture != null) auxTexture = this.texture;
            if (texture.Tag != null && texture.Tag.GetType() == typeof(Tourmaline.Formats.Msts.AceInfo))
                this.aceAlphaBits = ((Tourmaline.Formats.Msts.AceInfo)texture.Tag).AlphaBits;
            else
                this.aceAlphaBits = 0;
        }
        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            graphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

            var shader = Viewer.MaterialManager.SceneryShader;
            var level9_3 = true;
            if (ShaderPassesDarkShade == null) ShaderPassesDarkShade = shader.Techniques["DarkShadeLevel9_3"].Passes.GetEnumerator();
            if (ShaderPassesFullBright == null) ShaderPassesFullBright = shader.Techniques["FullBrightLevel9_3"].Passes.GetEnumerator();
            if (ShaderPassesHalfBright == null) ShaderPassesHalfBright = shader.Techniques["HalfBrightLevel9_3"].Passes.GetEnumerator();
            if (ShaderPassesImage == null) ShaderPassesImage = shader.Techniques["ImageLevel9_3"].Passes.GetEnumerator();
            if (ShaderPassesVegetation == null) ShaderPassesVegetation = shader.Techniques["VegetationLevel9_3"].Passes.GetEnumerator();

            shader.LightingDiffuse = (this.options & SceneryMaterialOptions.Diffuse) != 0 ? 1 : 0;

            // Set up for alpha blending and alpha test 
            if (GetBlending())
            {
                // Skip blend for near transparent alpha's (eliminates sorting issues for many simple alpha'd textures )
                if (previousMaterial == null  // Search for opaque pixels in alpha blended polygons
                    && (options & SceneryMaterialOptions.AlphaBlendingMask) != SceneryMaterialOptions.AlphaBlendingAdd)
                {
                    // Enable alpha blending for everything: this allows distance scenery to appear smoothly.
                    graphicsDevice.BlendState = BlendState.NonPremultiplied;
                    graphicsDevice.DepthStencilState = DepthStencilState.Default;
                    shader.ReferenceAlpha = 250;
                }
                else // Alpha blended pixels only
                {
                    shader.ReferenceAlpha = 10;  // ie default lightcone's are 9 in full transparent areas

                    // Set up for blending
                    if ((options & SceneryMaterialOptions.AlphaBlendingMask) == SceneryMaterialOptions.AlphaBlendingBlend)
                    {
                        graphicsDevice.BlendState = BlendState.NonPremultiplied;
                        graphicsDevice.DepthStencilState = DepthReadCompareLess; // To avoid processing already drawn opaque pixels
                    }
                    else
                    {
                        graphicsDevice.BlendState = BlendState.Additive;
                        graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
                    }
                }
            }
            else
            {
                graphicsDevice.BlendState = BlendState.Opaque;
                if ((options & SceneryMaterialOptions.AlphaTest) != 0)
                {
                    // Transparency testing is enabled
                    shader.ReferenceAlpha = 200;  // setting this to 128, chain link fences become solid at distance, at 200, they become
                }
                else
                {
                    // Solid rendering.
                    shader.ReferenceAlpha = -1;
                }
            }

            switch (options & SceneryMaterialOptions.ShaderMask)
            {
                case SceneryMaterialOptions.ShaderImage:
                    shader.CurrentTechnique = shader.Techniques["ImageLevel9_3"];
                    ShaderPasses = ShaderPassesImage;
                    break;
                case SceneryMaterialOptions.ShaderDarkShade:
                    shader.CurrentTechnique = shader.Techniques["DarkShadeLevel9_3"];
                    ShaderPasses = ShaderPassesDarkShade;
                    break;
                case SceneryMaterialOptions.ShaderHalfBright:
                    shader.CurrentTechnique = shader.Techniques["HalfBrightLevel9_3"];
                    ShaderPasses = ShaderPassesHalfBright;
                    break;
                case SceneryMaterialOptions.ShaderFullBright:
                    shader.CurrentTechnique = shader.Techniques["FullBrightLevel9_3"];
                    ShaderPasses = ShaderPassesFullBright;
                    break;
                case SceneryMaterialOptions.ShaderVegetation:
                case SceneryMaterialOptions.ShaderVegetation | SceneryMaterialOptions.ShaderFullBright:
                    shader.CurrentTechnique = shader.Techniques["VegetationLevel9_3"];
                    ShaderPasses = ShaderPassesVegetation;
                    break;
                default:
                    throw new InvalidDataException("Options has unexpected SceneryMaterialOptions.ShaderMask value.");
            }

            switch (options & SceneryMaterialOptions.SpecularMask)
            {
                case SceneryMaterialOptions.Specular0:
                    shader.LightingSpecular = 0;
                    break;
                case SceneryMaterialOptions.Specular25:
                    shader.LightingSpecular = 25;
                    break;
                case SceneryMaterialOptions.Specular750:
                    shader.LightingSpecular = 750;
                    break;
                default:
                    throw new InvalidDataException("Options has unexpected SceneryMaterialOptions.SpecularMask value.");
            }

            graphicsDevice.SamplerStates[0] = GetShadowTextureAddressMode();
            shader.ImageTexture = this.texture;
            shader.ImageTextureIsNight = false;
        }
        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.SceneryShader;
            var viewProj = XNAViewMatrix * XNAProjectionMatrix;

            ShaderPasses.Reset();
            while (ShaderPasses.MoveNext())
            {
                foreach (var item in renderItems)
                {
                    shader.SetMatrix(item.XNAMatrix, ref viewProj);
                    shader.ZBias = item.RenderPrimitive.ZBias;
                    ShaderPasses.Current.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
        }
        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            var shader = Viewer.MaterialManager.SceneryShader;
            shader.ImageTextureIsNight = false;
            shader.LightingDiffuse = 1;
            shader.LightingSpecular = 0;
            shader.ReferenceAlpha = 0;

            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
        }
        /// <summary>
        /// Return true if this material requires alpha blending
        /// </summary>
        /// <returns></returns>
        public override bool GetBlending()
        {
            bool alphaTestRequested = (options & SceneryMaterialOptions.AlphaTest) != 0;            // the artist requested alpha testing for this material
            bool alphaBlendRequested = (options & SceneryMaterialOptions.AlphaBlendingMask) != 0;   // the artist specified a blend capable shader

            return alphaBlendRequested                                   // the material is using a blend capable shader   
                    && (aceAlphaBits > 1                                    // and the original ace has more than 1 bit of alpha
                          || (aceAlphaBits == 1 && !alphaTestRequested));    //  or its just 1 bit, but with no alphatesting, we must blend it anyway

            // To summarize, assuming we are using a blend capable shader ..
            //     0 bits of alpha - never blend
            //     1 bit of alpha - only blend if the alpha test wasn't requested
            //     >1 bit of alpha - always blend
        }
        public override Texture2D GetShadowTexture(){return texture;}
        public override SamplerState GetShadowTextureAddressMode()
        {
            var mipMapBias = this.mipMapBias < -1 ? -1 : this.mipMapBias;
            TextureAddressMode textureAddressMode;
            switch (options & SceneryMaterialOptions.TextureAddressModeMask)
            {
                case SceneryMaterialOptions.TextureAddressModeWrap:
                    textureAddressMode = TextureAddressMode.Wrap; break;
                case SceneryMaterialOptions.TextureAddressModeMirror:
                    textureAddressMode = TextureAddressMode.Mirror; break;
                case SceneryMaterialOptions.TextureAddressModeClamp:
                    textureAddressMode = TextureAddressMode.Clamp; break;
                case SceneryMaterialOptions.TextureAddressModeBorder:
                    textureAddressMode = TextureAddressMode.Border; break;
                default:
                    throw new InvalidDataException("Options has unexpected SceneryMaterialOptions.TextureAddressModeMask value.");
            }

            if (!SamplerStates.ContainsKey(textureAddressMode))
                SamplerStates.Add(textureAddressMode, new Dictionary<float, SamplerState>());

            if (!SamplerStates[textureAddressMode].ContainsKey(mipMapBias))
                SamplerStates[textureAddressMode].Add(mipMapBias, new SamplerState
                {
                    AddressU = textureAddressMode,
                    AddressV = textureAddressMode,
                    Filter = TextureFilter.Anisotropic,
                    MaxAnisotropy = 16,
                    MipMapLevelOfDetailBias = mipMapBias
                });

            return SamplerStates[textureAddressMode][mipMapBias];
        }
    }
    internal class BasicMaterial : Materials.Material
    {
        public BasicMaterial(_3dTrainViewer viewer, string key)
            : base(viewer, key)
        {
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            foreach (var item in renderItems)
                item.RenderPrimitive.Draw(graphicsDevice);
        }
    }
    internal class BasicBlendedMaterial : Materials.BasicMaterial
    {
        public BasicBlendedMaterial(_3dTrainViewer viewer, string key)
            : base(viewer, key)
        {
        }

        public override bool GetBlending()
        {
            return true;
        }
    }
    internal class SpriteBatchMaterial : Materials.BasicBlendedMaterial
    {
        public readonly SpriteBatch SpriteBatch;

        readonly BlendState BlendState = BlendState.NonPremultiplied;
        readonly Effect Effect;

        public SpriteBatchMaterial(_3dTrainViewer viewer, Effect effect = null)
            : base(viewer, null)
        {
            SpriteBatch = new SpriteBatch(Viewer.GControl.GraphicsDevice);
            Effect = effect;
        }

        public SpriteBatchMaterial(_3dTrainViewer viewer, BlendState blendState, Effect effect = null)
            : this(viewer, effect: effect)
        {
            BlendState = blendState;
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState, effect: Effect);
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            SpriteBatch.End();

            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
        }
    }

    internal class EmptyMaterial : Materials.Material
    {
        internal EmptyMaterial(_3dTrainViewer viewer)
            : base(viewer, null)
        {
        }
    }
    internal class YellowMaterial : Materials.Material
    {
        static BasicEffect basicEffect;

        public YellowMaterial(_3dTrainViewer viewer)
            : base(viewer, null)
        {
            if (basicEffect == null)
            {
                basicEffect = new BasicEffect(Viewer.GControl.GraphicsDevice);
                basicEffect.Alpha = 1.0f;
                basicEffect.DiffuseColor = new Vector3(197.0f / 255.0f, 203.0f / 255.0f, 37.0f / 255.0f);
                basicEffect.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
                basicEffect.SpecularPower = 5.0f;
                basicEffect.AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f);

                basicEffect.DirectionalLight0.Enabled = true;
                basicEffect.DirectionalLight0.DiffuseColor = Vector3.One * 0.8f;
                basicEffect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(1.0f, -1.0f, -1.0f));
                basicEffect.DirectionalLight0.SpecularColor = Vector3.One;

                basicEffect.DirectionalLight1.Enabled = true;
                basicEffect.DirectionalLight1.DiffuseColor = new Vector3(0.5f, 0.5f, 0.5f);
                basicEffect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(-1.0f, -1.0f, 1.0f));
                basicEffect.DirectionalLight1.SpecularColor = new Vector3(0.5f, 0.5f, 0.5f);

                basicEffect.LightingEnabled = true;
            }
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {

            basicEffect.View = XNAViewMatrix;
            basicEffect.Projection = XNAProjectionMatrix;

            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                foreach (var item in renderItems)
                {
                    basicEffect.World = item.XNAMatrix;
                    pass.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
        }
    }
    internal class SolidColorMaterial : Materials.Material
    {
        BasicEffect basicEffect;

        internal SolidColorMaterial(_3dTrainViewer viewer, float a, float r, float g, float b)
            : base(viewer, null)
        {
            if (basicEffect == null)
            {
                basicEffect = new BasicEffect(Viewer.GControl.GraphicsDevice);
                basicEffect.Alpha = a;
                basicEffect.DiffuseColor = new Vector3(r, g, b);
                basicEffect.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
                basicEffect.SpecularPower = 5.0f;
                basicEffect.AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f);

                basicEffect.DirectionalLight0.Enabled = true;
                basicEffect.DirectionalLight0.DiffuseColor = Vector3.One * 0.8f;
                basicEffect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(1.0f, -1.0f, -1.0f));
                basicEffect.DirectionalLight0.SpecularColor = Vector3.One;

                basicEffect.DirectionalLight1.Enabled = true;
                basicEffect.DirectionalLight1.DiffuseColor = new Vector3(0.5f, 0.5f, 0.5f);
                basicEffect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(-1.0f, -1.0f, 1.0f));
                basicEffect.DirectionalLight1.SpecularColor = new Vector3(0.5f, 0.5f, 0.5f);

                basicEffect.LightingEnabled = true;
            }
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {

            basicEffect.View = XNAViewMatrix;
            basicEffect.Projection = XNAProjectionMatrix;

            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                foreach (var item in renderItems)
                {
                    basicEffect.World = item.XNAMatrix;
                    pass.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
        }
    }
    internal abstract class Material
    {
        public readonly _3dTrainViewer Viewer;
        readonly string Key;

        internal Material(_3dTrainViewer viewer, string key)
        {
            Viewer = viewer;
            Key = key;
        }

        public override string ToString()
        {
            if (String.IsNullOrEmpty(Key))
                return GetType().Name;
            return String.Format("{0}({1})", GetType().Name, Key);
        }

        public virtual void SetState(GraphicsDevice graphicsDevice, Material previousMaterial) { }
        public virtual void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix) { }
        public virtual void ResetState(GraphicsDevice graphicsDevice) { }

        public virtual bool GetBlending() { return false; }
        public virtual Texture2D GetShadowTexture() { return null; }
        public virtual SamplerState GetShadowTextureAddressMode() { return SamplerState.LinearWrap; }
        public int KeyLengthRemainder() //used as a "pseudorandom" number
        {
            if (String.IsNullOrEmpty(Key))
                return 0;
            return Key.Length % 10;
        }
    }

}
