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
    public class UpdateSettings : SettingsBase
    {
        public static readonly string SettingsFilePath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Updater.ini");

        #region User Settings

        // Please put all update settings in here as auto-properties. Public properties
        // of type 'string', 'int', 'bool', 'string[]' and 'int[]' are automatically loaded/saved.

        [Default("")]
        public string Channel { get; set; }
        [Default("")]
        public string URL { get; set; }
        public TimeSpan TTL { get; set; }
        [Default("")]
        public string ChangeLogLink { get; set; }

        #endregion

        public UpdateSettings()
            : base(SettingsStore.GetSettingStore(UpdateSettings.SettingsFilePath, null, "Settings"))
        {
            Load(new string[0]);
        }

        public UpdateSettings(string channel)
            : base(SettingsStore.GetSettingStore(UpdateSettings.SettingsFilePath, null, channel + "Settings"))
        {
            Load(new string[0]);
        }

        public string[] GetChannels()
        {
            // We are always a local INI settings store.
            return (from name in (SettingStore as SettingsStoreLocalIni).GetSectionNames()
                    where name.EndsWith("Settings")
                    select name.Replace("Settings", "")).ToArray();
        }

        public override object GetDefaultValue(string name)
        {
            var property = GetType().GetProperty(name);

            if (name == "TTL")
                return TimeSpan.FromDays(1);

            if (property.GetCustomAttributes(typeof(DefaultAttribute), false).Length > 0)
                return (property.GetCustomAttributes(typeof(DefaultAttribute), false)[0] as DefaultAttribute).Value;

            throw new InvalidDataException(String.Format("UserSetting {0} has no default value.", property.Name));
        }

        PropertyInfo GetProperty(string name)
        {
            return GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        }

        PropertyInfo[] GetProperties()
        {
            return GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy).ToArray();
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
                    Save(property.Name, property.PropertyType);
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
    }
    */
}
