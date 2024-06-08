using TOURMALINE.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TOURMALINE.Settings
{
    /*
    public class UpdateState : SettingsBase
    {
        #region User Settings

        // Please put all update settings in here as auto-properties. Public properties
        // of type 'string', 'int', 'bool', 'string[]' and 'int[]' are automatically loaded/saved.

        [Default(0)]
        public DateTime LastCheck { get; set; }
        [Default(0)]
        public DateTime NextCheck { get; set; }
        [Default("")]
        public string Update { get; set; }

        #endregion

        public UpdateState()
            : base(SettingsStore.GetSettingStore(UserSettings.SettingsFilePath, UserSettings.RegistryKey, "UpdateState"))
        {
            Load(new string[0]);
        }

        public override object GetDefaultValue(string name)
        {
            var property = GetType().GetProperty(name);

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
