using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using TOURMALINE.Common;

namespace TOURMALINE.Settings
{
    /*
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DefaultAttribute:Attribute
    {
        public readonly object Value;
        public DefaultAttribute(object value)
        {
            Value = value;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DoNotSaveAttribute:Attribute { }

    public class UserSettings : SettingsBase
    {
        public static readonly string RegistryKey;        // ie @"SOFTWARE\OpenRails\ORTS"
        public static readonly string SettingsFilePath;   // ie @"C:\Program Files\Open Rails\OpenRails.ini"
        public static readonly string UserDataFolder;     // ie @"C:\Users\Wayne\AppData\Roaming\Open Rails"
        public static readonly string DeletedSaveFolder;  // ie @"C:\Users\Wayne\AppData\Roaming\Open Rails\Deleted Saves"
        public static readonly string SavePackFolder;     // ie @"C:\Users\Wayne\AppData\Roaming\Open Rails\Save Packs"

        static UserSettings()
        {
            // Only one of these is allowed; if the INI file exists, we use that, otherwise we use the registry.
            RegistryKey = "SOFTWARE\\Montefaro\\Tourmaline";
            SettingsFilePath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Tourmaline.ini");
            if (File.Exists(SettingsFilePath))
                RegistryKey = null;
            else
                SettingsFilePath = null;

            UserDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Application.ProductName);
            // TODO: If using INI file, move these to application directory as well.
            if (!Directory.Exists(UserDataFolder)) Directory.CreateDirectory(UserDataFolder);
            DeletedSaveFolder = Path.Combine(UserDataFolder, "Deleted Saves");
            SavePackFolder = Path.Combine(UserDataFolder, "Save Packs");
        }

        readonly Dictionary<string, object> CustomDefaultValues = new Dictionary<string, object>();

        #region Menu_Selection enum
        public enum Menu_SelectionIndex
        {
            // De momento sólo hay dos cosas que podemos seleccionar.
            Folder = 0, //La carpeta donde están los contenidos
            Locomotive = 1  //El modelo de tren que vamos a usar en la animación.
        }
        #endregion

        /// <summary>
        /// Especifica el método de anti-aliasing. Por el momento MSAA es el único modo que soporta Monogame.
        /// </summary>
        public enum AntiAliasingMethod
        {
            /// <summary>
            /// No antialiasing
            /// </summary>
            None = 1,
            /// <summary>
            /// 2x multisampling
            /// </summary>
            MSAA2x = 2,
            /// <summary>
            /// 4x multisampling
            /// </summary>
            MSAA4x = 3,
            /// <summary>
            /// 8x multisampling
            /// </summary>
            MSAA8x = 4,
            /// <summary>
            /// 16x multisampling
            /// </summary>
            MSAA16x = 5,
            /// <summary>
            /// 32x multisampling
            /// </summary>
            MSAA32x = 6,
        }

        public enum DirectXFeature
        {
            Level9_1,
            Level9_3,
            Level10_0,
        }

        #region Ajustes del usuario

        // Please put all user settings in here as auto-properties. Public properties
        // of type 'string', 'int', 'bool', 'string[]' and 'int[]' are automatically loaded/saved.

        // Main menu settings:
        [Default(true)]
        public bool Logging { get; set; }
        [Default(false)]
        public bool FullScreen { get; set; }

        [Default("")]
        public String Language { get; set; }

        // Video settings:
        [Default(false)]
        public bool DynamicShadows { get; set; }
        [Default(false)]
        public bool ShadowAllShapes { get; set; }
        [Default(false)]
        public bool FastFullScreenAltTab { get; set; }
        [Default(false)]
        public bool WindowGlass { get; set; }
        [Default(false)]
        public bool ModelInstancing { get; set; }
        [Default(true)]
        public bool Wire { get; set; }
        [Default(false)]
        public bool VerticalSync { get; set; }
        [Default(2000)]
        public int ViewingDistance { get; set; }
        [Default(45)] // MSTS uses 60 FOV horizontally, on 4:3 displays this is 45 FOV vertically (what OR uses).
        public int ViewingFOV { get; set; }
        [Default(49)]
        public int WorldObjectDensity { get; set; }
        [Default("1024x768")]
        public string WindowSize { get; set; }
        [Default(20)]
        public int DayAmbientLight { get; set; }
        [Default(AntiAliasingMethod.MSAA2x)]
        public int AntiAliasing { get; set; }


        [Default(0)]
        public int LODBias { get; set; }
        [Default(false)]
        public bool PerformanceTuner { get; set; }
        [Default(true)]
        public bool SuppressShapeWarnings { get; set; }
        [Default(60)]
        public int PerformanceTunerTarget { get; set; }
        [Default(false)]
        public bool LODViewingExtention { get; set; }
        [Default(false)]
        public bool PreferDDSTexture { get; set; }

        // Hidden settings:
        [Default("TourmalineLog.txt")]
        public string LoggingFilename { get; set; }
        [Default("")] // Si se deja en blanco, Tourmaline usará el escritorio
        public string LoggingPath { get; set; }
        [Default("")]
        public string ScreenshotPath { get; set; }
        [Default("")]
        public string DirectXFeatureLevel { get; set; }
        public bool IsDirectXFeatureLevelIncluded(DirectXFeature level) => (int)level <= (int)Enum.Parse(typeof(DirectXFeature), "Level" + this.DirectXFeatureLevel);
        [Default(true)]
        public bool ShadowMapBlur { get; set; }
        [Default(4)]
        public int ShadowMapCount { get; set; }
        [Default(0)]
        public int ShadowMapDistance { get; set; }
        [Default(1024)]
        public int ShadowMapResolution { get; set; }
        [Default(10)]
        public int Multiplayer_UpdateInterval { get; set; }
        [Default("http://openrails.org/images/support-logos.jpg")]
        public string AvatarURL { get; set; }
        [Default(false)]
        public bool ShowAvatar { get; set; }
        [Default("0.0")] // Do not offer to restore/resume any saves this version or older. Updated whenever a younger save fails to restore.
        public string YoungestVersionFailedToRestore { get; set; }

        // Internal settings:
        [Default(false)]
        public bool DataLogger { get; set; }
        [Default(false)]
        public bool Profiling { get; set; }
        [Default(0)]
        public int ProfilingFrameCount { get; set; }
        [Default(0)]
        public int ProfilingTime { get; set; }
        [Default(0)]
        public int ReplayPauseBeforeEndS { get; set; }
        [Default(true)]
        public bool ReplayPauseBeforeEnd { get; set; }
        [Default(true)]
        public bool ShowErrorDialogs { get; set; }       
        [Default(new[] { 50, 50 })]
        public int[] WindowPosition_Activity { get; set; }

        // In-game settings:
        [Default(0x7)] // OSDLocations.DisplayState.Auto
        public int OSDLocationsState { get; set; }
        [Default(0x1)] // OSDCars.DisplayState.Trains
        public int OSDCarsState { get; set; }
        #endregion

        public FolderSettings Folders { get; private set; }
        public InputSettings Input { get; private set; }

        public UserSettings(IEnumerable<string> options)
            : base(SettingsStore.GetSettingStore(SettingsFilePath, RegistryKey, null))
        {
            CustomDefaultValues["LoggingPath"] = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            CustomDefaultValues["ScreenshotPath"] = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), Application.ProductName);
            Load(options);
            Folders = new FolderSettings(options);
            Input = new InputSettings(options);
        }

        /// <summary>
        /// Get a saving property from this instance by name.
        /// </summary>
        public SavingProperty<T> GetSavingProperty<T>(string name)
        {
            var property = GetProperty(name);
            if (property == null)
                return null;
            else
                return new SavingProperty<T>(this, property, AllowUserSettings);
        }

        public override object GetDefaultValue(string name)
        {
            var property = GetType().GetProperty(name);

            if (CustomDefaultValues.ContainsKey(property.Name))
                return CustomDefaultValues[property.Name];

            if (property.GetCustomAttributes(typeof(DefaultAttribute), false).Length > 0)
                return (property.GetCustomAttributes(typeof(DefaultAttribute), false)[0] as DefaultAttribute).Value;

            throw new InvalidDataException(String.Format("El ajuste de usuario {0} no tiene un valor por defecto.", property.Name));
        }

        PropertyInfo GetProperty(string name)
        {
            return GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        }

        PropertyInfo[] GetProperties()
        {
            return GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy).Where(pi => pi.Name != "Folders" && pi.Name != "Input").ToArray();
        }

        protected override object GetValue(string name)
        {
            return GetProperty(name).GetValue(this, null);
        }

        protected override void SetValue(string name, object value)
        {
            GetProperty(name).SetValue(this, value, null);
        }

        protected override void Load(Dictionary<string, string> optionsDictionary)
        {
            foreach (var property in GetProperties())
                Load(optionsDictionary, property.Name, property.PropertyType);
        }

        public override void Save()
        {
            foreach (var property in GetProperties())
                if (property.GetCustomAttributes(typeof(DoNotSaveAttribute), false).Length == 0)
                {
                    Console.WriteLine(property.Name, property.PropertyType);
                    Save(property.Name, property.PropertyType);
                }

            Folders.Save();
            Input.Save();
        }

        public override void Save(string name)
        {
            var property = GetProperty(name);
            if (property.GetCustomAttributes(typeof(DoNotSaveAttribute), false).Length == 0)
                Save(property.Name, property.PropertyType);
        }

        public override void Reset()
        {
            foreach (var property in GetProperties())
                Reset(property.Name);
        }

        public void Log()
        {
            foreach (var property in GetProperties().OrderBy(p => p.Name))
            {
                var value = property.GetValue(this, null);
                var source = Sources[property.Name] == Source.CommandLine ? "(command-line)" : Sources[property.Name] == Source.User ? "(user set)" : "";
                if (property.PropertyType == typeof(string[]))
                    Console.WriteLine("{0,-30} = {2,-14} {1}", property.Name, String.Join(", ", ((string[])value).Select(v => v.ToString()).ToArray()), source);
                else if (property.PropertyType == typeof(int[]))
                    Console.WriteLine("{0,-30} = {2,-14} {1}", property.Name, String.Join(", ", ((int[])value).Select(v => v.ToString()).ToArray()), source);
                else
                    Console.WriteLine("{0,-30} = {2,-14} {1}", property.Name, value, source);
            }
        }
    }
    public class SavingProperty<T>
    {
        private readonly UserSettings Settings;
        private readonly PropertyInfo Property;
        private readonly bool DoSave;

        internal SavingProperty(UserSettings settings, PropertyInfo property, bool allowSave = true)
        {
            Settings = settings;
            Property = property;
            DoSave = allowSave;
        }

        /// <summary>
        /// Get or set the current value of this property.
        /// </summary>
        public T Value
        {
            get => GetValue();
            set => SetValue(value);
        }

        /// <summary>
        /// Get the current value of this property.
        /// </summary>
        public T GetValue()
            => Property.GetValue(Settings) is T cast ? cast : default;

        /// <summary>
        /// Set the current value of this property.
        /// </summary>
        public void SetValue(T value)
        {
            if (!GetValue().Equals(value))
            {
                Property.SetValue(Settings, value);
                if (DoSave)
                    Settings.Save(Property.Name);
            }
        }
    }
    */
}
