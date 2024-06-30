using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Tourmaline.Viewer3D.Processes;
using Tourmaline.Viewer3D.TvForms;

namespace Tourmaline.Viewer3D.Materials
{
    internal class SharedTextureManager
    {
        //Almacén de texturas con memoria.
        //Sólo carga cada textura una sola vez. Si ya existía, no la volverá a cargar y la
        //pillará de la memoria.
        readonly GraphicsDevice graphicsDevice;
        Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();

        internal SharedTextureManager(GraphicsDevice graphicsDevice)
        {
            this.graphicsDevice = graphicsDevice;
        }
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
                            texture = Formats.Msts.AceFile.Texture2DFromFile(graphicsDevice, aceTexture);
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
                            texture = Formats.Msts.AceFile.Texture2DFromFile(graphicsDevice, path);
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
                                    texture = Formats.Msts.AceFile.Texture2DFromFile(graphicsDevice, search);
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
        internal SharedMaterialManager(GraphicsDevice gd)
        {
            SceneryShader = new SceneryShader(gd);
            MissingTexture = SharedTextureManager.Get(gd, Path.Combine(FirstLoadProcess.Instance.ResourcesPath, "blank.bmp"));
        }
        internal Material Load(string materialName, string textureName = null, int options = 0, float mimMapBias=0f, Effect effect = null)
        {
            if (null != textureName)
                textureName = textureName.ToLower();
            (string,string,int,float,Effect) materialKey = (materialName,textureName,options,mimMapBias, effect);
            if(!Materials.ContainsKey(materialKey))
            {
                switch(materialName)
                {
                    case "Scenery":
                        break;
                }

            }

            return null;
        }
        
        


    }

    internal class SceneryMaterial:Material
    {
        readonly SceneryMaterialOptions options;
        readonly float mimMapBias;
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


    }
}
